using YoutubeAiFactory.Domain.Common;

namespace YoutubeAiFactory.Domain.Scripts;

public sealed class VideoScriptBlockConflict
{
    private VideoScriptBlockConflict() { }

    public VideoScriptBlockConflict(Guid scriptBlockId, Guid researchConflictId)
    {
        ScriptBlockId = Guard.NotEmpty(scriptBlockId, nameof(scriptBlockId));
        ResearchConflictId = Guard.NotEmpty(researchConflictId, nameof(researchConflictId));
    }

    public Guid ScriptBlockId { get; private set; }
    public Guid ResearchConflictId { get; private set; }
}
