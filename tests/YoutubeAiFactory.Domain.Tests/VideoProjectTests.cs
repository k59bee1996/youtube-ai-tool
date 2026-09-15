using YoutubeAiFactory.Domain.Common;
using YoutubeAiFactory.Domain.Pilots;
using YoutubeAiFactory.Domain.Videos;

namespace YoutubeAiFactory.Domain.Tests;

public sealed class VideoProjectTests
{
    [Fact]
    public void Starts_draft_and_allows_only_the_defined_workflow_path()
    {
        var project = Create();

        Assert.Equal(VideoProjectStatus.Draft, project.Status);
        project.TransitionTo(VideoProjectStatus.ResearchQueued, DateTimeOffset.UtcNow);
        project.TransitionTo(VideoProjectStatus.Researching, DateTimeOffset.UtcNow);
        project.TransitionTo(VideoProjectStatus.ResearchReady, DateTimeOffset.UtcNow);

        Assert.Equal(VideoProjectStatus.ResearchReady, project.Status);
    }

    [Theory]
    [InlineData(VideoProjectStatus.Draft, VideoProjectStatus.ScriptReady)]
    [InlineData(VideoProjectStatus.Draft, VideoProjectStatus.OutlineReady)]
    [InlineData(VideoProjectStatus.Researching, VideoProjectStatus.ProductionReady)]
    public void Rejects_impossible_or_skipped_transitions(VideoProjectStatus current, VideoProjectStatus target)
    {
        var project = Create();
        if (current == VideoProjectStatus.Researching)
        {
            project.TransitionTo(VideoProjectStatus.ResearchQueued, DateTimeOffset.UtcNow);
            project.TransitionTo(VideoProjectStatus.Researching, DateTimeOffset.UtcNow);
        }

        Assert.Throws<DomainException>(() => project.TransitionTo(target, DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Rejects_same_state_and_keeps_source_lineage_immutable()
    {
        var project = Create();
        var sourcePilotVideoId = project.PilotVideoId;

        Assert.Throws<DomainException>(() => project.TransitionTo(VideoProjectStatus.Draft, DateTimeOffset.UtcNow));
        project.UpdateExecutionDetails("Refined working title", "Production note", DateTimeOffset.UtcNow);

        Assert.Equal(sourcePilotVideoId, project.PilotVideoId);
        Assert.Equal("Refined working title", project.WorkingTitle);
        Assert.Equal("Production note", project.ExecutionNotes);
    }

    [Fact]
    public void Research_failure_is_explicit_and_allows_a_new_research_run()
    {
        var project = Create();
        var now = DateTimeOffset.UtcNow;

        project.TransitionTo(VideoProjectStatus.ResearchQueued, now);
        project.TransitionTo(VideoProjectStatus.Researching, now);
        project.TransitionTo(VideoProjectStatus.ResearchFailed, now);
        project.TransitionTo(VideoProjectStatus.ResearchQueued, now);

        Assert.Equal(VideoProjectStatus.ResearchQueued, project.Status);
    }

    private static VideoProject Create() => new(Guid.NewGuid(), Guid.NewGuid(), 1, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
        "The Economics of Owning a Medieval Castle", "Historical economics", "Hidden costs", "Explainer", "History viewers",
        "Reveal the cost before the title card", "Castle against a ledger", "Understand the real ownership cost",
        PilotExperimentType.Packaging, "Hidden-cost framing should increase click intent.", "Framing", "CTR", "CTR rises against comparable pilots", "[]", DateTimeOffset.UtcNow);
}
