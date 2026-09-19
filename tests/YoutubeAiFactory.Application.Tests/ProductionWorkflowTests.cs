using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging.Abstractions;
using YoutubeAiFactory.Application.AI;
using YoutubeAiFactory.Application.Common;
using YoutubeAiFactory.Application.Outlines;
using YoutubeAiFactory.Application.Persistence;
using YoutubeAiFactory.Application.Production;
using YoutubeAiFactory.Application.Research;
using YoutubeAiFactory.Application.Scripts;
using YoutubeAiFactory.Application.Videos;
using YoutubeAiFactory.Domain.AI;
using YoutubeAiFactory.Domain.Competitors;
using YoutubeAiFactory.Domain.Jobs;
using YoutubeAiFactory.Domain.Production;
using YoutubeAiFactory.Domain.Projects;
using YoutubeAiFactory.Domain.Scripts;
using YoutubeAiFactory.Domain.Videos;

namespace YoutubeAiFactory.Application.Tests;

public sealed class ProductionWorkflowTests
{
    [Fact]
    public async Task Generation_is_idempotent_grounded_and_persists_exact_mapping_and_duration()
    {
        var fixture = new ProductionTestFixture();
        var provider = new QueueProvider(fixture.ValidResult(), PassedAudit());
        var workflow = CreateWorkflow(fixture, provider);

        var first = await workflow.Queue.HandleAsync(
            fixture.ProjectId,
            fixture.VideoProjectId,
            default
        );
        var duplicate = await workflow.Queue.HandleAsync(
            fixture.ProjectId,
            fixture.VideoProjectId,
            default
        );
        await workflow.Processor.ProcessNextAsync(default);

        var package = Assert.Single(workflow.Store.Packages);
        var details = workflow.Store.Details(package)!;
        Assert.Equal(first.JobId, duplicate.JobId);
        Assert.True(duplicate.Existing);
        Assert.Equal(ProductionGroundingStatus.Passed, package.GroundingStatus);
        Assert.Equal(
            fixture.Context.EstimatedDurationSeconds,
            details.Scenes.Sum(x => x.EstimatedDurationSeconds)
        );
        Assert.Equal(
            fixture.Context.Blocks.Select(x => x.Id),
            details
                .SceneBlocks.OrderBy(x =>
                    details.Scenes.Single(s => s.Id == x.ProductionSceneId).Sequence
                )
                .Select(x => x.ScriptBlockId)
        );
        Assert.All(
            provider.Requests,
            request => Assert.Equal(AiModelProfile.Reasoning, request.ModelProfile)
        );
        Assert.Equal(VideoProjectStatus.Packaging, fixture.ScriptFixture.Base.VideoProject.Status);
    }

    [Fact]
    public async Task Failed_grounding_receives_one_reasoning_correction_and_reaudit()
    {
        var fixture = new ProductionTestFixture();
        var corrected = fixture.ValidResult() with
        {
            VisualDirection = "Corrected evidence-safe direction",
        };
        var provider = new QueueProvider(
            fixture.ValidResult(),
            fixture.FailedAudit(),
            corrected,
            PassedAudit()
        );
        var workflow = CreateWorkflow(fixture, provider);

        await workflow.Queue.HandleAsync(fixture.ProjectId, fixture.VideoProjectId, default);
        await workflow.Processor.ProcessNextAsync(default);

        Assert.Equal(
            "Corrected evidence-safe direction",
            Assert.Single(workflow.Store.Packages).VisualDirection
        );
        Assert.Equal(
            [
                AiWorkflowProfiles.ProductionPackageGeneration,
                AiWorkflowProfiles.ProductionGroundingAudit,
                AiWorkflowProfiles.ProductionPackageCorrection,
                AiWorkflowProfiles.ProductionGroundingAudit,
            ],
            provider.Requests.Select(x => x.ModelProfile)
        );
    }

