using YoutubeAiFactory.Application.Common;
using YoutubeAiFactory.Domain.Competitors;

namespace YoutubeAiFactory.Application.Competitors;

public sealed class CompetitorAnalysisContextBuilder(CompetitorAnalysisOptions options)
{
    public CompetitorAnalysisContext Build(CompetitorChannel competitor)
    {
        ArgumentNullException.ThrowIfNull(competitor);
        if (options.MaxVideos is < 1 or > CompetitorAnalysisOptions.MaximumVideoLimit)
            throw new ApplicationValidationException($"CompetitorAnalysis:MaxVideos must be between 1 and {CompetitorAnalysisOptions.MaximumVideoLimit}.");
        var videos = competitor.Videos.OrderByDescending(video => video.PublishedAt).ToArray();
        if (videos.Length < options.MinimumVideos)
            throw new ApplicationValidationException("Collect at least one competitor video before running AI analysis.");

        var views = videos.Where(video => video.ViewCount.HasValue).Select(video => video.ViewCount!.Value).Order().ToArray();
        long? median = views.Length == 0 ? null : views.Length % 2 == 1 ? views[views.Length / 2] : (views[(views.Length / 2) - 1] + views[views.Length / 2]) / 2;
        var hasUsefulSample = views.Length >= 3 && median.GetValueOrDefault() > 0;
        var selected = SelectVideos(videos, median, hasUsefulSample);
        var limitations = new List<string>();
        if (videos.Length < 3) limitations.Add("Fewer than three collected videos limit performance confidence.");
        if (views.Length == 0) limitations.Add("View counts are unavailable for collected videos.");
        if (videos.Any(video => video.Description is null)) limitations.Add("Some video descriptions are unavailable.");
        limitations.Add("No transcripts, comments, or visual thumbnail analysis were supplied.");
        var dated = videos.Where(video => video.PublishedAt.HasValue).OrderByDescending(video => video.PublishedAt).ToArray();
        double? cadence = dated.Length < 2 ? null : dated.Zip(dated.Skip(1), (newer, older) => (newer.PublishedAt!.Value - older.PublishedAt!.Value).TotalDays).Average();
        return new CompetitorAnalysisContext(
            competitor.Id, competitor.Title!, competitor.Description, competitor.Handle, competitor.SubscriberCount,
            videos.Length, selected.Length, median, views.Length == 0 ? null : views.Average(), cadence,
            selected.Select(video => ToContext(video, median, hasUsefulSample)).ToArray(), limitations,
            competitor.LastCollectedAt!.Value);
    }

    private CompetitorVideo[] SelectVideos(CompetitorVideo[] videos, long? median, bool hasUsefulSample)
    {
        var chosen = new Dictionary<Guid, CompetitorVideo>();
        void Add(IEnumerable<CompetitorVideo> source) { foreach (var video in source) { if (chosen.Count == options.MaxVideos) break; chosen.TryAdd(video.Id, video); } }
        Add(videos.Take(Math.Min(10, options.MaxVideos)));
        if (hasUsefulSample)
        {
            Add(videos.OrderByDescending(video => Ratio(video, median)).Take(8));
            Add(videos.OrderBy(video => Ratio(video, median)).Take(6));
        }
        Add(videos.OrderByDescending(video => video.ViewCount).Take(options.MaxVideos));
        return chosen.Values.Take(options.MaxVideos).ToArray();
    }

    private static AnalysisVideoContext ToContext(CompetitorVideo video, long? median, bool hasUsefulSample)
    {
        var ratio = hasUsefulSample ? Ratio(video, median) : null;
        decimal? engagement = video.ViewCount is > 0 && (video.LikeCount.HasValue || video.CommentCount.HasValue)
            ? decimal.Round(((video.LikeCount ?? 0) + (video.CommentCount ?? 0)) * 100m / video.ViewCount.Value, 2) : null;
        return new AnalysisVideoContext(video.Id, video.Title, video.Description, video.PublishedAt, video.ViewCount,
            video.LikeCount, video.CommentCount, engagement, ratio, Classify(ratio, hasUsefulSample));
    }

    private static decimal? Ratio(CompetitorVideo video, long? median) => video.ViewCount.HasValue && median is > 0 ? decimal.Round((decimal)video.ViewCount.Value / median.Value, 2) : null;
    public static PerformanceClassification Classify(decimal? ratio, bool hasUsefulSample) => !hasUsefulSample || ratio is null ? PerformanceClassification.InsufficientData
        : ratio >= 2.5m ? PerformanceClassification.StrongOutlier
        : ratio >= 1.4m ? PerformanceClassification.AboveBaseline
        : ratio >= .7m ? PerformanceClassification.Typical
        : ratio >= .4m ? PerformanceClassification.BelowBaseline : PerformanceClassification.WeakPerformer;
}
