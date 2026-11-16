using NeoWatch.Core.Pipeline;

namespace NeoWatch.Tests;

public class DateWindowsTests
{
    [Fact]
    public void Split_RangeLongerThanMax_ProducesFullWindowThenRemainder()
    {
        var windows = DateWindows.Split(new DateOnly(2026, 11, 1), new DateOnly(2026, 11, 10), maxDays: 7);

        Assert.Equal(
            [
                new DateRange(new DateOnly(2026, 11, 1), new DateOnly(2026, 11, 7)),
                new DateRange(new DateOnly(2026, 11, 8), new DateOnly(2026, 11, 10)),
            ],
            windows);
    }

    [Fact]
    public void Split_SingleDay_ProducesOneWindow()
    {
        var day = new DateOnly(2026, 11, 15);

        var windows = DateWindows.Split(day, day);

        Assert.Equal([new DateRange(day, day)], windows);
    }

    [Fact]
    public void Split_RangeOfExactlyMaxDays_ProducesOneWindow()
    {
        var start = new DateOnly(2026, 11, 1);
        var end = new DateOnly(2026, 11, 7);

        var windows = DateWindows.Split(start, end, maxDays: 7);

        Assert.Equal([new DateRange(start, end)], windows);
    }

    [Fact]
    public void Split_EndBeforeStart_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(
            () => DateWindows.Split(new DateOnly(2026, 11, 10), new DateOnly(2026, 11, 1)));
    }

    [Fact]
    public void Split_MaxDaysZero_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => DateWindows.Split(new DateOnly(2026, 11, 1), new DateOnly(2026, 11, 10), maxDays: 0));
    }

    [Fact]
    public void Split_RangeCrossingMonthAndYearBoundary_SplitsAcrossBoundary()
    {
        var windows = DateWindows.Split(new DateOnly(2026, 12, 28), new DateOnly(2027, 1, 6), maxDays: 7);

        Assert.Equal(
            [
                new DateRange(new DateOnly(2026, 12, 28), new DateOnly(2027, 1, 3)),
                new DateRange(new DateOnly(2027, 1, 4), new DateOnly(2027, 1, 6)),
            ],
            windows);
    }
}