    [Fact]
    public async Task Structured_edit_requires_audit_only_revalidation_before_approval_and_export()
    {
        var fixture = new ProductionTestFixture();
        var provider = new QueueProvider(fixture.ValidResult(), PassedAudit(), PassedAudit());
        var workflow = CreateWorkflow(fixture, provider);
        await workflow.Queue.HandleAsync(fixture.ProjectId, fixture.VideoProjectId, default);
        await workflow.Processor.ProcessNextAsync(default);
        var details = workflow.Store.Details(Assert.Single(workflow.Store.Packages))!;

        var updated = await workflow.Edit.HandleAsync(
            fixture.ProjectId,
            fixture.VideoProjectId,
            details.Package.Id,
            EditRequest(details),
            default
        );
        Assert.Equal("Pending", updated.GroundingStatus);

        await workflow.Validate.HandleAsync(
            fixture.ProjectId,
            fixture.VideoProjectId,
            details.Package.Id,
            default
        );
        await workflow.Processor.ProcessNextAsync(default);
        var approved = await workflow.Approve.HandleAsync(
            fixture.ProjectId,
            fixture.VideoProjectId,
            details.Package.Id,
            default
        );
        var exported = await workflow.Export.HandleAsync(
            fixture.ProjectId,
            fixture.VideoProjectId,
            default
        );

        Assert.Equal("Approved", approved.Status);
        Assert.Equal(
            VideoProjectStatus.ProductionReady,
            fixture.ScriptFixture.Base.VideoProject.Status
        );
        Assert.Equal("production-package-export:v1", exported.SchemaVersion);
        Assert.Equal(fixture.ScriptDetails.Blocks[0].Text, exported.Scenes[0].Narration);
        Assert.Equal(3, provider.Requests.Count);
        Assert.Equal("production-grounding-audit", provider.Requests[^1].PromptKey);
    }

    [Fact]
    public async Task Terminal_initial_failure_restores_script_approved_and_creates_no_version()
    {
        var fixture = new ProductionTestFixture();
        var provider = new QueueProvider(
            new ExternalServiceException("Bad credentials", ExternalServiceFailure.Authentication)
        );
        var workflow = CreateWorkflow(fixture, provider);

        await workflow.Queue.HandleAsync(fixture.ProjectId, fixture.VideoProjectId, default);
        await workflow.Processor.ProcessNextAsync(default);

        Assert.Empty(workflow.Store.Packages);
        Assert.Equal(
            VideoProjectStatus.ScriptApproved,
            fixture.ScriptFixture.Base.VideoProject.Status
        );
        Assert.Equal(JobStatus.Failed, Assert.Single(workflow.Store.Jobs).Status);
    }

    private static ProductionGroundingAuditResult PassedAudit() =>
        new(ProductionGroundingStatus.Passed, []);

    private static UpdateProductionPackageRequest EditRequest(
        ProductionPackageWithDetails details
    ) =>
        new(
            "Updated documentary direction",
            details.Package.PacingDirection,
            details.Package.ColorDirection,
            details.Package.TypographyDirection,
            details.Package.AudioDirection,
            details.Package.ExperimentProductionNotes,
            details
                .Scenes.Select(x => new UpdateProductionSceneRequest(
                    x.Id,
                    x.NarrationSummary,
                    x.VisualStrategy,
                    x.TransitionIntent,
                    x.MusicBrief,
                    x.SoundEffectCue,
                    x.VoiceDirection
                ))
                .ToArray(),
            details
                .Shots.Select(x => new UpdateProductionShotRequest(
                    x.Id,
                    x.VisualDescription,
                    x.Composition,
                    x.MotionSuggestion,
                    x.Notes
                ))
                .ToArray(),
            details
                .Assets.Select(x => new UpdateProductionAssetRequest(
                    x.Id,
                    x.CreativeBrief,
                    x.GenerationPrompt,
                    x.SourceSearchBrief
                ))
                .ToArray(),
            details
                .OnScreenText.Select(x => new UpdateProductionOnScreenTextRequest(
                    x.Id,
                    x.Text,
                    x.TimingIntent
                ))
                .ToArray()
        );

