using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using NeoWatch.Core.Abstractions;
using NeoWatch.Core.Dtos;

namespace NeoWatch.Infrastructure;

/// <summary>
/// A "typed client": HttpClient is injected and configured by IHttpClientFactory
/// (base address, retries), so this class only knows about the endpoint itself.
/// </summary>
public sealed class NeoWsClient(HttpClient http, IOptions<NeoWsOptions> options) : INeoWsClient
{
    private readonly NeoWsOptions _options = options.Value;

    public async Task<NeoFeedResponse> GetFeedAsync(DateOnly start, DateOnly end, CancellationToken ct)
    {
        var url = $"neo/rest/v1/feed?start_date={start:yyyy-MM-dd}&end_date={end:yyyy-MM-dd}" +
                  $"&api_key={Uri.EscapeDataString(_options.ApiKey)}";

        var feed = await http.GetFromJsonAsync<NeoFeedResponse>(url, ct);
        return feed ?? throw new InvalidOperationException("NeoWs returned an empty body.");
    }
}
