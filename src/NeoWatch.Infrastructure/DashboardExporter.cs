using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace NeoWatch.Infrastructure;

/// <summary>
/// Writes a static JSON snapshot of the database for the published dashboard.
/// Built only from stored rows, so the API key and request URLs cannot leak into it.
/// </summary>
public sealed class DashboardExporter(NeoWatchDbContext db, TimeProvider time)
{
    public const double KmPerLunarDistance = 384_400;
    public const int ClosestCount = 25;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
    };

    public async Task ExportAsync(string path, CancellationToken ct)
    {
        var dashboard = await BuildAsync(ct);

        var directory = Path.GetDirectoryName(Path.GetFullPath(path));
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await using var stream = File.Create(path);
        await JsonSerializer.SerializeAsync(stream, dashboard, JsonOptions, ct);
    }

    private async Task<Dashboard> BuildAsync(CancellationToken ct)
    {
        var now = time.GetUtcNow().UtcDateTime;
        var today = DateOnly.FromDateTime(now);

        var approaches = db.CloseApproaches.AsNoTracking();

        var daily = await db.DailySummaries.AsNoTracking()
            .OrderBy(s => s.Date)
            .Select(s => new DailyPoint(s.Date, s.ApproachCount, s.HazardousCount))
            .ToListAsync(ct);

        var upcoming = await ToItemsAsync(approaches
            .Where(c => c.Asteroid.IsPotentiallyHazardous && c.Date >= today)
            .OrderBy(c => c.Date)
            .ThenBy(c => c.EpochMillis), ct);

        var closest = await ToItemsAsync(approaches
            .OrderBy(c => c.MissDistanceKm)
            .Take(ClosestCount), ct);

        var scatter = await approaches
            .Select(c => new ScatterPoint(
                c.Asteroid.Name, c.MissDistanceKm, c.Asteroid.DiameterMaxKm, c.Asteroid.IsPotentiallyHazardous))
            .ToListAsync(ct);

        var totals = new Totals(
            Asteroids: await db.Asteroids.CountAsync(ct),
            Approaches: await approaches.CountAsync(ct),
            HazardousApproaches: await approaches.CountAsync(c => c.Asteroid.IsPotentiallyHazardous, ct));

        // Nullable casts so Min/Max return null on an empty table instead of throwing.
        var firstDate = await approaches.MinAsync(c => (DateOnly?)c.Date, ct);
        var lastDate = await approaches.MaxAsync(c => (DateOnly?)c.Date, ct);

        return new Dashboard(now, firstDate, lastDate, totals, daily, upcoming, closest, scatter);
    }

    private static async Task<List<ApproachItem>> ToItemsAsync(
        IQueryable<Core.Models.CloseApproach> query, CancellationToken ct)
    {
        var rows = await query
            .Select(c => new
            {
                c.Asteroid.NeoId,
                c.Asteroid.Name,
                c.Date,
                c.MissDistanceKm,
                c.VelocityKmPerSec,
                c.Asteroid.DiameterMinKm,
                c.Asteroid.DiameterMaxKm,
                c.Asteroid.IsPotentiallyHazardous,
            })
            .ToListAsync(ct);

        return rows.Select(r => new ApproachItem(
                r.NeoId,
                r.Name,
                r.Date,
                r.MissDistanceKm,
                Math.Round(r.MissDistanceKm / KmPerLunarDistance, 2),
                r.VelocityKmPerSec,
                r.DiameterMinKm,
                r.DiameterMaxKm,
                r.IsPotentiallyHazardous,
                $"https://ssd.jpl.nasa.gov/tools/sbdb_lookup.html#/?sstr={Uri.EscapeDataString(r.NeoId)}"))
            .ToList();
    }

    private sealed record Dashboard(
        DateTime GeneratedAtUtc,
        DateOnly? FirstDate,
        DateOnly? LastDate,
        Totals Totals,
        List<DailyPoint> Daily,
        List<ApproachItem> UpcomingHazards,
        List<ApproachItem> Closest,
        List<ScatterPoint> Scatter);

    private sealed record Totals(int Asteroids, int Approaches, int HazardousApproaches);

    private sealed record DailyPoint(DateOnly Date, int ApproachCount, int HazardousCount);

    private sealed record ApproachItem(
        string NeoId,
        string Name,
        DateOnly Date,
        double MissDistanceKm,
        double LunarDistances,
        double VelocityKmPerSec,
        double DiameterMinKm,
        double DiameterMaxKm,
        bool Hazardous,
        string JplUrl);

    private sealed record ScatterPoint(string Name, double MissDistanceKm, double DiameterMaxKm, bool Hazardous);
}