    private static Workflow CreateWorkflow(ProductionTestFixture fixture, QueueProvider provider)
    {
        var options = new ProductionOptions { RunningJobLeaseSeconds = 30 };
        var store = new ProductionStore(fixture);
        var builder = new ProductionContextBuilder(options);
        var validator = new ProductionValidator(options);
        return new(
            store,
            new RunProductionPackageHandler(store, builder, options, TimeProvider.System),
            new ProductionPackageJobProcessor(
                store,
                provider,
                new FakeResolver(),
                builder,
                validator,
                options,
                TimeProvider.System,
                NullLogger<ProductionPackageJobProcessor>.Instance
            ),
            new UpdateProductionPackageHandler(store, TimeProvider.System),
            new ValidateProductionPackageHandler(store, builder, options, TimeProvider.System),
            new ApproveProductionPackageHandler(store, builder, validator, TimeProvider.System),
            new ExportProductionPackageHandler(store, TimeProvider.System)
        );
    }

    private sealed record Workflow(
        ProductionStore Store,
        RunProductionPackageHandler Queue,
        ProductionPackageJobProcessor Processor,
        UpdateProductionPackageHandler Edit,
        ValidateProductionPackageHandler Validate,
        ApproveProductionPackageHandler Approve,
        ExportProductionPackageHandler Export
    );

    private sealed class FakeResolver : IAiModelResolver
    {
        public ResolvedAiModel Resolve(AiModelProfile profile) =>
            new(profile, "fake", $"{profile.ToString().ToLowerInvariant()}-model", 30, 16_000);
    }

    private sealed class QueueProvider(params object[] results) : ILlmProvider
    {
        private readonly Queue<object> _results = new(results);
        public List<LlmRequest> Requests { get; } = [];

        public Task<LlmResult<T>> GenerateStructuredAsync<T>(
            LlmRequest request,
            CancellationToken cancellationToken
        )
        {
            Requests.Add(request);
            var next = _results.Dequeue();
            if (next is Exception exception)
                return Task.FromException<LlmResult<T>>(exception);
            return Task.FromResult(
                new LlmResult<T>(
                    (T)next,
                    "fake",
                    request.ResolvedModel?.Model ?? "model",
                    100,
                    50,
                    "{}"
                )
            );
        }
    }

    private sealed class ProductionStore(ProductionTestFixture fixture) : IYoutubeAiFactoryStore
    {
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
        {
            Converters = { new JsonStringEnumConverter() },
        };
        public List<Job> Jobs { get; } = [];
        public List<AiRun> AiRuns { get; } = [];
        public List<ProductionPackage> Packages { get; } = [];
        private readonly List<ProductionScene> _scenes = [];
        private readonly List<ProductionSceneScriptBlock> _sceneBlocks = [];
        private readonly List<ProductionShot> _shots = [];
        private readonly List<ProductionShotClaim> _shotClaims = [];
        private readonly List<ProductionAssetRequirement> _assets = [];
        private readonly List<ProductionAssetClaim> _assetClaims = [];
        private readonly List<ProductionOnScreenText> _texts = [];
        private readonly List<ProductionOnScreenTextClaim> _textClaims = [];

        public Task<bool> ProjectExistsAsync(Guid projectId, CancellationToken cancellationToken) =>
            Task.FromResult(projectId == fixture.ProjectId);

        public Task<Project?> GetProjectAsync(
            Guid projectId,
            CancellationToken cancellationToken
        ) =>
            Task.FromResult<Project?>(
                projectId == fixture.ProjectId ? fixture.ScriptFixture.Base.Project : null
            );

        public Task<IReadOnlyList<Project>> ListProjectsAsync(
            CancellationToken cancellationToken
        ) => Task.FromResult<IReadOnlyList<Project>>([fixture.ScriptFixture.Base.Project]);

        public void AddProject(Project project) { }

        public Task<IReadOnlyList<CompetitorChannel>> ListCompetitorsAsync(
            Guid projectId,
            CancellationToken cancellationToken
        ) => Task.FromResult<IReadOnlyList<CompetitorChannel>>([]);

