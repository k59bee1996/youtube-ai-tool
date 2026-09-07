using System.Net;
using System.Net.Http.Json;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using YoutubeAiFactory.Application.Competitors;
using YoutubeAiFactory.Application.Persistence;
using YoutubeAiFactory.Application.Projects;
using YoutubeAiFactory.Domain.Competitors;
using YoutubeAiFactory.Domain.Projects;
using YoutubeAiFactory.Tests.Fixtures;

namespace YoutubeAiFactory.IntegrationTests;

public sealed class Phase2ApiContractTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 9, 7, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Project_and_competitor_endpoints_support_create_read_list_and_refresh()
    {
        await using var fixture = new ApiFixture();

        using var projectResponse = await fixture.Client.PostAsJsonAsync("/api/projects", ProjectRequest("One"));
        Assert.Equal(HttpStatusCode.Created, projectResponse.StatusCode);
        var project = await ReadSuccessAsync<ProjectDto>(projectResponse);
        Assert.Equal($"/api/projects/{project.Id}", projectResponse.Headers.Location?.OriginalString);

        var retrievedProject = await fixture.Client.GetFromJsonAsync<ProjectDto>(
            $"/api/projects/{project.Id}");
        var projects = await fixture.Client.GetFromJsonAsync<ProjectDto[]>("/api/projects");

        Assert.Equal(project.Id, retrievedProject?.Id);
        Assert.Contains(projects!, candidate => candidate.Id == project.Id);

        using var createdResponse = await fixture.Client.PostAsJsonAsync(
            $"/api/projects/{project.Id}/competitors",
            new { youtubeUrl = "https://youtube.com/@practicalcreator" });
        Assert.Equal(HttpStatusCode.Created, createdResponse.StatusCode);
        var created = await ReadSuccessAsync<CompetitorDetailsDto>(createdResponse);
        Assert.Equal(
            $"/api/projects/{project.Id}/competitors/{created.Id}",
            createdResponse.Headers.Location?.OriginalString);

        using var refreshedResponse = await fixture.Client.PostAsJsonAsync(
            $"/api/projects/{project.Id}/competitors",
            new { youtubeUrl = $"https://youtube.com/channel/{YouTubeTestData.ChannelId}" });
        Assert.Equal(HttpStatusCode.OK, refreshedResponse.StatusCode);
        var refreshed = await ReadSuccessAsync<CompetitorDetailsDto>(refreshedResponse);

        var competitors = await fixture.Client.GetFromJsonAsync<CompetitorSummaryDto[]>(
            $"/api/projects/{project.Id}/competitors");
        var details = await fixture.Client.GetFromJsonAsync<CompetitorDetailsDto>(
            $"/api/projects/{project.Id}/competitors/{created.Id}");

        Assert.Equal(created.Id, refreshed.Id);
        Assert.Single(competitors!);
        Assert.Equal(created.Id, details?.Id);
        Assert.Equal(2, details?.Videos.Count);
        Assert.Single(fixture.Store.Competitors);
    }

    [Fact]
    public async Task Competitor_detail_is_scoped_to_the_route_project()
    {
        await using var fixture = new ApiFixture();
        var firstProject = await CreateProjectAsync(fixture.Client, "First");
        var secondProject = await CreateProjectAsync(fixture.Client, "Second");

        using var createResponse = await fixture.Client.PostAsJsonAsync(
            $"/api/projects/{firstProject.Id}/competitors",
            new { youtubeUrl = "https://youtube.com/@practicalcreator" });
        var competitor = await ReadSuccessAsync<CompetitorDetailsDto>(createResponse);

        using var wrongProjectResponse = await fixture.Client.GetAsync(
            $"/api/projects/{secondProject.Id}/competitors/{competitor.Id}");
        var problem = await wrongProjectResponse.Content.ReadFromJsonAsync<ProblemDetails>();

        Assert.Equal(HttpStatusCode.NotFound, wrongProjectResponse.StatusCode);
        Assert.Equal("Resource not found", problem?.Title);
    }

    [Fact]
    public async Task Provider_metadata_that_violates_domain_rules_returns_bad_gateway()
    {
        await using var fixture = new ApiFixture();
        var project = await CreateProjectAsync(fixture.Client, "Invalid provider data");
        var saveCallsBeforeCollection = fixture.Store.SaveCalls;
        fixture.YouTube.VideoResults =
        [
            new YouTubeVideoResult(
                "a1b2c3d4e5F",
                "Invalid metrics",
                null,
                "https://www.youtube.com/watch?v=a1b2c3d4e5F",
                null,
                null,
                ViewCount: -1,
                LikeCount: null,
                CommentCount: null,
                PublishedAt: null),
        ];

        using var response = await fixture.Client.PostAsJsonAsync(
            $"/api/projects/{project.Id}/competitors",
            new { youtubeUrl = "https://youtube.com/@practicalcreator" });
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();

        Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
        Assert.Equal("YouTube request failed", problem?.Title);
        Assert.Equal("YouTube returned metadata that failed validation.", problem?.Detail);
        Assert.Equal(saveCallsBeforeCollection, fixture.Store.SaveCalls);
        Assert.Empty(fixture.Store.Competitors);
    }

    [Fact]
    public async Task Invalid_user_url_remains_bad_request_without_calling_provider()
    {
        await using var fixture = new ApiFixture();
        var project = await CreateProjectAsync(fixture.Client, "Invalid input");

        using var response = await fixture.Client.PostAsJsonAsync(
            $"/api/projects/{project.Id}/competitors",
            new { youtubeUrl = "https://youtube.com/watch?v=a1b2c3d4e5F" });
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("Validation failed", problem?.Title);
        Assert.Equal(0, fixture.YouTube.ChannelRequests);
    }

    [Fact]
    public async Task Unsupported_request_media_type_preserves_framework_status_code()
    {
        await using var fixture = new ApiFixture();
        using var content = new StringContent("{}", Encoding.UTF8, "text/plain");

        using var response = await fixture.Client.PostAsync("/api/projects", content);

        Assert.Equal(HttpStatusCode.UnsupportedMediaType, response.StatusCode);
    }

    private static object ProjectRequest(string suffix) => new
    {
        name = $"Creator Research {suffix}",
        marketName = "Creator education",
        targetLanguage = "English",
        targetGeography = "Global",
        audienceDescription = "Independent video creators",
    };

    private static async Task<ProjectDto> CreateProjectAsync(HttpClient client, string suffix)
    {
        using var response = await client.PostAsJsonAsync("/api/projects", ProjectRequest(suffix));
        return await ReadSuccessAsync<ProjectDto>(response);
    }

    private static async Task<T> ReadSuccessAsync<T>(HttpResponseMessage response)
    {
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>()
            ?? throw new InvalidOperationException("The API returned an empty response.");
    }

    private sealed class ApiFixture : IAsyncDisposable
    {
        public ApiFixture()
        {
            Factory = new WebApplicationFactory<Program>()
                .WithWebHostBuilder(builder =>
                {
                    builder.UseSetting(
                        "ConnectionStrings:Database",
                        "Host=localhost;Database=unused;Username=unused;Password=unused");
                    builder.ConfigureServices(services =>
                    {
                        services.RemoveAll<IYoutubeAiFactoryStore>();
                        services.AddSingleton<IYoutubeAiFactoryStore>(Store);
                        services.RemoveAll<IYouTubeClient>();
                        services.AddSingleton<IYouTubeClient>(YouTube);
                        services.RemoveAll<TimeProvider>();
                        services.AddSingleton<TimeProvider>(new FixedTimeProvider(Now));
                    });
                });
            Client = Factory.CreateClient();
        }

        public ContractStore Store { get; } = new();

        public FakeYouTubeClient YouTube { get; } = new();

        public WebApplicationFactory<Program> Factory { get; }

        public HttpClient Client { get; }

        public async ValueTask DisposeAsync()
        {
            Client.Dispose();
            await Factory.DisposeAsync();
        }
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private sealed class ContractStore : IYoutubeAiFactoryStore
    {
        public List<Project> Projects { get; } = [];

        public List<CompetitorChannel> Competitors { get; } = [];

        public int SaveCalls { get; private set; }

        public Task<bool> ProjectExistsAsync(Guid projectId, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(Projects.Any(project => project.Id == projectId));
        }

        public Task<Project?> GetProjectAsync(Guid projectId, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(Projects.SingleOrDefault(project => project.Id == projectId));
        }

        public Task<IReadOnlyList<Project>> ListProjectsAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult<IReadOnlyList<Project>>(Projects.ToArray());
        }

        public void AddProject(Project project) => Projects.Add(project);

        public Task<IReadOnlyList<CompetitorChannel>> ListCompetitorsAsync(
            Guid projectId,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult<IReadOnlyList<CompetitorChannel>>(
                Competitors.Where(competitor => competitor.ProjectId == projectId).ToArray());
        }

        public Task<CompetitorChannel?> GetCompetitorAsync(
            Guid projectId,
            Guid competitorId,
            bool forUpdate,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(Competitors.SingleOrDefault(competitor =>
                competitor.ProjectId == projectId && competitor.Id == competitorId));
        }

        public Task<CompetitorChannel?> FindCompetitorByYoutubeChannelIdAsync(
            Guid projectId,
            string youtubeChannelId,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(Competitors.SingleOrDefault(competitor =>
                competitor.ProjectId == projectId &&
                competitor.YoutubeChannelId == youtubeChannelId));
        }

        public void AddCompetitor(CompetitorChannel competitor) => Competitors.Add(competitor);

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            SaveCalls++;
            return Task.CompletedTask;
        }
    }
}
