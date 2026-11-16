using NeoWatch.Core.Dtos;
using System.Globalization;

namespace NeoWatch.Core.Pipeline;

public static class NeoFeedMapper
{

    public static IReadOnlyList<ParsedApproach> Map(NeoFeedResponse feed)
    {
        
        var result = new List<ParsedApproach>();

        foreach (var asteroidsOnOneDay in feed.NearEarthObjects.Values)
        {
            foreach (var asteroid in asteroidsOnOneDay)
            {
                foreach (var approach in asteroid.CloseApproachData)
                {   
                    if (!double.TryParse(approach.MissDistance.Kilometers,
                            NumberStyles.Float, CultureInfo.InvariantCulture, out var missKm))
                    {
                        continue;
                    }
                    if(!double.TryParse(approach.RelativeVelocity.KilometersPerSecond,
                           NumberStyles.Float, CultureInfo.InvariantCulture, out var velocityKmPerSec))
                    {
                        continue;
                    }
                    if (!DateOnly.TryParseExact(approach.CloseApproachDate, "yyyy-MM-dd",
                            CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
                    {
                        continue;
                    }

                    if ((missKm < 0) || (velocityKmPerSec < 0) || string.IsNullOrWhiteSpace(asteroid.Id))
                    {
                        continue;
                    }

                    result.Add(new ParsedApproach(
                    NeoId: asteroid.Id,
                    Name: asteroid.Name,
                    AbsoluteMagnitude: asteroid.AbsoluteMagnitudeH,
                    DiameterMinKm: asteroid.EstimatedDiameter.Kilometers.Min,
                    DiameterMaxKm: asteroid.EstimatedDiameter.Kilometers.Max,
                    IsPotentiallyHazardous: asteroid.IsPotentiallyHazardous,
                    Date: date,
                    EpochMillis: approach.EpochDateCloseApproach,
                    VelocityKmPerSec: velocityKmPerSec,
                    MissDistanceKm: missKm,
                    OrbitingBody: approach.OrbitingBody));  
                }
            }
        }

        return result;
    }
}
