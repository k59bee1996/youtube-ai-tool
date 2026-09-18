using YoutubeAiFactory.Application.Common;
using YoutubeAiFactory.Application.Scripts;
using YoutubeAiFactory.Domain.Scripts;

namespace YoutubeAiFactory.Application.Tests;

public sealed class ScriptValidatorTests
{
    [Fact]
    public void Counts_words_and_estimates_runtime_without_trusting_model_metrics()
    {
        Assert.Equal(6, ScriptMetrics.CountWords("One, two; castle-owner's ledger 2026 — ready."));
        Assert.Equal(120, ScriptMetrics.EstimateDurationSeconds(300, 150));
    }

    [Fact]
    public void Accepts_exact_outline_mapping_and_computes_metrics_on_backend()
    {
        var fixture = new ScriptTestFixture();
        var validator = new ScriptValidator(fixture.Options);

        var metrics = validator.ValidateGenerated(fixture.ValidResult(), fixture.Context);

        Assert.Equal(300, metrics.TotalWordCount);
        Assert.Equal(120, metrics.EstimatedDurationSeconds);
        Assert.Empty(metrics.Warnings);
    }

    [Fact]
    public void Rejects_claim_from_a_different_outline_section()
    {
        var fixture = new ScriptTestFixture();
        var valid = fixture.ValidResult();
        var first = valid.Sections[0];
        var result = valid with
        {
            Sections = valid.Sections.Select(section => section.Sequence == 1
                ? section with
                {
                    Blocks =
                    [
                        first.Blocks[0] with
                        {
                            ClaimIds = [fixture.Context.Sections[1].Claims.Single().Id],
                        },
                    ],
                }
                : section).ToArray(),
        };

        var error = Assert.Throws<StructuredOutputException>(() =>
            new ScriptValidator(fixture.Options).ValidateGenerated(result, fixture.Context));

        Assert.Contains("outside", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Rejects_conflicted_claim_when_conflict_is_removed()
    {
        var fixture = new ScriptTestFixture();
        var valid = fixture.ValidResult();
        var result = valid with
        {
            Sections = valid.Sections.Select(section => section.Sequence == 3
                ? section with { Blocks = [section.Blocks[0] with { ConflictIds = [] }] }
                : section).ToArray(),
        };

        var error = Assert.Throws<StructuredOutputException>(() =>
            new ScriptValidator(fixture.Options).ValidateGenerated(result, fixture.Context));

        Assert.Contains("conflict", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Rejects_a_passed_audit_that_contains_an_error()
    {
        var fixture = new ScriptTestFixture();
        var result = fixture.ValidResult();
        var issue = new ScriptGroundingIssueResult(1, 1, ScriptGroundingIssueType.UnsupportedFact,
            ScriptGroundingIssueSeverity.Error, "An unsupported statement.",
            [fixture.Context.Sections[0].Claims.Single().Id], "The block exceeds the cited evidence.");

        var error = Assert.Throws<StructuredOutputException>(() => ScriptValidator.ValidateAudit(
            new ScriptGroundingAuditResult(ScriptGroundingStatus.Passed, [issue]), result, fixture.Context));

        Assert.Contains("status", error.Message, StringComparison.OrdinalIgnoreCase);
    }
}
