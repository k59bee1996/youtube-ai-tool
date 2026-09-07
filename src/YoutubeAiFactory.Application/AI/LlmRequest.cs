namespace YoutubeAiFactory.Application.AI;

public sealed record LlmRequest
{
    public LlmRequest(
        string promptKey,
        int promptVersion,
        string systemInstructions,
        string userContent,
        IReadOnlyDictionary<string, string>? modelConfiguration = null)
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
    }

    public string PromptKey { get; }

    public int PromptVersion { get; }

    public string SystemInstructions { get; }

    public string UserContent { get; }

    public IReadOnlyDictionary<string, string> ModelConfiguration { get; }
}
