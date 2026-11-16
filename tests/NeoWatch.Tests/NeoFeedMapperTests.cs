using NeoWatch.Core.Dtos;
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

    private static CloseApproachDto Approach(
        string velocity = "13.45", string missKm = "4500000.5", long epoch = 1794731400000) =>
        new("2026-11-15", epoch, new RelativeVelocityDto(velocity), new MissDistanceDto(missKm), "Earth");

    private static NeoDto Neo(string id, params CloseApproachDto[] approaches) =>
        new(id, $"({id})", 21.3, new EstimatedDiameterDto(new DiameterRangeDto(0.146, 0.327)), false, [.. approaches]);

    private static NeoFeedResponse Feed(params NeoDto[] neos) =>
        new(neos.Length, new Dictionary<string, List<NeoDto>> { ["2026-11-15"] = [.. neos] });

    [Fact]
    public void Map_UnparseableVelocity_SkipsRowAndKeepsOthers()
    {
        var feed = Feed(Neo("1", Approach(velocity: "abc")), Neo("2", Approach()));

        var approaches = NeoFeedMapper.Map(feed);

        Assert.Equal("2", Assert.Single(approaches).NeoId);
    }

    [Fact]
    public void Map_NegativeMissDistance_SkipsRow()
    {
        var feed = Feed(Neo("1", Approach(missKm: "-1.5")));

        var approaches = NeoFeedMapper.Map(feed);

        Assert.Empty(approaches);
    }

    [Fact]
    public void Map_AsteroidWithTwoApproaches_ProducesTwoRows()
    {
        var feed = Feed(Neo("1", Approach(epoch: 1794731400000), Approach(epoch: 1826267400000)));

        var approaches = NeoFeedMapper.Map(feed);

        Assert.Equal(2, approaches.Count);
        Assert.All(approaches, a => Assert.Equal("1", a.NeoId));
    }

    [Fact]
    public void Map_EmptyFeed_ReturnsEmptyList()
    {
        var feed = new NeoFeedResponse(0, []);

        var approaches = NeoFeedMapper.Map(feed);

        Assert.Empty(approaches);
    }
}
