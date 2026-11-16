namespace NeoWatch.Core.Pipeline;

public static class DateWindows
{
    /// <summary>NeoWs rejects feed requests longer than this many days.</summary>
    public const int MaxFeedDays = 7;

    /// <summary>
    /// Splits the inclusive range [start, end] into consecutive windows of at most
    /// <paramref name="maxDays"/> days each. The last window may be shorter.
    ///
    /// Example: Split(Nov 1, Nov 10, 7) => [Nov 1..Nov 7], [Nov 8..Nov 10]
    /// </summary>
    public static IReadOnlyList<DateRange> Split(DateOnly start, DateOnly end, int maxDays = MaxFeedDays)
    {
        if (maxDays < 1)
            throw new ArgumentOutOfRangeException(nameof(maxDays), "max days must be at least 1");
        if (end < start)
            throw new ArgumentException("end must not be before start", nameof(end));
            
        var windows = new List<DateRange>();
        var windowStart = start;

        while (windowStart <= end)
        {
            var windowEnd = windowStart.AddDays(maxDays - 1);
            if (windowEnd > end)
            windowEnd = end;

            windows.Add(new DateRange(windowStart, windowEnd));
            windowStart = windowEnd.AddDays(1);
        }

        

        return windows;

    }
}
