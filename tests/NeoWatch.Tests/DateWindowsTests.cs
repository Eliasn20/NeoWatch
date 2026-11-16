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

    // TODO (Elias): add tests for
    // - a range of exactly maxDays (one window, no empty second window)
    // - end before start throws ArgumentException
    // - maxDays of 0 throws ArgumentOutOfRangeException
    // - a range crossing a month and a year boundary
}
