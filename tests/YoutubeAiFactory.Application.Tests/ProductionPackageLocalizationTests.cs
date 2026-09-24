using System.Text.Json;
using YoutubeAiFactory.Application.Common;
using YoutubeAiFactory.Application.Localization;
using YoutubeAiFactory.Application.Production;
using YoutubeAiFactory.Domain.Localization;
using YoutubeAiFactory.Domain.Production;

namespace YoutubeAiFactory.Application.Tests;

public sealed class ProductionPackageLocalizationTests
{
    private static readonly JsonSerializerOptions WebJson = new(JsonSerializerDefaults.Web);

    [Fact]
    public void Accepts_translation_that_preserves_scene_shot_asset_and_index_identity()
    {
        var canonical = Canonical();
        var localized = Translation(canonical);

        LocalizedProductionPackageValidator.Validate(canonical, localized);
    }

    [Fact]
    public void Rejects_translation_that_reorders_shots_or_changes_empty_field_topology()
    {
        var canonical = Canonical();
        var valid = Translation(canonical);
        var scene = valid.Scenes[0];
        var reordered = valid with
        {
            Scenes = [scene with { Shots = scene.Shots.Reverse().ToArray() }],
        };
        Assert.Throws<StructuredOutputException>(() =>
            LocalizedProductionPackageValidator.Validate(canonical, reordered)
        );

        var changedEmptyField = valid with
        {
            Assets = [valid.Assets[0] with { SourceSearchBrief = "Không được thêm" }],
        };
        Assert.Throws<StructuredOutputException>(() =>
            LocalizedProductionPackageValidator.Validate(canonical, changedEmptyField)
        );
    }

    [Fact]
    public void Cached_translation_becomes_invalid_after_reader_facing_content_is_edited()
    {
        var canonical = Canonical();
        var localized = Translation(canonical);
        var cached = new ArtifactLocalization(
            LocalizableArtifactTypes.ProductionPackage,
            canonical.Package.Id,
            canonical.Package.Version,
            "vi",
            JsonSerializer.Serialize(localized, WebJson),
            Guid.NewGuid(),
            ProductionPackageLocalizationPrompt.Key,
            ProductionPackageLocalizationPrompt.Version,
            "fake",
            "model",
            DateTimeOffset.UtcNow
        );
        Assert.True(LocalizedProductionPackageCache.IsValid(canonical, cached));

        canonical.Scenes[0].Update(
            "Edited summary",
            canonical.Scenes[0].VisualStrategy,
            canonical.Scenes[0].TransitionIntent,
            canonical.Scenes[0].MusicBrief,
            canonical.Scenes[0].SoundEffectCue,
            canonical.Scenes[0].VoiceDirection
        );

        Assert.False(LocalizedProductionPackageCache.IsValid(canonical, cached));
    }

    private static LocalizedProductionPackageContent Translation(
        ProductionPackageWithDetails canonical
    )
    {
        var shots = canonical.Shots.GroupBy(x => x.ProductionSceneId).ToDictionary(x => x.Key);
        return new(
            LocalizedProductionPackageValidator.CreateFingerprint(canonical),
            [new(0, "VI Review historical representation.")],
            [new(0, "VI Explain the evidence limit.")],
            canonical
                .Scenes.OrderBy(x => x.Sequence)
                .Select(x => new LocalizedProductionScene(
                    x.Id,
                    $"VI {x.NarrationSummary}",
                    $"VI {x.VisualStrategy}",
                    shots[x.Id]
                        .OrderBy(y => y.Sequence)
                        .Select(y => new LocalizedProductionShot(
                            y.Id,
                            $"VI {y.VisualDescription}"
                        ))
                        .ToArray()
                ))
                .ToArray(),
            canonical
                .Assets.OrderBy(x => x.AssetKey)
                .Select(x => new LocalizedProductionAsset(
                    x.Id,
                    $"VI {x.CreativeBrief}",
                    x.SourceSearchBrief
                ))
                .ToArray()
        );
    }

    private static ProductionPackageWithDetails Canonical()
    {
        var issue = new ProductionGroundingIssueResult(
            1,
            1,
            null,
            null,
            ProductionGroundingIssueType.UnsupportedVisualFact,
            ProductionGroundingIssueSeverity.Warning,
            "Problematic text",
            [],
            "Explain the evidence limit."
        );
        var package = new ProductionPackage(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            1,
            Guid.NewGuid(),
            1,
            Guid.NewGuid(),
            1,
            1,
            Guid.NewGuid(),
            Guid.NewGuid(),
            "production-engine:v1",
            "production-package",
            1,
            new string('a', 64),
            "fake",
            "model",
            "English",
            120,
            "Documentary illustration",
            "Measured",
            "Muted",
            "Sans serif",
            "Sparse score",
            "Preserve the experiment.",
            "[\"Review historical representation.\"]",
            JsonSerializer.Serialize(new[] { issue }, ProductionPrompt.SerializerOptions),
            DateTimeOffset.UtcNow
        );
        var scene = new ProductionScene(
            package.Id,
            1,
            ProductionScenePurpose.Explain,
            "Explain the supported claim.",
            "Use a labelled diagram.",
            120,
            ProductionComplexity.Low,
            "Direct cut",
            "Sparse score",
            "No effect",
            "Measured delivery"
        );
        var first = new ProductionShot(
            scene.Id,
            1,
            ProductionShotType.Illustration,
            "A labelled diagram",
            "Wide",
            "Slow push",
            24,
            1,
            ProductionFactualityMode.EvidenceBasedDepiction,
            null,
            string.Empty
        );
        var second = new ProductionShot(
            scene.Id,
            2,
            ProductionShotType.SimpleBackground,
            "A neutral detail",
            "Close",
            "Static",
            96,
            4,
            ProductionFactualityMode.GenericAtmosphere,
            null,
            string.Empty
        );
        var asset = new ProductionAssetRequirement(
            package.Id,
            "diagram",
            ProductionAssetType.Illustration,
            ProductionAcquisitionMode.CreateGraphic,
            "Create an evidence-safe diagram.",
            "Canonical generation prompt",
            string.Empty,
            false,
            ProductionFactualityMode.EvidenceBasedDepiction,
            "diagram-base",
            ProductionComplexity.Low
        );
        return new(package, [scene], [], [first, second], [], [asset], [], [], []);
    }
}
