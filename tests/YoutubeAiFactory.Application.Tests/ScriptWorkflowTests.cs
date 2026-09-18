using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging.Abstractions;
using YoutubeAiFactory.Application.AI;
using YoutubeAiFactory.Application.Common;
using YoutubeAiFactory.Application.Outlines;
using YoutubeAiFactory.Application.Persistence;
using YoutubeAiFactory.Application.Research;
using YoutubeAiFactory.Application.Scripts;
using YoutubeAiFactory.Application.Videos;
using YoutubeAiFactory.Domain.AI;
using YoutubeAiFactory.Domain.Competitors;
using YoutubeAiFactory.Domain.Jobs;
using YoutubeAiFactory.Domain.Outlines;
using YoutubeAiFactory.Domain.Projects;
using YoutubeAiFactory.Domain.Scripts;
using YoutubeAiFactory.Domain.Videos;

namespace YoutubeAiFactory.Application.Tests;

public sealed class ScriptWorkflowTests
{
    [Fact]
    public async Task Generates_a_versioned_script_then_runs_a_separate_grounding_audit()
    {
        var fixture = new ScriptTestFixture();
        var provider = new QueueProvider(fixture.ValidResult(), PassedAudit());
        var workflow = CreateWorkflow(fixture, provider);

        var queued = await workflow.Queue.HandleAsync(fixture.Base.Project.Id, fixture.Base.VideoProject.Id,
            CancellationToken.None);
        var duplicate = await workflow.Queue.HandleAsync(fixture.Base.Project.Id, fixture.Base.VideoProject.Id,
            CancellationToken.None);
        await workflow.Processor.ProcessNextAsync(CancellationToken.None);

        Assert.False(queued.Existing);
        Assert.True(duplicate.Existing);
        Assert.Equal(queued.JobId, duplicate.JobId);
        var script = Assert.Single(workflow.Store.Scripts);
        Assert.Equal(1, script.Version);
        Assert.Equal(ScriptGroundingStatus.Passed, script.GroundingStatus);
        Assert.Equal(VideoProjectStatus.ScriptReady, fixture.Base.VideoProject.Status);
        Assert.Equal([AiModelProfile.Premium, AiModelProfile.Reasoning],
            provider.Requests.Select(item => item.ModelProfile).ToArray());
        Assert.Equal(2, workflow.Store.AiRuns.Count);
    }

    [Fact]
    public async Task Failed_grounding_is_corrected_once_and_reaudited_before_persistence()
    {
        var fixture = new ScriptTestFixture();
        var issue = new ScriptGroundingIssueResult(1, 1, ScriptGroundingIssueType.ClaimOverstatement,
            ScriptGroundingIssueSeverity.Error, "An overstatement.",
            [fixture.Context.Sections[0].Claims.Single().Id], "The wording exceeds the claim.");
        var provider = new QueueProvider(fixture.ValidResult(),
            new ScriptGroundingAuditResult(ScriptGroundingStatus.Failed, [issue]),
            fixture.ValidResult(), PassedAudit());
        var workflow = CreateWorkflow(fixture, provider);

        await workflow.Queue.HandleAsync(fixture.Base.Project.Id, fixture.Base.VideoProject.Id,
            CancellationToken.None);
        await workflow.Processor.ProcessNextAsync(CancellationToken.None);

        var script = Assert.Single(workflow.Store.Scripts);
        Assert.Equal([AiModelProfile.Premium, AiModelProfile.Reasoning, AiModelProfile.Premium,
                AiModelProfile.Reasoning],
            provider.Requests.Select(item => item.ModelProfile).ToArray());
        var correctionRun = Assert.Single(workflow.Store.AiRuns, run =>
            run.Workflow == "ScriptGroundingCorrection");
        Assert.Equal(correctionRun.Id, script.GenerationAiRunId);
        Assert.Equal("premium-model", script.Model);
    }

