using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using NeoWatch.Core.Abstractions;
using NeoWatch.Core.Dtos;
using NeoWatch.Infrastructure;
using NeoWatch.Worker;

namespace NeoWatch.Tests;

/// <summary>
/// Drives the worker in run-once mode end to end, with a fake NASA client and
/// in-memory SQLite. Environment.ExitCode is process-wide, so each test resets it.
/// </summary>
public sealed class IngestionWorkerTests : IDisposable
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    private readonly string _exportPath = Path.Combine(Path.GetTempPath(), $"neowatch-{Guid.NewGuid():N}", "data.json");

    public IngestionWorkerTests() => _connection.Open();

    public void Dispose()
    {
        Environment.ExitCode = 0;
        _connection.Dispose();
        var directory = Path.GetDirectoryName(_exportPath)!;
        if (Directory.Exists(directory))
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private async Task<FakeLifetime> RunOnceAsync(INeoWsClient client)
    {
        var options = new NeoWsOptions { RunOnce = true, ExportPath = _exportPath, BackfillDays = 14, LookaheadDays = 7 };

        var services = new ServiceCollection();
        services.AddDbContext<NeoWatchDbContext>(db => db.UseSqlite(_connection));
        services.AddScoped<IApproachRepository, ApproachRepository>();
        services.AddScoped<DashboardExporter>();
        services.AddScoped<IngestionPipeline>();
        services.AddSingleton(client);
        services.AddSingleton<TimeProvider>(new FakeTimeProvider(new DateTimeOffset(2026, 11, 16, 12, 0, 0, TimeSpan.Zero)));
        services.AddSingleton(Options.Create(options));
        services.AddLogging();

        await using var provider = services.BuildServiceProvider();
        using (var scope = provider.CreateScope())
        {
            scope.ServiceProvider.GetRequiredService<NeoWatchDbContext>().Database.EnsureCreated();
        }

        var lifetime = new FakeLifetime();
        var worker = new IngestionWorker(
            provider.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(options),
            lifetime,
            NullLogger<IngestionWorker>.Instance);

        await worker.StartAsync(CancellationToken.None);
        await worker.ExecuteTask!;
        return lifetime;
    }

    [Fact]
    public async Task RunOnce_EveryWindowFails_SetsExitCodeAndSkipsExport()
    {
        var lifetime = await RunOnceAsync(new FakeClient(failFirst: int.MaxValue));

        Assert.Equal(1, Environment.ExitCode);
        Assert.False(File.Exists(_exportPath));
        Assert.True(lifetime.StopRequested);
    }

    [Fact]
    public async Task RunOnce_SomeWindowsFail_StillExports()
    {
        var lifetime = await RunOnceAsync(new FakeClient(failFirst: 1));

        Assert.Equal(0, Environment.ExitCode);
        Assert.True(File.Exists(_exportPath));
        Assert.True(lifetime.StopRequested);
    }

    private sealed class FakeClient(int failFirst) : INeoWsClient
    {
        private int _calls;

        public Task<NeoFeedResponse> GetFeedAsync(DateOnly start, DateOnly end, CancellationToken ct) =>
            ++_calls <= failFirst
                ? throw new HttpRequestException("NeoWs unavailable")
                : Task.FromResult(new NeoFeedResponse(0, []));
    }

    private sealed class FakeLifetime : IHostApplicationLifetime
    {
        public bool StopRequested { get; private set; }
        public CancellationToken ApplicationStarted => CancellationToken.None;
        public CancellationToken ApplicationStopping => CancellationToken.None;
        public CancellationToken ApplicationStopped => CancellationToken.None;
        public void StopApplication() => StopRequested = true;
    }
}
