using System.Text.Json;
using YoutubeAiFactory.Application.AI;
using YoutubeAiFactory.Application.Opportunities;

namespace YoutubeAiFactory.Application.Localization;

public static class OpportunityReportLocalizationPrompt
{
    public const string Key = "opportunity-report-localization";
    public const int Version = 1;
    internal static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public static LlmRequest Create(OpportunityReportWithDetails canonical, bool correcting) => new(
        Key,
        Version,
        "Translate only the reader-facing, dynamic opportunity-analysis content into Vietnamese. Return one complete JSON object matching LocalizedOpportunityReportContent. Translate each opportunity name, including the short headline displayed at the top of each opportunity card. Never use null for a string, list, or list item. Preserve the exact list order and counts. Required shape: limitations[]; opportunities[] { name, description, audience, topic, contentFormat, angle, whyThisOpportunity, evidenceSummaries[], risks[], limitations[] }. Do not include or alter IDs, scores, metrics, confidence values, statuses, enums, versions, timestamps, URLs, evidence IDs/references, model metadata, or prompt metadata. Do not rewrite, improve, rank, or add business content. The surrounding application UI remains English; this is a Vietnamese reading aid only.",
        $"Canonical opportunity report JSON:\n{JsonSerializer.Serialize(ToSource(canonical), SerializerOptions)}" +
        (correcting ? "\nYour prior response failed structural validation. Preserve every list count and order exactly." : string.Empty),
        new Dictionary<string, string> { ["max_output_tokens"] = "5000" },
        OpportunityReportLocalizationOutputSchema.Create());

    private static OpportunityReportLocalizationSource ToSource(OpportunityReportWithDetails report) => new(
        LocalizedOpportunityReportValidator.Deserialize(report.Report.LimitationsJson),
        LocalizedOpportunityReportValidator.OrderedCandidates(report).Select(candidate => new OpportunityCandidateLocalizationSource(
            candidate.Candidate.Name,
            candidate.Candidate.Description,
            candidate.Candidate.Audience,
            candidate.Candidate.Topic,
            candidate.Candidate.ContentFormat,
            candidate.Candidate.Angle,
            candidate.Candidate.WhyThisOpportunity,
            candidate.Evidence.Select(evidence => evidence.Summary).ToArray(),
            LocalizedOpportunityReportValidator.Deserialize(candidate.Candidate.RisksJson),
            LocalizedOpportunityReportValidator.Deserialize(candidate.Candidate.LimitationsJson))).ToArray());

    private sealed record OpportunityReportLocalizationSource(
        IReadOnlyList<string> Limitations,
        IReadOnlyList<OpportunityCandidateLocalizationSource> Opportunities);

    private sealed record OpportunityCandidateLocalizationSource(
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
}
