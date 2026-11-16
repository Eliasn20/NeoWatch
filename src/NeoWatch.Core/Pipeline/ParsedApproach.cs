namespace NeoWatch.Core.Pipeline;

/// <summary>
/// The flat, validated, strongly typed output of the transform step.
/// One ParsedApproach = one asteroid passing one body once.
/// </summary>
public sealed record ParsedApproach(
    string NeoId,
    string Name,
    double AbsoluteMagnitude,
    double DiameterMinKm,
    double DiameterMaxKm,
    bool IsPotentiallyHazardous,
    DateOnly Date,
    long EpochMillis,
    double VelocityKmPerSec,
    double MissDistanceKm,
    string OrbitingBody);

/// <summary>An inclusive date range: both Start and End are part of it.</summary>
public readonly record struct DateRange(DateOnly Start, DateOnly End);

public readonly record struct UpsertResult(int Inserted, int Updated);
