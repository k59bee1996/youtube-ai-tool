namespace YoutubeAiFactory.Domain.Scripts;

public enum ScriptGroundingIssueType
{
    UnsupportedFact,
    UnsupportedNumber,
    UnsupportedDate,
    FabricatedQuote,
    ClaimOverstatement,
    ConflictMisrepresented,
    UncertaintyStrengthened,
    ClaimMismatch,
}
