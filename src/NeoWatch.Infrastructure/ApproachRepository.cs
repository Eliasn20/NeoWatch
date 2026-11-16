using Microsoft.EntityFrameworkCore;
using NeoWatch.Core.Abstractions;
using NeoWatch.Core.Models;
using NeoWatch.Core.Pipeline;

namespace NeoWatch.Infrastructure;

public sealed class ApproachRepository(NeoWatchDbContext db) : IApproachRepository
{
    public async Task<UpsertResult> UpsertAsync(IReadOnlyList<ParsedApproach> approaches, CancellationToken ct)
    {
       var neoIds = approaches.Select(a => a.NeoId).Distinct().ToList();
       
       var existing = await db.Asteroids
        .Include(a => a.CloseApproaches)
        .Where(a => neoIds.Contains(a.NeoId))
        .ToDictionaryAsync(a => a.NeoId, ct);

        var inserted = 0;
        var updated = 0;

        foreach (var p in approaches)
        {
            if (!existing.TryGetValue(p.NeoId, out var asteroid))
            {
                asteroid = new Asteroid { NeoId = p.NeoId, Name = p.Name };
                db.Asteroids.Add(asteroid);
                existing[p.NeoId] = asteroid;
                
            }

            asteroid.Name = p.Name;
            asteroid.AbsoluteMagnitude = p.AbsoluteMagnitude;
            asteroid.DiameterMinKm = p.DiameterMinKm;
            asteroid.DiameterMaxKm = p.DiameterMaxKm;
            asteroid.IsPotentiallyHazardous = p.IsPotentiallyHazardous;

            var approach = asteroid.CloseApproaches
                .FirstOrDefault(c => c.EpochMillis == p.EpochMillis);

            if (approach is null)
            {
                asteroid.CloseApproaches.Add(new CloseApproach
                {
                    Date = p.Date,
                    EpochMillis = p.EpochMillis,
                    VelocityKmPerSec = p.VelocityKmPerSec,
                    MissDistanceKm = p.MissDistanceKm,
                    OrbitingBody = p.OrbitingBody,
                });
                inserted++;
            }
            else if (approach.VelocityKmPerSec != p.VelocityKmPerSec || approach.MissDistanceKm != p.MissDistanceKm)
            {
                approach.VelocityKmPerSec = p.VelocityKmPerSec;
                approach.MissDistanceKm = p.MissDistanceKm;
                updated++;
            }
        }


        await db.SaveChangesAsync(ct);
        return new UpsertResult(inserted, updated);
    }

    public async Task SaveSummariesAsync(IReadOnlyList<DailySummary> summaries, CancellationToken ct)
    {
        foreach (var summary in summaries)
        {
            var existing = await db.DailySummaries.FindAsync([summary.Date], ct);
            if (existing is null)
            {
                db.DailySummaries.Add(summary);
            }
            else
            {
                db.Entry(existing).CurrentValues.SetValues(summary);
            }
        }

        await db.SaveChangesAsync(ct);
    }

    public async Task<HashSet<DateRange>> GetCompletedWindowsAsync(CancellationToken ct)
    {
        var rows = await db.IngestionRuns
            .Where(r => r.Succeeded)
            .Select(r => new { r.WindowStart, r.WindowEnd })
            .Distinct()
            .ToListAsync(ct);

        return rows.Select(r => new DateRange(r.WindowStart, r.WindowEnd)).ToHashSet();
    }

    public async Task RecordRunAsync(IngestionRun run, CancellationToken ct)
    {
        // If the run failed halfway, the change tracker may still hold broken
        // pending changes. Drop them so the audit row always gets written.
        db.ChangeTracker.Clear();
        db.IngestionRuns.Add(run);
        await db.SaveChangesAsync(ct);
    }
}
