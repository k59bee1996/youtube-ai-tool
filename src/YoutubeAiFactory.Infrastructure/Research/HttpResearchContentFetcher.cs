using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using YoutubeAiFactory.Application.Research;
using YoutubeAiFactory.Domain.Research;

namespace YoutubeAiFactory.Infrastructure.Research;

internal sealed partial class HttpResearchContentFetcher(HttpClient client, IOptions<ResearchFetchOptions> options) : IResearchContentFetcher
{
    public async Task<ResearchContentFetchResult> FetchAsync(ResearchContentFetchRequest request, CancellationToken cancellationToken)
    {
        var settings = options.Value;
        var maxExtractedCharacters = request.MaxExtractedCharacters is { } requestedLimit
            ? Math.Min(settings.MaxExtractedCharacters, requestedLimit) : settings.MaxExtractedCharacters;
        if (settings.TimeoutSeconds is < 1 or > 120 || settings.MaxRedirects is < 0 or > 10 ||
            settings.MaxResponseBytes is < 10_000 or > 10_000_000 || settings.MaxExtractedCharacters is < 500 or > 50_000)
            return Failed(ResearchSourceFetchStatus.Failed, request.Url, "Research fetch configuration is invalid.");
        if (!Uri.TryCreate(request.Url, UriKind.Absolute, out var current)) return Failed(ResearchSourceFetchStatus.Blocked, request.Url, "Source URL is not an absolute HTTP(S) URL.");
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(settings.TimeoutSeconds));
        try
        {
            for (var redirects = 0; redirects <= settings.MaxRedirects; redirects++)
            {
                if (!await ResearchUrlSafetyPolicy.IsAllowedAsync(current, timeout.Token)) return Failed(ResearchSourceFetchStatus.Blocked, request.Url, "Source URL targets a blocked internal or non-public host.", current.AbsoluteUri);
                using var message = new HttpRequestMessage(HttpMethod.Get, current);
                message.Headers.UserAgent.Add(new ProductInfoHeaderValue("YoutubeAiFactoryResearch", "1.0"));
                using var response = await client.SendAsync(message, HttpCompletionOption.ResponseHeadersRead, timeout.Token);
                if (IsRedirect(response.StatusCode))
                {
                    if (response.Headers.Location is null || redirects == settings.MaxRedirects) return Failed(ResearchSourceFetchStatus.Failed, request.Url, "Source exceeded the redirect limit.", current.AbsoluteUri);
                    current = response.Headers.Location.IsAbsoluteUri ? response.Headers.Location : new Uri(current, response.Headers.Location);
                    continue;
                }
                if (!response.IsSuccessStatusCode) return Failed(ResearchSourceFetchStatus.Failed, request.Url, $"Source returned HTTP {(int)response.StatusCode}.", current.AbsoluteUri);
                var mediaType = response.Content.Headers.ContentType?.MediaType;
                if (!IsHtml(mediaType)) return Failed(ResearchSourceFetchStatus.Unsupported, request.Url, "Source did not return supported HTML content.", current.AbsoluteUri, mediaType);
                var bytes = await ReadBoundedAsync(response, settings.MaxResponseBytes, timeout.Token);
                if (bytes is null) return Failed(ResearchSourceFetchStatus.Failed, request.Url, "Source response exceeded the configured size limit.", current.AbsoluteUri, mediaType);
                var html = Encoding.UTF8.GetString(bytes);
                var title = ExtractTitle(html);
                var text = NormalizeHtml(html, maxExtractedCharacters);
                return new ResearchContentFetchResult(string.IsNullOrWhiteSpace(text) ? ResearchSourceFetchStatus.Empty : ResearchSourceFetchStatus.Fetched,
                    request.Url, current.AbsoluteUri, mediaType, title, text, string.IsNullOrWhiteSpace(text) ? "Source did not contain usable readable text." : null, bytes.Length);
            }
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return Failed(ResearchSourceFetchStatus.Failed, request.Url, "Source request timed out.");
        }
        catch (HttpRequestException)
        {
            return Failed(ResearchSourceFetchStatus.Failed, request.Url, "Source could not be retrieved.");
        }
        return Failed(ResearchSourceFetchStatus.Failed, request.Url, "Source could not be retrieved.");
    }

    private static async Task<byte[]?> ReadBoundedAsync(HttpResponseMessage response, int maxBytes, CancellationToken cancellationToken)
    {
        if (response.Content.Headers.ContentLength is > 0 and var length && length > maxBytes) return null;
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        await using var buffer = new MemoryStream();
        var bytes = new byte[81920];
        int read;
        while ((read = await stream.ReadAsync(bytes, cancellationToken)) > 0)
        {
            if (buffer.Length + read > maxBytes) return null;
            await buffer.WriteAsync(bytes.AsMemory(0, read), cancellationToken);
        }
        return buffer.ToArray();
    }

    private static bool IsRedirect(HttpStatusCode status) => status is HttpStatusCode.Moved or HttpStatusCode.Redirect or HttpStatusCode.RedirectMethod or HttpStatusCode.TemporaryRedirect or HttpStatusCode.PermanentRedirect;
    private static bool IsHtml(string? mediaType) => mediaType is not null && (mediaType.Equals("text/html", StringComparison.OrdinalIgnoreCase) || mediaType.Equals("application/xhtml+xml", StringComparison.OrdinalIgnoreCase));
    private static ResearchContentFetchResult Failed(ResearchSourceFetchStatus status, string requestedUrl, string reason, string? finalUrl = null, string? contentType = null) =>
        new(status, requestedUrl, finalUrl, contentType, null, null, reason, 0);

    private static string? ExtractTitle(string html)
    {
        var match = TitleRegex().Match(html);
        return match.Success ? WebUtility.HtmlDecode(TagRegex().Replace(match.Groups[1].Value, " ")).Trim()[..Math.Min(1_000, WebUtility.HtmlDecode(TagRegex().Replace(match.Groups[1].Value, " ")).Trim().Length)] : null;
    }

    private static string NormalizeHtml(string html, int maxCharacters)
    {
        var withoutNoise = NoiseRegex().Replace(CommentRegex().Replace(html, " "), " ");
        var decoded = WebUtility.HtmlDecode(TagRegex().Replace(withoutNoise, " "));
        var text = WhitespaceRegex().Replace(decoded, " ").Trim();
        return text.Length <= maxCharacters ? text : text[..maxCharacters];
    }

    [GeneratedRegex("(?is)<title[^>]*>(.*?)</title>", RegexOptions.CultureInvariant)] private static partial Regex TitleRegex();
    [GeneratedRegex("(?is)<(script|style|noscript|nav|footer|header|aside)[^>]*>.*?</\\1>", RegexOptions.CultureInvariant)] private static partial Regex NoiseRegex();
    [GeneratedRegex("(?is)<!--.*?-->", RegexOptions.CultureInvariant)] private static partial Regex CommentRegex();
    [GeneratedRegex("(?is)<[^>]+>", RegexOptions.CultureInvariant)] private static partial Regex TagRegex();
    [GeneratedRegex("\\s+", RegexOptions.CultureInvariant)] private static partial Regex WhitespaceRegex();
}
