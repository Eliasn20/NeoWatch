using Microsoft.Extensions.Options;
using NeoWatch.Infrastructure;

namespace NeoWatch.Worker;

/// <summary>The long-running host: runs the pipeline now, then again on every timer tick.</summary>
public sealed class IngestionWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<NeoWsOptions> options,
    ILogger<IngestionWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
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
}
