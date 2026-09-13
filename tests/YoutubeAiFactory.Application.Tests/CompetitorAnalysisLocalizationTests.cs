using YoutubeAiFactory.Application.Common;
using YoutubeAiFactory.Application.Competitors;
using YoutubeAiFactory.Application.Localization;

namespace YoutubeAiFactory.Application.Tests;

public sealed class CompetitorAnalysisLocalizationTests
{
    [Fact]
    public void Validator_rejects_a_translation_that_changes_canonical_structure()
    {
        var source = new CompetitorAnalysisResult(
            Audience: new AudienceAnalysis(null, ["Creators"], ["Learn"], [], 60, ["Description"]),
            TopicClusters: [new TopicCluster("Research", "Description", [], 1, "Typical", 60)],
            TitlePatterns: [], ThumbnailPatterns: [], HookPatterns: [], ContentFormats: [], PerformanceInsights: [], PotentialWeaknesses: [], TransferableFormats: [], EvidenceNotes: [], Confidence: new AnalysisConfidence(60, "Moderate", []));
        var localized = new LocalizedCompetitorAnalysisContent(
            Audience: new LocalizedAudience(null, ["Creators"], ["Learn"], [], ["Description"]),
            TopicClusters: [], TitlePatterns: [], ThumbnailPatterns: [], HookPatterns: [], ContentFormats: [], PerformanceInsights: [], PotentialWeaknesses: [], TransferableFormats: [], EvidenceNotes: [], Confidence: new LocalizedConfidence("Moderate", []));

        Assert.Throws<StructuredOutputException>(() => LocalizedCompetitorAnalysisValidator.Validate(source, localized));
    }

    [Theory]
    [InlineData("vi", "vi")]
    [InlineData("VI", "vi")]
    public void Vietnamese_locale_is_normalized(string input, string expected) =>
        Assert.Equal(expected, RequestCompetitorAnalysisLocalizationHandler.NormalizeLocale(input));
}
