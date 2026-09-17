using YoutubeAiFactory.Domain.Common;

namespace YoutubeAiFactory.Domain.Outlines;

/// <summary>References a gap by its stable zero-based position in the immutable source ResearchReport payload.</summary>
public sealed class VideoOutlineSectionGap
{
    private VideoOutlineSectionGap() { }

    public VideoOutlineSectionGap(Guid outlineSectionId, int researchGapIndex)
    {
        OutlineSectionId = Guard.NotEmpty(outlineSectionId, nameof(outlineSectionId));
        if (researchGapIndex < 0) throw new DomainException("Research gap index cannot be negative.");
        ResearchGapIndex = researchGapIndex;
    }

    public Guid OutlineSectionId { get; private set; }
    public int ResearchGapIndex { get; private set; }
}
