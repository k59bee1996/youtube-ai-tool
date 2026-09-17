using System.Text.Json;
using System.Text.Json.Serialization;
using YoutubeAiFactory.Application.Research;
using YoutubeAiFactory.Application.Videos;
using YoutubeAiFactory.Domain.Ideas;
using YoutubeAiFactory.Domain.Opportunities;
using YoutubeAiFactory.Domain.Pilots;
using YoutubeAiFactory.Domain.Projects;
using YoutubeAiFactory.Domain.Research;
using YoutubeAiFactory.Domain.Videos;

namespace YoutubeAiFactory.Application.Tests;

internal sealed class OutlineTestFixture
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    public OutlineTestFixture(PilotExperimentType experimentType = PilotExperimentType.Packaging,
        string variable = "Hidden-cost framing", string control = "Keep topic and storytelling structure comparable",
        int unsupportedCriticalClaimCount = 0)
    {
        var now = DateTimeOffset.UtcNow;
        Project = new Project("History", new Market("Historical economics", "English", "Global"),
            new AudienceProfile("History viewers"), now);
        var opportunityReport = new OpportunityReport(Project.Id, 1, Guid.NewGuid(), "opportunity", 1, "fake", "fake",
            "score:v1", 1, "[]", now);
        Opportunity = new OpportunityCandidate(opportunityReport.Id, "Historical Ownership Economics", "Description",
            "History viewers", "Economics", "Explainer", "Hidden costs", "Recurring obligations create an opportunity.",
            80, 70, 40, 85, 75, 70, 80, 30, 85, 82m, "[]", "[]", now);
        Opportunity.SetDecision(OpportunityDecisionStatus.Approved);
        var generation = new IdeaGeneration(Project.Id, Opportunity.Id, opportunityReport.Id, 1, 1, Guid.NewGuid(),
            "ideas", 1, "fake", "fake", "score:v1", now);
        Idea = new VideoIdea(Project.Id, Opportunity.Id, generation.Id, "The Economics of Owning a Medieval Castle",
            "Historical economics", "Hidden costs", "Explainer", "History viewers", "Learn", "Reveal hidden obligations",
            "Castle and ledger", "Understand the real ownership cost", "Question", "Why care", "Hypothesis",
            80, 80, 70, 80, 75, 85, 85, 70, 80, 30, 20, 85, 82m, 0m, "score:v1", "[]", now);
        Idea.SetDecision(IdeaDecisionStatus.Approved);
        Pilot = new Pilot(Project.Id, 1, Guid.NewGuid(), "pilot", 1, "fake", "fake", "plan:v1", "Pilot", "Learn",
            "[]", "[]", "[]", 12, now);
        Pilot.Approve(now);
        var pilotSequence = experimentType switch
        {
            PilotExperimentType.Topic => 1,
            PilotExperimentType.Packaging => 5,
            _ => 9,
        };
        PilotVideo = new PilotVideo(Pilot.Id, Idea.Id, Opportunity.Id, pilotSequence, experimentType,
            "The selected variable should improve the primary metric.", variable, control, "CTR", "CTR improves", "Rationale");
        VideoProject = new VideoProject(Project.Id, Pilot.Id, Pilot.Version, PilotVideo.Id, Idea.Id, Opportunity.Id,
            Idea.WorkingTitle, Idea.Topic, Idea.Angle, Idea.ContentFormat, Idea.TargetAudience, Idea.HookConcept,
            Idea.ThumbnailConcept, Idea.ViewerPromise, experimentType, PilotVideo.Hypothesis, variable,
            PilotVideo.PrimaryMetric, PilotVideo.SuccessSignal, "[]", now);
        VideoProject.TransitionTo(VideoProjectStatus.ResearchQueued, now);
        VideoProject.TransitionTo(VideoProjectStatus.Researching, now);
        VideoProject.TransitionTo(VideoProjectStatus.ResearchReady, now);
        Source = new VideoProjectSource(Pilot, PilotVideo, Idea, Opportunity);

        var runId = Guid.NewGuid();
        var reportId = Guid.NewGuid();
        PrimarySource = new ResearchSource(runId, "https://archive.example/castle", "https://archive.example/castle",
            "archive.example", "Castle accounts", "Archive", null, now, ResearchSourceCategory.Primary,
            ResearchSourceFetchStatus.Fetched, "hash-one", null, null);
        SecondarySource = new ResearchSource(runId, "https://university.example/economics", "https://university.example/economics",
            "university.example", "Castle economics", "University", null, now, ResearchSourceCategory.Academic,
            ResearchSourceFetchStatus.Fetched, "hash-two", null, null);
        PrimaryEvidence = new ResearchEvidence(runId, PrimarySource.Id, ResearchEvidenceType.Fact,
            "Castle households required continuing staffing.", "Accounts list recurring household roles.", "ledger 12", 92, now);
        SecondaryEvidence = new ResearchEvidence(runId, SecondarySource.Id, ResearchEvidenceType.Fact,
            "Estate income supported recurring obligations.", "The analysis connects estate income to household expense.", "p. 18", 84, now);
        ContradictingEvidence = new ResearchEvidence(runId, SecondarySource.Id, ResearchEvidenceType.Statistic,
            "Annual cost estimates vary by estate and period.", "The study cautions against one universal figure.", "p. 24", 78, now);

        SupportedClaim = new ResearchClaim(reportId, "Ownership required continuing staffing obligations.",
            ResearchClaimType.Factual, 90, true, now);
        SupportedClaim.SetSupportStatus(ResearchClaimSupportStatus.Supported);
        CorroboratedClaim = new ResearchClaim(reportId, "Recurring costs were tied to estate income and obligations.",
            ResearchClaimType.Factual, 87, true, now);
        CorroboratedClaim.SetSupportStatus(ResearchClaimSupportStatus.Corroborated);
        ConflictedClaim = new ResearchClaim(reportId, "One universal annual maintenance estimate can be reconstructed.",
            ResearchClaimType.Numerical, 62, false, now);
        ConflictedClaim.SetSupportStatus(ResearchClaimSupportStatus.Conflicted);
        UnsupportedClaim = new ResearchClaim(reportId, "Every medieval lord was bankrupted by a castle.",
            ResearchClaimType.Factual, 15, false, now);
        UnsupportedClaim.SetSupportStatus(ResearchClaimSupportStatus.Unsupported);

        ClaimEvidence =
        [
            new(SupportedClaim.Id, PrimaryEvidence.Id, ResearchEvidenceStance.Support),
            new(CorroboratedClaim.Id, PrimaryEvidence.Id, ResearchEvidenceStance.Support),
            new(CorroboratedClaim.Id, SecondaryEvidence.Id, ResearchEvidenceStance.Support),
            new(ConflictedClaim.Id, PrimaryEvidence.Id, ResearchEvidenceStance.Support),
            new(ConflictedClaim.Id, ContradictingEvidence.Id, ResearchEvidenceStance.Contradict),
        ];
        Conflict = new ResearchConflict(reportId, ConflictedClaim.Id, PrimaryEvidence.Id, ContradictingEvidence.Id,
            "Available sources do not support one universal annual figure.", false, now);
        var payload = new ResearchReportPayload(
            new("Castle ownership combined prestige with continuing economic obligations.",
                [new("Recurring obligations drive the core mechanism.", "Mechanism", [SupportedClaim.Id, CorroboratedClaim.Id], [PrimaryEvidence.Id, SecondaryEvidence.Id])],
                [new("No reliable universal annual maintenance figure was found.", [ConflictedClaim.Id])],
                ["Exact costs vary by place and period."], ["Surviving records are incomplete."]),
            new("Medium", 2, 2, 4, 1, 1, 1, unsupportedCriticalClaimCount),
            new(2, 2, 2, 2, 2, 3, 4, 1, 1, 1, 1, 1, 0, 0),
            new("Establish the economics of ownership.", [new("What obligations persisted?", true)],
                [new("medieval castle household accounts", "Find primary evidence")], ["Staffing", "Income"], ["Incomplete records"]));
        var researchFingerprint = ResearchBriefBuilder.CreateFingerprint(ResearchBriefBuilder.Build(Project, VideoProject, Opportunity.Name));
        Report = new ResearchReport(Project.Id, VideoProject.Id, runId, 1, "research-engine:v1", researchFingerprint,
            JsonSerializer.Serialize(payload, JsonOptions), null, now, reportId);
        Research = new ResearchReportWithDetails(Report, [PrimarySource, SecondarySource],
            [PrimaryEvidence, SecondaryEvidence, ContradictingEvidence],
            [SupportedClaim, CorroboratedClaim, ConflictedClaim, UnsupportedClaim], ClaimEvidence, [Conflict]);
    }

    public Project Project { get; }
    public OpportunityCandidate Opportunity { get; }
    public VideoIdea Idea { get; }
    public Pilot Pilot { get; }
    public PilotVideo PilotVideo { get; }
    public VideoProject VideoProject { get; }
    public VideoProjectSource Source { get; }
    public ResearchReport Report { get; }
    public ResearchReportWithDetails Research { get; }
    public ResearchSource PrimarySource { get; }
    public ResearchSource SecondarySource { get; }
    public ResearchEvidence PrimaryEvidence { get; }
    public ResearchEvidence SecondaryEvidence { get; }
    public ResearchEvidence ContradictingEvidence { get; }
    public ResearchClaim SupportedClaim { get; }
    public ResearchClaim CorroboratedClaim { get; }
    public ResearchClaim ConflictedClaim { get; }
    public ResearchClaim UnsupportedClaim { get; }
    public ResearchConflict Conflict { get; }
    public IReadOnlyList<ResearchClaimEvidence> ClaimEvidence { get; }
}
