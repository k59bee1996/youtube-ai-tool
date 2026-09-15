using YoutubeAiFactory.Application.Common;
using YoutubeAiFactory.Application.Persistence;
using YoutubeAiFactory.Application.Videos;
using YoutubeAiFactory.Domain.Competitors;
using YoutubeAiFactory.Domain.Ideas;
using YoutubeAiFactory.Domain.Opportunities;
using YoutubeAiFactory.Domain.Pilots;
using YoutubeAiFactory.Domain.Projects;
using YoutubeAiFactory.Domain.Videos;

namespace YoutubeAiFactory.Application.Tests;

public sealed class VideoProjectWorkflowTests
{
    [Fact]
    public async Task Creates_an_idempotent_draft_execution_project_from_an_approved_pilot_video()
    {
        var store = new VideoProjectStore(PilotStatus.Approved, IdeaDecisionStatus.Approved, OpportunityDecisionStatus.Approved);
        var handler = new CreateVideoProjectHandler(store, TimeProvider.System);

        var first = await handler.HandleAsync(store.Project.Id, store.Pilot.Id, new CreateVideoProjectRequest(store.PilotVideo.Id), CancellationToken.None);
        var second = await handler.HandleAsync(store.Project.Id, store.Pilot.Id, new CreateVideoProjectRequest(store.PilotVideo.Id), CancellationToken.None);

        Assert.Equal(first.Id, second.Id);
        Assert.Single(store.Created);
        Assert.Equal("Draft", first.Status);
        Assert.Equal(store.Pilot.Version, first.PilotVersion);
        Assert.Equal(store.VideoIdea.Id, first.VideoIdeaId);
        Assert.Equal(store.Opportunity.Id, first.OpportunityId);
        Assert.Equal("The Economics of Owning a Medieval Castle", first.WorkingTitle);
    }

    [Fact]
    public async Task Rejects_a_draft_pilot_and_sources_that_require_review()
    {
        var draftStore = new VideoProjectStore(PilotStatus.Draft, IdeaDecisionStatus.Approved, OpportunityDecisionStatus.Approved);
        var draftHandler = new CreateVideoProjectHandler(draftStore, TimeProvider.System);
        await Assert.ThrowsAsync<ApplicationValidationException>(() => draftHandler.HandleAsync(draftStore.Project.Id, draftStore.Pilot.Id, new CreateVideoProjectRequest(draftStore.PilotVideo.Id), CancellationToken.None));

        var staleStore = new VideoProjectStore(PilotStatus.Approved, IdeaDecisionStatus.Rejected, OpportunityDecisionStatus.Approved);
        var staleHandler = new CreateVideoProjectHandler(staleStore, TimeProvider.System);
        await Assert.ThrowsAsync<ApplicationValidationException>(() => staleHandler.HandleAsync(staleStore.Project.Id, staleStore.Pilot.Id, new CreateVideoProjectRequest(staleStore.PilotVideo.Id), CancellationToken.None));
    }

    [Fact]
    public async Task Does_not_return_a_video_project_from_another_project_before_validating_slot_membership()
    {
        var store = new VideoProjectStore(PilotStatus.Approved, IdeaDecisionStatus.Approved, OpportunityDecisionStatus.Approved);
        var foreignPilotVideoId = Guid.NewGuid();
        store.Created.Add(new VideoProject(Guid.NewGuid(), Guid.NewGuid(), 1, foreignPilotVideoId, Guid.NewGuid(), Guid.NewGuid(),
            "Other project's video", "Topic", "Angle", "Explainer", "Audience", "Hook", "Thumbnail", "Promise",
            PilotExperimentType.Packaging, "Hypothesis", "Variable", "CTR", "Success", "[]", DateTimeOffset.UtcNow));
        var handler = new CreateVideoProjectHandler(store, TimeProvider.System);

        await Assert.ThrowsAsync<ResourceNotFoundException>(() => handler.HandleAsync(store.Project.Id, store.Pilot.Id, new CreateVideoProjectRequest(foreignPilotVideoId), CancellationToken.None));
    }

    private sealed class VideoProjectStore : IYoutubeAiFactoryStore
    {
        public Project Project { get; }
        public Pilot Pilot { get; }
        public PilotVideo PilotVideo { get; }
        public VideoIdea VideoIdea { get; }
        public OpportunityCandidate Opportunity { get; }
        public List<VideoProject> Created { get; } = [];

