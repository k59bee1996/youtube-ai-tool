using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using YoutubeAiFactory.Application.AI;
using YoutubeAiFactory.Application.Common;

namespace YoutubeAiFactory.Infrastructure.AI;

/// <summary>Small OpenAI-compatible adapter; business workflows retain provider-neutral contracts.</summary>
internal sealed class OpenAiLlmProvider(HttpClient client, IOptions<AiOptions> options) : ILlmProvider
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    public async Task<LlmResult<T>> GenerateStructuredAsync<T>(LlmRequest request, CancellationToken cancellationToken)
    {
        var settings = options.Value;
        if (string.IsNullOrWhiteSpace(settings.ApiKey))
            throw new ExternalServiceException("AI provider is not configured.", ExternalServiceFailure.Configuration);
        if (settings.TimeoutSeconds is < 5 or > 300)
            throw new ExternalServiceException("AI:TimeoutSeconds must be between 5 and 300.", ExternalServiceFailure.Configuration);
        client.Timeout = TimeSpan.FromSeconds(settings.TimeoutSeconds);
        using var message = new HttpRequestMessage(HttpMethod.Post, "chat/completions");
        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiKey);
        message.Content = new StringContent(JsonSerializer.Serialize(new
        {
            model = settings.Model,
            response_format = new { type = "json_object" },
            messages = new[] { new { role = "system", content = request.SystemInstructions }, new { role = "user", content = request.UserContent } },
            max_tokens = request.ModelConfiguration.TryGetValue("max_output_tokens", out var tokens) && int.TryParse(tokens, out var parsed) ? parsed : 5000,
        }), Encoding.UTF8, "application/json");
        using var response = await client.SendAsync(message, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new ExternalServiceException("AI provider did not complete the analysis request.", Classify(response.StatusCode));
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        try
        {
            using var document = JsonDocument.Parse(body);
            var root = document.RootElement;
            var output = root.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();
            if (string.IsNullOrWhiteSpace(output)) throw new StructuredOutputException("AI provider returned an empty structured response.");
            var value = JsonSerializer.Deserialize<T>(output, SerializerOptions)
                ?? throw new StructuredOutputException("AI provider returned an empty structured response.");
            var usage = root.TryGetProperty("usage", out var usageElement) ? usageElement : default;
            int? inputTokens = usage.ValueKind == JsonValueKind.Object && usage.TryGetProperty("prompt_tokens", out var input) ? input.GetInt32() : null;
            int? outputTokens = usage.ValueKind == JsonValueKind.Object && usage.TryGetProperty("completion_tokens", out var outputToken) ? outputToken.GetInt32() : null;
            var model = root.TryGetProperty("model", out var responseModel) ? responseModel.GetString() ?? settings.Model : settings.Model;
            return new LlmResult<T>(value, settings.Provider, model, inputTokens, outputTokens, output);
        }
        catch (StructuredOutputException) { throw; }
        catch (Exception exception) when (exception is JsonException or KeyNotFoundException or InvalidOperationException)
        {
            throw new StructuredOutputException("AI provider returned invalid structured output.", exception);
        }
    }

    private static ExternalServiceFailure Classify(HttpStatusCode statusCode) => statusCode switch
    {
        HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden => ExternalServiceFailure.Authentication,
        HttpStatusCode.TooManyRequests => ExternalServiceFailure.QuotaExceeded,
        HttpStatusCode.RequestTimeout or HttpStatusCode.BadGateway or HttpStatusCode.ServiceUnavailable or HttpStatusCode.GatewayTimeout => ExternalServiceFailure.Transient,
        _ => ExternalServiceFailure.UnexpectedResponse,
    };
}
