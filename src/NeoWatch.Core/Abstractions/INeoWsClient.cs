using NeoWatch.Core.Dtos;

namespace NeoWatch.Core.Abstractions;

public interface INeoWsClient
{
    /// <summary>Fetches the feed for an inclusive date range of at most 7 days.</summary>
    Task<NeoFeedResponse> GetFeedAsync(DateOnly start, DateOnly end, CancellationToken ct);
}
