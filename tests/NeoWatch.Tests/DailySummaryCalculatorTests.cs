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

    [Fact]
    public void Summarize_EmptyInput_ReturnsEmptyList()
    {
        var summaries = DailySummaryCalculator.Summarize([]);

        Assert.Empty(summaries);
    }

    [Fact]
    public void Summarize_UnorderedInput_ReturnsSummariesOrderedByDate()
    {
        static ParsedApproach On(DateOnly date) =>
            new("1", "(1)", 21.3, 0.146, 0.327, false, date, 0, 13.45, 4_500_000, "Earth");

        var approaches = new[]
        {
            On(new DateOnly(2026, 11, 17)),
            On(new DateOnly(2026, 11, 15)),
            On(new DateOnly(2026, 11, 16)),
        };

        var summaries = DailySummaryCalculator.Summarize(approaches);

        Assert.Equal(
            [new DateOnly(2026, 11, 15), new DateOnly(2026, 11, 16), new DateOnly(2026, 11, 17)],
            summaries.Select(s => s.Date));
    }
}
