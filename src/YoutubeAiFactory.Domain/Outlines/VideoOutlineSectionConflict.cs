using YoutubeAiFactory.Domain.Common;

namespace YoutubeAiFactory.Domain.Outlines;

public sealed class VideoOutlineSectionConflict
{
    private VideoOutlineSectionConflict() { }

    public VideoOutlineSectionConflict(Guid outlineSectionId, Guid researchConflictId)
    {
        OutlineSectionId = Guard.NotEmpty(outlineSectionId, nameof(outlineSectionId));
        ResearchConflictId = Guard.NotEmpty(researchConflictId, nameof(researchConflictId));
    }

    public Guid OutlineSectionId { get; private set; }
    public Guid ResearchConflictId { get; private set; }
}
