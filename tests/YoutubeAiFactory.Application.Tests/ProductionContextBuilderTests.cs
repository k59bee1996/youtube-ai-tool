using YoutubeAiFactory.Application.Production;
using YoutubeAiFactory.Application.Scripts;
using YoutubeAiFactory.Domain.Scripts;
using YoutubeAiFactory.Domain.Videos;

namespace YoutubeAiFactory.Application.Tests;

public sealed class ProductionContextBuilderTests
{
    [Fact]
    public void Builds_bounded_context_from_approved_script_with_exact_lineage_and_claims()
    {
        var fixture = new ScriptTestFixture();
        var details = ApprovedScript(fixture);
        var context = new ProductionContextBuilder(new()).Build(
            fixture.Base.Project,
            fixture.Base.VideoProject,
            fixture.Base.Source,
            details,
            fixture.Base.Research
        );
        Assert.Equal(details.Script.Id, context.VideoScriptId);
        Assert.Equal(details.Script.EstimatedDurationSeconds, context.EstimatedDurationSeconds);
        Assert.Equal(details.Blocks.Count, context.Blocks.Count);
        Assert.All(context.Blocks, block => Assert.NotEmpty(block.Claims));
        Assert.DoesNotContain(
            context.Blocks.SelectMany(x => x.Claims),
            claim => claim.SupportStatus == "Unsupported"
        );
    }

    [Fact]
    public void Rejects_a_ready_but_unapproved_script()
    {
        var fixture = new ScriptTestFixture();
        var details = CreateScript(fixture, false);
        Assert.ThrowsAny<Exception>(() =>
            new ProductionContextBuilder(new()).Build(
                fixture.Base.Project,
                fixture.Base.VideoProject,
                fixture.Base.Source,
                details,
                fixture.Base.Research
            )
        );
    }

    private static VideoScriptWithDetails ApprovedScript(ScriptTestFixture fixture) =>
        CreateScript(fixture, true);

    private static VideoScriptWithDetails CreateScript(ScriptTestFixture fixture, bool approve)
    {
        var result = fixture.ValidResult();
        var metrics = new ScriptValidator(fixture.Options).ValidateGenerated(
            result,
            fixture.Context
        );
        var now = DateTimeOffset.UtcNow;
        var script = new VideoScript(
            fixture.Base.Project.Id,
            fixture.Base.VideoProject.Id,
            fixture.Outline.Id,
            fixture.Outline.Version,
            fixture.Base.Report.Id,
            fixture.Base.Report.Version,
            1,
            Guid.NewGuid(),
            Guid.NewGuid(),
            "script-engine:v1",
            "script-generation",
            1,
            fixture.Context.InputFingerprint,
            "fake",
            "premium",
            "English",
            metrics.TotalWordCount,
            metrics.EstimatedDurationSeconds,
            "[]",
            "[]",
            now
        );
        var sections = new List<VideoScriptSection>();
        var blocks = new List<VideoScriptBlock>();
        var claims = new List<VideoScriptBlockClaim>();
        var conflicts = new List<VideoScriptBlockConflict>();
        foreach (var generated in result.Sections)
        {
            var words = metrics.SectionWordCounts[generated.Sequence];
            var section = new VideoScriptSection(
                script.Id,
                generated.OutlineSectionId,
                generated.Sequence,
                fixture.Context.Sections.Single(x => x.Sequence == generated.Sequence).Heading,
                words,
                ScriptMetrics.EstimateDurationSeconds(words, fixture.Options.PlanningWordsPerMinute)
            );
            sections.Add(section);
            foreach (var generatedBlock in generated.Blocks)
            {
                var block = new VideoScriptBlock(
                    section.Id,
                    generatedBlock.Sequence,
                    generatedBlock.Type,
                    generatedBlock.Text,
                    metrics.BlockWordCounts[(generated.Sequence, generatedBlock.Sequence)]
                );
                blocks.Add(block);
                claims.AddRange(
                    generatedBlock.ClaimIds.Select(x => new VideoScriptBlockClaim(block.Id, x))
                );
                conflicts.AddRange(
                    generatedBlock.ConflictIds.Select(x => new VideoScriptBlockConflict(
                        block.Id,
                        x
                    ))
                );
            }
        }
        if (approve)
        {
            script.Approve(now);
            fixture.Base.VideoProject.TransitionTo(VideoProjectStatus.ScriptGenerating, now);
            fixture.Base.VideoProject.TransitionTo(VideoProjectStatus.ScriptReady, now);
            fixture.Base.VideoProject.TransitionTo(VideoProjectStatus.ScriptApproved, now);
        }
        return new(script, sections, blocks, claims, conflicts);
    }
}
