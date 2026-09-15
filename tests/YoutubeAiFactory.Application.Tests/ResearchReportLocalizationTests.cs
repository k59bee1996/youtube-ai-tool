using System.Text.Json;
using YoutubeAiFactory.Application.Common;
using YoutubeAiFactory.Application.Localization;
using YoutubeAiFactory.Application.Research;
using YoutubeAiFactory.Domain.Research;

namespace YoutubeAiFactory.Application.Tests;

public sealed class ResearchReportLocalizationTests
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public void Validator_rejects_a_translation_that_reorders_indexed_findings()
    {
        var canonical = CreateCanonicalReport();
        var localized = new LocalizedResearchReportContent("Localized report", [
            new LocalizedResearchFinding(1, "Second localized finding", "Localized category"),
            new LocalizedResearchFinding(0, "First localized finding", "Localized category"),
        ], [], [], []);

        Assert.Throws<StructuredOutputException>(() => LocalizedResearchReportValidator.Validate(canonical, localized));
    }

    [Fact]
    public void Validator_accepts_the_exact_canonical_index_sequence()
    {
        var canonical = CreateCanonicalReport();
        var localized = new LocalizedResearchReportContent("Localized report", [
            new LocalizedResearchFinding(0, "First localized finding", "Localized category"),
            new LocalizedResearchFinding(1, "Second localized finding", "Localized category"),
        ], [], [], []);

        LocalizedResearchReportValidator.Validate(canonical, localized);
    }

    private static ResearchReportWithDetails CreateCanonicalReport()
    {
        var now = DateTimeOffset.UtcNow;
        var report = new ResearchReport(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1, "research-engine:v1", "input", JsonSerializer.Serialize(
            new ResearchReportPayload(
                new ResearchSynthesisResult("Summary", [
                    new ResearchFinding("Finding one", "Category", [Guid.NewGuid()], [Guid.NewGuid()]),
                    new ResearchFinding("Finding two", "Category", [Guid.NewGuid()], [Guid.NewGuid()]),
                ], [], [], []),
                new ResearchConfidenceSummary("Low", 1, 1, 2, 0, 2, 0, 0),
                new ResearchMetricsDto(1, 1, 1, 1, 1, 2, 2, 0, 2, 0, 0, 0, 0, 0),
                new ResearchPlan("Objective", [], [], [], [])), SerializerOptions), Guid.NewGuid(), now);
        return new ResearchReportWithDetails(report, [], [], [], [], []);
    }
}