        public VideoProjectStore(PilotStatus pilotStatus, IdeaDecisionStatus ideaStatus, OpportunityDecisionStatus opportunityStatus)
        {
            var now = DateTimeOffset.UtcNow;
            Project = new Project("History", new Market("History", "English", "Global"), new AudienceProfile("History viewers"), now);
            var report = new OpportunityReport(Project.Id, 1, Guid.NewGuid(), "opportunity", 1, "fake", "fake", "score:v1", 1, "[]", now);
            Opportunity = new OpportunityCandidate(report.Id, "Historical Ownership Economics", "Description", "History viewers", "Economics", "Explainer", "Hidden costs", "Why", 80, 70, 40, 85, 75, 70, 80, 30, 85, 82m, "[]", "[]", now);
            Opportunity.SetDecision(opportunityStatus);
            var generation = new IdeaGeneration(Project.Id, Opportunity.Id, report.Id, 1, 1, Guid.NewGuid(), "ideas", 1, "fake", "fake", "score:v1", now);
            VideoIdea = new VideoIdea(Project.Id, Opportunity.Id, generation.Id, "The Economics of Owning a Medieval Castle", "Historical economics", "Hidden costs", "Explainer", "History viewers", "Learn", "Reveal the cost", "Castle and ledger", "Understand ownership cost", "Question", "Why care", "Hypothesis", 80, 80, 70, 80, 75, 85, 85, 70, 80, 30, 20, 85, 82m, 0m, "score:v1", "[]", now);
            VideoIdea.SetDecision(ideaStatus);
            Pilot = new Pilot(Project.Id, 1, Guid.NewGuid(), "pilot", 1, "fake", "fake", "plan:v1", "Pilot", "Learn", "[]", "[]", "[\"A balance warning\"]", 12, now);
            if (pilotStatus == PilotStatus.Approved) Pilot.Approve(now);
            PilotVideo = new PilotVideo(Pilot.Id, VideoIdea.Id, Opportunity.Id, 6, PilotExperimentType.Packaging, "Hidden-cost framing should increase click intent.", "Framing", "Comparable topics", "CTR", "CTR increases", "Rationale");
        }

        public Task<Project?> GetProjectAsync(Guid projectId, CancellationToken cancellationToken) => Task.FromResult(projectId == Project.Id ? Project : null);
        public Task<bool> ProjectExistsAsync(Guid projectId, CancellationToken cancellationToken) => Task.FromResult(projectId == Project.Id);
        public Task<IReadOnlyList<Project>> ListProjectsAsync(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<Project>>([Project]);
        public void AddProject(Project project) { }
        public Task<IReadOnlyList<CompetitorChannel>> ListCompetitorsAsync(Guid projectId, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<CompetitorChannel>>([]);
        public Task<CompetitorChannel?> GetCompetitorAsync(Guid projectId, Guid competitorId, bool forUpdate, CancellationToken cancellationToken) => Task.FromResult<CompetitorChannel?>(null);
        public Task<CompetitorChannel?> FindCompetitorByYoutubeChannelIdAsync(Guid projectId, string youtubeChannelId, CancellationToken cancellationToken) => Task.FromResult<CompetitorChannel?>(null);
        public void AddCompetitor(CompetitorChannel competitor) { }
        public Task<Pilot?> GetPilotAsync(Guid projectId, Guid pilotId, bool forUpdate, CancellationToken cancellationToken) => Task.FromResult(projectId == Project.Id && pilotId == Pilot.Id ? Pilot : null);
        public Task<VideoProject?> GetVideoProjectByPilotVideoAsync(Guid projectId, Guid pilotVideoId, CancellationToken cancellationToken) => Task.FromResult<VideoProject?>(Created.SingleOrDefault(item => item.ProjectId == projectId && item.PilotVideoId == pilotVideoId));
        public Task<VideoProjectSource?> GetVideoProjectSourceAsync(Guid projectId, Guid pilotId, Guid pilotVideoId, CancellationToken cancellationToken) => Task.FromResult<VideoProjectSource?>(projectId == Project.Id && pilotId == Pilot.Id && pilotVideoId == PilotVideo.Id ? new VideoProjectSource(Pilot, PilotVideo, VideoIdea, Opportunity) : null);
        public Task<VideoProject> CreateVideoProjectIfAbsentAsync(VideoProject project, CancellationToken cancellationToken) { var existing = Created.SingleOrDefault(item => item.PilotVideoId == project.PilotVideoId); if (existing is not null) return Task.FromResult(existing); Created.Add(project); return Task.FromResult(project); }
        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
