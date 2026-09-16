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
using YoutubeAiFactory.Domain.Ideas;
using YoutubeAiFactory.Domain.Opportunities;
using YoutubeAiFactory.Domain.Outlines;
using YoutubeAiFactory.Domain.Pilots;
using YoutubeAiFactory.Domain.Projects;
using YoutubeAiFactory.Domain.Research;
using YoutubeAiFactory.Domain.Videos;
using YoutubeAiFactory.Infrastructure.Persistence;

namespace YoutubeAiFactory.IntegrationTests;

public sealed class Phase9ApiTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    [SqlServerFact]
    public async Task ResearchReady_to_generated_retrieved_and_approved_outline_is_a_complete_api_slice()
    {
        var connectionString = SqlServerPersistenceTests.GetConnectionString();
        var source = await SeedResearchReadyVideoAsync(connectionString);
        await using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ConnectionStrings:DefaultConnection", connectionString);
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<ILlmProvider>();
                services.RemoveAll<IAiModelResolver>();
                services.AddSingleton<ILlmProvider>(new OutlineProvider(source.ClaimId));
                services.AddSingleton<IAiModelResolver>(new OutlineResolver());
            });
        });
        using var client = factory.CreateClient();
        var route = $"/api/projects/{source.ProjectId}/video-projects/{source.VideoProjectId}";

        var queuedResponses = await Task.WhenAll(
            client.PostAsync($"{route}/outline:generate", null),
            client.PostAsync($"{route}/outline:generate", null));
        foreach (var response in queuedResponses)
            Assert.True(response.StatusCode == HttpStatusCode.Accepted,
                $"Expected Accepted, received {response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
        var queuedRuns = await Task.WhenAll(queuedResponses.Select(ReadAsync<RunResponse>));
        Assert.Single(queuedRuns.Select(item => item.JobId).Distinct());
        Assert.Single(queuedRuns, item => !item.Existing);
        Assert.Single(queuedRuns, item => item.Existing);

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var processor = scope.ServiceProvider.GetRequiredService<VideoOutlineJobProcessor>();
            Assert.True(await processor.ProcessNextAsync(CancellationToken.None));
        }

        var status = await client.GetFromJsonAsync<OutlineStatusResponse>($"{route}/outline/latest");
        Assert.NotNull(status?.LatestOutline);
        Assert.Equal("Ready", status.LatestOutline.Status);
        Assert.Equal(source.ResearchReportId, status.LatestOutline.ResearchReportId);
        Assert.Equal(source.ClaimId, status.LatestOutline.Sections[1].Claims.Single().Id);
        Assert.Equal(1, status.LatestOutline.Version);

        var history = await client.GetFromJsonAsync<List<OutlineHistoryResponse>>($"{route}/outlines");
        Assert.Single(history!);
        var approvedResponse = await client.PostAsync($"{route}/outlines/{status.LatestOutline.Id}:approve", null);
        var approved = await ReadAsync<OutlineResponse>(approvedResponse);
        Assert.Equal("Approved", approved.Status);
        Assert.NotNull(approved.ApprovedAt);

        var video = await client.GetFromJsonAsync<VideoProjectResponse>(route);
        Assert.Equal("OutlineApproved", video!.Status);
        Assert.Equal(HttpStatusCode.NotFound,
            (await client.GetAsync($"/api/projects/{Guid.NewGuid()}/video-projects/{source.VideoProjectId}/outlines/{approved.Id}")).StatusCode);
    }

    private static async Task<SourceIds> SeedResearchReadyVideoAsync(string connectionString)
    {
        var options = new DbContextOptionsBuilder<YoutubeAiFactoryDbContext>().UseSqlServer(connectionString).Options;
        await using var context = new YoutubeAiFactoryDbContext(options);
        await context.Database.EnsureDeletedAsync();
        await context.Database.MigrateAsync();
        var now = DateTimeOffset.UtcNow;
        var project = new Project("History", new Market("Historical economics", "English", "Global"), new AudienceProfile("History viewers"), now);
        var opportunityReport = new OpportunityReport(project.Id, 1, Guid.NewGuid(), "opportunity", 1, "Fake", "fake", "score:v1", 1, "[]", now);
        var opportunity = new OpportunityCandidate(opportunityReport.Id, "Historical Ownership Economics", "Description", "History viewers", "Economics", "Explainer", "Hidden costs", "Why", 80, 70, 40, 85, 75, 70, 80, 30, 85, 82m, "[]", "[]", now);
        opportunity.SetDecision(OpportunityDecisionStatus.Approved);
        var generation = new IdeaGeneration(project.Id, opportunity.Id, opportunityReport.Id, 1, 1, Guid.NewGuid(), "ideas", 1, "Fake", "fake", "score:v1", now);
        var idea = new VideoIdea(project.Id, opportunity.Id, generation.Id, "The Economics of Owning a Medieval Castle", "Historical economics", "Hidden costs", "Explainer", "History viewers", "Learn", "Reveal hidden obligations", "Castle and ledger", "Understand the real ownership cost", "Question", "Why care", "Hypothesis", 80, 80, 70, 80, 75, 85, 85, 70, 80, 30, 20, 85, 82m, 0m, "score:v1", "[]", now);
        idea.SetDecision(IdeaDecisionStatus.Approved);
        var pilot = new Pilot(project.Id, 1, Guid.NewGuid(), "pilot", 1, "Fake", "fake", "plan:v1", "Pilot", "Learn", "[]", "[]", "[]", 12, now);
        pilot.Approve(now);
        var pilotVideo = new PilotVideo(pilot.Id, idea.Id, opportunity.Id, 6, PilotExperimentType.Packaging,
            "Hidden-cost framing should increase click intent.", "Hidden-cost framing", "Keep storytelling comparable",
            "CTR", "CTR improves", "Rationale");
        var videoProject = new VideoProject(project.Id, pilot.Id, pilot.Version, pilotVideo.Id, idea.Id, opportunity.Id,
            idea.WorkingTitle, idea.Topic, idea.Angle, idea.ContentFormat, idea.TargetAudience, idea.HookConcept,
            idea.ThumbnailConcept, idea.ViewerPromise, pilotVideo.ExperimentType, pilotVideo.Hypothesis,
            pilotVideo.VariableBeingTested, pilotVideo.PrimaryMetric, pilotVideo.SuccessSignal, "[]", now);
        videoProject.TransitionTo(VideoProjectStatus.ResearchQueued, now);
        videoProject.TransitionTo(VideoProjectStatus.Researching, now);
        videoProject.TransitionTo(VideoProjectStatus.ResearchReady, now);
        var fingerprint = ResearchBriefBuilder.CreateFingerprint(ResearchBriefBuilder.Build(project, videoProject, opportunity.Name));
        var run = new ResearchRun(project.Id, videoProject.Id, "research-engine:v1", fingerprint, now);
        var reportId = Guid.NewGuid();
        var claim = new ResearchClaim(reportId, "Ownership required continuing staffing obligations.", ResearchClaimType.Factual, 90, true, now);
        claim.SetSupportStatus(ResearchClaimSupportStatus.Supported);
        var researchPayload = new ResearchReportPayload(
            new("Ownership combined prestige with continuing obligations.",
                [new("Recurring staffing is part of the core mechanism.", "Mechanism", [claim.Id], [])],
                [], [], []),
            new("Medium", 1, 1, 1, 0, 1, 0, 0),
            new(1, 1, 1, 1, 1, 1, 1, 0, 1, 0, 0, 0, 0, 0),
            new("Establish the ownership mechanism.", [new("What obligations persisted?", true)],
                [new("castle household accounts", "Find support")], ["Staffing"], []));
        var report = new ResearchReport(project.Id, videoProject.Id, run.Id, 1, "research-engine:v1", fingerprint,
            JsonSerializer.Serialize(researchPayload, JsonOptions), null, now, reportId);
        var researchSource = new ResearchSource(run.Id, "https://archive.example/castle", "https://archive.example/castle",
            "archive.example", "Castle accounts", "Archive", null, now, ResearchSourceCategory.Primary,
            ResearchSourceFetchStatus.Fetched, "hash", null, null);
        var evidence = new ResearchEvidence(run.Id, researchSource.Id, ResearchEvidenceType.Fact,
            "Staffing was recurring.", "Accounts list household roles.", "ledger 12", 92, now);
        var link = new ResearchClaimEvidence(claim.Id, evidence.Id, ResearchEvidenceStance.Support);
        context.AddRange(project, opportunityReport, opportunity, generation, idea, pilot, pilotVideo, videoProject,
            run, report, researchSource, evidence, claim, link);
        await context.SaveChangesAsync();
        return new(project.Id, videoProject.Id, report.Id, claim.Id);
    }

    private static async Task<T> ReadAsync<T>(HttpResponseMessage response)
    {
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>() ?? throw new InvalidOperationException("The API returned an empty response.");
    }

    private sealed class OutlineResolver : IAiModelResolver
    {
        public ResolvedAiModel Resolve(AiModelProfile profile) => new(profile, "Fake", $"{profile.ToString().ToLowerInvariant()}-model", 30, 8_000);
    }

    private sealed class OutlineProvider(Guid claimId) : ILlmProvider
    {
        public Task<LlmResult<T>> GenerateStructuredAsync<T>(LlmRequest request, CancellationToken cancellationToken)
        {
            var result = new OutlineGenerationResult(
                new(OutlineStructureType.Explainer, "What made ownership costly?", "Prestige carried recurring obligations.",
                    "Open on visible prestige versus hidden obligations.", "Move from context through mechanism to synthesis.",
                    "The viewer understands ownership as continuing obligations.", "Build toward the synthesis."),
                new("Preserves the packaging premise with comparable storytelling.", []),
                [
                    new(1, "The contradiction", OutlineSectionPurpose.Hook, "Frame the question.", "Introduce the contradiction without narration.", "What did ownership require?", "Move to context.", [], [], [], 45),
                    Section(2, OutlineSectionPurpose.Context),
                    Section(3, OutlineSectionPurpose.Explanation),
                    Section(4, OutlineSectionPurpose.Conclusion),
                ]);
            return Task.FromResult(new LlmResult<T>((T)(object)result, "Fake", request.ResolvedModel!.Model, 100, 50, "{}"));
        }

        private OutlineSectionResult Section(int sequence, OutlineSectionPurpose purpose) =>
            new(sequence, $"Section {sequence}", purpose, "Explain the claim's narrative role.",
                "Use only the referenced supported claim.", "How does this affect the premise?", "Carry the question forward.",
                [new(claimId, OutlineClaimUsageRole.Supporting)], [], [], 60);
    }

    private sealed record SourceIds(Guid ProjectId, Guid VideoProjectId, Guid ResearchReportId, Guid ClaimId);
    private sealed record RunResponse(Guid JobId, string Status, bool Existing);
    private sealed record OutlineStatusResponse(OutlineResponse? LatestOutline);
    private sealed record OutlineHistoryResponse(Guid Id, int Version);
    private sealed record VideoProjectResponse(string Status);
    private sealed record OutlineResponse(Guid Id, Guid ResearchReportId, int Version, string Status,
        IReadOnlyList<OutlineSectionResponse> Sections, DateTimeOffset? ApprovedAt);
    private sealed record OutlineSectionResponse(Guid Id, IReadOnlyList<OutlineClaimResponse> Claims);
    private sealed record OutlineClaimResponse(Guid Id);
}