    [Fact]
    public async Task Edit_requires_revalidation_before_latest_script_can_be_approved()
    {
        var fixture = new ScriptTestFixture();
        var provider = new QueueProvider(fixture.ValidResult(), PassedAudit(), PassedAudit());
        var workflow = CreateWorkflow(fixture, provider);
        await workflow.Queue.HandleAsync(fixture.Base.Project.Id, fixture.Base.VideoProject.Id,
            CancellationToken.None);
        await workflow.Processor.ProcessNextAsync(CancellationToken.None);
        var details = workflow.Store.Details(Assert.Single(workflow.Store.Scripts))!;

        await workflow.Edit.HandleAsync(fixture.Base.Project.Id, fixture.Base.VideoProject.Id, details.Script.Id,
            new UpdateVideoScriptRequest([new(details.Blocks[0].Id,
                details.Blocks[0].Text + " Carefully qualified.")]), CancellationToken.None);
        Assert.Equal(ScriptGroundingStatus.Pending, details.Script.GroundingStatus);
        await Assert.ThrowsAsync<ApplicationValidationException>(() => workflow.Approve.HandleAsync(
            fixture.Base.Project.Id, fixture.Base.VideoProject.Id, details.Script.Id, CancellationToken.None));

        await workflow.Validate.HandleAsync(fixture.Base.Project.Id, fixture.Base.VideoProject.Id,
            details.Script.Id, CancellationToken.None);
        await workflow.Processor.ProcessNextAsync(CancellationToken.None);
        var approved = await workflow.Approve.HandleAsync(fixture.Base.Project.Id, fixture.Base.VideoProject.Id,
            details.Script.Id, CancellationToken.None);

        Assert.Equal("Approved", approved.Status);
        Assert.Equal(VideoProjectStatus.ScriptApproved, fixture.Base.VideoProject.Status);
    }

    [Fact]
    public async Task Malformed_generation_output_uses_fast_repair_before_grounding()
    {
        var fixture = new ScriptTestFixture();
        var provider = new QueueProvider(
            new StructuredOutputException("Malformed Script JSON.", rawOutput: "{broken"),
            fixture.ValidResult(), PassedAudit());
        var workflow = CreateWorkflow(fixture, provider);

        await workflow.Queue.HandleAsync(fixture.Base.Project.Id, fixture.Base.VideoProject.Id,
            CancellationToken.None);
        await workflow.Processor.ProcessNextAsync(CancellationToken.None);

        var script = Assert.Single(workflow.Store.Scripts);
        Assert.Equal([AiModelProfile.Premium, AiModelProfile.Fast, AiModelProfile.Reasoning],
            provider.Requests.Select(item => item.ModelProfile).ToArray());
        var repairRun = Assert.Single(workflow.Store.AiRuns, run =>
            run.Workflow == "StructuredOutputRepair" && run.Status == AiRunStatus.Succeeded);
        Assert.Equal(repairRun.Id, script.GenerationAiRunId);
        Assert.Equal("fast-model", script.Model);
    }

    [Fact]
    public async Task Repeated_grounding_failure_is_bounded_and_creates_no_script_version()
    {
        var fixture = new ScriptTestFixture();
        var issue = new ScriptGroundingIssueResult(1, 1, ScriptGroundingIssueType.FabricatedQuote,
            ScriptGroundingIssueSeverity.Error, "A fabricated quote.",
            [fixture.Context.Sections[0].Claims.Single().Id], "No supplied evidence supports this quote.");
        var failed = new ScriptGroundingAuditResult(ScriptGroundingStatus.Failed, [issue]);
        var provider = new QueueProvider(fixture.ValidResult(), failed, fixture.ValidResult(), failed);
        var workflow = CreateWorkflow(fixture, provider);

        await workflow.Queue.HandleAsync(fixture.Base.Project.Id, fixture.Base.VideoProject.Id,
            CancellationToken.None);
        await workflow.Processor.ProcessNextAsync(CancellationToken.None);

        Assert.Empty(workflow.Store.Scripts);
        Assert.Equal(JobStatus.Failed, Assert.Single(workflow.Store.Jobs).Status);
        Assert.Equal(VideoProjectStatus.OutlineApproved, fixture.Base.VideoProject.Status);
        Assert.Equal(4, provider.Requests.Count);
    }

