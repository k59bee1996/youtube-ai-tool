using YoutubeAiFactory.Application.Common;

namespace YoutubeAiFactory.Application.Competitors;

public static class CompetitorAnalysisValidator
{
    public static void Validate(CompetitorAnalysisResult result, CompetitorAnalysisContext context)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(context);
        Require(result.Audience, "audience");
        Require(result.TopicClusters, "topic clusters");
        Require(result.TitlePatterns, "title patterns");
        Require(result.ThumbnailPatterns, "thumbnail patterns");
        Require(result.HookPatterns, "hook patterns");
        Require(result.ContentFormats, "content formats");
        Require(result.PerformanceInsights, "performance insights");
        Require(result.PotentialWeaknesses, "potential weaknesses");
        Require(result.TransferableFormats, "transferable formats");
        Require(result.EvidenceNotes, "evidence notes");
        Require(result.Confidence, "confidence");
        Require(result.Audience.LikelyInterests, "audience interests");
        Require(result.Audience.LikelyViewerIntent, "audience viewer intent");
        Require(result.Audience.GeographyHints, "audience geography hints");
        Require(result.Audience.Evidence, "audience evidence");
        Require(result.Confidence.Limitations, "analysis limitations");
        RequireItems(result.TopicClusters, "topic cluster", item => Require(item.ExampleVideoIds, "topic cluster examples"));
        RequireItems(result.TitlePatterns, "title pattern", item => Require(item.ExampleTitles, "title pattern examples"));
        RequireItems(result.ThumbnailPatterns, "thumbnail pattern", item =>
        {
            Require(item.EvidenceVideoIds, "thumbnail pattern evidence");
            Require(item.Limitations, "thumbnail pattern limitations");
        });
        RequireItems(result.HookPatterns, "hook pattern", item =>
        {
            Require(item.EvidenceVideoIds, "hook pattern evidence");
            Require(item.Limitations, "hook pattern limitations");
        });
        RequireItems(result.ContentFormats, "content format", item => Require(item.EvidenceVideoIds, "content format evidence"));
        RequireItems(result.PerformanceInsights, "performance insight", item => Require(item.SupportingVideoIds, "performance insight evidence"));
        RequireItems(result.PotentialWeaknesses, "potential weakness", item => Require(item.SupportingVideoIds, "potential weakness evidence"));
        RequireItems(result.TransferableFormats, "transferable format", item => Require(item.EvidenceVideoIds, "transferable format evidence"));
        RequireItems(result.EvidenceNotes, "evidence note", item => Require(item.VideoIds, "evidence note video IDs"));
        var videoIds = context.Videos.Select(video => video.Id).ToHashSet();
        ValidateConfidence(result.Confidence.OverallConfidence, "overall confidence");
        ValidateConfidence(result.Audience.Confidence, "audience confidence");
        ValidateReferences(result.TopicClusters.SelectMany(item => item.ExampleVideoIds), videoIds);
        ValidateReferences(result.ThumbnailPatterns.SelectMany(item => item.EvidenceVideoIds), videoIds);
        ValidateReferences(result.HookPatterns.SelectMany(item => item.EvidenceVideoIds), videoIds);
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
        foreach (var pattern in result.ThumbnailPatterns)
        {
            ValidateConfidence(pattern.Confidence, "thumbnail pattern confidence");
        }
        foreach (var pattern in result.HookPatterns)
        {
            ValidateConfidence(pattern.Confidence, "hook pattern confidence");
        }
        foreach (var confidence in result.ContentFormats.Select(item => item.Confidence)
            .Concat(result.PerformanceInsights.Select(item => item.Confidence))
            .Concat(result.PotentialWeaknesses.Select(item => item.Confidence))
            .Concat(result.TransferableFormats.Select(item => item.Confidence)))
            ValidateConfidence(confidence, "analysis confidence");
    }

    private static void Require<T>(T? value, string field)
    {
        if (value is null) throw new StructuredOutputException($"The {field} is required in structured output.");
    }

    private static void RequireItems<T>(IReadOnlyList<T> values, string field, Action<T> validate)
    {
        foreach (var value in values)
        {
            Require(value, field);
            validate(value);
        }
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
