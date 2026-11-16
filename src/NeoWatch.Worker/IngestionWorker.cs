using Microsoft.Extensions.Options;
using NeoWatch.Infrastructure;

namespace NeoWatch.Worker;

/// <summary>The long-running host: runs the pipeline now, then again on every timer tick.</summary>
public sealed class IngestionWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<NeoWsOptions> options,
    IHostApplicationLifetime lifetime,
    ILogger<IngestionWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (options.Value.RunOnce)
        {
            await RunOnceAsync(stoppingToken);
            return;
        }

        using var timer = new PeriodicTimer(TimeSpan.FromHours(options.Value.PollIntervalHours));

        do
        {
            try
            {
                // A BackgroundService is a singleton but DbContext is scoped,
                // so each run gets its own scope (and its own DbContext).
                using var scope = scopeFactory.CreateScope();
                var pipeline = scope.ServiceProvider.GetRequiredService<IngestionPipeline>();
                await pipeline.RunAsync(stoppingToken);
                logger.LogInformation("Run finished. Next run in {Hours} h", options.Value.PollIntervalHours);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Ingestion run failed");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task RunOnceAsync(CancellationToken stoppingToken)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            await scope.ServiceProvider.GetRequiredService<IngestionPipeline>().RunAsync(stoppingToken);

            var exportPath = options.Value.ExportPath;
            if (!string.IsNullOrWhiteSpace(exportPath))
            {
                await scope.ServiceProvider.GetRequiredService<DashboardExporter>().ExportAsync(exportPath, stoppingToken);
                logger.LogInformation("Dashboard exported to {Path}", Path.GetFullPath(exportPath));
            }

            logger.LogInformation("Run-once finished");
        }
        catch (Exception ex)
        {
            // A non-zero exit code is how the scheduled CI job learns the run failed.
            Environment.ExitCode = 1;
            logger.LogError(ex, "Run-once failed");
        }
        finally
        {
            lifetime.StopApplication();
        }
    }
}
