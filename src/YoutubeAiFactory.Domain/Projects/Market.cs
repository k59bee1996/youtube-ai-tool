using YoutubeAiFactory.Domain.Common;

namespace YoutubeAiFactory.Domain.Projects;

public sealed class Market
{
    private Market()
    {
    }

    public Market(string name, string targetLanguage, string targetGeography)
    {
        Name = Guard.Required(name, nameof(name), 200);
        TargetLanguage = Guard.Required(targetLanguage, nameof(targetLanguage), 50);
        TargetGeography = Guard.Required(targetGeography, nameof(targetGeography), 100);
    }

    public string Name { get; private set; } = string.Empty;

    public string TargetLanguage { get; private set; } = string.Empty;

    public string TargetGeography { get; private set; } = string.Empty;
}
