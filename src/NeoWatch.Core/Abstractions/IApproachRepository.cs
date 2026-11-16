using NeoWatch.Core.Models;
using NeoWatch.Core.Pipeline;

namespace NeoWatch.Core.Abstractions;

public interface IApproachRepository
{
    /// <summary>
    /// Inserts new asteroids and approaches, updates existing ones.
    /// Running it twice with the same input must leave the database unchanged.
    /// </summary>
    Task<UpsertResult> UpsertAsync(IReadOnlyList<ParsedApproach> approaches, CancellationToken ct);

    Task SaveSummariesAsync(IReadOnlyList<DailySummary> summaries, CancellationToken ct);

    Task<HashSet<DateRange>> GetCompletedWindowsAsync(CancellationToken ct);

    Task RecordRunAsync(IngestionRun run, CancellationToken ct);
}
