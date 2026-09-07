using System.Text.RegularExpressions;
using YoutubeAiFactory.Application.Common;

namespace YoutubeAiFactory.Application.Competitors;

public sealed partial class YouTubeChannelReferenceParser
{
    private const int MaximumUrlLength = 2_048;

    private static readonly HashSet<string> SupportedHosts = new(StringComparer.OrdinalIgnoreCase)
    {
        "youtube.com",
        "www.youtube.com",
        "m.youtube.com",
    };

    public static YouTubeChannelReference Parse(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            throw new ApplicationValidationException("Enter a valid absolute YouTube channel URL.");
        }

        var normalizedInput = input.Trim();
        if (normalizedInput.Length > MaximumUrlLength)
        {
            throw new ApplicationValidationException(
                $"The YouTube channel URL must be {MaximumUrlLength} characters or fewer.");
        }

        if (!Uri.TryCreate(normalizedInput, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            throw new ApplicationValidationException("Enter a valid absolute YouTube channel URL.");
        }

        if (!SupportedHosts.Contains(uri.Host))
        {
            throw new ApplicationValidationException("The URL must use the youtube.com domain.");
        }

        var segments = uri.AbsolutePath
            .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (segments.Length == 1 && segments[0].StartsWith('@'))
        {
            var handle = Uri.UnescapeDataString(segments[0][1..]);
            if (HandlePattern().IsMatch(handle))
            {
                return new YouTubeChannelReference(YouTubeChannelReferenceKind.Handle, handle);
            }

            throw new ApplicationValidationException("The YouTube handle is malformed.");
        }

        if (segments is ["channel", var channelId] && ChannelIdPattern().IsMatch(channelId))
        {
            return new YouTubeChannelReference(YouTubeChannelReferenceKind.ChannelId, channelId);
        }

        throw new ApplicationValidationException(
            "Supported YouTube URLs are youtube.com/@handle and youtube.com/channel/{channelId}.");
    }

    [GeneratedRegex(
        @"\A(?=.{1,30}\z)[\p{L}\p{N}](?:[\p{L}\p{M}\p{N}._·-]*[\p{L}\p{M}\p{N}])?\z",
        RegexOptions.CultureInvariant)]
    private static partial Regex HandlePattern();

    [GeneratedRegex(@"\AUC[A-Za-z0-9_-]{22}\z", RegexOptions.CultureInvariant)]
    private static partial Regex ChannelIdPattern();
}
