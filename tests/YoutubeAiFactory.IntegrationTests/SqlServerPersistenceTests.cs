using System.Text.Json;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using YoutubeAiFactory.Application.Common;
using YoutubeAiFactory.Domain.AI;
using YoutubeAiFactory.Domain.Competitors;
using YoutubeAiFactory.Domain.Ideas;
using YoutubeAiFactory.Domain.Opportunities;
using YoutubeAiFactory.Domain.Pilots;
using YoutubeAiFactory.Domain.Projects;
using YoutubeAiFactory.Infrastructure.Persistence;

namespace YoutubeAiFactory.IntegrationTests;

public sealed class SqlServerPersistenceTests
{
    [SqlServerFact]
    public async Task Migrations_persist_versioned_competitor_analysis_and_ai_run_provenance()
    {
        var options = CreateOptions();
        await using var context = new YoutubeAiFactoryDbContext(options);
        await context.Database.EnsureDeletedAsync();
        await context.Database.MigrateAsync();
        var now = DateTimeOffset.UtcNow;
        var project = new Project("Analysis – Nhà sáng tạo", new Market("Education", "English", "Global"), new AudienceProfile("Developers"), now);
        var competitor = CreateCompetitor(project.Id, "https://youtube.com/@analysis");
        var run = new AiRun("CompetitorAnalysis", project.Id, competitor.Id, "Fake", "fake-model", "competitor-analysis", 1, now);
        run.Complete(10, 20, 0.123456m, now.AddSeconds(1));
        var resultJson = JsonSerializer.Serialize(new
        {
            audience = new { summary = "Nhà sáng tạo nội dung" },
            diagnostic = new string('x', 16_000),
        });
        var analysis = new CompetitorAnalysis(competitor.Id, 1, run.Id, "competitor-analysis", 1, "Fake", "fake-model", now, 1,
            resultJson, now.AddSeconds(1));
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
        Assert.Equal(resultJson, stored.ResultJson);
        Assert.Equal(0.123456m, storedRun.EstimatedCost);
    }

    [SqlServerFact]
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

    [SqlServerFact]
    public async Task Migrations_persist_and_read_opportunity_idea_and_pilot_versions()
    {
        var options = CreateOptions();
        await using var context = new YoutubeAiFactoryDbContext(options);
        await context.Database.EnsureDeletedAsync();
        await context.Database.MigrateAsync();
        var now = DateTimeOffset.UtcNow;
        var project = new Project("Workflow persistence", new Market("Education", "English", "Global"), new AudienceProfile("Creators"), now);
        var report = new OpportunityReport(project.Id, 1, Guid.NewGuid(), "opportunity-analysis", 1, "Fake", "fake", "opportunity-score:v1", 1, "[]", now);
        var opportunity = new OpportunityCandidate(report.Id, "Opportunity", "Description", "Creators", "Topic", "Explainer", "Angle", "Why", 80, 70, 40, 85, 75, 70, 80, 30, 85, 82.40m, "[]", "[]", now);
        var generation = new IdeaGeneration(project.Id, opportunity.Id, report.Id, report.Version, 1, Guid.NewGuid(), "idea-generation", 1, "Fake", "fake", "idea-score:v1", now);
        var idea = new VideoIdea(project.Id, opportunity.Id, generation.Id, "Working title", "Topic", "Angle", "Explainer", "Creators", "Learn", "Hook", "Thumbnail", "Promise", "Question", "Why care", "Hypothesis", 80, 80, 70, 80, 75, 85, 85, 70, 80, 30, 20, 85, 82.40m, 0m, "idea-score:v1", "[]", now);
        var pilot = new Pilot(project.Id, 1, Guid.NewGuid(), "pilot-generation", 1, "Fake", "fake", "pilot-planning:v1", "Pilot", "Learn", "[]", "[]", "[]", 12, now);
        context.AddRange(project, report, opportunity, generation, idea, pilot);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        Assert.Equal(82.40m, (await context.OpportunityCandidates.SingleAsync()).OverallScore);
        Assert.Equal("Working title", (await context.VideoIdeas.SingleAsync()).WorkingTitle);
        Assert.Equal(1, (await context.Pilots.SingleAsync()).Version);

        context.Remove(await context.Projects.SingleAsync());
        await context.SaveChangesAsync();

        Assert.Equal(0, await context.OpportunityReports.CountAsync());
        Assert.Equal(0, await context.IdeaGenerations.CountAsync());
        Assert.Equal(0, await context.VideoIdeas.CountAsync());
        Assert.Equal(0, await context.Pilots.CountAsync());
    }

    [SqlServerFact]
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

