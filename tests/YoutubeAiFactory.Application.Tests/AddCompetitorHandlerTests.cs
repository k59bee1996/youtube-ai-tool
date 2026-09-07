using Microsoft.Extensions.Logging.Abstractions;
using YoutubeAiFactory.Application.Common;
using YoutubeAiFactory.Application.Competitors;
using YoutubeAiFactory.Application.Persistence;
using YoutubeAiFactory.Domain.Competitors;
using YoutubeAiFactory.Domain.Projects;
using YoutubeAiFactory.Tests.Fixtures;

namespace YoutubeAiFactory.Application.Tests;

public sealed class AddCompetitorHandlerTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 9, 6, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Handler_collects_and_persists_a_new_competitor()
    {
        var store = new FakeStore();
        var project = CreateProject();
        store.Projects.Add(project);
        var youTube = new FakeYouTubeClient();
        var handler = CreateHandler(store, youTube);

        var result = await handler.HandleAsync(
            new AddCompetitorCommand(project.Id, "https://youtube.com/@practicalcreator"),
            CancellationToken.None);

        Assert.True(result.Created);
        Assert.Single(store.Competitors);
        Assert.Equal(2, result.Competitor.Videos.Count);
        Assert.Equal(YouTubeTestData.ChannelId, result.Competitor.YoutubeChannelId);
        Assert.Equal(Now, result.Competitor.LastCollectedAt);
        Assert.Equal(1, store.SaveCalls);
    }

    [Fact]
    public async Task Handler_refreshes_existing_competitor_without_duplicating_it()
    {
        var store = new FakeStore();
        var project = CreateProject();
        store.Projects.Add(project);
        var competitor = new CompetitorChannel(
            project.Id,
            "https://youtube.com/@practicalcreator",
            Now.AddDays(-1));
        competitor.RecordMetadata(
            YouTubeTestData.ChannelId,
            "Old title",
            null,
            "@practicalcreator",
            null,
            1,
            1,
            1,
            null,
            Now.AddDays(-1));
        var firstVideo = YouTubeTestData.Videos().First();
        competitor.UpsertVideo(
            firstVideo.YoutubeVideoId,
            "Old video title",
            null,
            firstVideo.Url,
            null,
            null,
            1,
            null,
            null,
            null,
            Now.AddDays(-1));
        store.Competitors.Add(competitor);

        var youTube = new FakeYouTubeClient
        {
            ChannelResult = YouTubeTestData.Channel("Updated title"),
        };
        var handler = CreateHandler(store, youTube);

        var result = await handler.HandleAsync(
            new AddCompetitorCommand(project.Id, "https://youtube.com/channel/UC1234567890abcdefghij12"),
            CancellationToken.None);

        Assert.False(result.Created);
        Assert.Single(store.Competitors);
        Assert.Equal("Updated title", result.Competitor.Title);
        Assert.Equal(2, result.Competitor.Videos.Count);
        Assert.Equal("How to Research a Video", result.Competitor.Videos[0].Title);
    }

    [Fact]
    public async Task Handler_rejects_a_competitor_for_an_unknown_project()
    {
        var store = new FakeStore();
        var youTube = new FakeYouTubeClient();
        var handler = CreateHandler(store, youTube);

        await Assert.ThrowsAsync<ResourceNotFoundException>(() => handler.HandleAsync(
            new AddCompetitorCommand(Guid.NewGuid(), "https://youtube.com/@practicalcreator"),
            CancellationToken.None));

        Assert.Equal(0, youTube.ChannelRequests);
        Assert.Empty(store.Competitors);
    }

    [Fact]
    public async Task Handler_reports_invalid_collection_configuration_before_provider_call()
    {
        var store = new FakeStore();
        var project = CreateProject();
        store.Projects.Add(project);
        var youTube = new FakeYouTubeClient();
        var handler = CreateHandler(store, youTube, videoLimit: 51);

        var exception = await Assert.ThrowsAsync<ExternalServiceException>(() => handler.HandleAsync(
            new AddCompetitorCommand(project.Id, "https://youtube.com/@practicalcreator"),
            CancellationToken.None));

        Assert.Equal(ExternalServiceFailure.Configuration, exception.Failure);
        Assert.Equal(0, youTube.ChannelRequests);
    }

    [Fact]
    public async Task Handler_classifies_provider_metadata_validation_as_unexpected_response()
    {
        var store = new FakeStore();
        var project = CreateProject();
        store.Projects.Add(project);
        var youTube = new FakeYouTubeClient
        {
            VideoResults =
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
            ],
        };
        var handler = CreateHandler(store, youTube);

        var exception = await Assert.ThrowsAsync<ExternalServiceException>(() => handler.HandleAsync(
            new AddCompetitorCommand(project.Id, "https://youtube.com/@practicalcreator"),
            CancellationToken.None));

        Assert.Equal(ExternalServiceFailure.UnexpectedResponse, exception.Failure);
        Assert.IsType<YoutubeAiFactory.Domain.Common.DomainException>(exception.InnerException);
        Assert.Empty(store.Competitors);
        Assert.Equal(0, store.SaveCalls);
    }

    [Fact]
    public async Task Handler_preserves_caller_cancellation()
    {
        var store = new FakeStore();
        var project = CreateProject();
        store.Projects.Add(project);
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();
        var handler = CreateHandler(store, new CancellingYouTubeClient());

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => handler.HandleAsync(
            new AddCompetitorCommand(project.Id, "https://youtube.com/@practicalcreator"),
            cancellationSource.Token));

        Assert.Empty(store.Competitors);
        Assert.Equal(0, store.SaveCalls);
    }

    private static AddCompetitorHandler CreateHandler(
        IYoutubeAiFactoryStore store,
        IYouTubeClient youTubeClient,
        int videoLimit = 30) => new(
        store,
        youTubeClient,
        new CompetitorCollectionOptions { VideoLimit = videoLimit },
        new FixedTimeProvider(Now),
        NullLogger<AddCompetitorHandler>.Instance);

    private static Project CreateProject() => new(
        "Creator Research",
        new Market("Creator education", "English", "Global"),
        new AudienceProfile("Independent video creators"),
        Now);

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private sealed class CancellingYouTubeClient : IYouTubeClient
    {
        public Task<YouTubeChannelResult> GetChannelAsync(
            YouTubeChannelReference reference,
            CancellationToken cancellationToken) =>
            Task.FromCanceled<YouTubeChannelResult>(cancellationToken);

        public Task<IReadOnlyCollection<YouTubeVideoResult>> GetChannelVideosAsync(
            string uploadsPlaylistId,
            int limit,
            CancellationToken cancellationToken) =>
            throw new InvalidOperationException("Video collection should not be reached.");
    }

    private sealed class FakeStore : IYoutubeAiFactoryStore
    {
        public List<Project> Projects { get; } = [];

        public List<CompetitorChannel> Competitors { get; } = [];

        public int SaveCalls { get; private set; }

        public Task<bool> ProjectExistsAsync(Guid projectId, CancellationToken cancellationToken) =>
            Task.FromResult(Projects.Any(project => project.Id == projectId));

        public Task<Project?> GetProjectAsync(Guid projectId, CancellationToken cancellationToken) =>
            Task.FromResult(Projects.SingleOrDefault(project => project.Id == projectId));

        public Task<IReadOnlyList<Project>> ListProjectsAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Project>>(Projects);

        public void AddProject(Project project) => Projects.Add(project);

        public Task<IReadOnlyList<CompetitorChannel>> ListCompetitorsAsync(
            Guid projectId,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<CompetitorChannel>>(
                Competitors.Where(competitor => competitor.ProjectId == projectId).ToArray());

        public Task<CompetitorChannel?> GetCompetitorAsync(
            Guid projectId,
            Guid competitorId,
            bool forUpdate,
            CancellationToken cancellationToken) =>
            Task.FromResult(Competitors.SingleOrDefault(competitor =>
                competitor.ProjectId == projectId && competitor.Id == competitorId));

        public Task<CompetitorChannel?> FindCompetitorByYoutubeChannelIdAsync(
            Guid projectId,
            string youtubeChannelId,
            CancellationToken cancellationToken) =>
            Task.FromResult(Competitors.SingleOrDefault(competitor =>
                competitor.ProjectId == projectId &&
                competitor.YoutubeChannelId == youtubeChannelId));

        public void AddCompetitor(CompetitorChannel competitor) => Competitors.Add(competitor);

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            SaveCalls++;
            return Task.CompletedTask;
        }
    }
}
