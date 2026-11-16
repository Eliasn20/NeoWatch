using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NeoWatch.Core.Pipeline;
using NeoWatch.Infrastructure;

namespace NeoWatch.Tests;

/// <summary>
/// Runs against a real SQLite engine held in memory, so unique indexes and
/// foreign keys behave exactly as in production. The database lives as long as
/// the connection stays open, hence IDisposable.
/// </summary>
public sealed class ApproachRepositoryTests : IDisposable
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    private readonly DbContextOptions<NeoWatchDbContext> _options;

    public ApproachRepositoryTests()
    {
        _connection.Open();
        _options = new DbContextOptionsBuilder<NeoWatchDbContext>().UseSqlite(_connection).Options;

        using var db = new NeoWatchDbContext(_options);
        db.Database.EnsureCreated();
    }

    public void Dispose() => _connection.Dispose();

    private static ParsedApproach Sample(string neoId = "9000001", long epoch = 1794731400000, double missKm = 4_500_000) =>
        new(neoId, $"({neoId})", 21.3, 0.146, 0.327, true,
            new DateOnly(2026, 11, 15), epoch, 13.45, missKm, "Earth");

    [Fact]
    public async Task Upsert_NewApproach_InsertsAsteroidAndApproach()
    {
        await using var db = new NeoWatchDbContext(_options);
        var repository = new ApproachRepository(db);

        var result = await repository.UpsertAsync([Sample()], CancellationToken.None);

        Assert.Equal(new UpsertResult(Inserted: 1, Updated: 0), result);
        Assert.Equal(1, await db.Asteroids.CountAsync());
        Assert.Equal(1, await db.CloseApproaches.CountAsync());
    }

    [Fact]
    public async Task Upsert_SameBatchTwice_IsIdempotent()
    {
        // A fresh DbContext per call, as in the real worker (one scope per run).
        await using (var db = new NeoWatchDbContext(_options))
        {
            await new ApproachRepository(db).UpsertAsync([Sample()], CancellationToken.None);
        }

        await using (var db = new NeoWatchDbContext(_options))
        {
            var second = await new ApproachRepository(db).UpsertAsync([Sample()], CancellationToken.None);

            Assert.Equal(new UpsertResult(Inserted: 0, Updated: 0), second);
            Assert.Equal(1, await db.Asteroids.CountAsync());
            Assert.Equal(1, await db.CloseApproaches.CountAsync());
        }
    }

    [Fact]
    public async Task Upsert_ChangedDistance_UpdatesApproach()
    {
        // First run: save the asteroid with its original distance.
        await using (var db = new NeoWatchDbContext(_options))
        {
            await new ApproachRepository(db).UpsertAsync([Sample()], CancellationToken.None);
        }

        // Second run: same asteroid, same moment, different distance.
        await using (var db = new NeoWatchDbContext(_options))
        {
            var second = await new ApproachRepository(db)
                .UpsertAsync([Sample(missKm: 9_999_999)], CancellationToken.None);

            Assert.Equal(new UpsertResult(Inserted: 0, Updated: 1), second);
            Assert.Equal(1, await db.Asteroids.CountAsync());
            Assert.Equal(1, await db.CloseApproaches.CountAsync());
        }
    }

    [Fact]
    public async Task Upsert_ChangedDistance_PersistsNewValue()
    {
        await using (var db = new NeoWatchDbContext(_options))
        {
            await new ApproachRepository(db).UpsertAsync([Sample()], CancellationToken.None);
        }

        await using (var db = new NeoWatchDbContext(_options))
        {
            var second = await new ApproachRepository(db)
                .UpsertAsync([Sample(missKm: 9_999_999)], CancellationToken.None);

            Assert.Equal(new UpsertResult(Inserted: 0, Updated: 1), second);
        }

        await using (var db = new NeoWatchDbContext(_options))
        {
            var stored = await db.CloseApproaches.SingleAsync();
            Assert.Equal(9_999_999, stored.MissDistanceKm);
        }
    }

    [Fact]
    public async Task Upsert_SameAsteroidDifferentEpoch_AddsSecondApproach()
    {
        await using (var db = new NeoWatchDbContext(_options))
        {
            await new ApproachRepository(db).UpsertAsync([Sample()], CancellationToken.None);
        }

        await using (var db = new NeoWatchDbContext(_options))
        {
            var second = await new ApproachRepository(db)
                .UpsertAsync([Sample(epoch: 1826267400000)], CancellationToken.None);

            Assert.Equal(new UpsertResult(Inserted: 1, Updated: 0), second);
            Assert.Equal(1, await db.Asteroids.CountAsync());
            Assert.Equal(2, await db.CloseApproaches.CountAsync());
        }
    }

    [Fact]
    public async Task Upsert_TwoAsteroidsInOneBatch_InsertsBoth()
    {
        await using var db = new NeoWatchDbContext(_options);
        var repository = new ApproachRepository(db);

        var result = await repository.UpsertAsync([Sample("9000001"), Sample("9000002")], CancellationToken.None);

        Assert.Equal(new UpsertResult(Inserted: 2, Updated: 0), result);
        Assert.Equal(2, await db.Asteroids.CountAsync());
        Assert.Equal(2, await db.CloseApproaches.CountAsync());
    }

    [Fact]
    public async Task Upsert_SameAsteroidTwiceInOneBatch_InsertsOnce()
    {
        await using var db = new NeoWatchDbContext(_options);
        var repository = new ApproachRepository(db);

        var result = await repository.UpsertAsync([Sample(), Sample()], CancellationToken.None);

        Assert.Equal(new UpsertResult(Inserted: 1, Updated: 0), result);
        Assert.Equal(1, await db.Asteroids.CountAsync());
        Assert.Equal(1, await db.CloseApproaches.CountAsync());
    }
}
