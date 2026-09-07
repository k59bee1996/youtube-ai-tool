using YoutubeAiFactory.Domain.Common;

namespace YoutubeAiFactory.Domain.Competitors;

public sealed class CompetitorVideo
{
    private CompetitorVideo()
    {
    }

    internal CompetitorVideo(
        Guid competitorChannelId,
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
        Id = Guid.NewGuid();
        CompetitorChannelId = Guard.NotEmpty(competitorChannelId, nameof(competitorChannelId));
        YoutubeVideoId = Guard.Required(youtubeVideoId, nameof(youtubeVideoId), 100);
        RecordMetadata(
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
    }

    public Guid Id { get; private set; }

    public Guid CompetitorChannelId { get; private set; }

    public string YoutubeVideoId { get; private set; } = string.Empty;

    public string Title { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public string Url { get; private set; } = string.Empty;

    public string? ThumbnailUrl { get; private set; }

    public TimeSpan? Duration { get; private set; }

    public long? ViewCount { get; private set; }

    public long? LikeCount { get; private set; }

    public long? CommentCount { get; private set; }

    public DateTimeOffset? PublishedAt { get; private set; }

    public DateTimeOffset CollectedAt { get; private set; }

    internal void RecordMetadata(
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
        Title = Guard.Required(title, nameof(title), 500);
        Description = NormalizeOptional(description, 10_000, nameof(description));
        Url = RequireAbsoluteUri(url, nameof(url));
        ThumbnailUrl = string.IsNullOrWhiteSpace(thumbnailUrl)
            ? null
            : RequireAbsoluteUri(thumbnailUrl, nameof(thumbnailUrl));
        Duration = duration < TimeSpan.Zero
            ? throw new DomainException("duration cannot be negative.")
            : duration;
        ViewCount = NonNegative(viewCount, nameof(viewCount));
        LikeCount = NonNegative(likeCount, nameof(likeCount));
        CommentCount = NonNegative(commentCount, nameof(commentCount));
        PublishedAt = publishedAt;
        CollectedAt = collectedAt;
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