    [SqlServerFact]
    public async Task Pilot_revision_rejects_a_concurrent_draft_mutation()
    {
        var options = CreateOptions();
        await using (var setup = new YoutubeAiFactoryDbContext(options))
        {
            await setup.Database.EnsureDeletedAsync();
            await setup.Database.MigrateAsync();
            var now = DateTimeOffset.UtcNow;
            var project = new Project("Pilot concurrency", new Market("Education", "English", "Global"), new AudienceProfile("Creators"), now);
            var pilot = new Pilot(project.Id, 1, Guid.NewGuid(), "pilot-generation", 1, "fake", "fake", "pilot-planning:v1", "Pilot", "Learn", "[]", "[]", "[]", 12, now);
            setup.Projects.Add(project);
            setup.Pilots.Add(pilot);
            await setup.SaveChangesAsync();
        }

        await using var firstContext = new YoutubeAiFactoryDbContext(CreateOptions());
        await using var secondContext = new YoutubeAiFactoryDbContext(CreateOptions());
        var firstStore = new YoutubeAiFactoryStore(firstContext);
        var secondStore = new YoutubeAiFactoryStore(secondContext);
        var pilotId = await firstContext.Pilots.Select(item => item.Id).SingleAsync();
        var projectId = await firstContext.Pilots.Select(item => item.ProjectId).SingleAsync();
        var first = await firstStore.GetPilotAsync(projectId, pilotId, true, CancellationToken.None);
        var second = await secondStore.GetPilotAsync(projectId, pilotId, true, CancellationToken.None);

        first!.RecordDraftChange();
        await firstStore.SaveChangesAsync(CancellationToken.None);
        second!.RecordDraftChange();

        await Assert.ThrowsAsync<ResourceConflictException>(() => secondStore.SaveChangesAsync(CancellationToken.None));
    }

    [SqlServerFact]
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

        var exception = await Assert.ThrowsAsync<SqlException>(() =>
            context.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO [yaf].[competitor_videos]
                    ([id], [competitor_channel_id], [youtube_video_id], [title], [url], [collected_at])
                VALUES
                    ({Guid.NewGuid()}, {competitor.Id}, {"a1b2c3d4e5F"}, {"Duplicate"},
                     {"https://youtube.com/watch?v=a1b2c3d4e5F"}, {DateTimeOffset.UtcNow})
                """));

        Assert.True(exception.Number is 2601 or 2627);
    }

    [SqlServerFact]
    public async Task Database_preserves_case_distinct_youtube_channel_ids()
    {
        var options = CreateOptions();
        await using var context = new YoutubeAiFactoryDbContext(options);
        await context.Database.EnsureDeletedAsync();
        await context.Database.MigrateAsync();
        var project = new Project(
            "Case-sensitive identifiers",
            new Market("Education", "English", "Global"),
            new AudienceProfile("Developers"),
            DateTimeOffset.UtcNow);
        context.Projects.Add(project);
        context.CompetitorChannels.Add(CreateCompetitor(
            project.Id,
            "https://youtube.com/@upper",
            "UC1234567890abcdefghij12"));
        context.CompetitorChannels.Add(CreateCompetitor(
            project.Id,
            "https://youtube.com/@lower",
            "uc1234567890abcdefghij12"));

        await context.SaveChangesAsync();

        Assert.Equal(2, await context.CompetitorChannels.CountAsync());
    }

    [SqlServerFact]
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

        var exception = await Assert.ThrowsAsync<SqlException>(() =>
            context.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO [yaf].[competitor_channels]
                    ([id], [project_id], [source_url], [created_at])
                VALUES
                    ({Guid.NewGuid()}, {project.Id}, {"https://youtube.com/@incomplete"},
                     {DateTimeOffset.UtcNow})
                """));

        Assert.Equal(515, exception.Number);
    }

    internal static DbContextOptions<YoutubeAiFactoryDbContext> CreateOptions()
    {
        var connectionString = GetConnectionString();
        return new DbContextOptionsBuilder<YoutubeAiFactoryDbContext>()
            .UseSqlServer(connectionString)
            .Options;
    }

    internal static string GetConnectionString()
    {
        var connectionString = Environment.GetEnvironmentVariable("YAF_TEST_SQLSERVER")
            ?? throw new InvalidOperationException("YAF_TEST_SQLSERVER is required.");

        var builder = new SqlConnectionStringBuilder(connectionString);
        if (builder.InitialCatalog is not { } database ||
            !database.EndsWith("Tests", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Integration tests require a database name ending in 'Tests'.");
        }

        return connectionString;
    }

    private static CompetitorChannel CreateCompetitor(
        Guid projectId,
        string sourceUrl,
        string youtubeChannelId = "UC1234567890abcdefghij12")
    {
        var now = DateTimeOffset.UtcNow;
        var competitor = new CompetitorChannel(projectId, sourceUrl, now);
        competitor.RecordMetadata(
            youtubeChannelId,
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
