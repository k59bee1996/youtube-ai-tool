using System.Text;
using System.Text.RegularExpressions;
using YoutubeAiFactory.Domain.Research;

namespace YoutubeAiFactory.Application.Research;

/// <summary>Calculates dataset support states from persisted relationships; model assertions never set these states.</summary>
public sealed class ResearchSupportAnalyzer
{
    public static ResearchClaimSupportStatus GetStatus(IEnumerable<ResearchClaimEvidence> links,
        IReadOnlyDictionary<Guid, ResearchEvidence> evidence, IReadOnlyDictionary<Guid, ResearchSource> sources)
    {
        var materialized = links.ToArray();
        if (materialized.Any(link => link.Stance == ResearchEvidenceStance.Contradict)) return ResearchClaimSupportStatus.Conflicted;
        var supporting = materialized.Where(link => link.Stance == ResearchEvidenceStance.Support)
            .Select(link => evidence.TryGetValue(link.ResearchEvidenceId, out var item) ? item : null)
            .Where(item => item is not null).Cast<ResearchEvidence>().ToArray();
        if (supporting.Length == 0) return ResearchClaimSupportStatus.Unsupported;
        var independent = supporting.Select(item => sources.TryGetValue(item.ResearchSourceId, out var source) ? source : null)
            .Where(source => source is not null).Cast<ResearchSource>()
            .Select(source => $"{source.Domain.ToUpperInvariant()}|{source.ContentHash ?? source.CanonicalUrl}")
            .Distinct(StringComparer.Ordinal).Count();
        return independent >= 2 ? ResearchClaimSupportStatus.Corroborated : ResearchClaimSupportStatus.Supported;
    }

    public static string NormalizeStatement(string value) => Regex.Replace(value.ToLowerInvariant(), "[^a-z0-9]+", " ").Trim();
}
