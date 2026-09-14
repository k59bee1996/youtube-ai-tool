using YoutubeAiFactory.Application.Competitors;
using YoutubeAiFactory.Application.Common;

namespace YoutubeAiFactory.Application.Localization;

// These records contain only dynamic, reader-facing fields. Scores, IDs, evidence references, titles, templates, and metadata remain canonical.
public sealed record LocalizedCompetitorAnalysisContent(
    LocalizedAudience Audience, IReadOnlyList<LocalizedTopicCluster> TopicClusters, IReadOnlyList<LocalizedTitlePattern> TitlePatterns,
    IReadOnlyList<LocalizedEvidencePattern> ThumbnailPatterns, IReadOnlyList<LocalizedEvidencePattern> HookPatterns,
    IReadOnlyList<LocalizedContentFormat> ContentFormats, IReadOnlyList<LocalizedPerformanceInsight> PerformanceInsights, IReadOnlyList<LocalizedWeaknessInsight> PotentialWeaknesses,
    IReadOnlyList<LocalizedTransferableFormat> TransferableFormats, IReadOnlyList<LocalizedEvidenceNote> EvidenceNotes, LocalizedConfidence Confidence);
public sealed record LocalizedAudience(string? LikelyAgeRange, IReadOnlyList<string> LikelyInterests, IReadOnlyList<string> LikelyViewerIntent, IReadOnlyList<string> GeographyHints, IReadOnlyList<string> Evidence);
public sealed record LocalizedTopicCluster(string Name, string Description, string PerformanceSignal);
public sealed record LocalizedTitlePattern(string PatternName, string Description, string PerformanceSignal);
public sealed record LocalizedEvidencePattern(string PatternName, string Observation, IReadOnlyList<string> Limitations);
public sealed record LocalizedContentFormat(string Format, string PerformanceSignal);
public sealed record LocalizedPerformanceInsight(string Insight);
public sealed record LocalizedWeaknessInsight(string Observation);
public sealed record LocalizedTransferableFormat(string Format, string WhyItMayWork, string TransferableMechanic, string DoNotCopy);
public sealed record LocalizedEvidenceNote(string Note);
public sealed record LocalizedConfidence(string DataQuality, IReadOnlyList<string> Limitations);

public static class LocalizedCompetitorAnalysisValidator
{
    public static void Validate(CompetitorAnalysisResult source, LocalizedCompetitorAnalysisContent localized)
    {
        if (localized.Audience is null || localized.Confidence is null || localized.TopicClusters is null || localized.TitlePatterns is null || localized.ThumbnailPatterns is null || localized.HookPatterns is null || localized.ContentFormats is null || localized.PerformanceInsights is null || localized.PotentialWeaknesses is null || localized.TransferableFormats is null || localized.EvidenceNotes is null)
            throw new StructuredOutputException("Localized analysis is missing required reader-facing content.");
        if (localized.Audience.LikelyInterests is null || localized.Audience.LikelyViewerIntent is null || localized.Audience.GeographyHints is null || localized.Audience.Evidence is null || localized.Confidence.Limitations is null || localized.TopicClusters.Any(item => item is null) || localized.TitlePatterns.Any(item => item is null) || localized.ThumbnailPatterns.Any(item => item is null) || localized.HookPatterns.Any(item => item is null) || localized.ContentFormats.Any(item => item is null) || localized.PerformanceInsights.Any(item => item is null) || localized.PotentialWeaknesses.Any(item => item is null) || localized.TransferableFormats.Any(item => item is null) || localized.EvidenceNotes.Any(item => item is null))
            throw new StructuredOutputException("Localized analysis contains incomplete reader-facing content.");
        if (localized.TopicClusters.Count != source.TopicClusters.Count || localized.TitlePatterns.Count != source.TitlePatterns.Count || localized.ThumbnailPatterns.Count != source.ThumbnailPatterns.Count || localized.HookPatterns.Count != source.HookPatterns.Count || localized.ContentFormats.Count != source.ContentFormats.Count || localized.PerformanceInsights.Count != source.PerformanceInsights.Count || localized.PotentialWeaknesses.Count != source.PotentialWeaknesses.Count || localized.TransferableFormats.Count != source.TransferableFormats.Count || localized.EvidenceNotes.Count != source.EvidenceNotes.Count)
            throw new StructuredOutputException("Localized analysis must preserve the canonical artifact structure.");
        if (localized.Audience.LikelyInterests.Count != source.Audience.LikelyInterests.Count || localized.Audience.LikelyViewerIntent.Count != source.Audience.LikelyViewerIntent.Count || localized.Audience.GeographyHints.Count != source.Audience.GeographyHints.Count || localized.Audience.Evidence.Count != source.Audience.Evidence.Count || localized.Confidence.Limitations.Count != source.Confidence.Limitations.Count)
            throw new StructuredOutputException("Localized analysis must preserve all reader-facing list entries.");
    }
}
