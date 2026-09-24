using YoutubeAiFactory.Application.Common;
using YoutubeAiFactory.Application.Production;
using YoutubeAiFactory.Domain.Production;
using YoutubeAiFactory.Domain.Scripts;

namespace YoutubeAiFactory.Application.Tests;

public sealed class ProductionValidatorTests
{
    [Fact]
    public void Maps_every_block_once_and_allocates_exact_script_duration()
    {
        var (context, result) = Fixture();
        var timing = new ProductionValidator(new()).ValidateGenerated(result, context);
        Assert.Equal(context.EstimatedDurationSeconds, timing.SceneSeconds.Values.Sum());
        Assert.Equal(
            timing.SceneSeconds[1],
            timing.ShotSeconds.Where(x => x.Key.Scene == 1).Sum(x => x.Value)
        );
    }

    [Fact]
    public void Rejects_reordered_or_duplicated_script_blocks()
    {
        var (context, result) = Fixture();
        var changed = result with
        {
            Scenes =
            [
                result.Scenes[0] with
                {
                    ScriptBlockIds = [context.Blocks[1].Id],
                },
                result.Scenes[1] with
                {
                    ScriptBlockIds = [context.Blocks[1].Id],
                },
            ],
        };
        var error = Assert.Throws<StructuredOutputException>(() =>
            new ProductionValidator(new()).ValidateGenerated(changed, context)
        );
        Assert.Contains("exactly once", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Rejects_factual_overlay_without_claim()
    {
        var (context, result) = Fixture();
        var scene = result.Scenes[0];
        var changed = result with
        {
            Scenes =
            [
                scene with
                {
                    OnScreenText =
                    [
                        new(1, "1066", ProductionOnScreenTextType.Date, "During narration", []),
                    ],
                },
                result.Scenes[1],
            ],
        };
        Assert.Throws<StructuredOutputException>(() =>
            new ProductionValidator(new()).ValidateGenerated(changed, context)
        );
    }

    [Fact]
    public void Rejects_passed_audit_with_error()
    {
        var (context, result) = Fixture();
        var issue = new ProductionGroundingIssueResult(
            1,
            1,
            null,
            null,
            ProductionGroundingIssueType.UnsupportedVisualFact,
            ProductionGroundingIssueSeverity.Error,
            "Unsupported",
            [context.Blocks[0].Claims[0].Id],
            "Not supported"
        );
        Assert.Throws<StructuredOutputException>(() =>
            ProductionValidator.ValidateAudit(
                new(ProductionGroundingStatus.Passed, [issue]),
                result,
                context
            )
        );
    }

    [Fact]
    public void Rejects_shot_weight_above_supported_range()
    {
        var (context, result) = Fixture();
        var scene = result.Scenes[0];
        var changed = result with
        {
            Scenes =
            [
                scene with
                {
                    Shots = [scene.Shots[0] with { RelativeDurationWeight = 1_001 }],
                },
                result.Scenes[1],
            ],
        };

        Assert.Throws<StructuredOutputException>(() =>
            new ProductionValidator(new()).ValidateGenerated(changed, context)
        );
    }

    [Fact]
    public void Rejects_shot_notes_longer_than_persistence_contract()
    {
        var (context, result) = Fixture();
        var scene = result.Scenes[0];
        var changed = result with
        {
            Scenes =
            [
                scene with
                {
                    Shots = [scene.Shots[0] with { Notes = new string('n', 4_001) }],
                },
                result.Scenes[1],
            ],
        };

        Assert.Throws<StructuredOutputException>(() =>
            new ProductionValidator(new()).ValidateGenerated(changed, context)
        );
    }

    private static (ProductionGenerationContext, ProductionPackageResult) Fixture()
    {
        var claim = Guid.NewGuid();
        var b1 = Guid.NewGuid();
        var b2 = Guid.NewGuid();
        var context = new ProductionGenerationContext(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            1,
            Guid.NewGuid(),
            1,
            Guid.NewGuid(),
            1,
            "fingerprint",
            "Castle economics",
            "Understand cost",
            "Explainer",
            "English",
            "Global",
            "History viewers",
            "History audience",
            "Packaging",
            "Hidden cost framing",
            "Framing",
            "Hold topic constant",
            "CTR",
            "CTR increases",
            120,
            [
                new(
                    b1,
                    1,
                    1,
                    1,
                    ScriptBlockType.FactualNarration,
                    "A supported castle fact.",
                    100,
                    [
                        new(
                            claim,
                            "Supported fact",
                            "Corroborated",
                            true,
                            ["Evidence"],
                            ["https://example.com/source"]
                        ),
                    ],
                    []
                ),
                new(b2, 2, 1, 2, ScriptBlockType.NarrativeBridge, "A transition.", 100, [], []),
            ]
        );
        var shot1 = new ProductionShotResult(
            1,
            ProductionShotType.Illustration,
            "A labelled castle diagram",
            "Wide",
            "Slow push",
            1,
            ProductionFactualityMode.EvidenceBasedDepiction,
            "castle",
            [claim],
            ""
        );
        var shot2 = new ProductionShotResult(
            1,
            ProductionShotType.SimpleBackground,
            "A neutral transition",
            "Wide",
            "Static",
            1,
            ProductionFactualityMode.GenericAtmosphere,
            null,
            [],
            ""
        );
        var asset = new ProductionAssetResult(
            "castle",
            ProductionAssetType.Illustration,
            ProductionAcquisitionMode.CreateGraphic,
            "Labelled castle diagram",
            "",
            "",
            false,
            ProductionFactualityMode.EvidenceBasedDepiction,
            "castle-base",
            ProductionComplexity.Medium,
            [claim]
        );
        var scenes = new[]
        {
            new ProductionSceneResult(
                1,
                ProductionScenePurpose.Explain,
                "Explain fact",
                "Diagram",
                ProductionComplexity.Medium,
                "Cut",
                "Sparse",
                "None",
                "Measured",
                [b1],
                [shot1],
                []
            ),
            new ProductionSceneResult(
                2,
                ProductionScenePurpose.Transition,
                "Bridge",
                "Neutral",
                ProductionComplexity.Low,
                "Dissolve",
                "None",
                "None",
                "Measured",
                [b2],
                [shot2],
                []
            ),
        };
        return (
            context,
            new(
                "Documentary illustration",
                "Measured",
                "Muted",
                "Sans serif",
                "Sparse",
                "Keep packaging constant",
                scenes,
                [asset],
                []
            )
        );
    }
}
