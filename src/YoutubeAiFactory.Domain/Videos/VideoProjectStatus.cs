namespace YoutubeAiFactory.Domain.Videos;

public enum VideoProjectStatus
{
    Draft,
    ResearchQueued,
    Researching,
    ResearchReady,
    OutlineGenerating,
    OutlineReady,
    OutlineApproved,
    ScriptGenerating,
    ScriptReady,
    ScriptApproved,
    Packaging,
    ProductionReady,
}
