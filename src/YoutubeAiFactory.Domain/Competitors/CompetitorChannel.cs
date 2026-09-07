using YoutubeAiFactory.Domain.Common;

namespace YoutubeAiFactory.Domain.Competitors;

public sealed class CompetitorChannel
{
    private readonly List<CompetitorVideo> _videos = [];

    private CompetitorChannel()
    {
    }

    public CompetitorChannel(Guid projectId, string sourceUrl, DateTimeOffset createdAt)
    {
        Id = Guid.NewGuid();
        ProjectId = Guard.NotEmpty(projectId, nameof(projectId));
        SourceUrl = RequireAbsoluteUri(sourceUrl, nameof(sourceUrl));
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public Guid ProjectId { get; private set; }

    public string SourceUrl { get; private set; } = string.Empty;

    public string? YoutubeChannelId { get; private set; }

    public string? Title { get; private set; }

    public string? Description { get; private set; }

    public string? Handle { get; private set; }

    public string? ThumbnailUrl { get; private set; }

    public long? SubscriberCount { get; private set; }

    public long? VideoCount { get; private set; }

    public long? ViewCount { get; private set; }

    public DateTimeOffset? PublishedAt { get; private set; }

    public DateTimeOffset? LastCollectedAt { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public IReadOnlyCollection<CompetitorVideo> Videos => _videos.AsReadOnly();

    public void RecordMetadata(
        string youtubeChannelId,
        string title,
        string? description,
        string? handle,
        string? thumbnailUrl,
        long? subscriberCount,
        long? videoCount,
        long? viewCount,
        DateTimeOffset? publishedAt,
        DateTimeOffset collectedAt)
    {
        var normalizedChannelId = Guard.Required(youtubeChannelId, nameof(youtubeChannelId), 100);
        if (YoutubeChannelId is not null &&
            !string.Equals(YoutubeChannelId, normalizedChannelId, StringComparison.Ordinal))
        {
            throw new DomainException("A competitor's YouTube channel identity cannot be changed.");
        }

        YoutubeChannelId = normalizedChannelId;
        Title = Guard.Required(title, nameof(title), 500);
        Description = NormalizeOptional(description, 10_000, nameof(description));
        Handle = NormalizeOptional(handle, 100, nameof(handle));
        ThumbnailUrl = string.IsNullOrWhiteSpace(thumbnailUrl)
            ? null
            : RequireAbsoluteUri(thumbnailUrl, nameof(thumbnailUrl));
        SubscriberCount = NonNegative(subscriberCount, nameof(subscriberCount));
        VideoCount = NonNegative(videoCount, nameof(videoCount));
        ViewCount = NonNegative(viewCount, nameof(viewCount));
        PublishedAt = publishedAt;
        LastCollectedAt = collectedAt;
    }

    public CompetitorVideo UpsertVideo(
        string youtubeVideoId,
        string title,
        string? description,
        string url,
        string? thumbnailUrl,
        TimeSpan? duration,
        long? viewCount,
        long? likeCount,
        long? commentCount,
        DateTimeOffset? publishedAt,
        DateTimeOffset collectedAt)
    {
        var normalizedVideoId = Guard.Required(youtubeVideoId, nameof(youtubeVideoId), 100);
        var existing = _videos.SingleOrDefault(video => video.YoutubeVideoId == normalizedVideoId);

        if (existing is not null)
        {
            existing.RecordMetadata(
                title,
                description,
                url,
                thumbnailUrl,
                duration,
                viewCount,
                likeCount,
                commentCount,
                publishedAt,
                collectedAt);
            return existing;
        }

        var video = new CompetitorVideo(
            Id,
            normalizedVideoId,
            title,
            description,
            url,
            thumbnailUrl,
            duration,
            viewCount,
            likeCount,
            commentCount,
            publishedAt,
            collectedAt);
        _videos.Add(video);
        return video;
    }

    private static string RequireAbsoluteUri(string value, string parameterName)
    {
        var trimmed = Guard.Required(value, parameterName, 2_048);
        if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            throw new DomainException($"{parameterName} must be an absolute HTTP URL.");
        }

        return uri.AbsoluteUri;
    }

    private static string? NormalizeOptional(string? value, int maxLength, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        if (trimmed.Length > maxLength)
        {
            throw new DomainException($"{parameterName} must be {maxLength} characters or fewer.");
        }

        return trimmed;
    }

    private static long? NonNegative(long? value, string parameterName)
    {
        if (value < 0)
        {
            throw new DomainException($"{parameterName} cannot be negative.");
        }

        return value;
    }
}
