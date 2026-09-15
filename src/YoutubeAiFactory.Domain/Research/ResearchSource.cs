using YoutubeAiFactory.Domain.Common;

namespace YoutubeAiFactory.Domain.Research;

public sealed class ResearchSource
{
    private ResearchSource() { }

    public ResearchSource(Guid researchRunId, string url, string canonicalUrl, string domain, string? title,
        string? publisher, DateTimeOffset? publishedAt, DateTimeOffset retrievedAt, ResearchSourceCategory category,
        ResearchSourceFetchStatus fetchStatus, string? contentHash, string? qualityNotes, string? failureReason)
    {
        Id = Guid.NewGuid(); ResearchRunId = Guard.NotEmpty(researchRunId, nameof(researchRunId));
        Url = Guard.Required(url, nameof(url), 2_048); CanonicalUrl = Guard.Required(canonicalUrl, nameof(canonicalUrl), 2_048);
        Domain = Guard.Required(domain, nameof(domain), 255); Title = Optional(title, 1_000); Publisher = Optional(publisher, 500);
        PublishedAt = publishedAt; RetrievedAt = retrievedAt; Category = category; FetchStatus = fetchStatus;
        ContentHash = Optional(contentHash, 128); QualityNotes = Optional(qualityNotes, 1_000); FailureReason = Optional(failureReason, 1_000);
    }

    public Guid Id { get; private set; }
    public Guid ResearchRunId { get; private set; }
    public string Url { get; private set; } = string.Empty;
    public string CanonicalUrl { get; private set; } = string.Empty;
    public string Domain { get; private set; } = string.Empty;
    public string? Title { get; private set; }
    public string? Publisher { get; private set; }
    public DateTimeOffset? PublishedAt { get; private set; }
    public DateTimeOffset RetrievedAt { get; private set; }
    public ResearchSourceCategory Category { get; private set; }
    public ResearchSourceFetchStatus FetchStatus { get; private set; }
    public string? ContentHash { get; private set; }
    public string? QualityNotes { get; private set; }
    public string? FailureReason { get; private set; }

    private static string? Optional(string? value, int maxLength) => string.IsNullOrWhiteSpace(value) ? null : Guard.Required(value, nameof(value), maxLength);
}
