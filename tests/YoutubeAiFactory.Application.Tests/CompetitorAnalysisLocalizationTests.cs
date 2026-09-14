using System.Text.Json;
using System.Text.Json.Nodes;
using YoutubeAiFactory.Application.Common;
using YoutubeAiFactory.Application.Competitors;
using YoutubeAiFactory.Application.Localization;
using YoutubeAiFactory.Application.Opportunities;
using YoutubeAiFactory.Domain.Opportunities;

namespace YoutubeAiFactory.Application.Tests;

public sealed class CompetitorAnalysisLocalizationTests
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
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

    [Fact]
    public void Localized_contract_accepts_reader_text_in_canonical_object_shapes()
    {
        const string json = """
            {"audience":{"likelyAgeRange":null,"likelyInterests":[],"likelyViewerIntent":[],"geographyHints":[],"evidence":[]},"topicClusters":[],"titlePatterns":[],"thumbnailPatterns":[],"hookPatterns":[],"contentFormats":[],"performanceInsights":[{"insight":"Nội dung thực hiện tốt.","supportingVideoIds":[],"confidence":70}],"potentialWeaknesses":[{"observation":"Thiếu bằng chứng.","supportingVideoIds":[],"confidence":50}],"transferableFormats":[],"evidenceNotes":[{"note":"Dữ liệu metadata.","videoIds":[]}],"confidence":{"dataQuality":"Trung bình","limitations":[]}}
            """;

        var localized = JsonSerializer.Deserialize<LocalizedCompetitorAnalysisContent>(json, SerializerOptions);

        Assert.Equal("Nội dung thực hiện tốt.", localized!.PerformanceInsights[0].Insight);
        Assert.Equal("Thiếu bằng chứng.", localized.PotentialWeaknesses[0].Observation);
        Assert.Equal("Dữ liệu metadata.", localized.EvidenceNotes[0].Note);
    }

    [Fact]
    public void Localization_prompt_supplies_a_strict_output_schema()
    {
        var source = new CompetitorAnalysisResult(
            Audience: new AudienceAnalysis(null, [], [], [], 60, []),
            TopicClusters: [],
            TitlePatterns: [],
            ThumbnailPatterns: [],
            HookPatterns: [],
            ContentFormats: [],
            PerformanceInsights: [],
            PotentialWeaknesses: [],
            TransferableFormats: [],
            EvidenceNotes: [],
            Confidence: new AnalysisConfidence(60, "Moderate", []));

        var request = CompetitorAnalysisLocalizationPrompt.Create(source, correcting: false);

        Assert.NotNull(request.OutputSchema);
        Assert.True(request.OutputSchema!.AsObject().ContainsKey("required"));
    }

    [Fact]
    public void Validator_rejects_a_translation_with_missing_confidence()
    {
        var source = new CompetitorAnalysisResult(
            Audience: new AudienceAnalysis(null, [], [], [], 60, []),
            TopicClusters: [], TitlePatterns: [], ThumbnailPatterns: [], HookPatterns: [], ContentFormats: [], PerformanceInsights: [], PotentialWeaknesses: [], TransferableFormats: [], EvidenceNotes: [], Confidence: new AnalysisConfidence(60, "Moderate", []));
        var localized = new LocalizedCompetitorAnalysisContent(
            Audience: new LocalizedAudience(null, [], [], [], []),
            TopicClusters: [], TitlePatterns: [], ThumbnailPatterns: [], HookPatterns: [], ContentFormats: [], PerformanceInsights: [], PotentialWeaknesses: [], TransferableFormats: [], EvidenceNotes: [], Confidence: null!);

        Assert.Throws<StructuredOutputException>(() => LocalizedCompetitorAnalysisValidator.Validate(source, localized));
    }

    [Fact]
    public void Opportunity_validator_rejects_missing_dynamic_candidate_name()
    {
        var report = new OpportunityReport(Guid.NewGuid(), 1, Guid.NewGuid(), "opportunity", 1, "fake", "fake", "score:v1", 1, "[]", DateTimeOffset.UtcNow);
        var candidate = new OpportunityCandidate(report.Id, "Canonical name", "Description", "Audience", "Topic", "Format", "Angle", "Reason", 60, 60, 60, 60, 60, 60, 60, 60, 60, 60, "[]", "[]", DateTimeOffset.UtcNow);
        var source = new OpportunityReportWithDetails(report, [], [new OpportunityCandidateWithEvidence(candidate, [])], []);
        var localized = new LocalizedOpportunityReportContent([], [new LocalizedOpportunityCandidate(null!, "Mô tả", "Khán giả", "Chủ đề", "Định dạng", "Góc độ", "Lý do", [], [], [])]);

        Assert.Throws<StructuredOutputException>(() => LocalizedOpportunityReportValidator.Validate(source, localized));
    }

    [Fact]
    public void Opportunity_localization_prompt_supplies_a_strict_output_schema()
    {
        var report = new OpportunityReport(Guid.NewGuid(), 1, Guid.NewGuid(), "opportunity", 1, "fake", "fake", "score:v1", 1, "[]", DateTimeOffset.UtcNow);
        var candidate = new OpportunityCandidate(report.Id, "Canonical name", "Description", "Audience", "Topic", "Format", "Angle", "Reason", 60, 60, 60, 60, 60, 60, 60, 60, 60, 60, "[]", "[]", DateTimeOffset.UtcNow);
        var source = new OpportunityReportWithDetails(report, [], [new OpportunityCandidateWithEvidence(candidate, [])], []);

        var request = OpportunityReportLocalizationPrompt.Create(source, correcting: false);

        Assert.NotNull(request.OutputSchema);
        Assert.True(request.OutputSchema!.AsObject().ContainsKey("required"));
    }

    [Theory]
    [InlineData("vi", "vi")]
    [InlineData("VI", "vi")]
    public void Vietnamese_locale_is_normalized(string input, string expected) =>
        Assert.Equal(expected, RequestCompetitorAnalysisLocalizationHandler.NormalizeLocale(input));
}
