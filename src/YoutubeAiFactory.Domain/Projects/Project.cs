using YoutubeAiFactory.Domain.Common;

namespace YoutubeAiFactory.Domain.Projects;

public sealed class Project
{
    private Project()
    {
    }

    public Project(
        string name,
        Market market,
        AudienceProfile audience,
        DateTimeOffset createdAt)
    {
        Id = Guid.NewGuid();
        Name = Guard.Required(name, nameof(name), 200);
        Market = market ?? throw new DomainException("Market is required.");
        Audience = audience ?? throw new DomainException("Audience is required.");
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public Market Market { get; private set; } = null!;

    public AudienceProfile Audience { get; private set; } = null!;

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? UpdatedAt { get; private set; }

    public void UpdateSettings(
        string name,
        Market market,
        AudienceProfile audience,
        DateTimeOffset updatedAt)
    {
        Name = Guard.Required(name, nameof(name), 200);
        Market = market ?? throw new DomainException("Market is required.");
        Audience = audience ?? throw new DomainException("Audience is required.");
        UpdatedAt = updatedAt;
    }
}
