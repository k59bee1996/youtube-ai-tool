using Microsoft.Extensions.Logging.Abstractions;
using YoutubeAiFactory.Application.AI;
using YoutubeAiFactory.Application.Common;
using YoutubeAiFactory.Application.Competitors;
using YoutubeAiFactory.Application.Persistence;
using YoutubeAiFactory.Domain.AI;
using YoutubeAiFactory.Domain.Competitors;
using YoutubeAiFactory.Domain.Jobs;
using YoutubeAiFactory.Domain.Projects;

namespace YoutubeAiFactory.Application.Tests;

public sealed class CompetitorAnalysisWorkflowTests
{
    private static readonly System.Text.Json.JsonSerializerOptions WebJson = new(System.Text.Json.JsonSerializerDefaults.Web);
    [Fact]
    public void Context_builder_calculates_median_outlier_and_small_sample_limitations()
    {
        var competitor = CreateCompetitor(10, 100, 300);
        var context = new CompetitorAnalysisContextBuilder(new CompetitorAnalysisOptions { MaxVideos = 10 }).Build(competitor);

        Assert.Equal(100, context.MedianViews);
        Assert.Contains(context.Videos, video => video.Views == 300 && video.PerformanceClassification == PerformanceClassification.StrongOutlier);
        Assert.Equal(3, context.AnalyzedVideoCount);
        Assert.Contains("No transcripts, comments, or visual thumbnail analysis were supplied.", context.Limitations);
    }

    [Fact]
    public async Task Job_processor_retries_invalid_output_then_persists_a_versioned_analysis_and_ai_run()
    {
        var competitor = CreateCompetitor(50, 100, 300);
        var store = new AnalysisStore(competitor);
        var job = new Job("competitor-analysis", System.Text.Json.JsonSerializer.Serialize(
            new CompetitorAnalysisJobPayload(competitor.ProjectId, competitor.Id), WebJson), DateTimeOffset.UtcNow, 0);
        store.Job = job;
        var provider = new SequencedProvider(InvalidResult(), ValidResult(competitor.Videos.First().Id));
        var processor = new CompetitorAnalysisJobProcessor(store, provider,
            new CompetitorAnalysisContextBuilder(new CompetitorAnalysisOptions()),
            new CompetitorAnalysisOptions { MaxStructuredOutputRetries = 1 },
            TimeProvider.System, NullLogger<CompetitorAnalysisJobProcessor>.Instance);

        Assert.True(await processor.ProcessNextAsync(CancellationToken.None));
        var analysis = Assert.Single(store.Analyses);
        Assert.Equal(1, analysis.Version);
        Assert.Equal("competitor-analysis", analysis.PromptKey);
        Assert.Equal(1, analysis.PromptVersion);
        Assert.Equal(1, Assert.Single(store.Runs).RetryCount);
        Assert.Equal(AiRunStatus.Succeeded, store.Runs.Single().Status);
        Assert.Equal(JobStatus.Completed, job.Status);
    }

    [Fact]
    public void Validator_rejects_video_references_outside_the_bounded_context()
    {
        var competitor = CreateCompetitor(10, 100, 300);
        var context = new CompetitorAnalysisContextBuilder(new CompetitorAnalysisOptions()).Build(competitor);
        var invalid = ValidResult(Guid.NewGuid());
        Assert.Throws<StructuredOutputException>(() => CompetitorAnalysisValidator.Validate(invalid, context));
    }

    [Fact]
    public void Golden_fixture_retains_a_bounded_mix_of_outlier_typical_and_weak_video_evidence()
    {
        var competitor = CreateCompetitor([450, 390, 310, 120, 110, 105, 100, 95, 92, 90, 88, 85, 82, 80, 78, 76, 35, 30, 25, 20]);
        var context = new CompetitorAnalysisContextBuilder(new CompetitorAnalysisOptions { MaxVideos = 12 }).Build(competitor);

        Assert.Equal(20, context.TotalCollectedVideos);
        Assert.Equal(12, context.AnalyzedVideoCount);
        Assert.Contains(context.Videos, video => video.PerformanceClassification == PerformanceClassification.StrongOutlier);
        Assert.Contains(context.Videos, video => video.PerformanceClassification == PerformanceClassification.WeakPerformer);
        Assert.Contains(context.Videos, video => video.PerformanceClassification == PerformanceClassification.Typical);
    }