        public Task<CompetitorChannel?> GetCompetitorAsync(
            Guid projectId,
            Guid competitorId,
            bool forUpdate,
            CancellationToken cancellationToken
        ) => Task.FromResult<CompetitorChannel?>(null);

        public Task<CompetitorChannel?> FindCompetitorByYoutubeChannelIdAsync(
            Guid projectId,
            string youtubeChannelId,
            CancellationToken cancellationToken
        ) => Task.FromResult<CompetitorChannel?>(null);

        public void AddCompetitor(CompetitorChannel competitor) { }

        public Task<VideoProject?> GetVideoProjectAsync(
            Guid projectId,
            Guid videoProjectId,
            bool forUpdate,
            CancellationToken cancellationToken
        ) =>
            Task.FromResult<VideoProject?>(
                projectId == fixture.ProjectId && videoProjectId == fixture.VideoProjectId
                    ? fixture.ScriptFixture.Base.VideoProject
                    : null
            );

        public Task<VideoProjectSource?> GetVideoProjectSourceAsync(
            Guid projectId,
            Guid pilotId,
            Guid pilotVideoId,
            CancellationToken cancellationToken
        ) =>
            Task.FromResult<VideoProjectSource?>(
                projectId == fixture.ProjectId
                && pilotId == fixture.ScriptFixture.Base.Pilot.Id
                && pilotVideoId == fixture.ScriptFixture.Base.PilotVideo.Id
                    ? fixture.ScriptFixture.Base.Source
                    : null
            );

        public Task<ResearchReportWithDetails?> GetLatestResearchReportAsync(
            Guid projectId,
            Guid videoProjectId,
            CancellationToken cancellationToken
        ) => Task.FromResult<ResearchReportWithDetails?>(fixture.ScriptFixture.Base.Research);

        public Task<ResearchReportWithDetails?> GetResearchReportAsync(
            Guid projectId,
            Guid videoProjectId,
            Guid reportId,
            CancellationToken cancellationToken
        ) =>
            Task.FromResult<ResearchReportWithDetails?>(
                reportId == fixture.ScriptFixture.Base.Report.Id
                    ? fixture.ScriptFixture.Base.Research
                    : null
            );

        public Task<VideoOutlineWithDetails?> GetApprovedVideoOutlineAsync(
            Guid projectId,
            Guid videoProjectId,
            CancellationToken cancellationToken
        ) => Task.FromResult<VideoOutlineWithDetails?>(fixture.ScriptFixture.Details);

        public Task<VideoScriptWithDetails?> GetApprovedVideoScriptAsync(
            Guid projectId,
            Guid videoProjectId,
            CancellationToken cancellationToken
        ) => Task.FromResult<VideoScriptWithDetails?>(fixture.ScriptDetails);

        public Task<VideoScriptWithDetails?> GetVideoScriptAsync(
            Guid projectId,
            Guid videoProjectId,
            Guid scriptId,
            bool forUpdate,
            CancellationToken cancellationToken
        ) =>
            Task.FromResult<VideoScriptWithDetails?>(
                scriptId == fixture.ScriptDetails.Script.Id ? fixture.ScriptDetails : null
            );

        public Task<Job?> GetActiveProductionPackageJobAsync(
            Guid projectId,
            Guid videoProjectId,
            CancellationToken cancellationToken
        ) =>
            Task.FromResult<Job?>(
                Jobs.LastOrDefault(x =>
                    x.ProjectId == projectId
                    && x.VideoProjectId == videoProjectId
                    && x.Status is JobStatus.Queued or JobStatus.Running or JobStatus.Retrying
                )
            );

        public Task<Job?> GetLatestProductionPackageJobAsync(
            Guid projectId,
            Guid videoProjectId,
            CancellationToken cancellationToken
        ) => Task.FromResult<Job?>(Jobs.LastOrDefault());

