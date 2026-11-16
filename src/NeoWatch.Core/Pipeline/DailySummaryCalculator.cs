using NeoWatch.Core.Models;

namespace NeoWatch.Core.Pipeline;

public static class DailySummaryCalculator
{
    /// <summary>Builds one DailySummary per distinct date in the input.</summary>
    public static IReadOnlyList<DailySummary> Summarize(IEnumerable<ParsedApproach> approaches)
    {
        // TODO (Elias) - Milestone 5
        // GroupBy(a => a.Date), then for each group compute:
        //   ApproachCount, HazardousCount,
        //   closest approach  -> MinBy(a => a.MissDistanceKm)
        //   largest asteroid  -> MaxBy(a => a.DiameterMaxKm)
        // Return ordered by date. Empty input => empty list.
        throw new NotImplementedException();
    }
}