    [Fact]
    public async Task Fabricated_claim_ids_exhaust_bounded_generation_and_create_no_artifact()
    {
        var fixture = new ScriptTestFixture();
        var valid = fixture.ValidResult();
        var invalid = valid with
        {
            Sections = valid.Sections.Select((section, index) => index == 0
                ? section with
                {
                    Blocks = [section.Blocks[0] with { ClaimIds = [Guid.NewGuid()] }],
                }
                : section).ToArray(),
        };
        var provider = new QueueProvider(invalid, invalid);
        var workflow = CreateWorkflow(fixture, provider);

        await workflow.Queue.HandleAsync(fixture.Base.Project.Id, fixture.Base.VideoProject.Id,
            CancellationToken.None);
        await workflow.Processor.ProcessNextAsync(CancellationToken.None);

        Assert.Empty(workflow.Store.Scripts);
        Assert.Equal(JobStatus.Failed, Assert.Single(workflow.Store.Jobs).Status);
        Assert.Equal(VideoProjectStatus.OutlineApproved, fixture.Base.VideoProject.Status);
        Assert.Equal(AiRunStatus.Failed, Assert.Single(workflow.Store.AiRuns).Status);
    }

    private static ScriptGroundingAuditResult PassedAudit() =>
        new(ScriptGroundingStatus.Passed, []);

    private static Workflow CreateWorkflow(ScriptTestFixture fixture, QueueProvider provider)
    {
        var store = new ScriptStore(fixture);
        var builder = new ScriptGenerationContextBuilder(fixture.Options);
        var processor = new VideoScriptJobProcessor(store, provider, new FakeResolver(), builder,
            new ScriptValidator(fixture.Options), fixture.Options, TimeProvider.System,
            NullLogger<VideoScriptJobProcessor>.Instance);
        return new(store, new RunVideoScriptHandler(store, builder, fixture.Options, TimeProvider.System), processor,
            new UpdateVideoScriptHandler(store, builder, fixture.Options, TimeProvider.System),
            new ValidateVideoScriptHandler(store, builder, fixture.Options, TimeProvider.System),
            new ApproveVideoScriptHandler(store, builder, TimeProvider.System));
    }

    private sealed record Workflow(ScriptStore Store, RunVideoScriptHandler Queue,
        VideoScriptJobProcessor Processor, UpdateVideoScriptHandler Edit,
        ValidateVideoScriptHandler Validate, ApproveVideoScriptHandler Approve);

    private sealed class FakeResolver : IAiModelResolver
    {
        public ResolvedAiModel Resolve(AiModelProfile profile) =>
            new(profile, "fake", $"{profile.ToString().ToLowerInvariant()}-model", 30, 8_000);
    }

    private sealed class QueueProvider(params object[] results) : ILlmProvider
    {
        private readonly Queue<object> _results = new(results);
        public List<LlmRequest> Requests { get; } = [];

        public Task<LlmResult<T>> GenerateStructuredAsync<T>(LlmRequest request,
            CancellationToken cancellationToken)
        {
            Requests.Add(request);
            var next = _results.Dequeue();
            if (next is Exception exception) return Task.FromException<LlmResult<T>>(exception);
            return Task.FromResult(new LlmResult<T>((T)next, "fake", request.ResolvedModel?.Model ?? "model",
                100, 50, "{}"));
        }
    }

    private sealed class ScriptStore(ScriptTestFixture fixture) : IYoutubeAiFactoryStore
    {
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
        {
            Converters = { new JsonStringEnumConverter() },
        };
        public List<Job> Jobs { get; } = [];
        public List<AiRun> AiRuns { get; } = [];
        public List<VideoScript> Scripts { get; } = [];
        private readonly List<VideoScriptSection> _sections = [];
        private readonly List<VideoScriptBlock> _blocks = [];
        private readonly List<VideoScriptBlockClaim> _claims = [];
        private readonly List<VideoScriptBlockConflict> _conflicts = [];

