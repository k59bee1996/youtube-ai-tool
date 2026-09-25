using System.Text.Json.Nodes;

namespace YoutubeAiFactory.Application.AI;

public sealed record LlmRequest
{
    public LlmRequest(
        string promptKey,
        int promptVersion,
        string systemInstructions,
        string userContent,
        IReadOnlyDictionary<string, string>? modelConfiguration = null,
        JsonNode? outputSchema = null,
        AiModelProfile modelProfile = AiModelProfile.Reasoning,
        ResolvedAiModel? resolvedModel = null)
    {
        if (string.IsNullOrWhiteSpace(promptKey))
        {
            throw new ArgumentException("Prompt key is required.", nameof(promptKey));
        }

        if (promptVersion < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(promptVersion), "Prompt version must be at least 1.");
        }

        if (string.IsNullOrWhiteSpace(systemInstructions))
        {
            throw new ArgumentException("System instructions are required.", nameof(systemInstructions));
        }

        if (string.IsNullOrWhiteSpace(userContent))
        {
            throw new ArgumentException("User content is required.", nameof(userContent));
        }

        PromptKey = promptKey.Trim();
        PromptVersion = promptVersion;
        SystemInstructions = systemInstructions.Trim();
        UserContent = userContent.Trim();
        ModelConfiguration = modelConfiguration ?? new Dictionary<string, string>();
        OutputSchema = outputSchema?.DeepClone();
        ModelProfile = modelProfile;
        ResolvedModel = resolvedModel;
        RequestedAt = DateTimeOffset.UtcNow;
    }

    public string PromptKey { get; }

    public int PromptVersion { get; }

    public string SystemInstructions { get; }

    public string UserContent { get; }

    public IReadOnlyDictionary<string, string> ModelConfiguration { get; }

    /// <summary>A provider-neutral JSON Schema for the structured response, when supported.</summary>
    public JsonNode? OutputSchema { get; }

    /// <summary>The workflow's required intelligence class, never a frontend-selected model.</summary>
    public AiModelProfile ModelProfile { get; }

    /// <summary>Provider execution settings selected by infrastructure for this request.</summary>
    public ResolvedAiModel? ResolvedModel { get; }

    /// <summary>Time at which this provider request contract was created, used for effective-dated pricing.</summary>
    public DateTimeOffset RequestedAt { get; private init; }

    public LlmRequest WithResolvedModel(ResolvedAiModel model)
    {
        ArgumentNullException.ThrowIfNull(model);
        if (model.Profile != ModelProfile)
            throw new ArgumentException("Resolved model profile must match the request profile.", nameof(model));

        var resolved = new LlmRequest(PromptKey, PromptVersion, SystemInstructions, UserContent, ModelConfiguration, OutputSchema, ModelProfile, model)
        {
            RequestedAt = RequestedAt,
        };
        return resolved;
    }
}
