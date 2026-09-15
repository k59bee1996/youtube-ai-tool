using YoutubeAiFactory.Domain.Research;

namespace YoutubeAiFactory.Application.Research;

public sealed record ResearchBrief(
    Guid ProjectId,
    Guid VideoProjectId,
    string WorkingTitle,
    string Topic,
    string Angle,
    string ContentFormat,
    string TargetAudience,
    string ViewerPromise,
    string HookConcept,
    string TargetLanguage,
    string TargetGeography,
    string PilotExperimentType,
    string PilotHypothesis,
    string VariableBeingTested,
    string OpportunityName);

public sealed record ResearchQuestion(string Question, bool IsCentral);
public sealed record SearchQueryPlan(string Query, string Purpose);
public sealed record ResearchPlan(string Objective, IReadOnlyList<ResearchQuestion> Questions,
    IReadOnlyList<SearchQueryPlan> Queries, IReadOnlyList<string> PriorityFactAreas, IReadOnlyList<string> KnownRisks);

public sealed record ResearchQueryPlanResult(string Objective, IReadOnlyList<ResearchQuestion> Questions,
    IReadOnlyList<SearchQueryPlan> Queries, IReadOnlyList<string> PriorityFactAreas, IReadOnlyList<string> KnownRisks);

public sealed record ResearchSearchRequest(string Query, string Language, int MaxResults);
public sealed record ResearchSearchResult(string Url, string? Title, string? Snippet, DateTimeOffset? PublishedAt);
public sealed record ResearchSearchResultPage(IReadOnlyList<ResearchSearchResult> Results);

public interface IResearchSearchClient
{
    Task<ResearchSearchResultPage> SearchAsync(ResearchSearchRequest request, CancellationToken cancellationToken);
}

public sealed record ResearchContentFetchRequest(string Url, int? MaxExtractedCharacters = null);
public sealed record ResearchContentFetchResult(
    ResearchSourceFetchStatus Status,
    string RequestedUrl,
    string? FinalUrl,
    string? ContentType,
    string? Title,
    string? ExtractedText,
    string? FailureReason,
    int ResponseBytes);

public interface IResearchContentFetcher
{
    Task<ResearchContentFetchResult> FetchAsync(ResearchContentFetchRequest request, CancellationToken cancellationToken);
}

public enum ResearchSourceRelevance
{
    Relevant,
    PossiblyRelevant,
    Irrelevant,
}

public sealed record ResearchSourceRelevanceResult(ResearchSourceRelevance Relevance, string Reasoning);
public sealed record ResearchEvidenceExtractionResult(IReadOnlyList<ResearchEvidenceCandidate> Evidence);
public sealed record ResearchEvidenceCandidate(string Fact, string SupportingExcerpt, string SourceLocator, ResearchEvidenceType Type, decimal Confidence);
public sealed record ResearchContradictionAnalysisResult(IReadOnlyList<ResearchConflictCandidate> Conflicts);
public sealed record ResearchConflictCandidate(Guid ClaimId, Guid SupportingEvidenceId, Guid ContradictingEvidenceId, string Explanation, bool IsResolved);
public sealed record ResearchSynthesisResult(string ExecutiveSummary, IReadOnlyList<ResearchFinding> KeyFindings,
    IReadOnlyList<ResearchGap> Gaps, IReadOnlyList<string> Warnings, IReadOnlyList<string> Limitations);
public sealed record ResearchFinding(string Summary, string Category, IReadOnlyList<Guid> ClaimIds, IReadOnlyList<Guid> EvidenceIds);
public sealed record ResearchGap(string Description, IReadOnlyList<Guid> ClaimIds);

public sealed record ResearchConfidenceSummary(string Level, int SourceCount, int RelevantSourceCount, int ClaimCount,
    int CorroboratedClaimCount, int SingleSourceClaimCount, int ConflictedClaimCount, int UnsupportedCriticalClaimCount);
public sealed record ResearchMetricsDto(int QueryCount, int SearchResultCount, int FetchedSourceCount, int RelevantSourceCount,
    int UniqueDomainCount, int EvidenceCount, int ClaimCount, int CorroboratedClaimCount, int SingleSourceClaimCount,
    int ConflictedClaimCount, int UnsupportedClaimCount, int ConflictCount, int SearchFailureCount, int FetchFailureCount);
public sealed record ResearchReportPayload(ResearchSynthesisResult Synthesis, ResearchConfidenceSummary Confidence,
    ResearchMetricsDto Metrics, ResearchPlan Plan);

public sealed record ResearchJobDto(Guid Id, string Status, string? FailureReason);
public sealed record ResearchSourceDto(Guid Id, string Url, string CanonicalUrl, string Domain, string? Title,
    string? Publisher, DateTimeOffset? PublishedAt, DateTimeOffset RetrievedAt, string Category, string FetchStatus);
public sealed record ResearchEvidenceDto(Guid Id, Guid SourceId, string Type, string Fact, string SupportingExcerpt,
    string SourceLocator, decimal Confidence);
public sealed record ResearchClaimEvidenceDto(Guid EvidenceId, string Stance);
public sealed record ResearchClaimDto(Guid Id, string Statement, string Type, string SupportStatus, decimal Confidence,
    bool IsCritical, IReadOnlyList<ResearchClaimEvidenceDto> Evidence);
public sealed record ResearchConflictDto(Guid Id, Guid ClaimId, Guid SupportingEvidenceId, Guid ContradictingEvidenceId,
    string Explanation, bool IsResolved);
public sealed record ResearchReportDto(Guid Id, int Version, string ResearchAlgorithmVersion, DateTimeOffset CreatedAt,
    bool IsStale, ResearchSynthesisResult Synthesis, ResearchConfidenceSummary Confidence, ResearchMetricsDto Metrics,
    IReadOnlyList<ResearchSourceDto> Sources, IReadOnlyList<ResearchEvidenceDto> Evidence,
    IReadOnlyList<ResearchClaimDto> Claims, IReadOnlyList<ResearchConflictDto> Conflicts);
public sealed record ResearchStatusDto(ResearchReportDto? LatestReport, ResearchJobDto? ActiveJob, ResearchJobDto? LatestJob,
    string? LatestRunStatus, string? LatestRunFailureReason);
public sealed record RunVideoResearchResult(Guid JobId, string Status, bool Existing);
public sealed record ResearchRunDto(Guid Id, string Status, string ResearchAlgorithmVersion, string InputFingerprint,
    DateTimeOffset QueuedAt, DateTimeOffset? StartedAt, DateTimeOffset? CompletedAt, string? FailureReason,
    int SearchQueryCount, int SearchResultCount, int FetchedSourceCount, int RelevantSourceCount, int EvidenceCount,
    int ClaimCount, int ConflictCount, int SearchFailureCount, int FetchFailureCount);

public sealed record ResearchReportDetails(ResearchReportDto Report, IReadOnlyList<ResearchRunDto> Runs);
public sealed record ResearchReportHistoryItemDto(Guid Id, int Version, string ResearchAlgorithmVersion, DateTimeOffset CreatedAt);
public sealed record ResearchReportWithDetails(ResearchReport Report, IReadOnlyList<ResearchSource> Sources,
    IReadOnlyList<ResearchEvidence> Evidence, IReadOnlyList<ResearchClaim> Claims,
    IReadOnlyList<ResearchClaimEvidence> ClaimEvidence, IReadOnlyList<ResearchConflict> Conflicts);
