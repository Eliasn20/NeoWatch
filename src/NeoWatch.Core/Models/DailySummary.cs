namespace NeoWatch.Core.Models;

/// <summary>One row per calendar day. Date is the primary key.</summary>
public class DailySummary
{
    public DateOnly Date { get; set; }
    public int ApproachCount { get; set; }
    public int HazardousCount { get; set; }
    public double ClosestMissKm { get; set; }
    public required string ClosestAsteroidName { get; set; }
    public double LargestDiameterKm { get; set; }
    public required string LargestAsteroidName { get; set; }
}
