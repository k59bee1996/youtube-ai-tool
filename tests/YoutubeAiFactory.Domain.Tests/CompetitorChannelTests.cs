using YoutubeAiFactory.Domain.Common;
using YoutubeAiFactory.Domain.Competitors;

namespace YoutubeAiFactory.Domain.Tests;

public sealed class CompetitorChannelTests
{
    [Fact]
    public void UpsertVideo_updates_metadata_without_adding_a_duplicate()
    {
        var now = new DateTimeOffset(2026, 9, 6, 0, 0, 0, TimeSpan.Zero);
        var channel = new CompetitorChannel(
            Guid.NewGuid(),
            "https://youtube.com/@example",
            now);

        channel.UpsertVideo(
            "a1b2c3d4e5F",
            "Original title",
            null,
            "https://youtube.com/watch?v=a1b2c3d4e5F",
            null,
            null,
            10,
            null,
            null,
            now,
            now);
        channel.UpsertVideo(
            "a1b2c3d4e5F",
            "Updated title",
            "Description",
            "https://youtube.com/watch?v=a1b2c3d4e5F",
            "https://example.test/thumb.jpg",
            TimeSpan.FromMinutes(5),
            20,
            2,
            1,
            now,
            now.AddMinutes(1));

        var video = Assert.Single(channel.Videos);
        Assert.Equal("Updated title", video.Title);
        Assert.Equal(20, video.ViewCount);
        Assert.Equal(now.AddMinutes(1), video.CollectedAt);
    }

    [Fact]
    public void RecordMetadata_rejects_negative_metrics()
    {
        var channel = new CompetitorChannel(
            Guid.NewGuid(),
            "https://youtube.com/@example",
            DateTimeOffset.UtcNow);

        Assert.Throws<DomainException>(() => channel.RecordMetadata(
            "UC1234567890abcdefghij12",
            "Example",
            null,
            "@example",
            null,
            subscriberCount: -1,
            videoCount: null,
            viewCount: null,
            publishedAt: null,
            collectedAt: DateTimeOffset.UtcNow));
    }

    [Fact]
    public void RecordMetadata_cannot_change_a_resolved_channel_identity()
    {
        var now = new DateTimeOffset(2026, 9, 6, 0, 0, 0, TimeSpan.Zero);
        var channel = new CompetitorChannel(
            Guid.NewGuid(),
            "https://youtube.com/@example",
            now);
        channel.RecordMetadata(
            "UC1234567890abcdefghij12",
            "Example",
            null,
            "@example",
            null,
            null,
            null,
            null,
            null,
            now);

        var exception = Assert.Throws<DomainException>(() => channel.RecordMetadata(
            "UCabcdefghijklmnopqrstuv",
            "Another channel",
            null,
            "@another",
            null,
            null,
            null,
            null,
            null,
            now.AddMinutes(1)));

        Assert.Contains("identity", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("UC1234567890abcdefghij12", channel.YoutubeChannelId);
        Assert.Equal("Example", channel.Title);
    }
}
