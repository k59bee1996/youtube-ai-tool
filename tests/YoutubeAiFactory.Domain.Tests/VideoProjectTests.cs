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

    [Fact]
    public void Outline_generation_can_recover_or_complete_without_skipping_approval()
    {
        var project = Create();
        var now = DateTimeOffset.UtcNow;
        MoveToResearchReady(project, now);

        project.TransitionTo(VideoProjectStatus.OutlineGenerating, now);
        project.TransitionTo(VideoProjectStatus.ResearchReady, now);
        project.TransitionTo(VideoProjectStatus.OutlineGenerating, now);
        project.TransitionTo(VideoProjectStatus.OutlineReady, now);

        Assert.Throws<DomainException>(() => project.TransitionTo(VideoProjectStatus.ScriptGenerating, now));
        project.TransitionTo(VideoProjectStatus.OutlineApproved, now);
        Assert.Equal(VideoProjectStatus.OutlineApproved, project.Status);
    }

    [Fact]
    public void Ready_outline_state_can_rerun_research_without_erasing_history()
    {
        var project = Create();
        var now = DateTimeOffset.UtcNow;
        MoveToResearchReady(project, now);
        project.TransitionTo(VideoProjectStatus.OutlineGenerating, now);
        project.TransitionTo(VideoProjectStatus.OutlineReady, now);

        project.TransitionTo(VideoProjectStatus.ResearchQueued, now);

        Assert.Equal(VideoProjectStatus.ResearchQueued, project.Status);
    }

    [Fact]
    public void Script_workflow_can_recover_regenerate_and_requires_approval_before_packaging()
    {
        var project = Create();
        var now = DateTimeOffset.UtcNow;
        MoveToResearchReady(project, now);
        project.TransitionTo(VideoProjectStatus.OutlineGenerating, now);
        project.TransitionTo(VideoProjectStatus.OutlineReady, now);
        project.TransitionTo(VideoProjectStatus.OutlineApproved, now);

        project.TransitionTo(VideoProjectStatus.ScriptGenerating, now);
        project.TransitionTo(VideoProjectStatus.OutlineApproved, now);
        project.TransitionTo(VideoProjectStatus.ScriptGenerating, now);
        project.TransitionTo(VideoProjectStatus.ScriptReady, now);
        Assert.Throws<DomainException>(() => project.TransitionTo(VideoProjectStatus.Packaging, now));
        project.TransitionTo(VideoProjectStatus.ScriptGenerating, now);
        project.TransitionTo(VideoProjectStatus.ScriptReady, now);
        project.TransitionTo(VideoProjectStatus.ScriptApproved, now);
        project.TransitionTo(VideoProjectStatus.Packaging, now);

        Assert.Equal(VideoProjectStatus.Packaging, project.Status);
    }

    [Fact]
    public void Packaging_can_roll_back_after_initial_failure_or_complete_after_approval()
    {
        var project = Create(); var now = DateTimeOffset.UtcNow; MoveToResearchReady(project, now);
        project.TransitionTo(VideoProjectStatus.OutlineGenerating, now); project.TransitionTo(VideoProjectStatus.OutlineReady, now);
        project.TransitionTo(VideoProjectStatus.OutlineApproved, now); project.TransitionTo(VideoProjectStatus.ScriptGenerating, now);
        project.TransitionTo(VideoProjectStatus.ScriptReady, now); project.TransitionTo(VideoProjectStatus.ScriptApproved, now);
        project.TransitionTo(VideoProjectStatus.Packaging, now); project.TransitionTo(VideoProjectStatus.ScriptApproved, now);
        project.TransitionTo(VideoProjectStatus.Packaging, now); project.TransitionTo(VideoProjectStatus.ProductionReady, now);
        Assert.Equal(VideoProjectStatus.ProductionReady, project.Status);
    }

    private static void MoveToResearchReady(VideoProject project, DateTimeOffset now)
    {
        project.TransitionTo(VideoProjectStatus.ResearchQueued, now);
        project.TransitionTo(VideoProjectStatus.Researching, now);
        project.TransitionTo(VideoProjectStatus.ResearchReady, now);
    }

    private static VideoProject Create() => new(Guid.NewGuid(), Guid.NewGuid(), 1, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
        "The Economics of Owning a Medieval Castle", "Historical economics", "Hidden costs", "Explainer", "History viewers",
        "Reveal the cost before the title card", "Castle against a ledger", "Understand the real ownership cost",
        PilotExperimentType.Packaging, "Hidden-cost framing should increase click intent.", "Framing", "CTR", "CTR rises against comparable pilots", "[]", DateTimeOffset.UtcNow);
}
