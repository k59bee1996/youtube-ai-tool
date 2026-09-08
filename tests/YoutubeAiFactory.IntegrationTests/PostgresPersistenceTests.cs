using Microsoft.EntityFrameworkCore;
using Npgsql;
using YoutubeAiFactory.Application.Common;
using YoutubeAiFactory.Domain.AI;
using YoutubeAiFactory.Domain.Competitors;
using YoutubeAiFactory.Domain.Projects;
using YoutubeAiFactory.Infrastructure.Persistence;

namespace YoutubeAiFactory.IntegrationTests;

public sealed class PostgresPersistenceTests
{
    [PostgresFact]
    public async Task Migrations_persist_versioned_competitor_analysis_and_ai_run_provenance()
    {
        var options = CreateOptions();
        await using var context = new YoutubeAiFactoryDbContext(options);
        await context.Database.EnsureDeletedAsync();
        await context.Database.MigrateAsync();
        var now = DateTimeOffset.UtcNow;
        var project = new Project("Analysis", new Market("Education", "English", "Global"), new AudienceProfile("Developers"), now);
        var competitor = CreateCompetitor(project.Id, "https://youtube.com/@analysis");
        var run = new AiRun("CompetitorAnalysis", project.Id, competitor.Id, "Fake", "fake-model", "competitor-analysis", 1, now);
        run.Complete(10, 20, null, now.AddSeconds(1));
        var analysis = new CompetitorAnalysis(competitor.Id, 1, run.Id, "competitor-analysis", 1, "Fake", "fake-model", now, 1,
            "{\"audience\":{},\"confidence\":{}}", now.AddSeconds(1));
        context.Projects.Add(project);
        context.CompetitorChannels.Add(competitor);
        context.AiRuns.Add(run);
        context.CompetitorAnalyses.Add(analysis);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var stored = await context.CompetitorAnalyses.SingleAsync();
        var storedRun = await context.AiRuns.SingleAsync();
        Assert.Equal(1, stored.Version);
        Assert.Equal(competitor.Id, stored.CompetitorChannelId);
        Assert.Equal(run.Id, stored.AiRunId);
        Assert.Equal(competitor.Id, storedRun.CompetitorId);
    }

    [PostgresFact]
    public async Task Migrations_persist_and_read_project_competitor_and_videos()
    {
        var options = CreateOptions();
        await using var context = new YoutubeAiFactoryDbContext(options);
        await context.Database.EnsureDeletedAsync();
        await context.Database.MigrateAsync();

        var project = new Project(
            "Integration Test",
            new Market("Education", "English", "Global"),
            new AudienceProfile("Developers"),
            DateTimeOffset.UtcNow);

        context.Projects.Add(project);
        var competitor = CreateCompetitor(project.Id, "https://youtube.com/@practicalcreator");
        context.CompetitorChannels.Add(competitor);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var stored = await context.Projects.SingleAsync(candidate => candidate.Id == project.Id);
        var storedCompetitor = await context.CompetitorChannels
            .Include(channel => channel.Videos)
            .SingleAsync(channel => channel.Id == competitor.Id);

        Assert.Equal("Education", stored.Market.Name);
        Assert.Equal("Developers", stored.Audience.Description);
        Assert.Equal(125_000, storedCompetitor.SubscriberCount);
        var storedVideo = Assert.Single(storedCompetitor.Videos);
        Assert.Equal(42_000, storedVideo.ViewCount);
        Assert.Equal(TimeSpan.FromMinutes(12), storedVideo.Duration);
    }

    [PostgresFact]
    public async Task Store_maps_duplicate_channel_constraint_to_conflict()
    {
        var options = CreateOptions();
        await using var context = new YoutubeAiFactoryDbContext(options);
        await context.Database.EnsureDeletedAsync();
        await context.Database.MigrateAsync();
        var project = new Project(
            "Constraints",
            new Market("Education", "English", "Global"),
            new AudienceProfile("Developers"),
            DateTimeOffset.UtcNow);
        context.Projects.Add(project);
        await context.SaveChangesAsync();

        var store = new YoutubeAiFactoryStore(context);
        store.AddCompetitor(CreateCompetitor(project.Id, "https://youtube.com/@one"));
        store.AddCompetitor(CreateCompetitor(project.Id, "https://youtube.com/@two"));

        await Assert.ThrowsAsync<ResourceConflictException>(() =>
            store.SaveChangesAsync(CancellationToken.None));

        await using var verificationContext = new YoutubeAiFactoryDbContext(CreateOptions());
        Assert.Equal(0, await verificationContext.CompetitorChannels.CountAsync());
        Assert.Equal(0, await verificationContext.CompetitorVideos.CountAsync());
    }

