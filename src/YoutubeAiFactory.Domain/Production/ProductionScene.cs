using YoutubeAiFactory.Domain.Common;

namespace YoutubeAiFactory.Domain.Production;

public sealed class ProductionScene
{
    private ProductionScene() { }

    public ProductionScene(
        Guid packageId,
        int sequence,
        ProductionScenePurpose purpose,
        string narrationSummary,
        string visualStrategy,
        int estimatedDurationSeconds,
        ProductionComplexity complexity,
        string transitionIntent,
        string musicBrief,
        string soundEffectCue,
        string voiceDirection
    )
    {
        Id = Guid.NewGuid();
        ProductionPackageId = Guard.NotEmpty(packageId, nameof(packageId));
        if (sequence < 1 || estimatedDurationSeconds < 1)
            throw new DomainException("Scene sequence and duration must be positive.");
        Sequence = sequence;
        Purpose = purpose;
        EstimatedDurationSeconds = estimatedDurationSeconds;
        Complexity = complexity;
        Update(
            narrationSummary,
            visualStrategy,
            transitionIntent,
            musicBrief,
            soundEffectCue,
            voiceDirection
        );
    }

    public Guid Id { get; private set; }
    public Guid ProductionPackageId { get; private set; }
    public int Sequence { get; private set; }
    public ProductionScenePurpose Purpose { get; private set; }
    public string NarrationSummary { get; private set; } = string.Empty;
    public string VisualStrategy { get; private set; } = string.Empty;
    public int EstimatedDurationSeconds { get; private set; }
    public ProductionComplexity Complexity { get; private set; }
    public string TransitionIntent { get; private set; } = string.Empty;
    public string MusicBrief { get; private set; } = string.Empty;
    public string SoundEffectCue { get; private set; } = string.Empty;
    public string VoiceDirection { get; private set; } = string.Empty;

    public void Update(
        string summary,
        string visual,
        string transition,
        string music,
        string sfx,
        string voice
    )
    {
        NarrationSummary = Guard.Required(summary, nameof(summary), 2_000);
        VisualStrategy = Guard.Required(visual, nameof(visual), 4_000);
        TransitionIntent = Guard.Required(transition, nameof(transition), 1_000);
        MusicBrief = Guard.Required(music, nameof(music), 1_000);
        SoundEffectCue = Guard.Required(sfx, nameof(sfx), 1_000);
        VoiceDirection = Guard.Required(voice, nameof(voice), 1_000);
    }
}

public sealed class ProductionSceneScriptBlock
{
    private ProductionSceneScriptBlock() { }

    public ProductionSceneScriptBlock(
        Guid packageId,
        Guid sceneId,
        Guid scriptBlockId,
        int sequence
    )
    {
        ProductionPackageId = Guard.NotEmpty(packageId, nameof(packageId));
        ProductionSceneId = Guard.NotEmpty(sceneId, nameof(sceneId));
        ScriptBlockId = Guard.NotEmpty(scriptBlockId, nameof(scriptBlockId));
        if (sequence < 1)
            throw new DomainException("Mapping sequence must be positive.");
        Sequence = sequence;
    }

    public Guid ProductionPackageId { get; private set; }
    public Guid ProductionSceneId { get; private set; }
    public Guid ScriptBlockId { get; private set; }
    public int Sequence { get; private set; }
}
