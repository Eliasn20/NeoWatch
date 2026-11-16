using Microsoft.Extensions.Options;
using NeoWatch.Core.Abstractions;
using NeoWatch.Core.Models;
using NeoWatch.Core.Pipeline;
using NeoWatch.Infrastructure;

namespace NeoWatch.Worker;

/// <summary>One full run: work out which windows are needed, then fetch, map and store each.</summary>
public sealed class IngestionPipeline(
    INeoWsClient client,
    IApproachRepository repository,
    IOptions<NeoWsOptions> options,
    TimeProvider time,
    ILogger<IngestionPipeline> logger)
{
    private readonly NeoWsOptions _options = options.Value;

    public async Task RunAsync(CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(time.GetUtcNow().UtcDateTime);

        // Snap the start onto a fixed 7-day grid. Without this the windows would
        // shift by a day on every run and never match a previously completed window.
        var start = today.AddDays(-_options.BackfillDays);
        start = start.AddDays(-(start.DayNumber % DateWindows.MaxFeedDays));
        var end = today.AddDays(_options.LookaheadDays);

        var windows = DateWindows.Split(start, end);
        var completed = await repository.GetCompletedWindowsAsync(ct);

        foreach (var window in windows)
        {
            // Past windows never change, so skip the ones already ingested.
            // Windows touching today or the future are always re-fetched.
            if (window.End < today && completed.Contains(window))
            {
                continue;
            }

            await IngestWindowAsync(window, today, ct);
        }
    }

    private async Task IngestWindowAsync(DateRange window, DateOnly today, CancellationToken ct)
    {
        var run = new IngestionRun
        {
            WindowStart = window.Start,
            WindowEnd = window.End,
            StartedAtUtc = time.GetUtcNow().UtcDateTime,
        };

        try
        {
            var feed = await client.GetFeedAsync(window.Start, window.End, ct);
            var approaches = NeoFeedMapper.Map(feed);
            var result = await repository.UpsertAsync(approaches, ct);
            await repository.SaveSummariesAsync(DailySummaryCalculator.Summarize(approaches), ct);

            run.Succeeded = true;
            run.ApproachesSeen = approaches.Count;
            run.ApproachesInserted = result.Inserted;
            run.ApproachesUpdated = result.Updated;

            logger.LogInformation(
                "Window {Start}..{End}: {Seen} approaches, {Inserted} inserted, {Updated} updated",
                window.Start, window.End, approaches.Count, result.Inserted, result.Updated);

            LogHazards(approaches, today);
        }
        catch (Exception ex) when (ex is not OperationCanceledException and not NotImplementedException)
        {
            // One bad window must not stop the others; it is retried on the next run.
            run.Error = ex.Message;
            logger.LogError(ex, "Window {Start}..{End} failed", window.Start, window.End);
        }
        finally
        {
            run.FinishedAtUtc = time.GetUtcNow().UtcDateTime;
            await repository.RecordRunAsync(run, CancellationToken.None);
        }
    }

    private void LogHazards(IReadOnlyList<ParsedApproach> approaches, DateOnly today)
    {
        foreach (var a in approaches.Where(a => a.IsPotentiallyHazardous && a.Date >= today))
        {
            logger.LogWarning(
                "HAZARD {Name} passes {Body} on {Date} at {MissKm:N0} km, up to {DiameterKm:N2} km wide",
                a.Name, a.OrbitingBody, a.Date, a.MissDistanceKm, a.DiameterMaxKm);
        }
    }
}
