namespace YoutubeAiFactory.Application.Competitors;

public interface IYouTubeClient
{
    Task<YouTubeChannelResult> GetChannelAsync(
        YouTubeChannelReference reference,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<YouTubeVideoResult>> GetChannelVideosAsync(
        string uploadsPlaylistId,
        int limit,
        CancellationToken cancellationToken);
}

public sealed record YouTubeChannelResult(
    string YoutubeChannelId,
    string Title,
    string? Description,
    string? Handle,
    string? ThumbnailUrl,
    long? SubscriberCount,
    long? VideoCount,
    long? ViewCount,
    DateTimeOffset? PublishedAt,
    string UploadsPlaylistId);

public sealed record YouTubeVideoResult(
    string YoutubeVideoId,
    string Title,
    string? Description,
    string Url,
    string? ThumbnailUrl,
    TimeSpan? Duration,
    long? ViewCount,
    long? LikeCount,
    long? CommentCount,
    DateTimeOffset? PublishedAt);
