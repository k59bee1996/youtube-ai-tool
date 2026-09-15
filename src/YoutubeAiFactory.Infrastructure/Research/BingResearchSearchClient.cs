using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Options;
using YoutubeAiFactory.Application.Common;
using YoutubeAiFactory.Application.Research;

namespace YoutubeAiFactory.Infrastructure.Research;

/// <summary>Bing adapter kept behind the provider-neutral research search contract.</summary>
internal sealed class BingResearchSearchClient(HttpClient client, IOptions<ResearchSearchOptions> options) : IResearchSearchClient
{
    public async Task<ResearchSearchResultPage> SearchAsync(ResearchSearchRequest request, CancellationToken cancellationToken)
    {
        var settings = options.Value;
        if (string.IsNullOrWhiteSpace(settings.ApiKey))
            throw new ExternalServiceException("Research search is not configured.", ExternalServiceFailure.Configuration);
        if (settings.TimeoutSeconds is < 1 or > 120)
            throw new ExternalServiceException("Research search timeout configuration is invalid.", ExternalServiceFailure.Configuration);
        if (!Uri.TryCreate(settings.Endpoint, UriKind.Absolute, out var endpoint) || endpoint.Scheme != Uri.UriSchemeHttps)
            throw new ExternalServiceException("Research search endpoint must be an absolute HTTPS URL.", ExternalServiceFailure.Configuration);
        var query = $"?q={Uri.EscapeDataString(request.Query)}&count={request.MaxResults}&mkt={Uri.EscapeDataString(ToMarket(request.Language))}&textDecorations=false&textFormat=Raw";
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(settings.TimeoutSeconds));
        using var message = new HttpRequestMessage(HttpMethod.Get, new Uri(endpoint + query));
        message.Headers.Add("Ocp-Apim-Subscription-Key", settings.ApiKey);
        try
        {
            using var response = await client.SendAsync(message, HttpCompletionOption.ResponseHeadersRead, timeout.Token);
            if (!response.IsSuccessStatusCode)
                throw new ExternalServiceException("Research search provider did not accept the request.", Classify(response.StatusCode));
            await using var stream = await response.Content.ReadAsStreamAsync(timeout.Token);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: timeout.Token);
            var results = new List<ResearchSearchResult>();
            if (document.RootElement.TryGetProperty("webPages", out var pages) && pages.TryGetProperty("value", out var values) && values.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in values.EnumerateArray().Take(request.MaxResults))
                {
                    if (!item.TryGetProperty("url", out var urlNode) || urlNode.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(urlNode.GetString())) continue;
                    var title = item.TryGetProperty("name", out var name) && name.ValueKind == JsonValueKind.String ? name.GetString() : null;
                    var snippet = item.TryGetProperty("snippet", out var text) && text.ValueKind == JsonValueKind.String ? text.GetString() : null;
                    DateTimeOffset? published = item.TryGetProperty("dateLastCrawled", out var date) && date.ValueKind == JsonValueKind.String && DateTimeOffset.TryParse(date.GetString(), out var parsed) ? parsed : null;
                    results.Add(new ResearchSearchResult(urlNode.GetString()!, title, snippet, published));
                }
            }
            return new ResearchSearchResultPage(results);
        }
        catch (OperationCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            throw new ExternalServiceException("Research search timed out.", ExternalServiceFailure.Transient, exception);
        }
        catch (JsonException exception)
        {
            throw new ExternalServiceException("Research search returned malformed data.", ExternalServiceFailure.UnexpectedResponse, exception);
        }
    }

    private static string ToMarket(string language) => language.StartsWith("en", StringComparison.OrdinalIgnoreCase) ||
        language.Equals("English", StringComparison.OrdinalIgnoreCase) ? "en-US" : language;
    private static ExternalServiceFailure Classify(HttpStatusCode code) => code switch
    {
        HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden => ExternalServiceFailure.Authentication,
        HttpStatusCode.TooManyRequests => ExternalServiceFailure.QuotaExceeded,
        HttpStatusCode.RequestTimeout or HttpStatusCode.BadGateway or HttpStatusCode.ServiceUnavailable or HttpStatusCode.GatewayTimeout => ExternalServiceFailure.Transient,
        _ => ExternalServiceFailure.UnexpectedResponse,
    };
}
