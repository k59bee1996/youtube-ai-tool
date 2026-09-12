using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using YoutubeAiFactory.Application.Competitors;
using YoutubeAiFactory.Application.Projects;
using YoutubeAiFactory.Infrastructure.Persistence;
using YoutubeAiFactory.Tests.Fixtures;

namespace YoutubeAiFactory.IntegrationTests;

public sealed class Phase2ApiTests
{
    [SqlServerFact]
    public async Task Create_project_add_competitor_and_retrieve_persisted_result()
    {
        var connectionString = SqlServerPersistenceTests.GetConnectionString();
        await ResetDatabaseAsync(connectionString);
        var fakeYouTubeClient = new FakeYouTubeClient();
        await using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseSetting("ConnectionStrings:DefaultConnection", connectionString);
                builder.ConfigureServices(services =>
                {
                    services.RemoveAll<IYouTubeClient>();
                    services.AddSingleton<IYouTubeClient>(fakeYouTubeClient);
                });
            });
        using var client = factory.CreateClient();

        var projectResponse = await client.PostAsJsonAsync("/api/projects", new
        {
            name = "Creator Research",
            marketName = "Creator education",
            targetLanguage = "English",
            targetGeography = "Global",
            audienceDescription = "Independent video creators",
        });
        var project = await ReadSuccessAsync<ProjectDto>(projectResponse);

        var invalidResponse = await client.PostAsJsonAsync(
            $"/api/projects/{project.Id}/competitors",
            new { youtubeUrl = "https://example.com/channel" });
        Assert.Equal(HttpStatusCode.BadRequest, invalidResponse.StatusCode);
        var invalidProblem = await invalidResponse.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal("Validation failed", invalidProblem?.Title);
        Assert.Contains("youtube.com", invalidProblem?.Detail, StringComparison.OrdinalIgnoreCase);

        var missingProjectResponse = await client.PostAsJsonAsync(
            $"/api/projects/{Guid.NewGuid()}/competitors",
            new { youtubeUrl = "https://youtube.com/@practicalcreator" });
        Assert.Equal(HttpStatusCode.NotFound, missingProjectResponse.StatusCode);
        Assert.Equal(0, fakeYouTubeClient.ChannelRequests);

        var competitorResponse = await client.PostAsJsonAsync(
            $"/api/projects/{project.Id}/competitors",
            new { youtubeUrl = "https://youtube.com/@practicalcreator" });
        Assert.Equal(HttpStatusCode.Created, competitorResponse.StatusCode);
        var competitor = await ReadSuccessAsync<CompetitorDetailsDto>(competitorResponse);

        var duplicateResponse = await client.PostAsJsonAsync(
            $"/api/projects/{project.Id}/competitors",
            new { youtubeUrl = "https://youtube.com/channel/UC1234567890abcdefghij12" });
        Assert.Equal(HttpStatusCode.OK, duplicateResponse.StatusCode);
        var duplicate = await ReadSuccessAsync<CompetitorDetailsDto>(duplicateResponse);

        var retrieved = await client.GetFromJsonAsync<CompetitorDetailsDto>(
            $"/api/projects/{project.Id}/competitors/{competitor.Id}");

        Assert.NotNull(retrieved);
        Assert.Equal(competitor.Id, duplicate.Id);
        Assert.Equal(competitor.Id, retrieved.Id);
        Assert.Equal(2, retrieved.Videos.Count);
        Assert.Equal(2, fakeYouTubeClient.ChannelRequests);

        await using var context = new YoutubeAiFactoryDbContext(
            SqlServerPersistenceTests.CreateOptions());
        Assert.Equal(1, await context.CompetitorChannels.CountAsync());
        Assert.Equal(2, await context.CompetitorVideos.CountAsync());
    }

    private static async Task ResetDatabaseAsync(string connectionString)
    {
        var options = new DbContextOptionsBuilder<YoutubeAiFactoryDbContext>()
            .UseSqlServer(connectionString)
            .Options;
        await using var context = new YoutubeAiFactoryDbContext(options);
        await context.Database.EnsureDeletedAsync();
        await context.Database.MigrateAsync();
    }

    private static async Task<T> ReadSuccessAsync<T>(HttpResponseMessage response)
    {
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>()
            ?? throw new InvalidOperationException("The API returned an empty response.");
    }
}
