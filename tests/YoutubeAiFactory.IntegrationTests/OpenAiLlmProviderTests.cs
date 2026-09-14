using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Options;
using YoutubeAiFactory.Application.AI;
using YoutubeAiFactory.Application.Common;
using YoutubeAiFactory.Infrastructure.AI;

namespace YoutubeAiFactory.IntegrationTests;

public sealed class OpenAiLlmProviderTests
{
    [Fact]
    public async Task GenerateStructuredAsync_allows_a_second_request_on_the_same_http_client()
    {
        var handler = new QueueHttpMessageHandler(Response("first"), Response("second"));
        using var client = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.openai.com/v1/"),
        };
        var provider = new OpenAiLlmProvider(client, Options.Create(new AiOptions
        {
            ApiKey = "test-key",
        }));
        var request = new LlmRequest(
            "test-prompt",
            1,
            "system",
            "user",
            outputSchema: JsonNode.Parse("""{"type":"object","additionalProperties":false,"properties":{},"required":[]}"""))
            .WithResolvedModel(new ResolvedAiModel(AiModelProfile.Reasoning, "OpenAI", "test-model", 60, 5_000));

        var first = await provider.GenerateStructuredAsync<JsonElement>(request, CancellationToken.None);
        var second = await provider.GenerateStructuredAsync<JsonElement>(request, CancellationToken.None);

        Assert.Equal("first", first.Value.GetProperty("value").GetString());
        Assert.Equal("second", second.Value.GetProperty("value").GetString());
        Assert.Equal(2, handler.RequestCount);
        using var payload = JsonDocument.Parse(handler.RequestBodies[0]);
        Assert.Equal("json_schema", payload.RootElement.GetProperty("response_format").GetProperty("type").GetString());
        Assert.True(payload.RootElement.GetProperty("response_format").GetProperty("json_schema").GetProperty("strict").GetBoolean());
        Assert.Equal("developer", payload.RootElement.GetProperty("messages")[0].GetProperty("role").GetString());
        Assert.Equal(5_000, payload.RootElement.GetProperty("max_completion_tokens").GetInt32());
        Assert.False(payload.RootElement.TryGetProperty("max_tokens", out _));
    }

    [Fact]
    public async Task GenerateStructuredAsync_includes_safe_openai_error_details_without_returning_the_error_message()
    {
        var handler = new QueueHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent(
                """{"error":{"message":"Prompt content must not be exposed.","type":"invalid_request_error","code":"unsupported_parameter","param":"max_tokens"}}""",
                Encoding.UTF8,
                "application/json"),
        });
        using var client = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.openai.com/v1/"),
        };
        var provider = new OpenAiLlmProvider(client, Options.Create(new AiOptions { ApiKey = "test-key" }));
        var request = new LlmRequest("test-prompt", 1, "system", "user")
            .WithResolvedModel(new ResolvedAiModel(AiModelProfile.Reasoning, "OpenAI", "test-model", 60, 5_000));

        var exception = await Assert.ThrowsAsync<ExternalServiceException>(() => provider.GenerateStructuredAsync<JsonElement>(request, CancellationToken.None));

        Assert.Equal(ExternalServiceFailure.UnexpectedResponse, exception.Failure);
        Assert.Contains("HTTP 400", exception.Message, StringComparison.Ordinal);
        Assert.Contains("invalid_request_error", exception.Message, StringComparison.Ordinal);
        Assert.Contains("unsupported_parameter", exception.Message, StringComparison.Ordinal);
        Assert.Contains("max_tokens", exception.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("Prompt content", exception.Message, StringComparison.Ordinal);
    }

    private static HttpResponseMessage Response(string value) => new(HttpStatusCode.OK)
    {
        Content = new StringContent(
            JsonSerializer.Serialize(new
            {
                choices = new[] { new { message = new { content = JsonSerializer.Serialize(new { value }) } } },
                model = "test-model",
                usage = new { prompt_tokens = 1, completion_tokens = 1 },
            }),
            Encoding.UTF8,
            "application/json"),
    };

    private sealed class QueueHttpMessageHandler(params HttpResponseMessage[] responses) : HttpMessageHandler
    {
        private readonly Queue<HttpResponseMessage> responses = new(responses);
        public int RequestCount { get; private set; }
        public List<string> RequestBodies { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestCount++;
            RequestBodies.Add(await request.Content!.ReadAsStringAsync(cancellationToken));
            return responses.Dequeue();
        }
    }
}
