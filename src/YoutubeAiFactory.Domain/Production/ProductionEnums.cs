namespace YoutubeAiFactory.Domain.Production;

public enum ProductionPackageStatus
{
    Ready,
    Approved,
}

public enum ProductionGroundingStatus
{
    Pending,
    Passed,
    Failed,
}

public enum ProductionGroundingIssueSeverity
{
    Warning,
    Error,
}

public enum ProductionGroundingIssueType
{
    UnsupportedVisualFact,
    UnsupportedOnScreenText,
    UnsupportedNumber,
    UnsupportedDate,
    UnsupportedMap,
    UnsupportedChart,
    VisualScriptContradiction,
    ConflictMisrepresented,
    FalseArchivalImpression,
    ClaimMismatch,
}

public enum ProductionScenePurpose
{
    Hook,
    Establish,
    Explain,
    Escalate,
    Demonstrate,
    Compare,
    Conflict,
    Transition,
    Payoff,
    Conclusion,
}

public enum ProductionComplexity
{
    Low,
    Medium,
    High,
}

public enum ProductionShotType
{
    GeneratedStill,
    GeneratedMotion,
    StockFootage,
    ArchivalImage,
    ArchivalVideo,
    Illustration,
    Diagram,
    Chart,
    Map,
    TextGraphic,
    ScreenCapture,
    SimpleBackground,
}

public enum ProductionAssetType
{
    Image,
    Video,
    Illustration,
    Diagram,
    Chart,
    Map,
    TextGraphic,
    ScreenCapture,
    Background,
}

public enum ProductionAcquisitionMode
{
    Generate,
    CreateGraphic,
    SourceLicensed,
    SourcePublicDomain,
    Capture,
    ExistingAsset,
}

public enum ProductionFactualityMode
{
    GenericAtmosphere,
    IllustrativeReconstruction,
    EvidenceBasedDepiction,
    DataVisualization,
    TextOnly,
}

public enum ProductionOnScreenTextType
{
    Decorative,
    SectionCue,
    Explanation,
    Name,
    Number,
    Date,
    Quote,
    Statistic,
}
