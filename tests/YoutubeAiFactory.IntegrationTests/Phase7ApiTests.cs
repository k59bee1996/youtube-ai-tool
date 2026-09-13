using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using YoutubeAiFactory.Domain.Ideas;
using YoutubeAiFactory.Domain.Opportunities;
using YoutubeAiFactory.Domain.Pilots;
using YoutubeAiFactory.Domain.Projects;
using YoutubeAiFactory.Infrastructure.Persistence;

namespace YoutubeAiFactory.IntegrationTests;

public sealed class Phase7ApiTests
{
    [SqlServerFact]
    public async Task Approved_pilot_video_creates_an_idempotent_project_scoped_video_project()
    {
        var connectionString = SqlServerPersistenceTests.GetConnectionString();
        var source = await SeedApprovedPilotAsync(connectionString);
        await using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder => builder.UseSetting("ConnectionStrings:DefaultConnection", connectionString));
        using var client = factory.CreateClient();

        var first = await client.PostAsJsonAsync($"/api/projects/{source.ProjectId}/pilots/{source.PilotId}/video-projects", new { pilotVideoId = source.PilotVideoId });
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        var created = await ReadAsync<VideoProjectResponse>(first);
        Assert.Equal("Draft", created.Status);
        Assert.Equal(source.PilotVideoId, created.PilotVideoId);
        Assert.Equal(source.IdeaId, created.VideoIdeaId);
        Assert.Equal(source.OpportunityId, created.OpportunityId);

        var repeated = await client.PostAsJsonAsync($"/api/projects/{source.ProjectId}/pilots/{source.PilotId}/video-projects", new { pilotVideoId = source.PilotVideoId });
        var repeatedProject = await ReadAsync<VideoProjectResponse>(repeated);
        Assert.Equal(created.Id, repeatedProject.Id);
        var list = await client.GetFromJsonAsync<List<VideoProjectListResponse>>($"/api/projects/{source.ProjectId}/video-projects");
        Assert.Single(list!);

        var crossProject = await client.GetAsync($"/api/projects/{Guid.NewGuid()}/video-projects/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, crossProject.StatusCode);
    }

    private static async Task<SourceIds> SeedApprovedPilotAsync(string connectionString)
    {
        var options = new DbContextOptionsBuilder<YoutubeAiFactoryDbContext>().UseSqlServer(connectionString).Options;
        await using var context = new YoutubeAiFactoryDbContext(options);
        await context.Database.EnsureDeletedAsync();
        await context.Database.MigrateAsync();
        var now = DateTimeOffset.UtcNow;
        var project = new Project("History", new Market("History", "English", "Global"), new AudienceProfile("History viewers"), now);
        var report = new OpportunityReport(project.Id, 1, Guid.NewGuid(), "opportunity", 1, "Fake", "fake", "score:v1", 1, "[]", now);
        var opportunity = new OpportunityCandidate(report.Id, "Historical Ownership Economics", "Description", "History viewers", "Economics", "Explainer", "Hidden costs", "Why", 80, 70, 40, 85, 75, 70, 80, 30, 85, 82m, "[]", "[]", now);
        opportunity.SetDecision(OpportunityDecisionStatus.Approved);
        var generation = new IdeaGeneration(project.Id, opportunity.Id, report.Id, 1, 1, Guid.NewGuid(), "ideas", 1, "Fake", "fake", "score:v1", now);
        var idea = new VideoIdea(project.Id, opportunity.Id, generation.Id, "The Economics of Owning a Medieval Castle", "Economics", "Hidden costs", "Explainer", "History viewers", "Learn", "Hook", "Thumbnail", "Promise", "Question", "Why care", "Hypothesis", 80, 80, 70, 80, 75, 85, 85, 70, 80, 30, 20, 85, 82m, 0m, "score:v1", "[]", now);
        idea.SetDecision(IdeaDecisionStatus.Approved);
        var pilot = new Pilot(project.Id, 1, Guid.NewGuid(), "pilot", 1, "Fake", "fake", "plan:v1", "Pilot", "Learn", "[]", "[]", "[]", 12, now);
        pilot.Approve(now);
        var pilotVideo = new PilotVideo(pilot.Id, idea.Id, opportunity.Id, 6, PilotExperimentType.Packaging, "Hidden-cost framing should increase click intent.", "Framing", "Comparable topics", "CTR", "CTR improves", "Rationale");
        context.AddRange(project, report, opportunity, generation, idea, pilot, pilotVideo);
        await context.SaveChangesAsync();
        return new SourceIds(project.Id, pilot.Id, pilotVideo.Id, idea.Id, opportunity.Id);
    }

    private static async Task<T> ReadAsync<T>(HttpResponseMessage response)
    {
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>() ?? throw new InvalidOperationException("The API returned an empty response.");
    }

    private sealed record SourceIds(Guid ProjectId, Guid PilotId, Guid PilotVideoId, Guid IdeaId, Guid OpportunityId);
    private sealed record VideoProjectResponse(Guid Id, string Status, Guid PilotVideoId, Guid VideoIdeaId, Guid OpportunityId);
    private sealed record VideoProjectListResponse(Guid Id);
}
