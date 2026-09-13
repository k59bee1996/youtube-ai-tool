using YoutubeAiFactory.Domain.Ideas;
using YoutubeAiFactory.Domain.Opportunities;
using YoutubeAiFactory.Domain.Pilots;
using YoutubeAiFactory.Domain.Videos;

namespace YoutubeAiFactory.Application.Videos;

public sealed record VideoProjectSource(Pilot Pilot, PilotVideo PilotVideo, VideoIdea VideoIdea, OpportunityCandidate Opportunity);

public sealed record CreateVideoProjectRequest(Guid PilotVideoId);
public sealed record UpdateVideoProjectRequest(string WorkingTitle, string? ExecutionNotes);

public sealed record VideoProjectListItemDto(Guid Id, Guid PilotVideoId, string WorkingTitle, string Status, Guid VideoIdeaId,
    int PilotSequence, string ExperimentType, DateTimeOffset CreatedAt);

public sealed record VideoProjectDto(Guid Id, Guid ProjectId, Guid PilotId, int PilotVersion, Guid PilotVideoId,
    Guid VideoIdeaId, Guid OpportunityId, string WorkingTitle, string Status, string Topic, string Angle,
    string ContentFormat, string TargetAudience, string HookConcept, string ThumbnailConcept, string ViewerPromise,
    int PilotSequence, string ExperimentType, string PilotHypothesis, string VariableBeingTested, string PrimaryMetric,
    string SuccessSignal, string OpportunityName, decimal IdeaScore, string? ExecutionNotes, DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt, bool SourceRequiresReview, IReadOnlyList<string> SourceWarnings);
