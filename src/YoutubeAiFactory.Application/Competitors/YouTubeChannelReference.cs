namespace YoutubeAiFactory.Application.Competitors;

public enum YouTubeChannelReferenceKind
{
    Handle,
    ChannelId,
}

public sealed record YouTubeChannelReference(
    YouTubeChannelReferenceKind Kind,
    string Value);
