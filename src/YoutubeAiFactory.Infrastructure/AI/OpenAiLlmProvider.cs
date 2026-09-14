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
        var model = request.ResolvedModel
            ?? throw new ExternalServiceException("AI request model profile was not resolved.", ExternalServiceFailure.Configuration);
        if (!string.Equals(model.Provider, "OpenAI", StringComparison.OrdinalIgnoreCase))
            throw new ExternalServiceException($"Unsupported AI provider '{model.Provider}'.", ExternalServiceFailure.Configuration);
        // HttpClient instances are pooled by IHttpClientFactory. Its Timeout property becomes
        // immutable after the first request, so applying a per-request configuration here
        // prevents structured-output repair attempts from using the same client.
        using var timeoutCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCancellation.CancelAfter(TimeSpan.FromSeconds(model.TimeoutSeconds));
        using var message = new HttpRequestMessage(HttpMethod.Post, "chat/completions");
        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiKey);
        var responseFormat = request.OutputSchema is null
            ? new { type = "json_object" }
            : (object)new
            {
                type = "json_schema",
                json_schema = new
                {
                    name = $"{request.PromptKey.Replace('-', '_')}_v{request.PromptVersion}",
                    strict = true,
                    schema = request.OutputSchema,
                },
            };
        message.Content = new StringContent(JsonSerializer.Serialize(new
        {
            model = model.Model,
            response_format = responseFormat,
            messages = new[] { new { role = "developer", content = request.SystemInstructions }, new { role = "user", content = request.UserContent } },
            max_completion_tokens = request.ModelConfiguration.TryGetValue("max_output_tokens", out var tokens) && int.TryParse(tokens, out var parsed) ? parsed : model.MaxOutputTokens,
        }), Encoding.UTF8, "application/json");
        using var response = await client.SendAsync(message, HttpCompletionOption.ResponseHeadersRead, timeoutCancellation.Token);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(timeoutCancellation.Token);
            throw new ExternalServiceException(CreateFailureMessage(response.StatusCode, error), Classify(response.StatusCode));
        }
        var body = await response.Content.ReadAsStringAsync(timeoutCancellation.Token);
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
            var responseModelName = root.TryGetProperty("model", out var responseModel) ? responseModel.GetString() ?? model.Model : model.Model;
            return new LlmResult<T>(value, model.Provider, responseModelName, inputTokens, outputTokens, output);
        }
        catch (StructuredOutputException) { throw; }
        catch (Exception exception) when (exception is JsonException or KeyNotFoundException or InvalidOperationException)
        {
            throw new StructuredOutputException($"AI provider returned invalid structured output: {exception.Message}", exception);
        }
    }

    private static ExternalServiceFailure Classify(HttpStatusCode statusCode) => statusCode switch
    {
        HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden => ExternalServiceFailure.Authentication,
        HttpStatusCode.TooManyRequests => ExternalServiceFailure.QuotaExceeded,
        HttpStatusCode.RequestTimeout or HttpStatusCode.BadGateway or HttpStatusCode.ServiceUnavailable or HttpStatusCode.GatewayTimeout => ExternalServiceFailure.Transient,
        _ => ExternalServiceFailure.UnexpectedResponse,
    };

    private static string CreateFailureMessage(HttpStatusCode statusCode, string errorBody)
    {
        var details = ReadSafeErrorDetails(errorBody);
        return details.Count == 0
            ? $"OpenAI API request failed with HTTP {(int)statusCode}."
            : $"OpenAI API request failed with HTTP {(int)statusCode} ({string.Join("; ", details)}).";
    }

    private static List<string> ReadSafeErrorDetails(string errorBody)
    {
        try
        {
            using var document = JsonDocument.Parse(errorBody);
            if (!document.RootElement.TryGetProperty("error", out var error) || error.ValueKind != JsonValueKind.Object)
                return [];

            var details = new List<string>(3);
            AddSafeErrorDetail(error, "type", details);
            AddSafeErrorDetail(error, "code", details);
            AddSafeErrorDetail(error, "param", details);
            return details;
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static void AddSafeErrorDetail(JsonElement error, string propertyName, List<string> details)
    {
        if (error.TryGetProperty(propertyName, out var property) &&
            property.ValueKind == JsonValueKind.String &&
            property.GetString() is { Length: > 0 } value)
            details.Add($"{propertyName}: {value}");
    }
}
