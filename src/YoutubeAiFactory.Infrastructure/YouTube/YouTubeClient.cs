using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Xml;
using Microsoft.Extensions.Options;
using YoutubeAiFactory.Application.Common;
using YoutubeAiFactory.Application.Competitors;

namespace YoutubeAiFactory.Infrastructure.YouTube;

internal sealed class YouTubeClient(
    HttpClient httpClient,
    IOptions<YouTubeOptions> options) : IYouTubeClient
{
    private const int MaximumAttempts = 3;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly string _apiKey = options.Value.ApiKey;

    public async Task<YouTubeChannelResult> GetChannelAsync(
        YouTubeChannelReference reference,
        CancellationToken cancellationToken)
    {
        EnsureConfigured();
        var filter = reference.Kind switch
        {
            YouTubeChannelReferenceKind.Handle => $"forHandle={Uri.EscapeDataString($"@{reference.Value}")}",
            YouTubeChannelReferenceKind.ChannelId => $"id={Uri.EscapeDataString(reference.Value)}",
            _ => throw new ArgumentOutOfRangeException(nameof(reference)),
        };

        var response = await GetAsync<YouTubeListResponse<YouTubeChannelItem>>(
            $"channels?part=snippet,statistics,contentDetails&{filter}&maxResults=1",
            cancellationToken);
        if (response.Items is null)
        {
            throw UnexpectedResponse("YouTube returned malformed channel metadata.");
        }

        if (response.Items.Count == 0)
        {
            throw new ResourceNotFoundException("The YouTube channel could not be found.");
        }

        if (response.Items.Count != 1 || response.Items[0] is not { } item)
        {
            throw UnexpectedResponse("YouTube returned malformed channel metadata.");
        }

        if (string.IsNullOrWhiteSpace(item.Id) ||
            item.Snippet is not { } snippet ||
            string.IsNullOrWhiteSpace(snippet.Title) ||
            item.ContentDetails?.RelatedPlaylists is not { } relatedPlaylists ||
            string.IsNullOrWhiteSpace(relatedPlaylists.Uploads))
        {
            throw UnexpectedResponse("YouTube returned incomplete channel metadata.");
        }

        var statistics = item.Statistics;

        return new YouTubeChannelResult(
            item.Id,
            snippet.Title,
            snippet.Description,
            NormalizeHandle(snippet.CustomUrl),
            SelectThumbnail(snippet.Thumbnails),
            statistics?.HiddenSubscriberCount == true
                ? null
                : ParseCount(statistics?.SubscriberCount),
            ParseCount(statistics?.VideoCount),
            ParseCount(statistics?.ViewCount),
            snippet.PublishedAt,
            relatedPlaylists.Uploads);
    }

    public async Task<IReadOnlyCollection<YouTubeVideoResult>> GetChannelVideosAsync(
        string uploadsPlaylistId,
        int limit,
        CancellationToken cancellationToken)
    {
        EnsureConfigured();
        if (string.IsNullOrWhiteSpace(uploadsPlaylistId))
        {
            throw new ArgumentException("Uploads playlist ID is required.", nameof(uploadsPlaylistId));
        }

        if (limit is < 1 or > CompetitorCollectionOptions.MaximumVideoLimit)
        {
            throw new ArgumentOutOfRangeException(
                nameof(limit),
                $"Video limit must be between 1 and {CompetitorCollectionOptions.MaximumVideoLimit}.");
        }

        var playlist = await GetAsync<YouTubeListResponse<YouTubePlaylistItem>>(
            $"playlistItems?part=contentDetails&playlistId={Uri.EscapeDataString(uploadsPlaylistId)}&maxResults={limit}",
            cancellationToken);
        if (playlist.Items is null)
        {
            throw UnexpectedResponse("YouTube returned malformed playlist metadata.");
        }

        var videoIds = new List<string>(limit);
        var seenVideoIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var item in playlist.Items.Take(limit))
        {
            if (item?.ContentDetails is not { } contentDetails ||
                string.IsNullOrWhiteSpace(contentDetails.VideoId))
            {
                throw UnexpectedResponse("YouTube returned malformed playlist metadata.");
            }

            if (seenVideoIds.Add(contentDetails.VideoId))
            {
                videoIds.Add(contentDetails.VideoId);
            }
        }

        if (videoIds.Count == 0)
        {
            return [];
        }

        var videos = await GetAsync<YouTubeListResponse<YouTubeVideoItem>>(
            $"videos?part=snippet,contentDetails,statistics&id={string.Join(',', videoIds.Select(Uri.EscapeDataString))}&maxResults={videoIds.Count}",
            cancellationToken);
        if (videos.Items is null)
        {
            throw UnexpectedResponse("YouTube returned malformed video metadata.");
        }

        var order = videoIds
            .Select((id, index) => (id, index))
            .ToDictionary(pair => pair.id, pair => pair.index, StringComparer.Ordinal);
        var returnedVideoIds = new HashSet<string>(StringComparer.Ordinal);
        var requestedVideos = new List<YouTubeVideoItem>(Math.Min(limit, videos.Items.Count));

        foreach (var item in videos.Items)
        {
            if (item is null || string.IsNullOrWhiteSpace(item.Id))
            {
                throw UnexpectedResponse("YouTube returned malformed video metadata.");
            }

            if (!order.ContainsKey(item.Id) || !returnedVideoIds.Add(item.Id))
            {
                continue;
            }

            if (item.Snippet is not { } snippet || string.IsNullOrWhiteSpace(snippet.Title))
            {
                throw UnexpectedResponse("YouTube returned incomplete video metadata.");
            }

            requestedVideos.Add(item);
        }

        return requestedVideos
            .OrderBy(item => order[item.Id!])
            .Take(limit)
            .Select(item => new YouTubeVideoResult(
                item.Id!,
                item.Snippet!.Title!,
                item.Snippet.Description,
                $"https://www.youtube.com/watch?v={Uri.EscapeDataString(item.Id!)}",
                SelectThumbnail(item.Snippet.Thumbnails),
                ParseDuration(item.ContentDetails?.Duration),
                ParseCount(item.Statistics?.ViewCount),
                ParseCount(item.Statistics?.LikeCount),
                ParseCount(item.Statistics?.CommentCount),
                item.Snippet.PublishedAt))
            .ToArray();
    }

    private async Task<T> GetAsync<T>(string relativeUri, CancellationToken cancellationToken)
    {
        for (var attempt = 1; attempt <= MaximumAttempts; attempt++)
        {
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, relativeUri);
                request.Headers.TryAddWithoutValidation("X-Goog-Api-Key", _apiKey);
                using var response = await httpClient.SendAsync(
                    request,
                    HttpCompletionOption.ResponseContentRead,
                    cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<T>(JsonOptions, cancellationToken);
                    return result ?? throw new ExternalServiceException(
                        "YouTube returned an empty response.",
                        ExternalServiceFailure.UnexpectedResponse);
                }

                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                var failure = CreateFailure(response.StatusCode, errorBody);
                if (failure.Retryable && attempt < MaximumAttempts)
                {
                    await DelayBeforeRetryAsync(attempt, cancellationToken);
                    continue;
                }

                throw failure.Exception;
            }
            catch (HttpRequestException exception)
            {
                if (attempt < MaximumAttempts)
                {
                    await DelayBeforeRetryAsync(attempt, cancellationToken);
                    continue;
                }

                throw new ExternalServiceException(
                    "YouTube is temporarily unavailable. Try again shortly.",
                    ExternalServiceFailure.Transient,
                    exception);
            }
            catch (OperationCanceledException exception) when (!cancellationToken.IsCancellationRequested)
            {
                if (attempt < MaximumAttempts)
                {
                    await DelayBeforeRetryAsync(attempt, cancellationToken);
                    continue;
                }

                throw new ExternalServiceException(
                    "YouTube timed out. Try again shortly.",
                    ExternalServiceFailure.Transient,
                    exception);
            }
            catch (JsonException exception)
            {
                throw new ExternalServiceException(
                    "YouTube returned a response that could not be read.",
                    ExternalServiceFailure.UnexpectedResponse,
                    exception);
            }
        }

        throw new ExternalServiceException(
            "YouTube is temporarily unavailable. Try again shortly.",
            ExternalServiceFailure.Transient,
            null);
    }

    private static YouTubeFailure CreateFailure(HttpStatusCode statusCode, string body)
    {
        var reasons = ReadErrorReasons(body);
        if (ContainsReason(reasons, "quotaExceeded") ||
            ContainsReason(reasons, "dailyLimitExceeded") ||
            ContainsReason(reasons, "dailyLimitExceededUnreg"))
        {
            return new YouTubeFailure(
                new ExternalServiceException(
                    "YouTube quota is exhausted. Try again later.",
                    ExternalServiceFailure.QuotaExceeded),
                Retryable: false);
        }

        if (ContainsReason(reasons, "rateLimitExceeded") ||
            ContainsReason(reasons, "rateLimitExceededUnreg") ||
            ContainsReason(reasons, "userRateLimitExceeded") ||
            ContainsReason(reasons, "userRateLimitExceededUnreg") ||
            ContainsReason(reasons, "concurrentLimitExceeded") ||
            ContainsReason(reasons, "servingLimitExceeded"))
        {
            return new YouTubeFailure(
                new ExternalServiceException(
                    "YouTube is rate limited. Try again later.",
                    ExternalServiceFailure.QuotaExceeded),
                Retryable: true);
        }

        if (statusCode == HttpStatusCode.TooManyRequests)
        {
            return new YouTubeFailure(
                new ExternalServiceException(
                    "YouTube is rate limited. Try again later.",
                    ExternalServiceFailure.QuotaExceeded),
                Retryable: true);
        }

        if (IsTransient(statusCode))
        {
            return new YouTubeFailure(
                new ExternalServiceException(
                    "YouTube is temporarily unavailable. Try again shortly.",
                    ExternalServiceFailure.Transient),
                Retryable: true);
        }

        if (statusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden ||
            ContainsReason(reasons, "authError") ||
            ContainsReason(reasons, "keyInvalid") ||
            ContainsReason(reasons, "API_KEY_INVALID") ||
            ContainsReason(reasons, "accessNotConfigured"))
        {
            return new YouTubeFailure(
                new ExternalServiceException(
                    "YouTube rejected the configured API credentials.",
                    ExternalServiceFailure.Authentication),
                Retryable: false);
        }

        return new YouTubeFailure(
            new ExternalServiceException(
                "YouTube returned an unexpected response.",
                ExternalServiceFailure.UnexpectedResponse),
            Retryable: false);
    }

    private void EnsureConfigured()
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            throw new ExternalServiceException(
                "YouTube collection is unavailable because the API key is not configured.",
                ExternalServiceFailure.Configuration);
        }
    }

    private static bool IsTransient(HttpStatusCode statusCode) =>
        statusCode == HttpStatusCode.RequestTimeout || (int)statusCode >= 500;

    private static Task DelayBeforeRetryAsync(int attempt, CancellationToken cancellationToken) =>
        Task.Delay(TimeSpan.FromMilliseconds(200 * Math.Pow(2, attempt - 1)), cancellationToken);

    private static long? ParseCount(string? value) =>
        long.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var count) && count >= 0
            ? count
            : null;

    private static TimeSpan? ParseDuration(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        try
        {
            var duration = XmlConvert.ToTimeSpan(value);
            return duration >= TimeSpan.Zero ? duration : null;
        }
        catch (FormatException)
        {
            return null;
        }
    }

    private static string? NormalizeHandle(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.StartsWith('@') ? value : $"@{value}";
    }

    private static string? SelectThumbnail(YouTubeThumbnails? thumbnails) =>
        thumbnails?.Maxres?.Url ??
        thumbnails?.High?.Url ??
        thumbnails?.Medium?.Url ??
        thumbnails?.Default?.Url;

    private static ExternalServiceException UnexpectedResponse(string message) =>
        new(message, ExternalServiceFailure.UnexpectedResponse);

    private static HashSet<string> ReadErrorReasons(string body)
    {
        var reasons = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        try
        {
            using var document = JsonDocument.Parse(body);
            CollectErrorReasons(document.RootElement, reasons);
        }
        catch (JsonException)
        {
            // The status code still provides a safe fallback classification.
        }

        return reasons;
    }

    private static void CollectErrorReasons(JsonElement element, HashSet<string> reasons)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                if ((property.NameEquals("reason") || property.NameEquals("status")) &&
                    property.Value.ValueKind == JsonValueKind.String &&
                    property.Value.GetString() is { Length: > 0 } reason)
                {
                    reasons.Add(reason);
                }

                CollectErrorReasons(property.Value, reasons);
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                CollectErrorReasons(item, reasons);
            }
        }
    }

    private static bool ContainsReason(HashSet<string> reasons, string reason) => reasons.Contains(reason);

    private sealed record YouTubeFailure(ExternalServiceException Exception, bool Retryable);

    private sealed class YouTubeListResponse<TItem>
        where TItem : class
    {
        public List<TItem?>? Items { get; init; } = [];
    }

    private sealed class YouTubeChannelItem
    {
        public string? Id { get; init; }

        public YouTubeSnippet? Snippet { get; init; }

        public YouTubeChannelStatistics? Statistics { get; init; }

        public YouTubeChannelContentDetails? ContentDetails { get; init; }
    }

    private sealed class YouTubePlaylistItem
    {
        public YouTubePlaylistContentDetails? ContentDetails { get; init; }
    }

    private sealed class YouTubeVideoItem
    {
        public string? Id { get; init; }

        public YouTubeSnippet? Snippet { get; init; }

        public YouTubeVideoContentDetails? ContentDetails { get; init; }

        public YouTubeVideoStatistics? Statistics { get; init; }
    }

    private sealed class YouTubeSnippet
    {
        public string? Title { get; init; }

        public string? Description { get; init; }

        public string? CustomUrl { get; init; }

        public DateTimeOffset? PublishedAt { get; init; }

        public YouTubeThumbnails? Thumbnails { get; init; }
    }

    private sealed class YouTubeThumbnails
    {
        public YouTubeThumbnail? Default { get; init; }

        public YouTubeThumbnail? Medium { get; init; }

        public YouTubeThumbnail? High { get; init; }

        public YouTubeThumbnail? Maxres { get; init; }
    }

    private sealed class YouTubeThumbnail
    {
        public string? Url { get; init; }
    }

    private sealed class YouTubeChannelStatistics
    {
        public string? ViewCount { get; init; }

        public string? SubscriberCount { get; init; }

        public bool HiddenSubscriberCount { get; init; }

        public string? VideoCount { get; init; }
    }

    private sealed class YouTubeVideoStatistics
    {
        public string? ViewCount { get; init; }

        public string? LikeCount { get; init; }

        public string? CommentCount { get; init; }
    }

    private sealed class YouTubeChannelContentDetails
    {
        public YouTubeRelatedPlaylists? RelatedPlaylists { get; init; }
    }

    private sealed class YouTubeRelatedPlaylists
    {
        public string? Uploads { get; init; }
    }

    private sealed class YouTubePlaylistContentDetails
    {
        public string? VideoId { get; init; }
    }

    private sealed class YouTubeVideoContentDetails
    {
        public string? Duration { get; init; }
    }
}