    [PostgresFact]
    public async Task Database_rejects_duplicate_video_identity_within_a_competitor()
    {
        var options = CreateOptions();
        await using var context = new YoutubeAiFactoryDbContext(options);
        await context.Database.EnsureDeletedAsync();
        await context.Database.MigrateAsync();
        var project = new Project(
            "Video Constraints",
            new Market("Education", "English", "Global"),
            new AudienceProfile("Developers"),
            DateTimeOffset.UtcNow);
        var competitor = CreateCompetitor(project.Id, "https://youtube.com/@practicalcreator");
        context.Projects.Add(project);
        context.CompetitorChannels.Add(competitor);
        await context.SaveChangesAsync();

        var exception = await Assert.ThrowsAsync<PostgresException>(() =>
            context.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO yaf.competitor_videos
                    (id, competitor_channel_id, youtube_video_id, title, url, collected_at)
                VALUES
                    ({Guid.NewGuid()}, {competitor.Id}, {"a1b2c3d4e5F"}, {"Duplicate"},
                     {"https://youtube.com/watch?v=a1b2c3d4e5F"}, {DateTimeOffset.UtcNow})
                """));

        Assert.Equal(PostgresErrorCodes.UniqueViolation, exception.SqlState);
    }

    [PostgresFact]
    public async Task Database_requires_resolved_competitor_identity()
    {
        var options = CreateOptions();
        await using var context = new YoutubeAiFactoryDbContext(options);
        await context.Database.EnsureDeletedAsync();
        await context.Database.MigrateAsync();
        var project = new Project(
            "Required Identity",
            new Market("Education", "English", "Global"),
            new AudienceProfile("Developers"),
            DateTimeOffset.UtcNow);
        context.Projects.Add(project);
        await context.SaveChangesAsync();

        var exception = await Assert.ThrowsAsync<PostgresException>(() =>
            context.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO yaf.competitor_channels
                    (id, project_id, source_url, created_at)
                VALUES
                    ({Guid.NewGuid()}, {project.Id}, {"https://youtube.com/@incomplete"},
                     {DateTimeOffset.UtcNow})
                """));

        Assert.Equal(PostgresErrorCodes.NotNullViolation, exception.SqlState);
    }

    internal static DbContextOptions<YoutubeAiFactoryDbContext> CreateOptions()
    {
        var connectionString = GetConnectionString();
        return new DbContextOptionsBuilder<YoutubeAiFactoryDbContext>()
            .UseNpgsql(connectionString)
            .Options;
    }

    internal static string GetConnectionString()
    {
        var connectionString = Environment.GetEnvironmentVariable("YAF_TEST_POSTGRES")
            ?? throw new InvalidOperationException("YAF_TEST_POSTGRES is required.");

        var builder = new NpgsqlConnectionStringBuilder(connectionString);
        if (builder.Database is not { } database ||
            !database.EndsWith("_tests", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Integration tests require a database name ending in '_tests'.");
        }

        return connectionString;
    }

    private static CompetitorChannel CreateCompetitor(Guid projectId, string sourceUrl)
    {
        var now = DateTimeOffset.UtcNow;
        var competitor = new CompetitorChannel(projectId, sourceUrl, now);
        competitor.RecordMetadata(
            "UC1234567890abcdefghij12",
            "Practical Creator",
            "Evidence-based creator education.",
            "@practicalcreator",
            "https://example.test/channel.jpg",
            125_000,
            240,
            14_000_000,
            new DateTimeOffset(2020, 4, 3, 0, 0, 0, TimeSpan.Zero),
            now);
        competitor.UpsertVideo(
            "a1b2c3d4e5F",
            "How to Research a Video",
            "A repeatable research process.",
            "https://youtube.com/watch?v=a1b2c3d4e5F",
            "https://example.test/video.jpg",
            TimeSpan.FromMinutes(12),
            42_000,
            1_900,
            143,
            new DateTimeOffset(2026, 7, 1, 0, 0, 0, TimeSpan.Zero),
            now);
        return competitor;
    }
}
