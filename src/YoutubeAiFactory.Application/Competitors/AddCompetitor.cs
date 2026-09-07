using System.Diagnostics;
using Microsoft.Extensions.Logging;
using YoutubeAiFactory.Application.Common;
using YoutubeAiFactory.Application.Persistence;
using YoutubeAiFactory.Domain.Common;
using YoutubeAiFactory.Domain.Competitors;

namespace YoutubeAiFactory.Application.Competitors;

public sealed record AddCompetitorCommand(Guid ProjectId, string YoutubeUrl);

public sealed record AddCompetitorResult(CompetitorDetailsDto Competitor, bool Created);

public sealed class AddCompetitorHandler(
    IYoutubeAiFactoryStore store,
    IYouTubeClient youTubeClient,
    CompetitorCollectionOptions options,
    TimeProvider timeProvider,
    ILogger<AddCompetitorHandler> logger)
{
    private static readonly Action<ILogger, Guid, string, int, Exception?> LogCollectionStarted =
        LoggerMessage.Define<Guid, string, int>(
            LogLevel.Information,
            new EventId(1, nameof(LogCollectionStarted)),
            "Collecting competitor for project {ProjectId}, channel {YoutubeChannelId}, requesting {VideosRequested} videos.");

    private static readonly Action<ILogger, Guid, Guid, string, int, long, Exception?> LogCollectionCompleted =
        LoggerMessage.Define<Guid, Guid, string, int, long>(
            LogLevel.Information,
            new EventId(2, nameof(LogCollectionCompleted)),
            "Collected competitor {CompetitorId} for project {ProjectId}, channel {YoutubeChannelId}: {VideosRetrieved} videos in {CollectionDurationMs} ms.");

    private static readonly Action<ILogger, Guid, string, string, long, Exception?> LogCollectionFailed =
        LoggerMessage.Define<Guid, string, string, long>(
            LogLevel.Warning,
            new EventId(3, nameof(LogCollectionFailed)),
            "Competitor collection failed for project {ProjectId}, channel {YoutubeChannelId}, with {Failure} after {CollectionDurationMs} ms.");

    private static readonly Action<ILogger, Guid, string, long, Exception?> LogCollectionCancelled =
        LoggerMessage.Define<Guid, string, long>(
            LogLevel.Information,
            new EventId(4, nameof(LogCollectionCancelled)),
            "Competitor collection was cancelled for project {ProjectId}, channel {YoutubeChannelId}, after {CollectionDurationMs} ms.");

    private static readonly Func<ILogger, Guid, IDisposable?> BeginCollectionScope =
        LoggerMessage.DefineScope<Guid>("Competitor collection for project {ProjectId}");

    public async Task<AddCompetitorResult> HandleAsync(
        AddCompetitorCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (!await store.ProjectExistsAsync(command.ProjectId, cancellationToken))
        {
            throw new ResourceNotFoundException($"Project '{command.ProjectId}' was not found.");
        }

        if (options.VideoLimit is < 1 or > CompetitorCollectionOptions.MaximumVideoLimit)
        {
            throw new ExternalServiceException(
                $"CompetitorCollection:VideoLimit must be between 1 and {CompetitorCollectionOptions.MaximumVideoLimit}.",
                ExternalServiceFailure.Configuration);
        }

        var reference = YouTubeChannelReferenceParser.Parse(command.YoutubeUrl);
        using var collectionScope = BeginCollectionScope(logger, command.ProjectId);
        var stopwatch = Stopwatch.StartNew();
        string? youtubeChannelId = null;

        try
        {
            var channelResult = await youTubeClient.GetChannelAsync(reference, cancellationToken);
            youtubeChannelId = channelResult.YoutubeChannelId;

            LogCollectionStarted(
                logger,
                command.ProjectId,
                channelResult.YoutubeChannelId,
                options.VideoLimit,
                null);

            var videos = await youTubeClient.GetChannelVideosAsync(
                channelResult.UploadsPlaylistId,
                options.VideoLimit,
                cancellationToken);

            var competitor = await store.FindCompetitorByYoutubeChannelIdAsync(
                command.ProjectId,
                channelResult.YoutubeChannelId,
                cancellationToken);
            var created = competitor is null;
            var collectedAt = timeProvider.GetUtcNow();

            competitor ??= new CompetitorChannel(command.ProjectId, command.YoutubeUrl, collectedAt);

            try
            {
                competitor.RecordMetadata(
                    channelResult.YoutubeChannelId,
                    channelResult.Title,
                    channelResult.Description,
                    channelResult.Handle,
                    channelResult.ThumbnailUrl,
                    channelResult.SubscriberCount,
                    channelResult.VideoCount,
                    channelResult.ViewCount,
                    channelResult.PublishedAt,
                    collectedAt);

                foreach (var video in videos)
                {
                    competitor.UpsertVideo(
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
                        collectedAt);
                }
            }
            catch (DomainException exception)
            {
                throw new ExternalServiceException(
                    "YouTube returned metadata that failed validation.",
                    ExternalServiceFailure.UnexpectedResponse,
                    exception);
            }

            if (created)
            {
                store.AddCompetitor(competitor);
            }

            await store.SaveChangesAsync(cancellationToken);
            stopwatch.Stop();

            LogCollectionCompleted(
                logger,
                competitor.Id,
                command.ProjectId,
                channelResult.YoutubeChannelId,
                videos.Count,
                stopwatch.ElapsedMilliseconds,
                null);

            return new AddCompetitorResult(CompetitorDetailsDto.FromDomain(competitor), created);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            stopwatch.Stop();
            LogCollectionCancelled(
                logger,
                command.ProjectId,
                youtubeChannelId ?? "unresolved",
                stopwatch.ElapsedMilliseconds,
                null);
            throw;
        }
        catch (Exception exception)
        {
            stopwatch.Stop();
            var failure = exception is ExternalServiceException externalServiceException
                ? externalServiceException.Failure.ToString()
                : exception.GetType().Name;
            LogCollectionFailed(
                logger,
                command.ProjectId,
                youtubeChannelId ?? "unresolved",
                failure,
                stopwatch.ElapsedMilliseconds,
                exception);
            throw;
        }
    }
}