        public Task<Job> EnqueueProductionPackageJobAsync(
            Job job,
            CancellationToken cancellationToken
        )
        {
            var active = Jobs.LastOrDefault(x =>
                x.Status is JobStatus.Queued or JobStatus.Running or JobStatus.Retrying
            );
            if (active is not null)
                return Task.FromResult(active);
            Jobs.Add(job);
            return Task.FromResult(job);
        }

        public Task<Job?> TryClaimNextProductionPackageJobAsync(
            DateTimeOffset now,
            DateTimeOffset staleRunningBefore,
            CancellationToken cancellationToken
        )
        {
            var job = Jobs.FirstOrDefault(x =>
                x.Status is JobStatus.Queued or JobStatus.Retrying && x.AvailableAt <= now
            );
            job?.Start(now);
            return Task.FromResult<Job?>(job);
        }

        public Task RequeueProductionPackageJobAsync(
            Guid jobId,
            CancellationToken cancellationToken
        )
        {
            Jobs.Single(x => x.Id == jobId).Requeue(DateTimeOffset.UtcNow);
            return Task.CompletedTask;
        }

        public Task FailProductionPackageJobAsync(
            Guid jobId,
            Guid? aiRunId,
            string reason,
            bool retryable,
            DateTimeOffset failedAt,
            DateTimeOffset? retryAt,
            CancellationToken cancellationToken
        )
        {
            var job = Jobs.Single(x => x.Id == jobId);
            job.Fail(reason, retryable, failedAt, retryAt);
            var payload = JsonSerializer.Deserialize<ProductionJobPayload>(
                job.Payload,
                JsonOptions
            )!;
            if (
                job.Status == JobStatus.Failed
                && payload.Operation == ProductionJobOperation.Generate
                && payload.ReturnStatus == VideoProjectStatus.ScriptApproved.ToString()
                && fixture.ScriptFixture.Base.VideoProject.Status == VideoProjectStatus.Packaging
            )
                fixture.ScriptFixture.Base.VideoProject.TransitionTo(
                    VideoProjectStatus.ScriptApproved,
                    failedAt
                );
            return Task.CompletedTask;
        }

        public Task<bool> CompleteProductionPackageJobAsync(
            Guid jobId,
            Guid leaseId,
            DateTimeOffset completedAt,
            CancellationToken cancellationToken
        )
        {
            var job = Jobs.Single(x => x.Id == jobId);
            if (job.LeaseId != leaseId)
                return Task.FromResult(false);
            job.Complete(completedAt);
            return Task.FromResult(true);
        }

        public Task<int> GetNextProductionPackageVersionAsync(
            Guid projectId,
            Guid videoProjectId,
            CancellationToken cancellationToken
        ) => Task.FromResult(Packages.Count == 0 ? 1 : Packages.Max(x => x.Version) + 1);

        public Task<ProductionPackageWithDetails?> GetLatestProductionPackageAsync(
            Guid projectId,
            Guid videoProjectId,
            bool forUpdate,
            CancellationToken cancellationToken
        ) =>
            Task.FromResult(
                Details(
                    Packages
                        .Where(x => x.ProjectId == projectId && x.VideoProjectId == videoProjectId)
                        .OrderByDescending(x => x.Version)
                        .FirstOrDefault()
                )
            );

        public Task<ProductionPackageWithDetails?> GetProductionPackageAsync(
            Guid projectId,
            Guid videoProjectId,
            Guid packageId,
            bool forUpdate,
            CancellationToken cancellationToken
        ) =>
            Task.FromResult(
                Details(
                    Packages.SingleOrDefault(x =>
                        x.ProjectId == projectId
                        && x.VideoProjectId == videoProjectId
                        && x.Id == packageId
                    )
                )
            );

        public Task<ProductionPackageWithDetails?> GetApprovedProductionPackageAsync(
            Guid projectId,
            Guid videoProjectId,
            CancellationToken cancellationToken
        ) =>
            Task.FromResult(
                Details(Packages.SingleOrDefault(x => x.Status == ProductionPackageStatus.Approved))
            );

