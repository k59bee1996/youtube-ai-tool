using YoutubeAiFactory.Application.Competitors;
using YoutubeAiFactory.Domain.Competitors;
using YoutubeAiFactory.Domain.Opportunities;

namespace YoutubeAiFactory.Application.Opportunities;

public sealed record OpportunityAnalysisJobPayload(Guid ProjectId);
public sealed record OpportunityEvidenceContext(string Id, Guid CompetitorAnalysisId, Guid CompetitorId, Guid? VideoId, string Kind, string Summary, int ObservedDemandSignal, int Confidence);
public sealed record OpportunityAnalysisSourceContext(Guid CompetitorId, Guid CompetitorAnalysisId, int CompetitorAnalysisVersion);
public sealed record OpportunityAnalysisContext(Guid ProjectId, string Market, string TargetLanguage, string TargetGeography,
    string Audience, int AnalyzedCompetitorCount, IReadOnlyList<OpportunityEvidenceContext> Evidence, IReadOnlyList<string> Limitations,
    IReadOnlyList<OpportunityAnalysisSourceContext> Sources);
public sealed record OpportunityCandidateResult(string Name, string Description, string Audience, string Topic, string ContentFormat,
    string Angle, string WhyThisOpportunity, int NoveltySignal, int AudienceFitSignal, int TransferabilitySignal,
    int StoryPotential, int ProductionComplexity, int Confidence, IReadOnlyList<string> EvidenceIds,
    IReadOnlyList<string> Risks, IReadOnlyList<string> Limitations);
public sealed record OpportunityAnalysisResult(IReadOnlyList<OpportunityCandidateResult> Opportunities, IReadOnlyList<string> Limitations);
public sealed record OpportunityScoreBreakdown(int ObservedDemandSignal, int NoveltySignal, int CompetitionRiskSignal,
    int AudienceFitSignal, int TransferabilitySignal, int EvidenceStrength, int StoryPotential, int ProductionComplexity, decimal OverallScore);
public sealed record OpportunityEvidenceDto(Guid Id, Guid CompetitorChannelId, Guid CompetitorAnalysisId, Guid? CompetitorVideoId, string EvidenceId, string Summary);
public sealed record OpportunityCandidateDto(Guid Id, string Name, string Description, string Audience, string Topic, string ContentFormat,
    string Angle, string WhyThisOpportunity, OpportunityScoreBreakdown Scores, int Confidence, IReadOnlyList<string> Risks,
    IReadOnlyList<string> Limitations, string DecisionStatus, IReadOnlyList<OpportunityEvidenceDto> Evidence);
public sealed record OpportunityReportSourceDto(Guid CompetitorChannelId, Guid CompetitorAnalysisId, int CompetitorAnalysisVersion);
public sealed record OpportunityReportDto(Guid Id, int Version, string PromptKey, int PromptVersion, string Provider, string Model,
    string ScoringAlgorithmVersion, DateTimeOffset CreatedAt, bool IsStale, IReadOnlyList<OpportunityReportSourceDto> Sources,
    IReadOnlyList<string> Limitations, IReadOnlyList<OpportunityCandidateDto> Opportunities);
public sealed record OpportunityJobDto(Guid Id, string Status, string? FailureReason);
public sealed record OpportunityStatusDto(OpportunityReportDto? LatestReport, OpportunityJobDto? ActiveJob, OpportunityJobDto? LatestJob,
    int CompetitorCount, int AnalyzedCompetitorCount);
public sealed record RunOpportunityAnalysisResult(Guid JobId, string Status, bool Existing);
public sealed record CurrentCompetitorAnalysis(Guid CompetitorId, string CompetitorTitle, CompetitorAnalysis Analysis);
