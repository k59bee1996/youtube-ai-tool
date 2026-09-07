using System.Net;
using System.Text;
using Microsoft.Extensions.Options;
using YoutubeAiFactory.Application.Common;
using YoutubeAiFactory.Application.Competitors;
using YoutubeAiFactory.Infrastructure.YouTube;

namespace YoutubeAiFactory.IntegrationTests;

public sealed class YouTubeClientTests
{
    [Fact]
    public async Task Client_maps_channel_and_video_responses_without_putting_key_in_url()
    {
        var handler = new QueueHttpMessageHandler(
            JsonResponse(ChannelJson),
            JsonResponse(PlaylistJson),
            JsonResponse(VideosJson));
        var client = CreateClient(handler);

        var channel = await client.GetChannelAsync(
            new YouTubeChannelReference(YouTubeChannelReferenceKind.Handle, "practicalcreator"),
            CancellationToken.None);
        var videos = await client.GetChannelVideosAsync(
            channel.UploadsPlaylistId,
            30,
            CancellationToken.None);

        Assert.Equal("UC1234567890abcdefghij12", channel.YoutubeChannelId);
        Assert.Equal("@practicalcreator", channel.Handle);
        Assert.Equal(125_000, channel.SubscriberCount);
        var video = Assert.Single(videos);
        Assert.Equal(TimeSpan.FromMinutes(12), video.Duration);
        Assert.Equal(42_000, video.ViewCount);
        Assert.All(handler.Requests, request =>
        {
            Assert.DoesNotContain("test-api-key", request.Uri, StringComparison.Ordinal);
            Assert.Equal("test-api-key", request.ApiKey);
        });
        Assert.Contains("forHandle=%40practicalcreator", handler.Requests[0].Uri, StringComparison.Ordinal);
        Assert.Contains("maxResults=30", handler.Requests[1].Uri, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Client_maps_authentication_failure_without_retrying()
    {
        var handler = new QueueHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.Forbidden)
        {
            Content = new StringContent("{\"error\":{\"message\":\"API key invalid\"}}"),
        });
        var client = CreateClient(handler);

        var exception = await Assert.ThrowsAsync<ExternalServiceException>(() => client.GetChannelAsync(
            new YouTubeChannelReference(YouTubeChannelReferenceKind.Handle, "practicalcreator"),
            CancellationToken.None));

        Assert.Equal(ExternalServiceFailure.Authentication, exception.Failure);
        Assert.Single(handler.Requests);
    }

    [Fact]
    public async Task Client_returns_not_found_when_channel_lookup_is_empty()
    {
        var handler = new QueueHttpMessageHandler(JsonResponse("{\"items\":[]}"));
        var client = CreateClient(handler);

        await Assert.ThrowsAsync<ResourceNotFoundException>(() => client.GetChannelAsync(
            new YouTubeChannelReference(YouTubeChannelReferenceKind.Handle, "missingchannel"),
            CancellationToken.None));

        Assert.Single(handler.Requests);
    }

    [Fact]
    public async Task Client_fails_before_http_when_api_key_is_missing()
    {
        var handler = new QueueHttpMessageHandler();
        var client = CreateClient(handler, string.Empty);

        var exception = await Assert.ThrowsAsync<ExternalServiceException>(() => client.GetChannelAsync(
            new YouTubeChannelReference(YouTubeChannelReferenceKind.Handle, "practicalcreator"),
            CancellationToken.None));

        Assert.Equal(ExternalServiceFailure.Configuration, exception.Failure);
        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task Client_does_not_retry_quota_exhaustion()
    {
        var handler = new QueueHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.Forbidden)
        {
            Content = new StringContent(
                "{\"error\":{\"errors\":[{\"reason\":\"quotaExceeded\"}]}}"),
        });
        var client = CreateClient(handler);

        var exception = await Assert.ThrowsAsync<ExternalServiceException>(() => client.GetChannelAsync(
            new YouTubeChannelReference(YouTubeChannelReferenceKind.Handle, "practicalcreator"),
            CancellationToken.None));