        public Task<IReadOnlyList<ProductionPackage>> ListProductionPackagesAsync(
            Guid projectId,
            Guid videoProjectId,
            CancellationToken cancellationToken
        ) => Task.FromResult<IReadOnlyList<ProductionPackage>>(Packages.ToArray());

        public void AddProductionPackage(ProductionPackage package) => Packages.Add(package);

        public void AddProductionScene(ProductionScene scene) => _scenes.Add(scene);

        public void AddProductionSceneScriptBlock(ProductionSceneScriptBlock link) =>
            _sceneBlocks.Add(link);

        public void AddProductionShot(ProductionShot shot) => _shots.Add(shot);

        public void AddProductionShotClaim(ProductionShotClaim claim) => _shotClaims.Add(claim);

        public void AddProductionAsset(ProductionAssetRequirement asset) => _assets.Add(asset);

        public void AddProductionAssetClaim(ProductionAssetClaim claim) => _assetClaims.Add(claim);

        public void AddProductionOnScreenText(ProductionOnScreenText text) => _texts.Add(text);

        public void AddProductionOnScreenTextClaim(ProductionOnScreenTextClaim claim) =>
            _textClaims.Add(claim);

        public void AddAiRun(AiRun aiRun) => AiRuns.Add(aiRun);

        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public ProductionPackageWithDetails? Details(ProductionPackage? package)
        {
            if (package is null)
                return null;
            var scenes = _scenes.Where(x => x.ProductionPackageId == package.Id).ToArray();
            var sceneIds = scenes.Select(x => x.Id).ToHashSet();
            var shots = _shots.Where(x => sceneIds.Contains(x.ProductionSceneId)).ToArray();
            var shotIds = shots.Select(x => x.Id).ToHashSet();
            var assets = _assets.Where(x => x.ProductionPackageId == package.Id).ToArray();
            var assetIds = assets.Select(x => x.Id).ToHashSet();
            var texts = _texts.Where(x => sceneIds.Contains(x.ProductionSceneId)).ToArray();
            var textIds = texts.Select(x => x.Id).ToHashSet();
            return new(
                package,
                scenes,
                _sceneBlocks.Where(x => x.ProductionPackageId == package.Id).ToArray(),
                shots,
                _shotClaims.Where(x => shotIds.Contains(x.ProductionShotId)).ToArray(),
                assets,
                _assetClaims
                    .Where(x => assetIds.Contains(x.ProductionAssetRequirementId))
                    .ToArray(),
                texts,
                _textClaims.Where(x => textIds.Contains(x.ProductionOnScreenTextId)).ToArray()
            );
        }
    }

    private sealed class ProductionTestFixture
    {
        public ProductionTestFixture()
        {
            ScriptFixture = new ScriptTestFixture();
            ScriptDetails = CreateApprovedScript(ScriptFixture);
            var builder = new ProductionContextBuilder(
                new ProductionOptions { RunningJobLeaseSeconds = 30 }
            );
            Context = builder.Build(
                ScriptFixture.Base.Project,
                ScriptFixture.Base.VideoProject,
                ScriptFixture.Base.Source,
                ScriptDetails,
                ScriptFixture.Base.Research
            );
        }

        public ScriptTestFixture ScriptFixture { get; }
        public VideoScriptWithDetails ScriptDetails { get; }
        public ProductionGenerationContext Context { get; }
        public Guid ProjectId => ScriptFixture.Base.Project.Id;
        public Guid VideoProjectId => ScriptFixture.Base.VideoProject.Id;

