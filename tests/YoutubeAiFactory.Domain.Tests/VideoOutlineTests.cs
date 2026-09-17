using YoutubeAiFactory.Domain.Common;
using YoutubeAiFactory.Domain.Outlines;
using YoutubeAiFactory.Domain.Pilots;

namespace YoutubeAiFactory.Domain.Tests;

public sealed class VideoOutlineTests
{
    [Fact]
    public void Ready_outline_requires_transition_review_after_reorder_before_approval()
    {
        var outline = CreateOutline();
        var now = DateTimeOffset.UtcNow;

        outline.RecordReorder(now);

        Assert.True(outline.TransitionsRequireReview);
        Assert.Throws<DomainException>(() => outline.Approve(now));

        outline.RecordEdit(now.AddSeconds(1), 180);
        outline.Approve(now.AddSeconds(2));

        Assert.Equal(VideoOutlineStatus.Approved, outline.Status);
        Assert.Equal(180, outline.TotalEstimatedSeconds);
        Assert.Equal(now.AddSeconds(2), outline.ApprovedAt);
    }

    [Fact]
    public void Approved_outline_is_immutable()
    {
        var outline = CreateOutline();
        outline.Approve(DateTimeOffset.UtcNow);

        Assert.Throws<DomainException>(() => outline.RecordEdit(DateTimeOffset.UtcNow, outline.TotalEstimatedSeconds));
        Assert.Throws<DomainException>(() => outline.RecordReorder(DateTimeOffset.UtcNow));
        Assert.Throws<DomainException>(() => outline.Approve(DateTimeOffset.UtcNow));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Section_rejects_non_positive_sequence(int sequence) =>
        Assert.Throws<DomainException>(() => new VideoOutlineSection(Guid.NewGuid(), sequence, "Context",
            OutlineSectionPurpose.Context, "Explain the mechanism.", "Use supported research.", null, null, 60));

    [Theory]
    [InlineData(14)]
    [InlineData(1801)]
    public void Section_rejects_out_of_range_duration(int seconds) =>
        Assert.Throws<DomainException>(() => new VideoOutlineSection(Guid.NewGuid(), 1, "Context",
            OutlineSectionPurpose.Context, "Explain the mechanism.", "Use supported research.", null, null, seconds));

    private static VideoOutline CreateOutline() => new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1, 1,
        Guid.NewGuid(), "outline-engine:v1", "outline-generation", 1, new string('a', 64), "fake", "reasoning-model",
        OutlineStructureType.Explainer, "What made ownership costly?", "Prestige carried recurring obligations.",
        "Open on the visible-wealth versus hidden-cost contradiction.", "Understand the real ownership cost.",
        "Move from ownership context through recurring obligations to the economic payoff.",
        "Ownership was a system of continuing obligations, not a one-time purchase.",
        "Use concise setup, escalating evidence, and a synthesis.", PilotExperimentType.Packaging, "Hidden-cost framing",
        "Keep the packaging promise stable.", "The outline preserves the hidden-cost premise without overstating it.",
        "[]", "[]", 420, DateTimeOffset.UtcNow);
}