        Assert.Equal(ExternalServiceFailure.QuotaExceeded, exception.Failure);
        Assert.Single(handler.Requests);
    }

    [Theory]
    [InlineData(HttpStatusCode.RequestTimeout, null)]
    [InlineData(HttpStatusCode.TooManyRequests, null)]
    [InlineData(HttpStatusCode.InternalServerError, null)]
    [InlineData(HttpStatusCode.Forbidden, "rateLimitExceeded")]
    public async Task Client_retries_transient_provider_failures(
        HttpStatusCode statusCode,
        string? reason)
    {
        var errorJson = reason is null
            ? "{\"error\":{}}"
            : $"{{\"error\":{{\"errors\":[{{\"reason\":\"{reason}\"}}]}}}}";
        var handler = new QueueHttpMessageHandler(
            new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(errorJson),
            },
            JsonResponse(ChannelJson));
        var client = CreateClient(handler);

        var result = await client.GetChannelAsync(
            new YouTubeChannelReference(YouTubeChannelReferenceKind.Handle, "practicalcreator"),
            CancellationToken.None);

        Assert.Equal("UC1234567890abcdefghij12", result.YoutubeChannelId);
        Assert.Equal(2, handler.Requests.Count);
    }

    [Fact]
    public async Task Client_stops_after_bounded_transient_retries()
    {
        static HttpResponseMessage ServerError() => new(HttpStatusCode.ServiceUnavailable)
        {
            Content = new StringContent("{\"error\":{}}"),
        };

        var handler = new QueueHttpMessageHandler(ServerError(), ServerError(), ServerError());
        var client = CreateClient(handler);

        var exception = await Assert.ThrowsAsync<ExternalServiceException>(() => client.GetChannelAsync(
            new YouTubeChannelReference(YouTubeChannelReferenceKind.Handle, "practicalcreator"),
            CancellationToken.None));

        Assert.Equal(ExternalServiceFailure.Transient, exception.Failure);
        Assert.Equal(3, handler.Requests.Count);
    }

    [Fact]
    public async Task Client_maps_timeouts_to_transient_failure_after_bounded_retries()
    {
        var handler = new TimeoutHttpMessageHandler();
        var client = CreateClient(handler);

        var exception = await Assert.ThrowsAsync<ExternalServiceException>(() => client.GetChannelAsync(
            new YouTubeChannelReference(YouTubeChannelReferenceKind.Handle, "practicalcreator"),
            CancellationToken.None));

        Assert.Equal(ExternalServiceFailure.Transient, exception.Failure);
        Assert.Equal(3, handler.RequestCount);
    }

    [Fact]
    public async Task Client_timeout_covers_response_content_after_headers()
    {
        var handler = new QueueHttpMessageHandler(
            StallingResponse(),
            StallingResponse(),
            StallingResponse());
        var client = CreateClient(handler, timeout: TimeSpan.FromMilliseconds(50));

        var exception = await Assert.ThrowsAsync<ExternalServiceException>(() => client.GetChannelAsync(
            new YouTubeChannelReference(YouTubeChannelReferenceKind.Handle, "practicalcreator"),
            CancellationToken.None));

        Assert.Equal(ExternalServiceFailure.Transient, exception.Failure);
        Assert.Equal(3, handler.Requests.Count);
    }

    [Fact]
    public async Task Client_propagates_caller_cancellation_without_retrying()
    {
        var handler = new CancelableHttpMessageHandler();
        var client = CreateClient(handler);
        using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => client.GetChannelAsync(
            new YouTubeChannelReference(YouTubeChannelReferenceKind.Handle, "practicalcreator"),
            cancellation.Token));

        Assert.Equal(1, handler.RequestCount);
    }

    [Theory]
    [InlineData("{\"items\":null}")]
    [InlineData("{\"items\":[null]}")]
    [InlineData("{\"items\":[{\"id\":\"UC1234567890abcdefghij12\",\"snippet\":null,\"contentDetails\":{\"relatedPlaylists\":{\"uploads\":\"UU1234567890abcdefghij12\"}}}]}")]
    [InlineData("{\"items\":[{\"id\":\"UC1234567890abcdefghij12\",\"snippet\":{\"title\":\"Example\"},\"contentDetails\":null}]}")]
    [InlineData("{\"items\":[{\"id\":\"one\",\"snippet\":{\"title\":\"One\"},\"contentDetails\":{\"relatedPlaylists\":{\"uploads\":\"uploads-one\"}}},{\"id\":\"two\",\"snippet\":{\"title\":\"Two\"},\"contentDetails\":{\"relatedPlaylists\":{\"uploads\":\"uploads-two\"}}}]}")]
    public async Task Client_maps_malformed_channel_shapes_to_unexpected_response(string json)
    {
        var handler = new QueueHttpMessageHandler(JsonResponse(json));
        var client = CreateClient(handler);

        var exception = await Assert.ThrowsAsync<ExternalServiceException>(() => client.GetChannelAsync(
            new YouTubeChannelReference(YouTubeChannelReferenceKind.Handle, "practicalcreator"),
            CancellationToken.None));

        Assert.Equal(ExternalServiceFailure.UnexpectedResponse, exception.Failure);
        Assert.Single(handler.Requests);
    }

    [Fact]
    public async Task Client_handles_missing_optional_channel_and_video_objects()
    {
        const string channelJson = """
            {
              "items": [{
                "id": "UC1234567890abcdefghij12",
                "snippet": {
                  "title": "Minimal Channel",
                  "thumbnails": null
                },
                "statistics": null,
                "contentDetails": {
                  "relatedPlaylists": { "uploads": "UU1234567890abcdefghij12" }
                }
              }]
            }
            """;
        const string videosJson = """
            {
              "items": [{
                "id": "a1b2c3d4e5F",
                "snippet": {
                  "title": "Minimal Video",
                  "thumbnails": null
                },
                "contentDetails": null,
                "statistics": null
              }]
            }
            """;
        var handler = new QueueHttpMessageHandler(
            JsonResponse(channelJson),
            JsonResponse(PlaylistJson),
            JsonResponse(videosJson));
        var client = CreateClient(handler);

        var channel = await client.GetChannelAsync(
            new YouTubeChannelReference(YouTubeChannelReferenceKind.Handle, "minimal"),
            CancellationToken.None);
        var videos = await client.GetChannelVideosAsync(
            channel.UploadsPlaylistId,
            10,
            CancellationToken.None);

        Assert.Null(channel.SubscriberCount);
        Assert.Null(channel.VideoCount);
        Assert.Null(channel.ViewCount);
        Assert.Null(channel.ThumbnailUrl);
        var video = Assert.Single(videos);
        Assert.Null(video.Duration);
        Assert.Null(video.ViewCount);
        Assert.Null(video.LikeCount);
        Assert.Null(video.CommentCount);
        Assert.Null(video.ThumbnailUrl);
    }

    [Fact]
    public async Task Client_returns_empty_collection_for_channel_with_no_videos()
    {
        var handler = new QueueHttpMessageHandler(JsonResponse("{\"items\":[]}"));
        var client = CreateClient(handler);

        var videos = await client.GetChannelVideosAsync(
            "UU1234567890abcdefghij12",
            30,
            CancellationToken.None);

        Assert.Empty(videos);
        Assert.Single(handler.Requests);
    }

    [Theory]
    [InlineData("{\"items\":null}")]
    [InlineData("{\"items\":[null]}")]
    [InlineData("{\"items\":[{\"contentDetails\":null}]}")]
    public async Task Client_maps_malformed_playlist_shapes_to_unexpected_response(string json)
    {
        var handler = new QueueHttpMessageHandler(JsonResponse(json));
        var client = CreateClient(handler);

        var exception = await Assert.ThrowsAsync<ExternalServiceException>(() =>
            client.GetChannelVideosAsync(
                "UU1234567890abcdefghij12",
                30,
                CancellationToken.None));

        Assert.Equal(ExternalServiceFailure.UnexpectedResponse, exception.Failure);
        Assert.Single(handler.Requests);
    }

    [Theory]
    [InlineData("{\"items\":null}")]
    [InlineData("{\"items\":[null]}")]
    [InlineData("{\"items\":[{\"id\":\"a1b2c3d4e5F\",\"snippet\":null}]}")]
    public async Task Client_maps_malformed_requested_video_shapes_to_unexpected_response(
        string videosJson)
    {
        var handler = new QueueHttpMessageHandler(
            JsonResponse(PlaylistJson),
            JsonResponse(videosJson));
        var client = CreateClient(handler);

        var exception = await Assert.ThrowsAsync<ExternalServiceException>(() =>
            client.GetChannelVideosAsync(
                "UU1234567890abcdefghij12",
                30,
                CancellationToken.None));

        Assert.Equal(ExternalServiceFailure.UnexpectedResponse, exception.Failure);
        Assert.Equal(2, handler.Requests.Count);
    }

    [Fact]
    public async Task Client_returns_only_unique_requested_videos_in_playlist_order_and_within_limit()
    {
        const string playlistJson = """
            {
              "items": [
                { "contentDetails": { "videoId": "firstVideo1" } },
                { "contentDetails": { "videoId": "secondVideo" } },
                { "contentDetails": { "videoId": "thirdVideo3" } }
              ]
            }
            """;
        const string videosJson = """
            {
              "items": [
                { "id": "unrequested", "snippet": { "title": "Wrong channel" } },
                { "id": "secondVideo", "snippet": { "title": "Second" } },
                { "id": "firstVideo1", "snippet": { "title": "First" } },
                { "id": "firstVideo1", "snippet": { "title": "Duplicate" } },
                { "id": "thirdVideo3", "snippet": { "title": "Beyond limit" } }
              ]
            }
            """;
        var handler = new QueueHttpMessageHandler(
            JsonResponse(playlistJson),
            JsonResponse(videosJson));
        var client = CreateClient(handler);

        var videos = await client.GetChannelVideosAsync(
            "UU1234567890abcdefghij12",
            2,
            CancellationToken.None);

        Assert.Collection(
            videos,
            video => Assert.Equal("firstVideo1", video.YoutubeVideoId),
            video => Assert.Equal("secondVideo", video.YoutubeVideoId));
        Assert.DoesNotContain("thirdVideo3", handler.Requests[1].Uri, StringComparison.Ordinal);
        Assert.Contains("maxResults=2", handler.Requests[1].Uri, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(51)]
    public async Task Client_rejects_video_limits_outside_supported_range(int limit)
    {
        var handler = new QueueHttpMessageHandler();
        var client = CreateClient(handler);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            client.GetChannelVideosAsync(
                "UU1234567890abcdefghij12",
                limit,
                CancellationToken.None));

        Assert.Empty(handler.Requests);
    }

    private static YouTubeClient CreateClient(
        HttpMessageHandler handler,
        string apiKey = "test-api-key",
        TimeSpan? timeout = null) => new(
            new HttpClient(handler)
            {
                BaseAddress = new Uri("https://www.googleapis.com/youtube/v3/"),
                Timeout = timeout ?? TimeSpan.FromSeconds(30),
            },
            Options.Create(new YouTubeOptions { ApiKey = apiKey }));

    private static HttpResponseMessage JsonResponse(string json) => new(HttpStatusCode.OK)
    {
        Content = new StringContent(json, Encoding.UTF8, "application/json"),
    };

    private static HttpResponseMessage StallingResponse() => new(HttpStatusCode.OK)
    {
        Content = new StallingHttpContent(),
    };

    private sealed class QueueHttpMessageHandler(params HttpResponseMessage[] responses)
        : HttpMessageHandler
    {
        private readonly Queue<HttpResponseMessage> _responses = new(responses);

        public List<CapturedRequest> Requests { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var apiKey = request.Headers.TryGetValues("X-Goog-Api-Key", out var values)
                ? values.SingleOrDefault()
                : null;
            Requests.Add(new CapturedRequest(
                request.RequestUri?.AbsoluteUri ?? string.Empty,
                apiKey));
            return Task.FromResult(_responses.Dequeue());
        }
    }

    private sealed record CapturedRequest(string Uri, string? ApiKey);

    private sealed class TimeoutHttpMessageHandler : HttpMessageHandler
    {
        public int RequestCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestCount++;
            return Task.FromException<HttpResponseMessage>(new TaskCanceledException("Simulated timeout."));
        }
    }

    private sealed class CancelableHttpMessageHandler : HttpMessageHandler
    {
        public int RequestCount { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestCount++;
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            throw new InvalidOperationException("Cancellation should end the request.");
        }
    }

    private sealed class StallingHttpContent : HttpContent
    {
        protected override Task SerializeToStreamAsync(
            Stream stream,
            TransportContext? context) => Task.CompletedTask;

        protected override Task SerializeToStreamAsync(
            Stream stream,
            TransportContext? context,
            CancellationToken cancellationToken) =>
            Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);

        protected override bool TryComputeLength(out long length)
        {
            length = 0;
            return false;
        }
    }

    private const string ChannelJson = """
        {
          "items": [{
            "id": "UC1234567890abcdefghij12",
            "snippet": {
              "title": "Practical Creator",
              "description": "Evidence-based creator education.",
              "customUrl": "@practicalcreator",
              "publishedAt": "2020-04-03T00:00:00Z",
              "thumbnails": { "high": { "url": "https://example.test/channel.jpg" } }
            },
            "statistics": {
              "viewCount": "14000000",
              "subscriberCount": "125000",
              "hiddenSubscriberCount": false,
              "videoCount": "240"
            },
            "contentDetails": {
              "relatedPlaylists": { "uploads": "UU1234567890abcdefghij12" }
            }
          }]
        }
        """;

    private const string PlaylistJson = """
        {
          "items": [{ "contentDetails": { "videoId": "a1b2c3d4e5F" } }]
        }
        """;

    private const string VideosJson = """
        {
          "items": [{
            "id": "a1b2c3d4e5F",
            "snippet": {
              "title": "How to Research a Video",
              "description": "A repeatable research process.",
              "publishedAt": "2026-07-01T00:00:00Z",
              "thumbnails": { "high": { "url": "https://example.test/video.jpg" } }
            },
            "contentDetails": { "duration": "PT12M" },
            "statistics": { "viewCount": "42000", "likeCount": "1900", "commentCount": "143" }
          }]
        }
        """;
}
