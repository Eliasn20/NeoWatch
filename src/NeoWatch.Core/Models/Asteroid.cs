namespace NeoWatch.Core.Models;

/// <summary>One near-Earth object. One row per asteroid, however many times it passes Earth.</summary>
public class Asteroid
{
    public int Id { get; set; }

    /// <summary>NASA's own identifier (the "id" field in NeoWs). Unique in the database.</summary>
    public required string NeoId { get; set; }

    public required string Name { get; set; }
    public double AbsoluteMagnitude { get; set; }
    public double DiameterMinKm { get; set; }
    public double DiameterMaxKm { get; set; }
    public bool IsPotentiallyHazardous { get; set; }

    public List<CloseApproach> CloseApproaches { get; set; } = [];
}