    private static CompetitorAnalysisResult ValidResult(Guid videoId) => new(
        new AudienceAnalysis(null, ["Creators"], ["Learn"], [], 60, ["Channel description"]),
        [new TopicCluster("Research", "Research videos", [videoId], 1, "Above baseline", 70)],
        [new TitlePattern("How to", "Instructional framing", "How to {task}", ["How to research"], 1, "Typical", 65)],
        [new ContentFormatInsight("Explainer", [videoId], "Typical", 70)],
        [new PerformanceInsight("Research content performed above baseline.", [videoId], 65)],
        [],
        [new TransferableFormat("Explainer", "Clear utility", [videoId], "Use a clear task framing.", "Titles, subjects, scripts, and artwork.", 65)],
        [new EvidenceNote("Metadata only.", [videoId])],
        new AnalysisConfidence(65, "Moderate", ["No transcripts available."]));

    private static CompetitorAnalysisResult InvalidResult() => ValidResult(Guid.NewGuid());

    private static CompetitorChannel CreateCompetitor(params long[] views)
    {
        var now = DateTimeOffset.UtcNow;
        var project = new Project("Project", new Market("Market", "English", "Global"), new AudienceProfile("Audience"), now);
        var competitor = new CompetitorChannel(project.Id, "https://youtube.com/@creator", now);
        competitor.RecordMetadata("UC1234567890abcdefghij12", "Creator", "Description", "@creator", null, null, null, null, null, now);
        for (var index = 0; index < views.Length; index++)
            competitor.UpsertVideo($"video{index}", $"Video {index}", null, $"https://youtube.com/watch?v={index}", null, null, views[index], null, null, now.AddDays(-index), now);
        return competitor;
    }

    private sealed class SequencedProvider(params CompetitorAnalysisResult[] results) : ILlmProvider
    {
        private readonly Queue<CompetitorAnalysisResult> _results = new(results);
        public Task<LlmResult<T>> GenerateStructuredAsync<T>(LlmRequest request, CancellationToken cancellationToken) =>
            Task.FromResult(new LlmResult<T>((T)(object)_results.Dequeue(), "Fake", "fake-model", 12, 34, null));
    }

    private sealed class AnalysisStore(CompetitorChannel competitor) : IYoutubeAiFactoryStore
    {
        public Job? Job { get; set; }
        public List<CompetitorAnalysis> Analyses { get; } = [];
        public List<AiRun> Runs { get; } = [];
        public Task<bool> ProjectExistsAsync(Guid projectId, CancellationToken cancellationToken) => Task.FromResult(projectId == competitor.ProjectId);
        public Task<Project?> GetProjectAsync(Guid projectId, CancellationToken cancellationToken) => Task.FromResult<Project?>(null);
        public Task<IReadOnlyList<Project>> ListProjectsAsync(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<Project>>([]);
        public void AddProject(Project project) { }
        public Task<IReadOnlyList<CompetitorChannel>> ListCompetitorsAsync(Guid projectId, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<CompetitorChannel>>([competitor]);
        public Task<CompetitorChannel?> GetCompetitorAsync(Guid projectId, Guid competitorId, bool forUpdate, CancellationToken cancellationToken) => Task.FromResult<CompetitorChannel?>(projectId == competitor.ProjectId && competitorId == competitor.Id ? competitor : null);
        public Task<CompetitorChannel?> FindCompetitorByYoutubeChannelIdAsync(Guid projectId, string youtubeChannelId, CancellationToken cancellationToken) => Task.FromResult<CompetitorChannel?>(null);
        public void AddCompetitor(CompetitorChannel channel) { }
        public Task<Job?> TryClaimNextCompetitorAnalysisJobAsync(DateTimeOffset now, CancellationToken cancellationToken)
        {
            if (Job?.Status != JobStatus.Queued) return Task.FromResult<Job?>(null);
            Job.Start(now); return Task.FromResult<Job?>(Job);
        }
        public Task<int> GetNextCompetitorAnalysisVersionAsync(Guid competitorId, CancellationToken cancellationToken) => Task.FromResult(Analyses.Count + 1);
        public void AddCompetitorAnalysis(CompetitorAnalysis analysis) => Analyses.Add(analysis);
        public void AddAiRun(AiRun run) => Runs.Add(run);
        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
