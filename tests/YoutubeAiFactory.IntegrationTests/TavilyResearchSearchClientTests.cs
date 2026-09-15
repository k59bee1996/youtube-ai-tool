using System.Net;
using System.Text;
using Microsoft.Extensions.Options;
using YoutubeAiFactory.Application.Research;
using YoutubeAiFactory.Infrastructure.Research;

namespace YoutubeAiFactory.IntegrationTests;

public sealed class TavilyResearchSearchClientTests
{
    [Fact]
    public async Task Uses_supported_tavily_search_contract_without_treating_snippets_as_evidence()
    {
        var handler = new StubHandler("""
            {"results":[{"url":"https://example.org/source","title":"A source","content":"Discovery snippet","published_date":"2026-01-02"}]}
            """);
        using var client = new HttpClient(handler);
        var search = new TavilyResearchSearchClient(client, Options.Create(new ResearchSearchOptions { ApiKey = "test-key" }));

        var result = await search.SearchAsync(new ResearchSearchRequest("castle economics", "English", 3), CancellationToken.None);

        var source = Assert.Single(result.Results);
        Assert.Equal("https://example.org/source", source.Url);
        Assert.Equal("A source", source.Title);
        Assert.Equal("Discovery snippet", source.Snippet);
        Assert.Equal(HttpMethod.Post, handler.Method);
        Assert.Equal("Bearer", handler.AuthorizationScheme);
        Assert.Contains("\"query\":\"castle economics\"", handler.RequestBody, StringComparison.Ordinal);
        Assert.Contains("\"includeRawContent\":false", handler.RequestBody, StringComparison.Ordinal);
    }

    private sealed class StubHandler(string body) : HttpMessageHandler
    {
        public HttpMethod? Method { get; private set; }
        public string? AuthorizationScheme { get; private set; }
        public string RequestBody { get; private set; } = string.Empty;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Method = request.Method;
            AuthorizationScheme = request.Headers.Authorization?.Scheme;
            RequestBody = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
        }
    }
}
