using YoutubeAiFactory.Application.Common;
using YoutubeAiFactory.Application.Scripts;
using YoutubeAiFactory.Domain.Videos;

namespace YoutubeAiFactory.Application.Tests;

public sealed class ScriptGenerationContextBuilderTests
{
    [Fact]
    public void Builds_section_scoped_evidence_context_in_project_target_language()
    {
        var fixture = new ScriptTestFixture();

        var context = fixture.Context;

        Assert.Equal("English", context.ContentLanguage);
        Assert.Equal(fixture.Outline.Id, context.VideoOutlineId);
        Assert.Equal(fixture.Base.Report.Id, context.ResearchReportId);
        Assert.Equal(4, context.Sections.Count);
        Assert.Equal(fixture.Base.SupportedClaim.Id, context.Sections[0].Claims.Single().Id);
        Assert.DoesNotContain(context.Sections[0].Claims, item => item.Id == fixture.Base.CorroboratedClaim.Id);
        Assert.Equal(fixture.Base.Conflict.Id, context.Sections[2].Conflicts.Single().Id);
        Assert.Single(context.Sections[2].ResearchGaps);
        Assert.Equal(64, context.InputFingerprint.Length);
    }

    [Fact]
    public void Remains_valid_after_queue_transitions_project_to_script_generating()
    {
        var fixture = new ScriptTestFixture();
        fixture.Base.VideoProject.TransitionTo(VideoProjectStatus.ScriptGenerating, DateTimeOffset.UtcNow);
        var builder = new ScriptGenerationContextBuilder(fixture.Options);

        var context = builder.Build(fixture.Base.Project, fixture.Base.VideoProject, fixture.Base.Source,
            fixture.Details, fixture.Base.Research);

        Assert.Equal(fixture.Outline.Id, context.VideoOutlineId);
    }

    [Fact]
    public void Rejects_stale_outline_after_material_video_context_change()
    {
        var fixture = new ScriptTestFixture();
        fixture.Base.VideoProject.UpdateExecutionDetails("A materially different title", null,
            DateTimeOffset.UtcNow.AddMinutes(1));
        var builder = new ScriptGenerationContextBuilder(fixture.Options);

        var error = Assert.Throws<ApplicationValidationException>(() => builder.Build(
            fixture.Base.Project, fixture.Base.VideoProject, fixture.Base.Source, fixture.Details,
            fixture.Base.Research));

        Assert.Contains("outdated", error.Message, StringComparison.OrdinalIgnoreCase);
    }
}
