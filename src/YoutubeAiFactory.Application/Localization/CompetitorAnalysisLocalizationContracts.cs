using YoutubeAiFactory.Application.Competitors;
using YoutubeAiFactory.Application.Common;

namespace YoutubeAiFactory.Application.Localization;

// These records contain only dynamic, reader-facing fields. Scores, IDs, evidence references, titles, templates, and metadata remain canonical.
public sealed record LocalizedCompetitorAnalysisContent(
    LocalizedAudience Audience, IReadOnlyList<LocalizedTopicCluster> TopicClusters, IReadOnlyList<LocalizedTitlePattern> TitlePatterns,
    IReadOnlyList<LocalizedEvidencePattern> ThumbnailPatterns, IReadOnlyList<LocalizedEvidencePattern> HookPatterns,
    IReadOnlyList<LocalizedContentFormat> ContentFormats, IReadOnlyList<string> PerformanceInsights, IReadOnlyList<string> PotentialWeaknesses,
    IReadOnlyList<LocalizedTransferableFormat> TransferableFormats, IReadOnlyList<string> EvidenceNotes, LocalizedConfidence Confidence);
public sealed record LocalizedAudience(string? LikelyAgeRange, IReadOnlyList<string> LikelyInterests, IReadOnlyList<string> LikelyViewerIntent, IReadOnlyList<string> GeographyHints, IReadOnlyList<string> Evidence);
public sealed record LocalizedTopicCluster(string Name, string Description, string PerformanceSignal);
public sealed record LocalizedTitlePattern(string PatternName, string Description, string PerformanceSignal);
public sealed record LocalizedEvidencePattern(string PatternName, string Observation, IReadOnlyList<string> Limitations);
public sealed record LocalizedContentFormat(string Format, string PerformanceSignal);
public sealed record LocalizedTransferableFormat(string Format, string WhyItMayWork, string TransferableMechanic, string DoNotCopy);
public sealed record LocalizedConfidence(string DataQuality, IReadOnlyList<string> Limitations);

public static class LocalizedCompetitorAnalysisValidator
{
    public static void Validate(CompetitorAnalysisResult source, LocalizedCompetitorAnalysisContent localized)
    {
        if (localized.TopicClusters.Count != source.TopicClusters.Count || localized.TitlePatterns.Count != source.TitlePatterns.Count || localized.ThumbnailPatterns.Count != source.ThumbnailPatterns.Count || localized.HookPatterns.Count != source.HookPatterns.Count || localized.ContentFormats.Count != source.ContentFormats.Count || localized.PerformanceInsights.Count != source.PerformanceInsights.Count || localized.PotentialWeaknesses.Count != source.PotentialWeaknesses.Count || localized.TransferableFormats.Count != source.TransferableFormats.Count || localized.EvidenceNotes.Count != source.EvidenceNotes.Count)
            throw new StructuredOutputException("Localized analysis must preserve the canonical artifact structure.");
        if (localized.Audience.LikelyInterests.Count != source.Audience.LikelyInterests.Count || localized.Audience.LikelyViewerIntent.Count != source.Audience.LikelyViewerIntent.Count || localized.Audience.GeographyHints.Count != source.Audience.GeographyHints.Count || localized.Audience.Evidence.Count != source.Audience.Evidence.Count || localized.Confidence.Limitations.Count != source.Confidence.Limitations.Count)
            throw new StructuredOutputException("Localized analysis must preserve all reader-facing list entries.");
    }
}