        public ProductionPackageResult ValidResult()
        {
            var scenes = Context
                .Blocks.Select(block => new ProductionSceneResult(
                    block.GlobalSequence,
                    block.GlobalSequence == 1
                        ? ProductionScenePurpose.Hook
                        : ProductionScenePurpose.Explain,
                    $"Plan block {block.GlobalSequence}.",
                    "Evidence-safe illustration",
                    ProductionComplexity.Low,
                    "Direct cut",
                    "Sparse score",
                    "No effect",
                    "Measured delivery",
                    [block.Id],
                    [
                        new ProductionShotResult(
                            1,
                            ProductionShotType.Illustration,
                            "Illustrate only the supported statement.",
                            "Wide composition",
                            "Slow push",
                            1,
                            ProductionFactualityMode.EvidenceBasedDepiction,
                            null,
                            block.Claims.Select(x => x.Id).ToArray(),
                            "Do not imply archival authenticity."
                        ),
                    ],
                    []
                ))
                .ToArray();
            return new(
                "Evidence-safe documentary illustration",
                "Measured",
                "Muted stone palette",
                "Readable sans serif",
                "Sparse score under narration",
                "Preserve the Pilot variable and control strategy.",
                scenes,
                [],
                []
            );
        }

        public ProductionGroundingAuditResult FailedAudit()
        {
            var claim = Context.Blocks[0].Claims[0].Id;
            return new(
                ProductionGroundingStatus.Failed,
                [
                    new ProductionGroundingIssueResult(
                        1,
                        1,
                        null,
                        null,
                        ProductionGroundingIssueType.FalseArchivalImpression,
                        ProductionGroundingIssueSeverity.Error,
                        "The visual may appear archival.",
                        [claim],
                        "Label the treatment as an illustration."
                    ),
                ]
            );
        }

        private static VideoScriptWithDetails CreateApprovedScript(ScriptTestFixture fixture)
        {
            var result = fixture.ValidResult();
            var metrics = new ScriptValidator(fixture.Options).ValidateGenerated(
                result,
                fixture.Context
            );
            var now = DateTimeOffset.UtcNow;
            var script = new VideoScript(
                fixture.Base.Project.Id,
                fixture.Base.VideoProject.Id,
                fixture.Outline.Id,
                fixture.Outline.Version,
                fixture.Base.Report.Id,
                fixture.Base.Report.Version,
                1,
                Guid.NewGuid(),
                Guid.NewGuid(),
                "script-engine:v1",
                "script-generation",
                1,
                fixture.Context.InputFingerprint,
                "fake",
                "premium",
                "English",
                metrics.TotalWordCount,
                metrics.EstimatedDurationSeconds,
                "[]",
                "[]",
                now
            );
            var sections = new List<VideoScriptSection>();
            var blocks = new List<VideoScriptBlock>();
            var claims = new List<VideoScriptBlockClaim>();
            var conflicts = new List<VideoScriptBlockConflict>();
            foreach (var generated in result.Sections)
            {
                var words = metrics.SectionWordCounts[generated.Sequence];
                var section = new VideoScriptSection(
                    script.Id,
                    generated.OutlineSectionId,
                    generated.Sequence,
                    fixture.Context.Sections.Single(x => x.Sequence == generated.Sequence).Heading,
                    words,
                    ScriptMetrics.EstimateDurationSeconds(
                        words,
                        fixture.Options.PlanningWordsPerMinute
                    )
                );
                sections.Add(section);
                foreach (var generatedBlock in generated.Blocks)
                {
                    var block = new VideoScriptBlock(
                        section.Id,
                        generatedBlock.Sequence,
                        generatedBlock.Type,
                        generatedBlock.Text,
                        metrics.BlockWordCounts[(generated.Sequence, generatedBlock.Sequence)]
                    );
                    blocks.Add(block);
                    claims.AddRange(
                        generatedBlock.ClaimIds.Select(x => new VideoScriptBlockClaim(block.Id, x))
                    );
                    conflicts.AddRange(
                        generatedBlock.ConflictIds.Select(x => new VideoScriptBlockConflict(
                            block.Id,
                            x
                        ))
                    );
                }
            }
            script.Approve(now);
            fixture.Base.VideoProject.TransitionTo(VideoProjectStatus.ScriptGenerating, now);
            fixture.Base.VideoProject.TransitionTo(VideoProjectStatus.ScriptReady, now);
            fixture.Base.VideoProject.TransitionTo(VideoProjectStatus.ScriptApproved, now);
            return new(script, sections, blocks, claims, conflicts);
        }
    }
}
