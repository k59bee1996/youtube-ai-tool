using YoutubeAiFactory.Application.Common;

namespace YoutubeAiFactory.Application.Competitors;

public static class CompetitorAnalysisValidator
{
    public static void Validate(CompetitorAnalysisResult result, CompetitorAnalysisContext context)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(context);
        var videoIds = context.Videos.Select(video => video.Id).ToHashSet();
        ValidateConfidence(result.Confidence.OverallConfidence, "overall confidence");
        ValidateConfidence(result.Audience.Confidence, "audience confidence");
        ValidateReferences(result.TopicClusters.SelectMany(item => item.ExampleVideoIds), videoIds);
        ValidateReferences(result.ContentFormats.SelectMany(item => item.EvidenceVideoIds), videoIds);
        ValidateReferences(result.PerformanceInsights.SelectMany(item => item.SupportingVideoIds), videoIds);
        ValidateReferences(result.PotentialWeaknesses.SelectMany(item => item.SupportingVideoIds), videoIds);
        ValidateReferences(result.TransferableFormats.SelectMany(item => item.EvidenceVideoIds), videoIds);
        ValidateReferences(result.EvidenceNotes.SelectMany(item => item.VideoIds), videoIds);
        foreach (var item in result.TopicClusters)
        {
            ValidateConfidence(item.Confidence, "topic confidence");
            if (item.Frequency < 0 || item.Frequency > context.AnalyzedVideoCount)
                throw new StructuredOutputException("A topic frequency falls outside the analyzed video count.");
        }
        foreach (var item in result.TitlePatterns)
        {
            ValidateConfidence(item.Confidence, "title pattern confidence");
            if (item.ObservedFrequency < 0 || item.ObservedFrequency > context.AnalyzedVideoCount)
                throw new StructuredOutputException("A title-pattern frequency falls outside the analyzed video count.");
        }
        foreach (var confidence in result.ContentFormats.Select(item => item.Confidence)
            .Concat(result.PerformanceInsights.Select(item => item.Confidence))
            .Concat(result.PotentialWeaknesses.Select(item => item.Confidence))
            .Concat(result.TransferableFormats.Select(item => item.Confidence)))
            ValidateConfidence(confidence, "analysis confidence");
    }

    private static void ValidateReferences(IEnumerable<Guid> references, HashSet<Guid> knownVideoIds)
    {
        if (references.Any(id => id == Guid.Empty || !knownVideoIds.Contains(id)))
            throw new StructuredOutputException("Analysis references a video that was not included in its evidence context.");
    }

    private static void ValidateConfidence(int confidence, string field)
    {
        if (confidence is < 0 or > 100) throw new StructuredOutputException($"The {field} must be between 0 and 100.");
    }
}
