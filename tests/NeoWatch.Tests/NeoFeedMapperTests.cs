using NeoWatch.Core.Pipeline;
using NeoWatch.Tests.Fixtures;

namespace NeoWatch.Tests;

public class NeoFeedMapperTests
{
    [Fact]
    public void Fixture_Deserializes_IntoDtos()
    {
        // Passes from day one: it checks the DTOs, not your mapper.
        var feed = FeedFixture.Load();

        Assert.Equal(3, feed.ElementCount);
        Assert.Equal(2, feed.NearEarthObjects["2026-11-15"].Count);
        Assert.Equal("13.4567", feed.NearEarthObjects["2026-11-15"][0].CloseApproachData[0].RelativeVelocity.KilometersPerSecond);
    }

    [Fact]
    public void Map_FlattensEveryApproach()
    {
        var approaches = NeoFeedMapper.Map(FeedFixture.Load());

        Assert.Equal(3, approaches.Count);
    }

    [Fact]
    public void Map_ParsesStringNumbersAndDates()
    {
        var approaches = NeoFeedMapper.Map(FeedFixture.Load());

        var first = Assert.Single(approaches, a => a.NeoId == "9000001");
        Assert.Equal(new DateOnly(2026, 11, 15), first.Date);
        Assert.Equal(13.4567, first.VelocityKmPerSec, precision: 4);
        Assert.Equal(4502895.25, first.MissDistanceKm, precision: 2);
        Assert.True(first.IsPotentiallyHazardous);
    }

    // TODO (Elias): add tests for the validation rules, building small
    // NeoFeedResponse objects by hand instead of using the fixture:
    // - unparseable velocity ("abc") => row skipped, others kept
    // - negative miss distance => row skipped
    // - an asteroid with two close approaches => two rows
    // - empty feed => empty list
}
