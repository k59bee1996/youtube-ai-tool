using YoutubeAiFactory.Domain.Common;

namespace YoutubeAiFactory.Domain.Production;

public sealed class ProductionAssetRequirement
{
    private ProductionAssetRequirement() { }

    public ProductionAssetRequirement(
        Guid packageId,
        string assetKey,
        ProductionAssetType assetType,
        ProductionAcquisitionMode acquisitionMode,
        string creativeBrief,
        string generationPrompt,
        string sourceSearchBrief,
        bool rightsVerificationRequired,
        ProductionFactualityMode factualityMode,
        string reuseKey,
        ProductionComplexity complexity
    )
    {
        Id = Guid.NewGuid();
        ProductionPackageId = Guard.NotEmpty(packageId, nameof(packageId));
        AssetKey = Guard.Required(assetKey, nameof(assetKey), 100);
        AssetType = assetType;
        AcquisitionMode = acquisitionMode;
        RightsVerificationRequired = rightsVerificationRequired;
        FactualityMode = factualityMode;
        ReuseKey = Guard.Required(reuseKey, nameof(reuseKey), 100);
        Complexity = complexity;
        Update(creativeBrief, generationPrompt, sourceSearchBrief);
    }

    public Guid Id { get; private set; }
    public Guid ProductionPackageId { get; private set; }
    public string AssetKey { get; private set; } = string.Empty;
    public ProductionAssetType AssetType { get; private set; }
    public ProductionAcquisitionMode AcquisitionMode { get; private set; }
    public string CreativeBrief { get; private set; } = string.Empty;
    public string GenerationPrompt { get; private set; } = string.Empty;
    public string SourceSearchBrief { get; private set; } = string.Empty;
    public bool RightsVerificationRequired { get; private set; }
    public ProductionFactualityMode FactualityMode { get; private set; }
    public string ReuseKey { get; private set; } = string.Empty;
    public ProductionComplexity Complexity { get; private set; }

    public void Update(string brief, string prompt, string search)
    {
        CreativeBrief = Guard.Required(brief, nameof(brief), 4_000);
        GenerationPrompt = string.IsNullOrWhiteSpace(prompt) ? string.Empty : prompt.Trim();
        SourceSearchBrief = string.IsNullOrWhiteSpace(search) ? string.Empty : search.Trim();
    }
}

public sealed class ProductionAssetClaim
{
    private ProductionAssetClaim() { }

    public ProductionAssetClaim(Guid assetId, Guid claimId)
    {
        ProductionAssetRequirementId = Guard.NotEmpty(assetId, nameof(assetId));
        ResearchClaimId = Guard.NotEmpty(claimId, nameof(claimId));
    }

    public Guid ProductionAssetRequirementId { get; private set; }
    public Guid ResearchClaimId { get; private set; }
}
