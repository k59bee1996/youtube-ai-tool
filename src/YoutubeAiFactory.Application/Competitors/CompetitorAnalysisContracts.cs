using YoutubeAiFactory.Domain.Competitors;

namespace YoutubeAiFactory.Application.Competitors;

public sealed record CompetitorAnalysisResult(
    AudienceAnalysis Audience,
    IReadOnlyList<TopicCluster> TopicClusters,
    IReadOnlyList<TitlePattern> TitlePatterns,
    IReadOnlyList<ThumbnailPattern> ThumbnailPatterns,
    IReadOnlyList<HookPattern> HookPatterns,
    IReadOnlyList<ContentFormatInsight> ContentFormats,
    IReadOnlyList<PerformanceInsight> PerformanceInsights,
    IReadOnlyList<WeaknessInsight> PotentialWeaknesses,
    IReadOnlyList<TransferableFormat> TransferableFormats,
    IReadOnlyList<EvidenceNote> EvidenceNotes,
    AnalysisConfidence Confidence);

public sealed record AudienceAnalysis(
    string? LikelyAgeRange,
    IReadOnlyList<string> LikelyInterests,
    IReadOnlyList<string> LikelyViewerIntent,
    IReadOnlyList<string> GeographyHints,
    int Confidence,
    IReadOnlyList<string> Evidence);

public sealed record TopicCluster(string Name, string Description, IReadOnlyList<Guid> ExampleVideoIds, int Frequency, string PerformanceSignal, int Confidence);
public sealed record TitlePattern(string PatternName, string Description, string Template, IReadOnlyList<string> ExampleTitles, int ObservedFrequency, string PerformanceSignal, int Confidence);
public sealed record ThumbnailPattern(string PatternName, string Observation, IReadOnlyList<Guid> EvidenceVideoIds, int Confidence, IReadOnlyList<string> Limitations);
public sealed record HookPattern(string PatternName, string Observation, IReadOnlyList<Guid> EvidenceVideoIds, int Confidence, IReadOnlyList<string> Limitations);
public sealed record ContentFormatInsight(string Format, IReadOnlyList<Guid> EvidenceVideoIds, string PerformanceSignal, int Confidence);
public sealed record PerformanceInsight(string Insight, IReadOnlyList<Guid> SupportingVideoIds, int Confidence);
public sealed record WeaknessInsight(string Observation, IReadOnlyList<Guid> SupportingVideoIds, int Confidence);
public sealed record TransferableFormat(string Format, string WhyItMayWork, IReadOnlyList<Guid> EvidenceVideoIds, string TransferableMechanic, string DoNotCopy, int Confidence);
public sealed record EvidenceNote(string Note, IReadOnlyList<Guid> VideoIds);
public sealed record AnalysisConfidence(int OverallConfidence, string DataQuality, IReadOnlyList<string> Limitations);

public sealed record CompetitorAnalysisDto(
    Guid Id,
    int Version,
    string PromptKey,
    int PromptVersion,
    string Provider,
    string Model,
    DateTimeOffset SourceDataAsOf,
    int AnalyzedVideoCount,
    DateTimeOffset CreatedAt,
    bool IsStale,
    CompetitorAnalysisResult Result);

public sealed record AnalysisJobDto(Guid Id, string Status, string? FailureReason);
public sealed record CompetitorAnalysisStatusDto(CompetitorAnalysisDto? LatestAnalysis, AnalysisJobDto? ActiveJob, AnalysisJobDto? LatestJob);
public sealed record RunCompetitorAnalysisResult(Guid JobId, string Status, bool Existing);

public sealed record AnalysisVideoContext(
    Guid Id, string Title, string? Description, DateTimeOffset? PublishedAt, long? Views,
    long? Likes, long? Comments, decimal? EngagementRate, decimal? OutlierRatio,
    PerformanceClassification PerformanceClassification);

public sealed record CompetitorAnalysisContext(
    Guid CompetitorId, string ChannelTitle, string? ChannelDescription, string? Handle,
    long? SubscriberCount, int TotalCollectedVideos, int AnalyzedVideoCount, long? MedianViews,
    double? AverageViews, double? AveragePublishingDays, IReadOnlyList<AnalysisVideoContext> Videos,
    IReadOnlyList<string> Limitations, DateTimeOffset SourceDataAsOf);
