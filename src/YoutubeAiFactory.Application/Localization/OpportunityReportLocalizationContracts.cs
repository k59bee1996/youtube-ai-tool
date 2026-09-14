using System.Text.Json;
using YoutubeAiFactory.Application.Common;
using YoutubeAiFactory.Application.Opportunities;

namespace YoutubeAiFactory.Application.Localization;

// These records deliberately omit scores, decisions, IDs, and evidence references.
// They are a read-only Vietnamese explanation layer over the canonical report.
public sealed record LocalizedOpportunityReportContent(
    IReadOnlyList<string> Limitations,
    IReadOnlyList<LocalizedOpportunityCandidate> Opportunities);

public sealed record LocalizedOpportunityCandidate(
    string Name,
    string Description,
    string Audience,
    string Topic,
    string ContentFormat,
    string Angle,
    string WhyThisOpportunity,
    IReadOnlyList<string> EvidenceSummaries,
    IReadOnlyList<string> Risks,
    IReadOnlyList<string> Limitations);

public static class LocalizedOpportunityReportValidator
{
    public static void Validate(OpportunityReportWithDetails source, LocalizedOpportunityReportContent localized)
    {
        var sourceLimitations = Deserialize(source.Report.LimitationsJson);
        if (localized.Limitations is null || localized.Opportunities is null || localized.Limitations.Any(string.IsNullOrWhiteSpace) || localized.Opportunities.Any(item => item is null))
            throw new StructuredOutputException("Localized opportunity report is missing required reader-facing content.");
        var candidates = OrderedCandidates(source);
        if (localized.Limitations.Count != sourceLimitations.Count || localized.Opportunities.Count != candidates.Count)
            throw new StructuredOutputException("Localized opportunity report must preserve the canonical artifact structure.");

        for (var index = 0; index < candidates.Count; index++)
        {
            var canonical = candidates[index];
            var translated = localized.Opportunities[index];
            if (string.IsNullOrWhiteSpace(translated.Name) || string.IsNullOrWhiteSpace(translated.Description) ||
                string.IsNullOrWhiteSpace(translated.Audience) || string.IsNullOrWhiteSpace(translated.Topic) ||
                string.IsNullOrWhiteSpace(translated.ContentFormat) || string.IsNullOrWhiteSpace(translated.Angle) ||
                string.IsNullOrWhiteSpace(translated.WhyThisOpportunity) || translated.EvidenceSummaries is null ||
                translated.Risks is null || translated.Limitations is null || translated.EvidenceSummaries.Any(string.IsNullOrWhiteSpace) ||
                translated.Risks.Any(string.IsNullOrWhiteSpace) || translated.Limitations.Any(string.IsNullOrWhiteSpace))
                throw new StructuredOutputException("Localized opportunity report contains incomplete reader-facing content.");
            if (translated.EvidenceSummaries.Count != canonical.Evidence.Count ||
                translated.Risks.Count != Deserialize(canonical.Candidate.RisksJson).Count ||
                translated.Limitations.Count != Deserialize(canonical.Candidate.LimitationsJson).Count)
                throw new StructuredOutputException("Localized opportunity report must preserve all reader-facing list entries.");
        }
    }

    internal static IReadOnlyList<string> Deserialize(string json) =>
        JsonSerializer.Deserialize<string[]>(json, OpportunityReportLocalizationPrompt.SerializerOptions) ?? [];

    internal static IReadOnlyList<OpportunityCandidateWithEvidence> OrderedCandidates(OpportunityReportWithDetails report) =>
        report.Candidates.OrderByDescending(item => item.Candidate.OverallScore).ToArray();
}
