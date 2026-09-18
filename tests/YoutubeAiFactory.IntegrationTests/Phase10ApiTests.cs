using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using YoutubeAiFactory.Application.AI;
using YoutubeAiFactory.Application.Outlines;
using YoutubeAiFactory.Application.Research;
using YoutubeAiFactory.Application.Scripts;
using YoutubeAiFactory.Domain.AI;
using YoutubeAiFactory.Domain.Ideas;
using YoutubeAiFactory.Domain.Opportunities;
using YoutubeAiFactory.Domain.Outlines;
using YoutubeAiFactory.Domain.Pilots;
using YoutubeAiFactory.Domain.Projects;
using YoutubeAiFactory.Domain.Research;
using YoutubeAiFactory.Domain.Scripts;
using YoutubeAiFactory.Domain.Videos;
using YoutubeAiFactory.Infrastructure.Persistence;

namespace YoutubeAiFactory.IntegrationTests;

public sealed class Phase10ApiTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    [SqlServerFact]
    public async Task Approved_outline_to_edited_revalidated_and_approved_script_is_a_complete_api_slice()
    {
        var connectionString = SqlServerPersistenceTests.GetConnectionString();
        var source = await SeedApprovedOutlineAsync(connectionString);
        await using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ConnectionStrings:DefaultConnection", connectionString);
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<ILlmProvider>();
                services.RemoveAll<IAiModelResolver>();
                services.AddSingleton<ILlmProvider>(new ScriptProvider(source.SectionIds, source.ClaimId));
                services.AddSingleton<IAiModelResolver>(new ScriptResolver());
            });
        });
        using var client = factory.CreateClient();
        var route = $"/api/projects/{source.ProjectId}/video-projects/{source.VideoProjectId}";

        var queuedResponses = await Task.WhenAll(
            client.PostAsync($"{route}/script:generate", null),
            client.PostAsync($"{route}/script:generate", null));
        foreach (var response in queuedResponses)
            Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        var runs = await Task.WhenAll(queuedResponses.Select(ReadAsync<RunVideoScriptResult>));
        Assert.Single(runs.Select(item => item.JobId).Distinct());
        Assert.Single(runs, item => !item.Existing);
        Assert.Single(runs, item => item.Existing);

        await ProcessOneAsync(factory);
        var status = await client.GetFromJsonAsync<ScriptStatusDto>($"{route}/script/latest");
        var script = Assert.IsType<VideoScriptDto>(status!.LatestScript);
        Assert.Equal("Ready", script.Status);
        Assert.Equal("Passed", script.GroundingStatus);
        Assert.Equal(source.OutlineId, script.VideoOutlineId);
        Assert.Equal(source.ResearchReportId, script.ResearchReportId);
        Assert.Equal(source.ClaimId, script.Sections[0].Blocks[0].Claims.Single().Id);
        Assert.NotEmpty(script.Sections[0].Blocks[0].Claims.Single().Evidence);

        var firstBlock = script.Sections[0].Blocks[0];
        var update = new UpdateVideoScriptRequest([new(firstBlock.Id,
            firstBlock.Text + " The surviving records still require careful qualification.")]);
        var editedResponse = await client.PatchAsJsonAsync($"{route}/scripts/{script.Id}", update);
        var edited = await ReadAsync<VideoScriptDto>(editedResponse);
        Assert.Equal("Pending", edited.GroundingStatus);
        Assert.Equal(HttpStatusCode.BadRequest,
            (await client.PostAsync($"{route}/scripts/{script.Id}:approve", null)).StatusCode);

        Assert.Equal(HttpStatusCode.Accepted,
            (await client.PostAsync($"{route}/scripts/{script.Id}:validate", null)).StatusCode);
        await ProcessOneAsync(factory);
        var approved = await ReadAsync<VideoScriptDto>(
            await client.PostAsync($"{route}/scripts/{script.Id}:approve", null));
        Assert.Equal("Approved", approved.Status);
        Assert.NotNull(approved.ApprovedAt);

        var video = await client.GetFromJsonAsync<VideoProjectStatusResponse>(route);
        Assert.Equal("ScriptApproved", video!.Status);
        Assert.Equal(HttpStatusCode.NotFound,
            (await client.GetAsync($"/api/projects/{Guid.NewGuid()}/video-projects/{source.VideoProjectId}/scripts/{script.Id}")).StatusCode);

        await using var verifyScope = factory.Services.CreateAsyncScope();
        var db = verifyScope.ServiceProvider.GetRequiredService<YoutubeAiFactoryDbContext>();
        Assert.Equal(4, await db.VideoScriptSections.CountAsync(item => item.VideoScriptId == script.Id));
        Assert.Equal(4, await db.VideoScriptBlocks.CountAsync(item =>
            db.VideoScriptSections.Where(section => section.VideoScriptId == script.Id)
                .Select(section => section.Id).Contains(item.VideoScriptSectionId)));
        Assert.Equal(4, await db.VideoScriptBlockClaims.CountAsync(item =>
            db.VideoScriptBlocks.Where(block => db.VideoScriptSections
                    .Where(section => section.VideoScriptId == script.Id).Select(section => section.Id)
                    .Contains(block.VideoScriptSectionId))
                .Select(block => block.Id).Contains(item.ScriptBlockId)));
    }

    private static async Task ProcessOneAsync(WebApplicationFactory<Program> factory)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var processor = scope.ServiceProvider.GetRequiredService<VideoScriptJobProcessor>();
        Assert.True(await processor.ProcessNextAsync(CancellationToken.None));
    }

    private static async Task<SourceIds> SeedApprovedOutlineAsync(string connectionString)
    {
        var options = new DbContextOptionsBuilder<YoutubeAiFactoryDbContext>()
            .UseSqlServer(connectionString).Options;
        await using var context = new YoutubeAiFactoryDbContext(options);
        await context.Database.EnsureDeletedAsync();
        await context.Database.MigrateAsync();
        var now = DateTimeOffset.UtcNow;
        var project = new Project("History", new Market("Historical economics", "English", "Global"),
            new AudienceProfile("History viewers"), now);
        var opportunityReport = new OpportunityReport(project.Id, 1, Guid.NewGuid(), "opportunity", 1,
            "Fake", "fake", "score:v1", 1, "[]", now);
        var opportunity = new OpportunityCandidate(opportunityReport.Id, "Historical Ownership Economics",
            "Description", "History viewers", "Economics", "Explainer", "Hidden costs", "Why",
            80, 70, 40, 85, 75, 70, 80, 30, 85, 82m, "[]", "[]", now);
        opportunity.SetDecision(OpportunityDecisionStatus.Approved);
        var generation = new IdeaGeneration(project.Id, opportunity.Id, opportunityReport.Id, 1, 1,
            Guid.NewGuid(), "ideas", 1, "Fake", "fake", "score:v1", now);
        var idea = new VideoIdea(project.Id, opportunity.Id, generation.Id,
            "The Economics of Owning a Medieval Castle", "Historical economics", "Hidden costs", "Explainer",
            "History viewers", "Learn", "Reveal hidden obligations", "Castle and ledger",
            "Understand the real ownership cost", "Question", "Why care", "Hypothesis",
            80, 80, 70, 80, 75, 85, 85, 70, 80, 30, 20, 85, 82m, 0m, "score:v1", "[]", now);
        idea.SetDecision(IdeaDecisionStatus.Approved);
        var pilot = new Pilot(project.Id, 1, Guid.NewGuid(), "pilot", 1, "Fake", "fake", "plan:v1",
            "Pilot", "Learn", "[]", "[]", "[]", 12, now);
        pilot.Approve(now);
        var pilotVideo = new PilotVideo(pilot.Id, idea.Id, opportunity.Id, 6, PilotExperimentType.Packaging,
            "Hidden-cost framing should increase click intent.", "Hidden-cost framing",
            "Keep storytelling comparable", "CTR", "CTR improves", "Rationale");
        var video = new VideoProject(project.Id, pilot.Id, pilot.Version, pilotVideo.Id, idea.Id, opportunity.Id,
            idea.WorkingTitle, idea.Topic, idea.Angle, idea.ContentFormat, idea.TargetAudience, idea.HookConcept,
            idea.ThumbnailConcept, idea.ViewerPromise, pilotVideo.ExperimentType, pilotVideo.Hypothesis,
            pilotVideo.VariableBeingTested, pilotVideo.PrimaryMetric, pilotVideo.SuccessSignal, "[]", now);
        video.TransitionTo(VideoProjectStatus.ResearchQueued, now);
        video.TransitionTo(VideoProjectStatus.Researching, now);
        video.TransitionTo(VideoProjectStatus.ResearchReady, now);
        var researchFingerprint = ResearchBriefBuilder.CreateFingerprint(
            ResearchBriefBuilder.Build(project, video, opportunity.Name));
        var researchRun = new ResearchRun(project.Id, video.Id, "research-engine:v1", researchFingerprint, now);
        var reportId = Guid.NewGuid();
        var claim = new ResearchClaim(reportId, "Ownership required continuing staffing obligations.",
            ResearchClaimType.Factual, 90, true, now);
        claim.SetSupportStatus(ResearchClaimSupportStatus.Supported);
        var payload = new ResearchReportPayload(
            new("Ownership combined prestige with continuing obligations.",
                [new("Recurring staffing is the core mechanism.", "Mechanism", [claim.Id], [])], [], [], []),
            new("High", 1, 1, 1, 0, 1, 0, 0), new(1, 1, 1, 1, 1, 1, 1, 0, 1, 0, 0, 0, 0, 0),
            new("Establish the ownership mechanism.", [new("What obligations persisted?", true)],
                [new("castle household accounts", "Find support")], ["Staffing"], []));
        var report = new ResearchReport(project.Id, video.Id, researchRun.Id, 1, "research-engine:v1",
            researchFingerprint, JsonSerializer.Serialize(payload, JsonOptions), null, now, reportId);
        var researchSource = new ResearchSource(researchRun.Id, "https://archive.example/castle",
            "https://archive.example/castle", "archive.example", "Castle accounts", "Archive", null, now,
            ResearchSourceCategory.Primary, ResearchSourceFetchStatus.Fetched, "hash", null, null);
        var evidence = new ResearchEvidence(researchRun.Id, researchSource.Id, ResearchEvidenceType.Fact,
            "Staffing was recurring.", "Accounts list household roles.", "ledger 12", 92, now);
        var evidenceLink = new ResearchClaimEvidence(claim.Id, evidence.Id, ResearchEvidenceStance.Support);
        var outlineRun = new AiRun("OutlineGeneration", project.Id, "Fake", "reasoning-model",
            "outline-generation", 1, now, videoProjectId: video.Id, researchReportId: report.Id);
        outlineRun.Complete(100, 50, null, now.AddSeconds(1));
        var outlineFingerprint = OutlineGenerationContextBuilder.CreateFingerprint(project, video,
            new(pilot, pilotVideo, idea, opportunity), report);
        var outline = new VideoOutline(project.Id, video.Id, report.Id, 1, 1, outlineRun.Id,
            "outline-engine:v1", "outline-generation", 1, outlineFingerprint, "Fake", "reasoning-model",
            OutlineStructureType.Explainer, "What did ownership require?", "Prestige required obligations.",
            "Open on prestige versus recurring costs.", video.ViewerPromise,
            "Move from symbol to recurring mechanism and synthesis.", "Ownership was an operating system.",
            "Build through evidence.", PilotExperimentType.Packaging, pilotVideo.VariableBeingTested,
            pilotVideo.ControlStrategy, "Preserve the packaging experiment.", "[]", "[]", 240, now);
        var sections = Enumerable.Range(1, 4).Select(sequence => new VideoOutlineSection(outline.Id, sequence,
            $"Section {sequence}", sequence == 1 ? OutlineSectionPurpose.Hook :
                sequence == 4 ? OutlineSectionPurpose.Payoff : OutlineSectionPurpose.Explanation,
            "Advance the approved narrative.", "Use only assigned evidence.", "What does this prove?",
            "Carry the question forward.", 60)).ToArray();
        var sectionClaims = sections.Select(section => new VideoOutlineSectionClaim(section.Id, claim.Id,
            OutlineClaimUsageRole.Supporting)).ToArray();
        outline.Approve(now.AddSeconds(2));
        video.TransitionTo(VideoProjectStatus.OutlineGenerating, now);
        video.TransitionTo(VideoProjectStatus.OutlineReady, now);
        video.TransitionTo(VideoProjectStatus.OutlineApproved, now);

        context.AddRange(project, opportunityReport, opportunity, generation, idea, pilot, pilotVideo, video,
            researchRun, report, researchSource, evidence, claim, evidenceLink, outlineRun, outline);
        context.AddRange(sections);
        context.AddRange(sectionClaims);
        await context.SaveChangesAsync();
        return new(project.Id, video.Id, report.Id, outline.Id, claim.Id,
            sections.OrderBy(item => item.Sequence).Select(item => item.Id).ToArray());
    }

    private static async Task<T> ReadAsync<T>(HttpResponseMessage response)
    {
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>() ??
            throw new InvalidOperationException("The API returned an empty response.");
    }

    private sealed class ScriptResolver : IAiModelResolver
    {
        public ResolvedAiModel Resolve(AiModelProfile profile) =>
            new(profile, "Fake", $"{profile.ToString().ToLowerInvariant()}-model", 30, 8_000);
    }

    private sealed class ScriptProvider(IReadOnlyList<Guid> sectionIds, Guid claimId) : ILlmProvider
    {
        public Task<LlmResult<T>> GenerateStructuredAsync<T>(LlmRequest request,
            CancellationToken cancellationToken)
        {
            object result = typeof(T) == typeof(ScriptGroundingAuditResult)
                ? new ScriptGroundingAuditResult(ScriptGroundingStatus.Passed, [])
                : Script();
            return Task.FromResult(new LlmResult<T>((T)result, "Fake", request.ResolvedModel!.Model,
                100, 50, "{}"));
        }

        private VideoScriptResult Script()
        {
            var narration = string.Join(' ', Enumerable.Repeat("grounded", 120));
            return new(sectionIds.Select((id, index) => new ScriptSectionResult(id, index + 1,
                [new ScriptBlockResult(1, ScriptBlockType.FactualNarration, narration, [claimId], [])]))
                .ToArray(), "Ownership depended on continuing obligations.");
        }
    }

    private sealed record SourceIds(Guid ProjectId, Guid VideoProjectId, Guid ResearchReportId,
        Guid OutlineId, Guid ClaimId, IReadOnlyList<Guid> SectionIds);
    private sealed record VideoProjectStatusResponse(string Status);
}
