using System.Text.Json;
using YoutubeAiFactory.Application.Common;
using YoutubeAiFactory.Application.Localization;
using YoutubeAiFactory.Application.Outlines;
using YoutubeAiFactory.Domain.Localization;
using YoutubeAiFactory.Domain.Outlines;
using YoutubeAiFactory.Domain.Pilots;

namespace YoutubeAiFactory.Application.Tests;

public sealed class VideoOutlineLocalizationTests
{
    [Fact]
    public void Accepts_translation_that_preserves_outline_section_identity_order_and_null_topology()
    {
        var canonical = Canonical();
        var localized = new LocalizedVideoOutlineContent(LocalizedVideoOutlineValidator.CreateFingerprint(canonical),
            "Câu hỏi", "Mâu thuẫn", "Mở đầu", "Lời hứa",
            "Tiến trình", "Kết quả", "Nhịp độ", "Giữ nguyên thử nghiệm",
            [new(0, "Rủi ro")], [new(0, "Cảnh báo")],
            canonical.Sections.Select(section => new LocalizedOutlineSection(section.Id, $"VI {section.Heading}",
                $"VI {section.Objective}", $"VI {section.Summary}",
                section.ViewerQuestion is null ? null : $"VI {section.ViewerQuestion}",
                section.TransitionIntent is null ? null : $"VI {section.TransitionIntent}")).ToArray());

        LocalizedVideoOutlineValidator.Validate(canonical, localized);
    }

    [Fact]
    public void Rejects_translation_that_reorders_sections_or_changes_null_topology()
    {
        var canonical = Canonical();
        var reversed = canonical.Sections.Reverse().Select(section => new LocalizedOutlineSection(section.Id,
            "Tiêu đề", "Mục tiêu", "Tóm tắt", section.ViewerQuestion, section.TransitionIntent)).ToArray();
        var localized = new LocalizedVideoOutlineContent(LocalizedVideoOutlineValidator.CreateFingerprint(canonical),
            "Câu hỏi", "Mâu thuẫn", "Mở đầu", "Lời hứa",
            "Tiến trình", "Kết quả", "Nhịp độ", "Giữ nguyên thử nghiệm",
            [new(0, "Rủi ro")], [new(0, "Cảnh báo")], reversed);

        Assert.Throws<StructuredOutputException>(() => LocalizedVideoOutlineValidator.Validate(canonical, localized));

        var wrongNull = localized with
        {
            Sections = canonical.Sections.Select(section => new LocalizedOutlineSection(section.Id,
                "Tiêu đề", "Mục tiêu", "Tóm tắt", "Không được thêm", section.TransitionIntent)).ToArray(),
        };
        Assert.Throws<StructuredOutputException>(() => LocalizedVideoOutlineValidator.Validate(canonical, wrongNull));
    }

    [Fact]
    public void Cached_translation_becomes_invalid_after_the_ready_outline_is_edited()
    {
        var canonical = Canonical();
        var createdAt = canonical.Outline.CreatedAt.AddMinutes(1);
        var content = new LocalizedVideoOutlineContent(LocalizedVideoOutlineValidator.CreateFingerprint(canonical),
            "Question", "Tension", "Hook", "Promise", "Progression",
            "Payoff", "Pacing", "Alignment", [new(0, "Risk")], [new(0, "Warning")],
            canonical.Sections.Select(section => new LocalizedOutlineSection(section.Id, section.Heading,
                section.Objective, section.Summary, section.ViewerQuestion, section.TransitionIntent)).ToArray());
        var cached = new ArtifactLocalization(LocalizableArtifactTypes.VideoOutline, canonical.Outline.Id,
            canonical.Outline.Version, "vi", JsonSerializer.Serialize(content), Guid.NewGuid(),
            VideoOutlineLocalizationPrompt.Key, VideoOutlineLocalizationPrompt.Version, "Fake", "fake", createdAt);

        Assert.True(LocalizedVideoOutlineCache.IsValid(canonical, cached));

        canonical.Sections[0].UpdatePlanningContent("Edited heading", canonical.Sections[0].Objective,
            canonical.Sections[0].Summary, canonical.Sections[0].ViewerQuestion,
            canonical.Sections[0].TransitionIntent, canonical.Sections[0].EstimatedSeconds);
        canonical.Outline.RecordEdit(createdAt.AddSeconds(1), canonical.Outline.TotalEstimatedSeconds);
        var lateStaleTranslation = new ArtifactLocalization(LocalizableArtifactTypes.VideoOutline,
            canonical.Outline.Id, canonical.Outline.Version, "vi", JsonSerializer.Serialize(content), Guid.NewGuid(),
            VideoOutlineLocalizationPrompt.Key, VideoOutlineLocalizationPrompt.Version, "Fake", "fake",
            createdAt.AddSeconds(2));

        Assert.False(LocalizedVideoOutlineCache.IsValid(canonical, cached));
        Assert.False(LocalizedVideoOutlineCache.IsValid(canonical, lateStaleTranslation));
    }

    private static VideoOutlineWithDetails Canonical()
    {
        var outline = new VideoOutline(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1, 1, Guid.NewGuid(),
            "outline-engine:v1", "outline-generation", 1, new string('a', 64), "fake", "model",
            OutlineStructureType.Explainer, "Question", "Tension", "Hook", "Promise", "Progression", "Payoff",
            "Pacing", PilotExperimentType.Packaging, "Variable", "Control", "Alignment",
            "[\"Risk\"]", "[\"Warning\"]", 120, DateTimeOffset.UtcNow);
        var first = new VideoOutlineSection(outline.Id, 1, "First", OutlineSectionPurpose.Hook,
            "Frame the question", "Planning summary", null, "Move to context", 60);
        var second = new VideoOutlineSection(outline.Id, 2, "Second", OutlineSectionPurpose.Conclusion,
            "Resolve the question", "Planning synthesis", null, null, 60);
        return new(outline, [first, second], [], [], []);
    }
}
