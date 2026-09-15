using System.Net;

namespace YoutubeAiFactory.Application.Research;

public sealed class ResearchUrlCanonicalizer
{
    private static readonly HashSet<string> TrackingParameters = new(StringComparer.OrdinalIgnoreCase)
    {
        "utm_source", "utm_medium", "utm_campaign", "utm_term", "utm_content", "gclid", "fbclid",
    };

    public static string Canonicalize(string value)
    {
        if (value.Length > 2_048) throw new ArgumentException("Research source URLs cannot exceed 2,048 characters.", nameof(value));
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            throw new ArgumentException("Research source URLs must be absolute HTTP(S) URLs.", nameof(value));
        var builder = new UriBuilder(uri) { Fragment = string.Empty, Host = uri.IdnHost.ToLowerInvariant() };
        if ((builder.Scheme == Uri.UriSchemeHttp && builder.Port == 80) || (builder.Scheme == Uri.UriSchemeHttps && builder.Port == 443)) builder.Port = -1;
        var query = ParseQuery(uri.Query).Where(pair => !TrackingParameters.Contains(pair.Key)).ToArray();
        builder.Query = string.Join("&", query.Select(pair => $"{Uri.EscapeDataString(pair.Key)}={Uri.EscapeDataString(pair.Value)}"));
        return builder.Uri.AbsoluteUri.TrimEnd('/');
    }

    private static IEnumerable<KeyValuePair<string, string>> ParseQuery(string query)
    {
        foreach (var item in query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var split = item.Split('=', 2);
            var key = WebUtility.UrlDecode(split[0]);
            if (string.IsNullOrWhiteSpace(key)) continue;
            yield return new KeyValuePair<string, string>(key, split.Length == 2 ? WebUtility.UrlDecode(split[1]) : string.Empty);
        }
    }
}
