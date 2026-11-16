using NeoWatch.Core.Pipeline;
using NeoWatch.Tests.Fixtures;

namespace NeoWatch.Tests;

public class DailySummaryCalculatorTests
{
    [Fact]
    public void Summarize_SampleFeed_ProducesOneRowPerDay()
    {
        var approaches = NeoFeedMapper.Map(FeedFixture.Load());

        var summaries = DailySummaryCalculator.Summarize(approaches);

        Assert.Equal(2, summaries.Count);

        var first = summaries[0];
        Assert.Equal(new DateOnly(2026, 11, 15), first.Date);
        Assert.Equal(2, first.ApproachCount);
        Assert.Equal(1, first.HazardousCount);
        Assert.Equal("(2099 AA1)", first.ClosestAsteroidName);
        Assert.Equal("(2099 AA1)", first.LargestAsteroidName);
    }

    [Fact]
    public void Summarize_RealFeed_MatchesKnownTotals()
    {
        var approaches = NeoFeedMapper.Map(FeedFixture.Load("feed-real.json"));

        var summaries = DailySummaryCalculator.Summarize(approaches);

        Assert.Equal(7, summaries.Count);
        Assert.Equal(38, summaries.Sum(s => s.ApproachCount));
        Assert.Equal(3, summaries.Sum(s => s.HazardousCount));
    }
}