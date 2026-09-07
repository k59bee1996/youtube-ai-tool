using YoutubeAiFactory.Domain.Competitors;

namespace YoutubeAiFactory.Application.Competitors;

public sealed record CompetitorSummaryDto(
    Guid Id,
    Guid ProjectId,
    string YoutubeChannelId,
    string Title,
    string? Handle,
    string? ThumbnailUrl,
    long? SubscriberCount,
    long? VideoCount,
    long? ViewCount,
    DateTimeOffset LastCollectedAt);

public sealed record CompetitorVideoDto(
    Guid Id,
    string YoutubeVideoId,
    string Title,
    string? Description,
    string Url,
    string? ThumbnailUrl,
    TimeSpan? Duration,
    long? ViewCount,
    long? LikeCount,
    long? CommentCount,
    DateTimeOffset? PublishedAt,
    DateTimeOffset CollectedAt);

public sealed record CompetitorDetailsDto(
    Guid Id,
    Guid ProjectId,
    string SourceUrl,
    string YoutubeChannelId,
    string Title,
    string? Description,
    string? Handle,
    string? ThumbnailUrl,
    long? SubscriberCount,
    long? VideoCount,
    long? ViewCount,
    DateTimeOffset? PublishedAt,
    DateTimeOffset LastCollectedAt,
    IReadOnlyList<CompetitorVideoDto> Videos)
{
    internal static CompetitorDetailsDto FromDomain(CompetitorChannel channel) => new(
        channel.Id,
        channel.ProjectId,
        channel.SourceUrl,
        channel.YoutubeChannelId!,
        channel.Title!,
        channel.Description,
        channel.Handle,
        channel.ThumbnailUrl,
        channel.SubscriberCount,
        channel.VideoCount,
        channel.ViewCount,
        channel.PublishedAt,
        channel.LastCollectedAt!.Value,
        channel.Videos
            .OrderByDescending(video => video.PublishedAt)
            .Select(video => new CompetitorVideoDto(
                video.Id,
                video.YoutubeVideoId,
                video.Title,
                video.Description,
                video.Url,
                video.ThumbnailUrl,
                video.Duration,
                video.ViewCount,
                video.LikeCount,
                video.CommentCount,
                video.PublishedAt,
                video.CollectedAt))
            .ToArray());

    internal static CompetitorSummaryDto ToSummary(CompetitorChannel channel) => new(
        channel.Id,
        channel.ProjectId,
        channel.YoutubeChannelId!,
        channel.Title!,
        channel.Handle,
        channel.ThumbnailUrl,
        channel.SubscriberCount,
        channel.VideoCount,
        channel.ViewCount,
        channel.LastCollectedAt!.Value);
}
