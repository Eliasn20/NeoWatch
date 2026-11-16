using System.Text.Json.Serialization;

namespace NeoWatch.Core.Dtos;

// These records mirror the raw JSON from GET /neo/rest/v1/feed.
// They are deliberately "dumb": no logic, names match the API.
// Note the API quirk: velocity and miss distance arrive as STRINGS, not numbers.

public sealed record NeoFeedResponse(
    [property: JsonPropertyName("element_count")] int ElementCount,
    [property: JsonPropertyName("near_earth_objects")] Dictionary<string, List<NeoDto>> NearEarthObjects);

public sealed record NeoDto(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("absolute_magnitude_h")] double AbsoluteMagnitudeH,
    [property: JsonPropertyName("estimated_diameter")] EstimatedDiameterDto EstimatedDiameter,
    [property: JsonPropertyName("is_potentially_hazardous_asteroid")] bool IsPotentiallyHazardous,
    [property: JsonPropertyName("close_approach_data")] List<CloseApproachDto> CloseApproachData);

public sealed record EstimatedDiameterDto(
    [property: JsonPropertyName("kilometers")] DiameterRangeDto Kilometers);

public sealed record DiameterRangeDto(
    [property: JsonPropertyName("estimated_diameter_min")] double Min,
    [property: JsonPropertyName("estimated_diameter_max")] double Max);

public sealed record CloseApproachDto(
    [property: JsonPropertyName("close_approach_date")] string CloseApproachDate,
    [property: JsonPropertyName("epoch_date_close_approach")] long EpochDateCloseApproach,
    [property: JsonPropertyName("relative_velocity")] RelativeVelocityDto RelativeVelocity,
    [property: JsonPropertyName("miss_distance")] MissDistanceDto MissDistance,
    [property: JsonPropertyName("orbiting_body")] string OrbitingBody);

public sealed record RelativeVelocityDto(
    [property: JsonPropertyName("kilometers_per_second")] string KilometersPerSecond);

public sealed record MissDistanceDto(
    [property: JsonPropertyName("kilometers")] string Kilometers);
