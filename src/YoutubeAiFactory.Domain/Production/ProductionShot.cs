using YoutubeAiFactory.Domain.Common;

namespace YoutubeAiFactory.Domain.Production;

public sealed class ProductionShot
{
    private ProductionShot() { }

    public ProductionShot(
        Guid sceneId,
        int sequence,
        ProductionShotType shotType,
        string visualDescription,
        string composition,
        string motionSuggestion,
        int estimatedDurationSeconds,
        ProductionFactualityMode factualityMode,
        Guid? assetRequirementId,
        string notes
    )
    {
        Id = Guid.NewGuid();
        ProductionSceneId = Guard.NotEmpty(sceneId, nameof(sceneId));
        if (sequence < 1 || estimatedDurationSeconds < 1)
            throw new DomainException("Shot sequence and duration must be positive.");
        Sequence = sequence;
        ShotType = shotType;
        EstimatedDurationSeconds = estimatedDurationSeconds;
        FactualityMode = factualityMode;
        AssetRequirementId = assetRequirementId;
        Update(visualDescription, composition, motionSuggestion, notes);
    }

    public Guid Id { get; private set; }
    public Guid ProductionSceneId { get; private set; }
    public int Sequence { get; private set; }
    public ProductionShotType ShotType { get; private set; }
    public string VisualDescription { get; private set; } = string.Empty;
    public string Composition { get; private set; } = string.Empty;
    public string MotionSuggestion { get; private set; } = string.Empty;
    public int EstimatedDurationSeconds { get; private set; }
    public ProductionFactualityMode FactualityMode { get; private set; }
    public Guid? AssetRequirementId { get; private set; }
    public string Notes { get; private set; } = string.Empty;

    public void Update(string visual, string composition, string motion, string notes)
    {
        VisualDescription = Guard.Required(visual, nameof(visual), 4_000);
        Composition = Guard.Required(composition, nameof(composition), 2_000);
        MotionSuggestion = Guard.Required(motion, nameof(motion), 2_000);
        Notes = string.IsNullOrWhiteSpace(notes) ? string.Empty : notes.Trim();
    }
}

public sealed class ProductionShotClaim
{
    private ProductionShotClaim() { }

    public ProductionShotClaim(Guid shotId, Guid claimId)
    {
        ProductionShotId = Guard.NotEmpty(shotId, nameof(shotId));
        ResearchClaimId = Guard.NotEmpty(claimId, nameof(claimId));
    }

    public Guid ProductionShotId { get; private set; }
    public Guid ResearchClaimId { get; private set; }
}

public sealed class ProductionOnScreenText
{
    private ProductionOnScreenText() { }

    public ProductionOnScreenText(
        Guid sceneId,
        int sequence,
        string text,
        ProductionOnScreenTextType type,
        string timingIntent
    )
    {
        Id = Guid.NewGuid();
        ProductionSceneId = Guard.NotEmpty(sceneId, nameof(sceneId));
        if (sequence < 1)
            throw new DomainException("On-screen text sequence must be positive.");
        Sequence = sequence;
        Type = type;
        Update(text, timingIntent);
    }

    public Guid Id { get; private set; }
    public Guid ProductionSceneId { get; private set; }
    public int Sequence { get; private set; }
    public string Text { get; private set; } = string.Empty;
    public ProductionOnScreenTextType Type { get; private set; }
    public string TimingIntent { get; private set; } = string.Empty;

    public void Update(string text, string timing)
    {
        Text = Guard.Required(text, nameof(text), 1_000);
        TimingIntent = Guard.Required(timing, nameof(timing), 500);
    }
}

public sealed class ProductionOnScreenTextClaim
{
    private ProductionOnScreenTextClaim() { }

    public ProductionOnScreenTextClaim(Guid textId, Guid claimId)
    {
        ProductionOnScreenTextId = Guard.NotEmpty(textId, nameof(textId));
        ResearchClaimId = Guard.NotEmpty(claimId, nameof(claimId));
    }

    public Guid ProductionOnScreenTextId { get; private set; }
    public Guid ResearchClaimId { get; private set; }
}
