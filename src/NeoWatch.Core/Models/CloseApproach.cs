namespace NeoWatch.Core.Models;

/// <summary>One pass of one asteroid near a body. Unique on (AsteroidId, EpochMillis).</summary>
public class CloseApproach
{
    public int Id { get; set; }

    public int AsteroidId { get; set; }
    public Asteroid Asteroid { get; set; } = null!;

    public DateOnly Date { get; set; }

    /// <summary>Unix time in milliseconds of the closest point, as NASA reports it.</summary>
    public long EpochMillis { get; set; }

    public double VelocityKmPerSec { get; set; }
    public double MissDistanceKm { get; set; }
    public required string OrbitingBody { get; set; }
}
