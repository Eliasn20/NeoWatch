namespace NeoWatch.Core.Models;

/// <summary>Audit row: one per date window the pipeline attempted.</summary>
public class IngestionRun
{
    public int Id { get; set; }
    public DateOnly WindowStart { get; set; }
    public DateOnly WindowEnd { get; set; }
    public DateTime StartedAtUtc { get; set; }
    public DateTime? FinishedAtUtc { get; set; }
    public bool Succeeded { get; set; }
    public int ApproachesSeen { get; set; }
    public int ApproachesInserted { get; set; }
    public int ApproachesUpdated { get; set; }
    public string? Error { get; set; }
}
