using Microsoft.EntityFrameworkCore;
using NeoWatch.Core.Abstractions;
using NeoWatch.Core.Models;
using NeoWatch.Core.Pipeline;

namespace NeoWatch.Infrastructure;

public sealed class ApproachRepository(NeoWatchDbContext db) : IApproachRepository
{
    public Task<UpsertResult> UpsertAsync(IReadOnlyList<ParsedApproach> approaches, CancellationToken ct)
    {
        // TODO (Elias) - Milestone 3. This is the heart of the project.
        //
        // Goal: after this method, the database reflects `approaches`, and calling it
        // again with the same list inserts 0 and changes nothing.
        //
        // Suggested shape (2 queries + 1 SaveChanges, not one query per row):
        // 1. Collect the distinct NeoIds in the batch. Load the matching Asteroids,
        //    including their CloseApproaches, into a Dictionary keyed by NeoId:
        //      db.Asteroids.Include(a => a.CloseApproaches)
        //                  .Where(a => neoIds.Contains(a.NeoId))
        //                  .ToDictionaryAsync(a => a.NeoId, ct)
        // 2. For each ParsedApproach:
        //      - asteroid missing  -> create it, db.Asteroids.Add, put it in the dictionary
        //      - asteroid present  -> refresh its fields (NASA revises estimates)
        //      - approach with the same EpochMillis missing -> add it, count as inserted
        //      - present -> update velocity/distance, count as updated only if a value changed
        // 3. await db.SaveChangesAsync(ct) once, return the counts.
        //
        // When you write it, change the signature to `public async Task<UpsertResult> ...`.
        throw new NotImplementedException();
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
