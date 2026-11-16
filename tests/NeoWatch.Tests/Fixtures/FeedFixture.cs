using System.Text.Json;
using NeoWatch.Core.Dtos;

namespace NeoWatch.Tests.Fixtures;

public static class FeedFixture
{
    /// <summary>
    /// feed-sample.json is a hand-written sample in the NeoWs feed shape with
    /// invented asteroids. Milestone 2: save a real response next to it and add a
    /// test that loads that too.
    /// </summary>
    public static NeoFeedResponse Load(string fileName = "feed-sample.json")
    {
        var json = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", fileName));
        return JsonSerializer.Deserialize<NeoFeedResponse>(json)
               ?? throw new InvalidOperationException("Fixture deserialized to null.");
    }
}
