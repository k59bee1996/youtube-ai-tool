using YoutubeAiFactory.Domain.Common;

namespace YoutubeAiFactory.Domain.Scripts;

public sealed class VideoScriptBlockClaim
{
    private VideoScriptBlockClaim() { }

    public VideoScriptBlockClaim(Guid scriptBlockId, Guid researchClaimId)
    {
        ScriptBlockId = Guard.NotEmpty(scriptBlockId, nameof(scriptBlockId));
        ResearchClaimId = Guard.NotEmpty(researchClaimId, nameof(researchClaimId));
    }

    public Guid ScriptBlockId { get; private set; }
    public Guid ResearchClaimId { get; private set; }
}
