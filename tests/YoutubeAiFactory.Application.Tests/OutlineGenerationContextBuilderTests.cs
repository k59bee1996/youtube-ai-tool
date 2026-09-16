using YoutubeAiFactory.Application.Common;
using YoutubeAiFactory.Application.Outlines;
using YoutubeAiFactory.Application.Research;
using YoutubeAiFactory.Domain.Research;

namespace YoutubeAiFactory.Application.Tests;

public sealed class OutlineGenerationContextBuilderTests
{
    [Fact]
    public void Builds_bounded_context_from_supported_report_knowledge_without_raw_pages()
    {
        var fixture = new OutlineTestFixture();
        var builder = new OutlineGenerationContextBuilder(new OutlineOptions
        {
            MaxClaimsForOutline = 2,
            MaxEvidenceExcerptsForOutline = 2,
            MaxConflictItemsForOutline = 1,
            MaxResearchGapsForOutline = 1,
        });

        var context = builder.Build(fixture.Project, fixture.VideoProject, fixture.Source, fixture.Research);

        Assert.Equal(fixture.Report.Id, context.ResearchReportId);
        Assert.Equal(fixture.PilotVideo.ControlStrategy, context.ControlStrategy);
        Assert.Equal(2, context.Claims.Count);
        Assert.DoesNotContain(context.Claims, claim => claim.Id == fixture.UnsupportedClaim.Id);
        Assert.True(context.Evidence.Count <= 2);
        Assert.Single(context.ResearchGaps);
        Assert.Equal(64, context.OutlineInputFingerprint.Length);
    }

    [Fact]
    public void Blocks_stale_research_after_material_video_context_changes()
    {
        var fixture = new OutlineTestFixture();
        fixture.VideoProject.UpdateExecutionDetails("Changed title", null, DateTimeOffset.UtcNow.AddMinutes(1));
        var builder = new OutlineGenerationContextBuilder(new OutlineOptions());

        var error = Assert.Throws<ApplicationValidationException>(() =>
            builder.Build(fixture.Project, fixture.VideoProject, fixture.Source, fixture.Research));

        Assert.Contains("outdated", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Blocks_research_with_unsupported_critical_premise()
    {
        var fixture = new OutlineTestFixture(unsupportedCriticalClaimCount: 1);
        var builder = new OutlineGenerationContextBuilder(new OutlineOptions());

        var error = Assert.Throws<ApplicationValidationException>(() =>
            builder.Build(fixture.Project, fixture.VideoProject, fixture.Source, fixture.Research));

        Assert.Contains("unsupported critical", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Rejects_claims_injected_from_another_research_report()
    {
        var fixture = new OutlineTestFixture();
        var foreign = new ResearchClaim(Guid.NewGuid(), "A foreign claim.", ResearchClaimType.Factual, 80, false, DateTimeOffset.UtcNow);
        foreign.SetSupportStatus(ResearchClaimSupportStatus.Supported);
        var research = fixture.Research with { Claims = fixture.Research.Claims.Append(foreign).ToArray() };
        var builder = new OutlineGenerationContextBuilder(new OutlineOptions());

        var error = Assert.Throws<ApplicationValidationException>(() =>
            builder.Build(fixture.Project, fixture.VideoProject, fixture.Source, research));

        Assert.Contains("cross-report", error.Message, StringComparison.OrdinalIgnoreCase);
    }
}
