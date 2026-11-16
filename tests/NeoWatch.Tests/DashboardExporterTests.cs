using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using NeoWatch.Core.Pipeline;
using NeoWatch.Infrastructure;

namespace NeoWatch.Tests;

/// <summary>Same in-memory SQLite setup as ApproachRepositoryTests.</summary>
public sealed class DashboardExporterTests : IDisposable
{
    private static readonly DateOnly Today = new(2026, 11, 16);

    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    private readonly DbContextOptions<NeoWatchDbContext> _options;
    private readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 11, 16, 12, 0, 0, TimeSpan.Zero));
    private readonly string _outputPath = Path.Combine(Path.GetTempPath(), $"neowatch-{Guid.NewGuid():N}", "data.json");

    public DashboardExporterTests()
    {
        _connection.Open();
        _options = new DbContextOptionsBuilder<NeoWatchDbContext>().UseSqlite(_connection).Options;

        using var db = new NeoWatchDbContext(_options);
        db.Database.EnsureCreated();
    }

    public void Dispose()
    {
        _connection.Dispose();
        var directory = Path.GetDirectoryName(_outputPath)!;
        if (Directory.Exists(directory))
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static ParsedApproach Approach(
        string neoId, DateOnly date, double missKm = 4_500_000, bool hazardous = false, long epoch = 1794731400000) =>
        new(neoId, $"({neoId})", 21.3, 0.146, 0.327, hazardous, date, epoch, 13.45, missKm, "Earth");

    private async Task SeedAsync(params ParsedApproach[] approaches)
    {
        await using var db = new NeoWatchDbContext(_options);
        await new ApproachRepository(db).UpsertAsync(approaches, CancellationToken.None);
    }

    private async Task<JsonElement> ExportAsync()
    {
        await using (var db = new NeoWatchDbContext(_options))
        {
            await new DashboardExporter(db, _time).ExportAsync(_outputPath, CancellationToken.None);
        }

        using var doc = JsonDocument.Parse(await File.ReadAllTextAsync(_outputPath));
        return doc.RootElement.Clone();
    }

    [Fact]
    public async Task Export_PastHazard_IsExcludedFromUpcomingHazards()
    {
        await SeedAsync(
            Approach("1", Today.AddDays(-1), hazardous: true),
            Approach("2", Today, hazardous: true),
            Approach("3", Today.AddDays(3), hazardous: true),
            Approach("4", Today.AddDays(1), hazardous: false));

        var json = await ExportAsync();

        var ids = json.GetProperty("upcomingHazards").EnumerateArray()
            .Select(e => e.GetProperty("neoId").GetString());
        Assert.Equal(["2", "3"], ids);
    }

    [Fact]
    public async Task Export_ManyApproaches_ClosestIsAscendingAndCappedAt25()
    {
        // Seeded in descending distance so ordering has to come from the exporter.
        var approaches = Enumerable.Range(1, 30)
            .Select(i => Approach(i.ToString(), Today, missKm: (31 - i) * 100_000.0))
            .ToArray();
        await SeedAsync(approaches);

        var json = await ExportAsync();

        var distances = json.GetProperty("closest").EnumerateArray()
            .Select(e => e.GetProperty("missDistanceKm").GetDouble())
            .ToList();
        Assert.Equal(25, distances.Count);
        Assert.Equal(distances.Order(), distances);
        Assert.Equal(100_000, distances[0]);

        var first = json.GetProperty("closest")[0];
        Assert.Equal(0.26, first.GetProperty("lunarDistances").GetDouble());
        Assert.Equal("https://ssd.jpl.nasa.gov/tools/sbdb_lookup.html#/?sstr=30", first.GetProperty("jplUrl").GetString());
    }

    [Fact]
    public async Task Export_SeededDatabase_TotalsAreCorrect()
    {
        await SeedAsync(
            Approach("1", Today, hazardous: true),
            Approach("1", Today.AddDays(30), hazardous: true, epoch: 1797323400000),
            Approach("2", Today),
            Approach("3", Today.AddDays(-5)));

        var json = await ExportAsync();

        var totals = json.GetProperty("totals");
        Assert.Equal(3, totals.GetProperty("asteroids").GetInt32());
        Assert.Equal(4, totals.GetProperty("approaches").GetInt32());
        Assert.Equal(2, totals.GetProperty("hazardousApproaches").GetInt32());
        Assert.Equal("2026-11-11", json.GetProperty("firstDate").GetString());
        Assert.Equal("2026-12-16", json.GetProperty("lastDate").GetString());
        Assert.Equal(4, json.GetProperty("scatter").GetArrayLength());
    }

    [Fact]
    public async Task Export_EmptyDatabase_WritesValidJsonWithEmptyArrays()
    {
        var json = await ExportAsync();

        Assert.Equal(0, json.GetProperty("daily").GetArrayLength());
        Assert.Equal(0, json.GetProperty("upcomingHazards").GetArrayLength());
        Assert.Equal(0, json.GetProperty("closest").GetArrayLength());
        Assert.Equal(0, json.GetProperty("scatter").GetArrayLength());
        Assert.Equal(0, json.GetProperty("totals").GetProperty("approaches").GetInt32());
        Assert.Equal(JsonValueKind.Null, json.GetProperty("firstDate").ValueKind);
        Assert.Equal(JsonValueKind.Null, json.GetProperty("lastDate").ValueKind);
    }
}
