using NeoWatch.Core.Models;

namespace NeoWatch.Core.Pipeline;

public static class DailySummaryCalculator
{
    /// <summary>Builds one DailySummary per distinct date in the input.</summary>
    public static IReadOnlyList<DailySummary> Summarize(IEnumerable<ParsedApproach> approaches)
    {
        return approaches
            .GroupBy(a=> a.Date)
            .OrderBy(g => g.Key)
            .Select(g => new DailySummary
            {
               Date = g.Key,
               ApproachCount = g.Count(),
               HazardousCount = g.Count(a => a.IsPotentiallyHazardous),
               ClosestMissKm = g.Min(a => a.MissDistanceKm),
               ClosestAsteroidName = g.MinBy(a => a.MissDistanceKm)!.Name,
               LargestDiameterKm = g.Max(a => a.DiameterMaxKm),
               LargestAsteroidName = g.MaxBy(a => a.DiameterMaxKm)!.Name,
            })
            .ToList();
    }
}
