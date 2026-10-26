using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using YoutubeAiFactory.Application.AI;
using YoutubeAiFactory.Application.Common;
using YoutubeAiFactory.Application.Outlines;
using YoutubeAiFactory.Application.Persistence;
using YoutubeAiFactory.Application.Research;
using YoutubeAiFactory.Application.Videos;
using YoutubeAiFactory.Domain.AI;
using YoutubeAiFactory.Domain.Competitors;
using YoutubeAiFactory.Domain.Jobs;
using YoutubeAiFactory.Domain.Outlines;
using YoutubeAiFactory.Domain.Projects;
using YoutubeAiFactory.Domain.Videos;

namespace YoutubeAiFactory.Application.Tests;

public sealed class OutlineWorkflowTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task Queues_idempotently_preserves_versions_and_approves_latest_outline()
    {
        var fixture = new OutlineTestFixture();
        var provider = new QueueProvider(ValidResult(fixture.SupportedClaim.Id), ValidResult(fixture.SupportedClaim.Id));
        var workflow = CreateWorkflow(fixture, provider);

        var first = await workflow.Queue.HandleAsync(fixture.Project.Id, fixture.VideoProject.Id, CancellationToken.None);
        var duplicate = await workflow.Queue.HandleAsync(fixture.Project.Id, fixture.VideoProject.Id, CancellationToken.None);
        Assert.False(first.Existing);
        Assert.True(duplicate.Existing);
        Assert.Equal(first.JobId, duplicate.JobId);

        await workflow.Processor.ProcessNextAsync(CancellationToken.None);
        Assert.Equal(VideoProjectStatus.OutlineReady, fixture.VideoProject.Status);
        Assert.Single(workflow.Store.Outlines);
        Assert.Equal(1, workflow.Store.Outlines[0].Version);
        Assert.Equal(fixture.VideoProject.ViewerPromise, workflow.Store.Outlines[0].ViewerPromise);
        Assert.Equal(265, workflow.Store.Outlines[0].TotalEstimatedSeconds);

        await workflow.Queue.HandleAsync(fixture.Project.Id, fixture.VideoProject.Id, CancellationToken.None);
        await workflow.Processor.ProcessNextAsync(CancellationToken.None);
        Assert.Equal([1, 2], workflow.Store.Outlines.Select(item => item.Version).ToArray());

        var approved = await workflow.Approve.HandleAsync(fixture.Project.Id, fixture.VideoProject.Id,
            workflow.Store.Outlines.Single(item => item.Version == 2).Id, CancellationToken.None);

        Assert.Equal("Approved", approved.Status);
        Assert.Equal(VideoProjectStatus.OutlineApproved, fixture.VideoProject.Status);
        Assert.Equal(VideoOutlineStatus.Ready, workflow.Store.Outlines.Single(item => item.Version == 1).Status);
        Assert.NotEmpty(approved.Sections.SelectMany(item => item.Claims));
    }

    [Fact]
    public async Task Semantic_claim_error_is_corrected_with_reasoning_profile()
    {
        var fixture = new OutlineTestFixture();
        var provider = new QueueProvider(ValidResult(Guid.NewGuid()), ValidResult(fixture.SupportedClaim.Id));
        var workflow = CreateWorkflow(fixture, provider);

        await workflow.Queue.HandleAsync(fixture.Project.Id, fixture.VideoProject.Id, CancellationToken.None);
        await workflow.Processor.ProcessNextAsync(CancellationToken.None);

        Assert.Single(workflow.Store.Outlines);
        Assert.Equal([AiModelProfile.Reasoning, AiModelProfile.Reasoning], provider.Requests.Select(item => item.ModelProfile).ToArray());
        Assert.Equal(2, workflow.Store.AiRuns.Count(run => run.Workflow == "OutlineGeneration"));
        Assert.All(workflow.Store.AiRuns.Where(run => run.Workflow == "OutlineGeneration"), run => Assert.Equal(0, run.RetryCount));
        Assert.Contains(workflow.Store.AiRuns, run => run.Workflow == "OutlineGeneration" && run.Status == AiRunStatus.Failed);
    }

    [Fact]
    public async Task Malformed_output_uses_fast_structural_repair()
    {
        var fixture = new OutlineTestFixture();
        var provider = new QueueProvider(
            new StructuredOutputException("Malformed outline JSON.", rawOutput: "{broken"),
            ValidResult(fixture.SupportedClaim.Id));
        var workflow = CreateWorkflow(fixture, provider);

        await workflow.Queue.HandleAsync(fixture.Project.Id, fixture.VideoProject.Id, CancellationToken.None);
        await workflow.Processor.ProcessNextAsync(CancellationToken.None);

        Assert.Single(workflow.Store.Outlines);
        Assert.Equal([AiModelProfile.Reasoning, AiModelProfile.Fast], provider.Requests.Select(item => item.ModelProfile).ToArray());
        Assert.Contains(workflow.Store.AiRuns, run => run.Workflow == "StructuredOutputRepair" && run.Status == AiRunStatus.Succeeded);
    }

    [Fact]
    public async Task Structured_repair_attempts_are_bounded_across_reasoning_retries()
    {
        var fixture = new OutlineTestFixture();
        var provider = new QueueProvider(
            new StructuredOutputException("Malformed outline JSON.", rawOutput: "{broken-1"),
            new StructuredOutputException("Repair remained malformed.", rawOutput: "{broken-repair"),
            new StructuredOutputException("Second outline remained malformed.", rawOutput: "{broken-2"));
        var workflow = CreateWorkflow(fixture, provider,
            new OutlineOptions { MaxGenerationRetries = 1, MaxStructuredRepairAttempts = 1 });

        await workflow.Queue.HandleAsync(fixture.Project.Id, fixture.VideoProject.Id, CancellationToken.None);
        await workflow.Processor.ProcessNextAsync(CancellationToken.None);

        Assert.Empty(workflow.Store.Outlines);
        Assert.Equal([AiModelProfile.Reasoning, AiModelProfile.Fast, AiModelProfile.Reasoning],
            provider.Requests.Select(item => item.ModelProfile).ToArray());
        Assert.Single(workflow.Store.AiRuns, run => run.Workflow == "StructuredOutputRepair");
        Assert.Equal(VideoProjectStatus.ResearchReady, fixture.VideoProject.Status);
    }

    [Fact]
    public async Task Failed_regeneration_keeps_previous_version_and_recovers_ready_state()
    {
        var fixture = new OutlineTestFixture();
        var provider = new QueueProvider(ValidResult(fixture.SupportedClaim.Id), ValidResult(Guid.NewGuid()));
        var options = new OutlineOptions { MaxGenerationRetries = 0 };
        var workflow = CreateWorkflow(fixture, provider, options);
        await workflow.Queue.HandleAsync(fixture.Project.Id, fixture.VideoProject.Id, CancellationToken.None);
        await workflow.Processor.ProcessNextAsync(CancellationToken.None);
        var firstOutline = Assert.Single(workflow.Store.Outlines);

        await workflow.Queue.HandleAsync(fixture.Project.Id, fixture.VideoProject.Id, CancellationToken.None);
        await workflow.Processor.ProcessNextAsync(CancellationToken.None);

        Assert.Same(firstOutline, Assert.Single(workflow.Store.Outlines));
        Assert.Equal(VideoProjectStatus.OutlineReady, fixture.VideoProject.Status);
        Assert.Equal(JobStatus.Failed, workflow.Store.Jobs[^1].Status);
        Assert.Contains(workflow.Store.AiRuns, run => run.Status == AiRunStatus.Failed);
    }

    [Theory]
    [InlineData(ExternalServiceFailure.Transient)]
    [InlineData(ExternalServiceFailure.QuotaExceeded)]
    public async Task Transient_provider_failures_schedule_only_a_bounded_job_retry(ExternalServiceFailure failure)
    {
        var fixture = new OutlineTestFixture();
        var provider = new QueueProvider(new ExternalServiceException("Provider unavailable.", failure));
        var workflow = CreateWorkflow(fixture, provider, new OutlineOptions { MaxJobRetries = 1 });

        await workflow.Queue.HandleAsync(fixture.Project.Id, fixture.VideoProject.Id, CancellationToken.None);
        await workflow.Processor.ProcessNextAsync(CancellationToken.None);

        var job = Assert.Single(workflow.Store.Jobs);
        Assert.Equal(JobStatus.Retrying, job.Status);
        Assert.Equal(1, job.RetryCount);
        Assert.Equal(VideoProjectStatus.OutlineGenerating, fixture.VideoProject.Status);
        Assert.Empty(workflow.Store.Outlines);
    }

    [Fact]
    public async Task Permanent_provider_failure_does_not_retry_and_restores_research_ready_state()
    {
        var fixture = new OutlineTestFixture();
        var provider = new QueueProvider(new ExternalServiceException("Provider credentials are invalid.",
            ExternalServiceFailure.Authentication));
        var workflow = CreateWorkflow(fixture, provider);

        await workflow.Queue.HandleAsync(fixture.Project.Id, fixture.VideoProject.Id, CancellationToken.None);
        await workflow.Processor.ProcessNextAsync(CancellationToken.None);

        Assert.Equal(JobStatus.Failed, Assert.Single(workflow.Store.Jobs).Status);
        Assert.Equal(VideoProjectStatus.ResearchReady, fixture.VideoProject.Status);
        Assert.Empty(workflow.Store.Outlines);
    }

    [Fact]
    public async Task Stale_ready_outline_cannot_be_approved()
    {
        var fixture = new OutlineTestFixture();
        var provider = new QueueProvider(ValidResult(fixture.SupportedClaim.Id));
        var workflow = CreateWorkflow(fixture, provider);
        await workflow.Queue.HandleAsync(fixture.Project.Id, fixture.VideoProject.Id, CancellationToken.None);
        await workflow.Processor.ProcessNextAsync(CancellationToken.None);
        fixture.VideoProject.UpdateExecutionDetails("A materially changed title", null, DateTimeOffset.UtcNow.AddMinutes(1));

        var error = await Assert.ThrowsAsync<ApplicationValidationException>(() => workflow.Approve.HandleAsync(
            fixture.Project.Id, fixture.VideoProject.Id, workflow.Store.Outlines.Single().Id, CancellationToken.None));

        Assert.Contains("outdated", error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(VideoOutlineStatus.Ready, workflow.Store.Outlines.Single().Status);
    }

    private static Workflow CreateWorkflow(OutlineTestFixture fixture, QueueProvider provider, OutlineOptions? options = null)
    {
        options ??= new OutlineOptions();
        var store = new OutlineStore(fixture);
        var builder = new OutlineGenerationContextBuilder(options);
        var validator = new OutlineValidator(options);
        var processor = new VideoOutlineJobProcessor(store, provider, new FakeResolver(), builder, validator, options,
            TimeProvider.System, NullLogger<VideoOutlineJobProcessor>.Instance);
        return new(store, new RunVideoOutlineHandler(store, builder, options, TimeProvider.System), processor,
            new ApproveVideoOutlineHandler(store, builder, TimeProvider.System));
    }

    private static OutlineGenerationResult ValidResult(Guid claimId) => new(
        new(OutlineStructureType.Explainer, "What made ownership costly?", "Prestige carried recurring obligations.",
            "Open on visible prestige versus hidden obligations.",
            "Move from necessary context through the recurring-cost mechanism to synthesis.",
            "The viewer understands ownership as continuing economic obligations.", "Build steadily toward the synthesis."),
        new("Preserves the packaging premise and keeps the storytelling control comparable.", []),
        [
            new(1, "The contradiction", OutlineSectionPurpose.Hook, "Frame the central question.",
                "Introduce the evidence-backed contradiction without finished narration.", "What did ownership really require?",
                "Move to the minimum historical context.", [], [], [], 45),
            Section(2, OutlineSectionPurpose.Context, claimId),
            new(3, "Reset the question", OutlineSectionPurpose.PatternInterrupt,
                "Refresh attention by reframing the open question.",
                "Plan a brief contrast or question using only already established context.", "What remains unexplained?",
                "Return to the evidence-backed mechanism.", [], [], [], 20),
            Section(4, OutlineSectionPurpose.Explanation, claimId),
            Section(5, OutlineSectionPurpose.Conclusion, claimId),
            new(6, "Next step", OutlineSectionPurpose.CTA, "Close with a planning-level next-viewer action.",
                "Reserve a concise CTA intent without final spoken wording.", null, null, [], [], [], 20),
        ]);

    private static OutlineSectionResult Section(int sequence, OutlineSectionPurpose purpose, Guid claimId) =>
        new(sequence, $"Section {sequence}", purpose, "Explain the referenced evidence's narrative role.",
            "Use only the referenced claim and preserve its qualification.", "How does this affect the premise?",
            "Carry the open question forward.", [new(claimId, OutlineClaimUsageRole.Supporting)], [], [], 60);

    private sealed record Workflow(OutlineStore Store, RunVideoOutlineHandler Queue,
        VideoOutlineJobProcessor Processor, ApproveVideoOutlineHandler Approve);

    private sealed class FakeResolver : IAiModelResolver
    {
        public ResolvedAiModel Resolve(AiModelProfile profile) => new(profile, "fake", $"{profile.ToString().ToLowerInvariant()}-model", 30, 8_000);
    }

    private sealed class QueueProvider(params object[] results) : ILlmProvider
    {
        private readonly Queue<object> _results = new(results);
        public List<LlmRequest> Requests { get; } = [];

        public Task<LlmResult<T>> GenerateStructuredAsync<T>(LlmRequest request, CancellationToken cancellationToken)
        {
            Requests.Add(request);
            var next = _results.Dequeue();
            if (next is Exception exception) return Task.FromException<LlmResult<T>>(exception);
            return Task.FromResult(new LlmResult<T>((T)next, "fake", request.ResolvedModel?.Model ?? "model", 100, 50, "{}"));
        }
    }

    private sealed class OutlineStore(OutlineTestFixture fixture) : IYoutubeAiFactoryStore
    {
        public List<Job> Jobs { get; } = [];
        public List<AiRun> AiRuns { get; } = [];
        public List<VideoOutline> Outlines { get; } = [];
        private readonly List<VideoOutlineSection> _sections = [];
        private readonly List<VideoOutlineSectionClaim> _claims = [];
        private readonly List<VideoOutlineSectionConflict> _conflicts = [];
        private readonly List<VideoOutlineSectionGap> _gaps = [];

        public Task<Project?> GetProjectAsync(Guid projectId, CancellationToken cancellationToken) =>
            Task.FromResult<Project?>(projectId == fixture.Project.Id ? fixture.Project : null);
        public Task<bool> ProjectExistsAsync(Guid projectId, CancellationToken cancellationToken) =>
            Task.FromResult(projectId == fixture.Project.Id);
        public Task<IReadOnlyList<Project>> ListProjectsAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Project>>([fixture.Project]);
        public void AddProject(Project project) { }
        public Task<IReadOnlyList<CompetitorChannel>> ListCompetitorsAsync(Guid projectId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<CompetitorChannel>>([]);
        public Task<CompetitorChannel?> GetCompetitorAsync(Guid projectId, Guid competitorId, bool forUpdate, CancellationToken cancellationToken) =>
            Task.FromResult<CompetitorChannel?>(null);
        public Task<CompetitorChannel?> FindCompetitorByYoutubeChannelIdAsync(Guid projectId, string youtubeChannelId, CancellationToken cancellationToken) =>
            Task.FromResult<CompetitorChannel?>(null);
        public void AddCompetitor(CompetitorChannel competitor) { }
        public Task<VideoProject?> GetVideoProjectAsync(Guid projectId, Guid videoProjectId, bool forUpdate, CancellationToken cancellationToken) =>
            Task.FromResult<VideoProject?>(projectId == fixture.Project.Id && videoProjectId == fixture.VideoProject.Id ? fixture.VideoProject : null);
        public Task<VideoProjectSource?> GetVideoProjectSourceAsync(Guid projectId, Guid pilotId, Guid pilotVideoId, CancellationToken cancellationToken) =>
            Task.FromResult<VideoProjectSource?>(projectId == fixture.Project.Id && pilotId == fixture.Pilot.Id && pilotVideoId == fixture.PilotVideo.Id ? fixture.Source : null);
        public Task<ResearchReportWithDetails?> GetLatestResearchReportAsync(Guid projectId, Guid videoProjectId, CancellationToken cancellationToken) =>
            Task.FromResult<ResearchReportWithDetails?>(projectId == fixture.Project.Id && videoProjectId == fixture.VideoProject.Id ? fixture.Research : null);
        public Task<ResearchReportWithDetails?> GetResearchReportAsync(Guid projectId, Guid videoProjectId, Guid reportId, CancellationToken cancellationToken) =>
            Task.FromResult<ResearchReportWithDetails?>(projectId == fixture.Project.Id && videoProjectId == fixture.VideoProject.Id && reportId == fixture.Report.Id ? fixture.Research : null);

        public Task<Job?> GetActiveVideoOutlineJobAsync(Guid projectId, Guid videoProjectId, CancellationToken cancellationToken) =>
            Task.FromResult<Job?>(Jobs.LastOrDefault(job => job.ProjectId == projectId && job.VideoProjectId == videoProjectId &&
                job.Status is JobStatus.Queued or JobStatus.Running or JobStatus.Retrying));
        public Task<Job?> GetLatestVideoOutlineJobAsync(Guid projectId, Guid videoProjectId, CancellationToken cancellationToken) =>
            Task.FromResult<Job?>(Jobs.LastOrDefault(job => job.ProjectId == projectId && job.VideoProjectId == videoProjectId));
        public Task<Job> EnqueueVideoOutlineJobAsync(Job job, CancellationToken cancellationToken)
        {
            var active = Jobs.LastOrDefault(item => item.Status is JobStatus.Queued or JobStatus.Running or JobStatus.Retrying);
            if (active is not null) return Task.FromResult(active);
            Jobs.Add(job);
            return Task.FromResult(job);
        }
        public Task<Job?> TryClaimNextVideoOutlineJobAsync(DateTimeOffset now, DateTimeOffset staleRunningBefore, CancellationToken cancellationToken)
        {
            var job = Jobs.FirstOrDefault(item => item.Status is JobStatus.Queued or JobStatus.Retrying && item.AvailableAt <= now);
            job?.Start(now);
            return Task.FromResult<Job?>(job);
        }
        public Task RequeueVideoOutlineJobAsync(Guid jobId, CancellationToken cancellationToken)
        {
            Jobs.Single(item => item.Id == jobId).Requeue(DateTimeOffset.UtcNow);
            return Task.CompletedTask;
        }
        public Task FailVideoOutlineJobAsync(Guid jobId, Guid? aiRunId, string reason, bool retryable,
            DateTimeOffset failedAt, DateTimeOffset? retryAt, CancellationToken cancellationToken)
        {
            var job = Jobs.Single(item => item.Id == jobId);
            job.Fail(reason, retryable, failedAt, retryAt);
            if (job.Status == JobStatus.Failed && fixture.VideoProject.Status == VideoProjectStatus.OutlineGenerating)
            {
                var payload = JsonSerializer.Deserialize<OutlineJobPayload>(job.Payload, JsonOptions)!;
                fixture.VideoProject.TransitionTo(Enum.Parse<VideoProjectStatus>(payload.ReturnStatus), failedAt);
            }
            return Task.CompletedTask;
        }
        public Task<int> GetNextVideoOutlineVersionAsync(Guid projectId, Guid videoProjectId, CancellationToken cancellationToken) =>
            Task.FromResult(Outlines.Count == 0 ? 1 : Outlines.Max(item => item.Version) + 1);
        public Task<VideoOutlineWithDetails?> GetLatestVideoOutlineAsync(Guid projectId, Guid videoProjectId, bool forUpdate, CancellationToken cancellationToken) =>
            Task.FromResult(Details(Outlines.Where(item => item.ProjectId == projectId && item.VideoProjectId == videoProjectId)
                .OrderByDescending(item => item.Version).FirstOrDefault()));
        public Task<VideoOutlineWithDetails?> GetVideoOutlineAsync(Guid projectId, Guid videoProjectId, Guid outlineId, bool forUpdate, CancellationToken cancellationToken) =>
            Task.FromResult(Details(Outlines.SingleOrDefault(item => item.ProjectId == projectId && item.VideoProjectId == videoProjectId && item.Id == outlineId)));
        public Task<VideoOutlineWithDetails?> GetApprovedVideoOutlineAsync(Guid projectId, Guid videoProjectId, CancellationToken cancellationToken) =>
            Task.FromResult(Details(Outlines.SingleOrDefault(item => item.ProjectId == projectId && item.VideoProjectId == videoProjectId && item.Status == VideoOutlineStatus.Approved)));
        public Task<IReadOnlyList<VideoOutline>> ListVideoOutlinesAsync(Guid projectId, Guid videoProjectId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<VideoOutline>>(Outlines.Where(item => item.ProjectId == projectId && item.VideoProjectId == videoProjectId).ToArray());
        public void AddVideoOutline(VideoOutline outline) => Outlines.Add(outline);
        public void AddVideoOutlineSection(VideoOutlineSection section) => _sections.Add(section);
        public void AddVideoOutlineSectionClaim(VideoOutlineSectionClaim claim) => _claims.Add(claim);
        public void AddVideoOutlineSectionConflict(VideoOutlineSectionConflict conflict) => _conflicts.Add(conflict);
        public void AddVideoOutlineSectionGap(VideoOutlineSectionGap gap) => _gaps.Add(gap);
        public void AddAiRun(AiRun aiRun) => AiRuns.Add(aiRun);
        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        private VideoOutlineWithDetails? Details(VideoOutline? outline) => outline is null ? null : new(outline,
            _sections.Where(item => item.VideoOutlineId == outline.Id).ToArray(),
            _claims.Where(item => _sections.Any(section => section.VideoOutlineId == outline.Id && section.Id == item.OutlineSectionId)).ToArray(),
            _conflicts.Where(item => _sections.Any(section => section.VideoOutlineId == outline.Id && section.Id == item.OutlineSectionId)).ToArray(),
            _gaps.Where(item => _sections.Any(section => section.VideoOutlineId == outline.Id && section.Id == item.OutlineSectionId)).ToArray());
    }
}
