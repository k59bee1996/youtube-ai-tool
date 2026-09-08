using System.Text.Json;
using YoutubeAiFactory.Application.Common;
using YoutubeAiFactory.Application.Competitors;
using YoutubeAiFactory.Domain.Projects;

namespace YoutubeAiFactory.Application.Opportunities;

public sealed class OpportunityAnalysisContextBuilder(OpportunityAnalysisOptions options)
{
    public OpportunityAnalysisContext Build(Project project, IReadOnlyList<CurrentCompetitorAnalysis> analyses)
    {
        if (analyses.Count == 0) throw new ApplicationValidationException("Analyze at least one competitor before generating opportunities.");
        if (options.MaxCompetitors is < 1 or > 30 || options.MaxEvidenceItems is < 1 or > 200)
            throw new ApplicationValidationException("Opportunity analysis context limits are outside the supported range.");
        var selected = analyses.OrderByDescending(item => Parse(item.Analysis).Confidence.OverallConfidence).ThenBy(item => item.CompetitorId).Take(options.MaxCompetitors).ToArray();
        var evidence = new List<OpportunityEvidenceContext>();
        foreach (var source in selected)
        {
            var result = Parse(source.Analysis);
            Add(evidence, source, "audience", 0, null, string.Join("; ", result.Audience.LikelyInterests), result.Audience.Confidence, "Audience signal");
            foreach (var (item, index) in result.TopicClusters.Select((x, i) => (x, i)))
                Add(evidence, source, "topic", index, item.ExampleVideoIds.Count == 0 ? null : item.ExampleVideoIds[0], item.Description, item.Confidence, item.PerformanceSignal);
            foreach (var (item, index) in result.ContentFormats.Select((x, i) => (x, i)))
                Add(evidence, source, "format", index, item.EvidenceVideoIds.Count == 0 ? null : item.EvidenceVideoIds[0], item.Format, item.Confidence, item.PerformanceSignal);
            foreach (var (item, index) in result.TransferableFormats.Select((x, i) => (x, i)))
                Add(evidence, source, "transferable", index, item.EvidenceVideoIds.Count == 0 ? null : item.EvidenceVideoIds[0], $"{item.Format}: {item.TransferableMechanic}", item.Confidence, "Transferable format");
        }
        var bounded = evidence.OrderByDescending(item => item.Confidence).ThenBy(item => item.Id).Take(options.MaxEvidenceItems).ToArray();
        var limitations = new List<string> { "Signals reflect only the analyzed competitor dataset, not the whole YouTube market." };
        if (selected.Length == 1) limitations.Add("Only one competitor has been analyzed; cross-competitor confidence is limited.");
        if (analyses.Count > selected.Length) limitations.Add($"Context was bounded to {selected.Length} higher-confidence analyses.");
        return new OpportunityAnalysisContext(project.Id, project.Market.Name, project.Market.TargetLanguage, project.Market.TargetGeography,
            project.Audience.Description, selected.Length, bounded, limitations);
    }
    private static void Add(List<OpportunityEvidenceContext> target, CurrentCompetitorAnalysis source, string kind, int index, Guid? videoId, string summary, int confidence, string performance)
    {
        if (string.IsNullOrWhiteSpace(summary)) return;
        var id = $"{source.Analysis.Id:N}:{kind}:{index}";
        target.Add(new OpportunityEvidenceContext(id, source.Analysis.Id, source.CompetitorId, videoId == Guid.Empty ? null : videoId, kind,
            summary, ToDemand(performance), confidence));
    }
    private static int ToDemand(string signal) => signal.Contains("outlier", StringComparison.OrdinalIgnoreCase) || signal.Contains("strong", StringComparison.OrdinalIgnoreCase) ? 90
        : signal.Contains("above", StringComparison.OrdinalIgnoreCase) ? 75 : signal.Contains("weak", StringComparison.OrdinalIgnoreCase) ? 25 : 50;
    private static CompetitorAnalysisResult Parse(Domain.Competitors.CompetitorAnalysis analysis) => JsonSerializer.Deserialize<CompetitorAnalysisResult>(analysis.ResultJson, CompetitorAnalysisPrompt.SerializerOptions)
        ?? throw new ApplicationValidationException("A persisted competitor analysis could not be read.");
}
