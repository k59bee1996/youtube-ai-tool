using YoutubeAiFactory.Domain.Opportunities;

namespace YoutubeAiFactory.Application.Opportunities;

public sealed record OpportunityCandidateWithEvidence(OpportunityCandidate Candidate, IReadOnlyList<OpportunityEvidence> Evidence);
public sealed record OpportunityReportWithDetails(OpportunityReport Report, IReadOnlyList<OpportunityReportSource> Sources,
    IReadOnlyList<OpportunityCandidateWithEvidence> Candidates, IReadOnlyList<string> Limitations);