        public Task<Project?> GetProjectAsync(Guid projectId, CancellationToken cancellationToken) =>
            Task.FromResult<Project?>(projectId == fixture.Base.Project.Id ? fixture.Base.Project : null);
        public Task<bool> ProjectExistsAsync(Guid projectId, CancellationToken cancellationToken) =>
            Task.FromResult(projectId == fixture.Base.Project.Id);
        public Task<IReadOnlyList<Project>> ListProjectsAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Project>>([fixture.Base.Project]);
        public void AddProject(Project project) { }
        public Task<IReadOnlyList<CompetitorChannel>> ListCompetitorsAsync(Guid projectId,
            CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<CompetitorChannel>>([]);
        public Task<CompetitorChannel?> GetCompetitorAsync(Guid projectId, Guid competitorId, bool forUpdate,
            CancellationToken cancellationToken) => Task.FromResult<CompetitorChannel?>(null);
        public Task<CompetitorChannel?> FindCompetitorByYoutubeChannelIdAsync(Guid projectId,
            string youtubeChannelId, CancellationToken cancellationToken) => Task.FromResult<CompetitorChannel?>(null);
        public void AddCompetitor(CompetitorChannel competitor) { }
        public Task<VideoProject?> GetVideoProjectAsync(Guid projectId, Guid videoProjectId, bool forUpdate,
            CancellationToken cancellationToken) => Task.FromResult<VideoProject?>(projectId == fixture.Base.Project.Id &&
                videoProjectId == fixture.Base.VideoProject.Id ? fixture.Base.VideoProject : null);
        public Task<VideoProjectSource?> GetVideoProjectSourceAsync(Guid projectId, Guid pilotId,
            Guid pilotVideoId, CancellationToken cancellationToken) => Task.FromResult<VideoProjectSource?>(
                projectId == fixture.Base.Project.Id && pilotId == fixture.Base.Pilot.Id &&
                pilotVideoId == fixture.Base.PilotVideo.Id ? fixture.Base.Source : null);
        public Task<ResearchReportWithDetails?> GetLatestResearchReportAsync(Guid projectId, Guid videoProjectId,
            CancellationToken cancellationToken) => Task.FromResult<ResearchReportWithDetails?>(
                projectId == fixture.Base.Project.Id && videoProjectId == fixture.Base.VideoProject.Id
                    ? fixture.Base.Research : null);
        public Task<ResearchReportWithDetails?> GetResearchReportAsync(Guid projectId, Guid videoProjectId,
            Guid reportId, CancellationToken cancellationToken) => Task.FromResult<ResearchReportWithDetails?>(
                reportId == fixture.Base.Report.Id ? fixture.Base.Research : null);
        public Task<VideoOutlineWithDetails?> GetApprovedVideoOutlineAsync(Guid projectId, Guid videoProjectId,
            CancellationToken cancellationToken) => Task.FromResult<VideoOutlineWithDetails?>(
                projectId == fixture.Base.Project.Id && videoProjectId == fixture.Base.VideoProject.Id
                    ? fixture.Details : null);

        public Task<Job?> GetActiveVideoScriptJobAsync(Guid projectId, Guid videoProjectId,
            CancellationToken cancellationToken) => Task.FromResult<Job?>(Jobs.LastOrDefault(job =>
                job.ProjectId == projectId && job.VideoProjectId == videoProjectId &&
                job.Status is JobStatus.Queued or JobStatus.Running or JobStatus.Retrying));
        public Task<Job?> GetLatestVideoScriptJobAsync(Guid projectId, Guid videoProjectId,
            CancellationToken cancellationToken) => Task.FromResult<Job?>(Jobs.LastOrDefault());
        public Task<Job> EnqueueVideoScriptJobAsync(Job job, CancellationToken cancellationToken)
        {
            var active = Jobs.LastOrDefault(item =>
                item.Status is JobStatus.Queued or JobStatus.Running or JobStatus.Retrying);
            if (active is not null) return Task.FromResult(active);
            Jobs.Add(job);
            return Task.FromResult(job);
        }
        public Task<Job?> TryClaimNextVideoScriptJobAsync(DateTimeOffset now, DateTimeOffset staleRunningBefore,
            CancellationToken cancellationToken)
        {
            var job = Jobs.FirstOrDefault(item =>
                item.Status is JobStatus.Queued or JobStatus.Retrying && item.AvailableAt <= now);
            job?.Start(now);
            return Task.FromResult<Job?>(job);
        }
        public Task RequeueVideoScriptJobAsync(Guid jobId, CancellationToken cancellationToken)
        {
            Jobs.Single(item => item.Id == jobId).Requeue(DateTimeOffset.UtcNow);
            return Task.CompletedTask;
        }
        public Task FailVideoScriptJobAsync(Guid jobId, Guid? aiRunId, string reason, bool retryable,
            DateTimeOffset failedAt, DateTimeOffset? retryAt, CancellationToken cancellationToken)
        {
            var job = Jobs.Single(item => item.Id == jobId);
            job.Fail(reason, retryable, failedAt, retryAt);
            if (job.Status == JobStatus.Failed && fixture.Base.VideoProject.Status == VideoProjectStatus.ScriptGenerating)
            {
                var payload = JsonSerializer.Deserialize<ScriptJobPayload>(job.Payload, JsonOptions)!;
                fixture.Base.VideoProject.TransitionTo(Enum.Parse<VideoProjectStatus>(payload.ReturnStatus), failedAt);
            }
            return Task.CompletedTask;
        }
        public Task<int> GetNextVideoScriptVersionAsync(Guid projectId, Guid videoProjectId,
            CancellationToken cancellationToken) => Task.FromResult(Scripts.Count == 0 ? 1 : Scripts.Max(x => x.Version) + 1);
        public Task<VideoScriptWithDetails?> GetLatestVideoScriptAsync(Guid projectId, Guid videoProjectId,
            bool forUpdate, CancellationToken cancellationToken) => Task.FromResult(Details(Scripts
                .Where(item => item.ProjectId == projectId && item.VideoProjectId == videoProjectId)
                .OrderByDescending(item => item.Version).FirstOrDefault()));
        public Task<VideoScriptWithDetails?> GetVideoScriptAsync(Guid projectId, Guid videoProjectId,
            Guid scriptId, bool forUpdate, CancellationToken cancellationToken) => Task.FromResult(Details(Scripts
                .SingleOrDefault(item => item.ProjectId == projectId && item.VideoProjectId == videoProjectId &&
                    item.Id == scriptId)));
        public Task<IReadOnlyList<VideoScript>> ListVideoScriptsAsync(Guid projectId, Guid videoProjectId,
            CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<VideoScript>>(Scripts.ToArray());
        public void AddVideoScript(VideoScript script) => Scripts.Add(script);
        public void AddVideoScriptSection(VideoScriptSection section) => _sections.Add(section);
        public void AddVideoScriptBlock(VideoScriptBlock block) => _blocks.Add(block);
        public void AddVideoScriptBlockClaim(VideoScriptBlockClaim claim) => _claims.Add(claim);
        public void AddVideoScriptBlockConflict(VideoScriptBlockConflict conflict) => _conflicts.Add(conflict);
        public void AddAiRun(AiRun aiRun) => AiRuns.Add(aiRun);
        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public VideoScriptWithDetails? Details(VideoScript? script)
        {
            if (script is null) return null;
            var sections = _sections.Where(item => item.VideoScriptId == script.Id).ToArray();
            var sectionIds = sections.Select(item => item.Id).ToHashSet();
            var blocks = _blocks.Where(item => sectionIds.Contains(item.VideoScriptSectionId)).ToArray();
            var blockIds = blocks.Select(item => item.Id).ToHashSet();
            return new(script, sections, blocks, _claims.Where(item => blockIds.Contains(item.ScriptBlockId)).ToArray(),
                _conflicts.Where(item => blockIds.Contains(item.ScriptBlockId)).ToArray());
        }
    }
}
