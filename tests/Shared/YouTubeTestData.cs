using YoutubeAiFactory.Application.Competitors;

namespace YoutubeAiFactory.Tests.Fixtures;

public static class YouTubeTestData
{
    public const string ChannelId = "UC1234567890abcdefghij12";
    public const string UploadsPlaylistId = "UU1234567890abcdefghij12";

    public static YouTubeChannelResult Channel(string title = "Practical Creator") => new(
        ChannelId,
        title,
        "Evidence-based creator education.",
        "@practicalcreator",
        "https://example.test/channel.jpg",
        125_000,
        240,
        14_000_000,
        new DateTimeOffset(2020, 4, 3, 0, 0, 0, TimeSpan.Zero),
        UploadsPlaylistId);

    public static IReadOnlyCollection<YouTubeVideoResult> Videos() =>
    [
        new(
            "a1b2c3d4e5F",
            "How to Research a Video",
            "A repeatable research process.",
            "https://www.youtube.com/watch?v=a1b2c3d4e5F",
            "https://example.test/video-1.jpg",
            TimeSpan.FromMinutes(12),
            42_000,
            1_900,
            143,
            new DateTimeOffset(2026, 7, 1, 0, 0, 0, TimeSpan.Zero)),
        new(
            "z9y8x7w6v5U",
            "A Better YouTube Hook",
            "Hook mechanics explained.",
            "https://www.youtube.com/watch?v=z9y8x7w6v5U",
            "https://example.test/video-2.jpg",
            TimeSpan.FromMinutes(8),
            31_000,
            1_200,
            87,
            new DateTimeOffset(2026, 6, 15, 0, 0, 0, TimeSpan.Zero)),
    ];
}

public sealed class FakeYouTubeClient : IYouTubeClient
{
    public int ChannelRequests { get; private set; }

    public int VideoRequests { get; private set; }

    public YouTubeChannelResult ChannelResult { get; set; } = YouTubeTestData.Channel();

    public IReadOnlyCollection<YouTubeVideoResult> VideoResults { get; set; } = YouTubeTestData.Videos();

    public Task<YouTubeChannelResult> GetChannelAsync(
        YouTubeChannelReference reference,
        CancellationToken cancellationToken)
    {
        ChannelRequests++;
        return Task.FromResult(ChannelResult);
    }

    public Task<IReadOnlyCollection<YouTubeVideoResult>> GetChannelVideosAsync(
        string uploadsPlaylistId,
        int limit,
        CancellationToken cancellationToken)
    {
        VideoRequests++;
        return Task.FromResult<IReadOnlyCollection<YouTubeVideoResult>>(
            VideoResults.Take(limit).ToArray());
    }
}
