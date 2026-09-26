using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using YoutubeAiFactory.Application.AI;
using YoutubeAiFactory.Application.Common;
using YoutubeAiFactory.Infrastructure;
using YoutubeAiFactory.Infrastructure.AI;

namespace YoutubeAiFactory.IntegrationTests;

public sealed class OpenAiLlmProviderTests
{
    [Fact]
    public void Llm_http_client_defers_timeout_enforcement_to_the_resolved_model()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Server=(localdb)\\MSSQLLocalDB;Database=YoutubeAiFactoryTests;Trusted_Connection=True",
            })
            .Build();
        var services = new ServiceCollection();
        services.AddInfrastructure(configuration);
        using var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IHttpClientFactory>();

        using var client = factory.CreateClient(nameof(ILlmProvider));

        Assert.Equal(Timeout.InfiniteTimeSpan, client.Timeout);
    }

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
    public async Task GenerateStructuredAsync_records_optional_usage_and_uses_historical_configured_pricing()
    {
        var handler = new QueueHttpMessageHandler(ResponseWithUsage());
        using var client = new HttpClient(handler) { BaseAddress = new Uri("https://api.openai.com/v1/") };
        var provider = new OpenAiLlmProvider(client, Options.Create(new AiOptions
        {
            ApiKey = "test-key",
            Pricing =
            [
                new AiModelPricingOptions
                {
                    Provider = "OpenAI", Model = "test-model", Currency = "USD", PriceVersion = "fixture-v1",
                    InputPricePerMillionTokens = 2m, CachedInputPricePerMillionTokens = 1m,
                    OutputPricePerMillionTokens = 4m, EffectiveFrom = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
                },
            ],
        }));
        var request = new LlmRequest("test-prompt", 1, "system", "user")
            .WithResolvedModel(new ResolvedAiModel(AiModelProfile.Reasoning, "OpenAI", "test-model", 60, 5_000));

        var result = await provider.GenerateStructuredAsync<JsonElement>(request, CancellationToken.None);

        Assert.Equal(1_000, result.InputTokens);
        Assert.Equal(2_000, result.OutputTokens);
        Assert.Equal(200, result.CachedInputTokens);
        Assert.Equal(20, result.ReasoningTokens);
        Assert.Equal(0.0098m, result.CalculatedEstimatedCost);
        Assert.Equal("USD", result.Currency);
        Assert.Equal("fixture-v1", result.PricingVersion);
    }

    [Fact]
    public async Task GenerateStructuredAsync_includes_safe_openai_error_details_and_a_context_diagnostic_without_returning_the_error_message()
    {
        var handler = new QueueHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent(
                """{"error":{"message":"This model's maximum context length is 16,384 tokens. Prompt content must not be exposed.","type":"invalid_request_error","code":"unsupported_parameter","param":"messages"}}""",
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
        Assert.Contains("messages", exception.Message, StringComparison.Ordinal);
        Assert.Contains("diagnostic: context_limit", exception.Message, StringComparison.Ordinal);
        Assert.Contains("model: test-model", exception.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("Prompt content", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GenerateStructuredAsync_treats_an_uncaller_cancelled_request_as_a_transient_provider_failure()
    {
        using var client = new HttpClient(new CancelledHttpMessageHandler())
        {
            BaseAddress = new Uri("https://api.openai.com/v1/"),
        };
        var provider = new OpenAiLlmProvider(client, Options.Create(new AiOptions { ApiKey = "test-key" }));
        var request = new LlmRequest("test-prompt", 1, "system", "user")
            .WithResolvedModel(new ResolvedAiModel(AiModelProfile.Reasoning, "OpenAI", "test-model", 60, 5_000));

        var exception = await Assert.ThrowsAsync<ExternalServiceException>(() => provider.GenerateStructuredAsync<JsonElement>(request, CancellationToken.None));

        Assert.Equal(ExternalServiceFailure.Transient, exception.Failure);
        Assert.Contains("cancelled", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GenerateStructuredAsync_includes_safe_diagnostics_for_an_empty_model_response()
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""{"id":"chatcmpl_test","choices":[{"finish_reason":"length","message":{"content":null,"refusal":"Do not expose this refusal text."}}]}""", Encoding.UTF8, "application/json"),
        };
        response.Headers.Add("x-request-id", "req_test");
        var handler = new QueueHttpMessageHandler(response);
        using var client = new HttpClient(handler) { BaseAddress = new Uri("https://api.openai.com/v1/") };
        var provider = new OpenAiLlmProvider(client, Options.Create(new AiOptions { ApiKey = "test-key" }));
        var request = new LlmRequest("test-prompt", 1, "system", "user")
            .WithResolvedModel(new ResolvedAiModel(AiModelProfile.Reasoning, "OpenAI", "test-model", 60, 5_000));

        var exception = await Assert.ThrowsAsync<StructuredOutputException>(() => provider.GenerateStructuredAsync<JsonElement>(request, CancellationToken.None));

        Assert.Contains("response_id: chatcmpl_test", exception.Message, StringComparison.Ordinal);
        Assert.Contains("finish_reason: length", exception.Message, StringComparison.Ordinal);
        Assert.Contains("request_id: req_test", exception.Message, StringComparison.Ordinal);
        Assert.Contains("model_refusal: true", exception.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("Do not expose", exception.Message, StringComparison.Ordinal);
        Assert.Single(handler.ClientRequestIds);
        Assert.NotEqual(Guid.Empty.ToString(), handler.ClientRequestIds[0]);
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

    private static HttpResponseMessage ResponseWithUsage() => new(HttpStatusCode.OK)
    {
        Content = new StringContent(
            """{"choices":[{"message":{"content":"{\"value\":\"priced\"}"}}],"model":"test-model","usage":{"prompt_tokens":1000,"completion_tokens":2000,"prompt_tokens_details":{"cached_tokens":200},"completion_tokens_details":{"reasoning_tokens":20}}}""",
            Encoding.UTF8,
            "application/json"),
    };

    private sealed class QueueHttpMessageHandler(params HttpResponseMessage[] responses) : HttpMessageHandler
    {
        private readonly Queue<HttpResponseMessage> responses = new(responses);
        public int RequestCount { get; private set; }
        public List<string> RequestBodies { get; } = [];
        public List<string> ClientRequestIds { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestCount++;
            RequestBodies.Add(await request.Content!.ReadAsStringAsync(cancellationToken));
            ClientRequestIds.Add(request.Headers.GetValues("X-Client-Request-Id").Single());
            return responses.Dequeue();
        }
    }

    private sealed class CancelledHttpMessageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromException<HttpResponseMessage>(new TaskCanceledException("Simulated transport cancellation."));
    }
}
