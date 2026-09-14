using Microsoft.Extensions.Options;
using YoutubeAiFactory.Application.AI;
using YoutubeAiFactory.Application.Common;

namespace YoutubeAiFactory.Infrastructure.AI;

internal sealed class ConfigurationAiModelResolver(IOptions<AiOptions> options) : IAiModelResolver
{
    public ResolvedAiModel Resolve(AiModelProfile profile)
    {
        var key = profile.ToString();
        if (!options.Value.Models.TryGetValue(key, out var configured))
            throw new ApplicationValidationException($"AI model profile '{key}' is not configured.");
        if (!string.Equals(configured.Provider, "OpenAI", StringComparison.OrdinalIgnoreCase))
            throw new ApplicationValidationException($"AI model profile '{key}' specifies unsupported provider '{configured.Provider}'.");
        if (string.IsNullOrWhiteSpace(configured.Model))
            throw new ApplicationValidationException($"AI model profile '{key}' must specify a model.");
        if (configured.TimeoutSeconds is < 5 or > 300)
            throw new ApplicationValidationException($"AI model profile '{key}' timeout must be between 5 and 300 seconds.");
        if (configured.MaxOutputTokens is < 1 or > 128_000)
            throw new ApplicationValidationException($"AI model profile '{key}' max output tokens must be between 1 and 128000.");

        return new ResolvedAiModel(profile, configured.Provider.Trim(), configured.Model.Trim(), configured.TimeoutSeconds, configured.MaxOutputTokens);
    }
}
