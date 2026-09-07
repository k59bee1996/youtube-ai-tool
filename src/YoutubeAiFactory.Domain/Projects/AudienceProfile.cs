using YoutubeAiFactory.Domain.Common;

namespace YoutubeAiFactory.Domain.Projects;

public sealed class AudienceProfile
{
    private AudienceProfile()
    {
    }

    public AudienceProfile(string description)
    {
        Description = Guard.Required(description, nameof(description), 2_000);
    }

    public string Description { get; private set; } = string.Empty;
}
