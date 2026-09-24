using YoutubeAiFactory.Application.Common;
using YoutubeAiFactory.Domain.Production;

namespace YoutubeAiFactory.Application.Production;

public sealed record ProductionTimingPlan(
    IReadOnlyDictionary<int, int> SceneSeconds,
    IReadOnlyDictionary<(int Scene, int Shot), int> ShotSeconds
);

public sealed class ProductionValidator(ProductionOptions options)
{
    public ProductionTimingPlan ValidateGenerated(
        ProductionPackageResult result,
        ProductionGenerationContext context
    )
    {
        ValidateOptions(options);
        Required(result.VisualDirection, "visual direction", 4_000);
        Required(result.PacingDirection, "pacing direction", 2_000);
        Required(result.ColorDirection, "color direction", 2_000);
        Required(result.TypographyDirection, "typography direction", 2_000);
        Required(result.AudioDirection, "audio direction", 2_000);
        Required(result.ExperimentProductionNotes, "experiment production notes", 4_000);
        if (
            result.Scenes is null
            || result.Scenes.Count is < 1
            || result.Scenes.Count > options.MaxScenes
        )
            throw Invalid(
                $"Production package must contain between 1 and {options.MaxScenes} scenes."
            );
        if (result.AssetRequirements is null || result.AssetRequirements.Count > options.MaxAssets)
            throw Invalid($"Production package cannot exceed {options.MaxAssets} assets.");
        if (
            result.Warnings is null
            || result.Warnings.Any(warning =>
                string.IsNullOrWhiteSpace(warning) || warning.Length > 2_000
            )
        )
            throw Invalid("Production package warnings must be non-empty bounded strings.");
        Contiguous(result.Scenes.Select(x => x.Sequence), "scene");
        if (result.Scenes.Any(scene => scene.ScriptBlockIds is null))
            throw Invalid("Every scene must include ScriptBlock mappings.");
        var expectedBlocks = context
            .Blocks.OrderBy(x => x.GlobalSequence)
            .Select(x => x.Id)
            .ToArray();
        var mappedBlocks = result
            .Scenes.OrderBy(x => x.Sequence)
            .SelectMany(x => x.ScriptBlockIds)
            .ToArray();
        if (!mappedBlocks.SequenceEqual(expectedBlocks))
            throw Invalid(
                "Scenes must map every Script block exactly once, contiguously, and in approved narration order."
            );
        if (
            result.AssetRequirements.Any(asset => string.IsNullOrWhiteSpace(asset.AssetKey))
            || result
                .AssetRequirements.Select(asset => asset.AssetKey)
                .Distinct(StringComparer.Ordinal)
                .Count() != result.AssetRequirements.Count
        )
            throw Invalid("Asset keys must be present and unique.");
        var assets = result.AssetRequirements.ToDictionary(x => x.AssetKey, StringComparer.Ordinal);
        if (
            result.AssetRequirements.Count(x => x.AssetType == ProductionAssetType.Chart)
                > options.MaxCharts
            || result.AssetRequirements.Count(x => x.AssetType == ProductionAssetType.Map)
                > options.MaxMaps
            || result.AssetRequirements.Count(x =>
                x.AcquisitionMode == ProductionAcquisitionMode.Generate
                && x.AssetType == ProductionAssetType.Video
            ) > options.MaxGeneratedMotionAssets
        )
            throw Invalid("Production package exceeds configured high-cost asset limits.");

        var blocks = context.Blocks.ToDictionary(x => x.Id);
        var sceneSeconds = Allocate(
            context.EstimatedDurationSeconds,
            result
                .Scenes.Select(scene => scene.ScriptBlockIds.Sum(id => blocks[id].WordCount))
                .ToArray()
        );
        var sceneTiming = new Dictionary<int, int>();
        var shotTiming = new Dictionary<(int, int), int>();
        var referencedAssetKeys = new HashSet<string>(StringComparer.Ordinal);
        foreach (var scene in result.Scenes.OrderBy(x => x.Sequence))
        {
            if (scene.ScriptBlockIds is null || scene.ScriptBlockIds.Count == 0)
                throw Invalid("Every scene must map at least one Script block.");
            if (
                scene.Shots is null
                || scene.Shots.Count is < 1
                || scene.Shots.Count > options.MaxShotsPerScene
            )
                throw Invalid("Each scene must contain a bounded shot plan.");
            if (
                scene.OnScreenText is null
                || scene.OnScreenText.Count > options.MaxOnScreenTextPerScene
            )
                throw Invalid("Scene on-screen text exceeds its configured limit.");
            Contiguous(scene.Shots.Select(x => x.Sequence), "shot");
            Contiguous(scene.OnScreenText.Select(x => x.Sequence), "on-screen text");
            Required(scene.NarrationSummary, "narration summary", 2_000);
            Required(scene.VisualStrategy, "visual strategy", 4_000);
            Required(scene.TransitionIntent, "transition intent", 1_000);
            Required(scene.MusicBrief, "music brief", 1_000);
            Required(scene.SoundEffectCue, "sound effect cue", 1_000);
            Required(scene.VoiceDirection, "voice direction", 1_000);
            var allowedClaims = scene
                .ScriptBlockIds.SelectMany(id => blocks[id].Claims)
                .Select(x => x.Id)
                .ToHashSet();
            var seconds = sceneSeconds[scene.Sequence - 1];
            sceneTiming[scene.Sequence] = seconds;
            var weights = scene.Shots.Select(x => x.RelativeDurationWeight).ToArray();
            if (weights.Any(x => x is <= 0 or > 1_000))
                throw Invalid("Shot duration weights must be positive and no greater than 1000.");
            var allocatedShots = Allocate(
                seconds,
                weights.Select(x => (int)Math.Max(1, decimal.Round(x * 100))).ToArray()
            );
            foreach (var shot in scene.Shots)
            {
                ValidateClaimRefs(shot.ClaimIds, allowedClaims, "shot");
                Required(shot.VisualDescription, "shot visual description", 4_000);
                Required(shot.Composition, "shot composition", 2_000);
                Required(shot.MotionSuggestion, "shot motion", 2_000);
                Optional(shot.Notes, "shot notes", 4_000);
                if (shot.AssetKey is not null && !assets.ContainsKey(shot.AssetKey))
                    throw Invalid("A shot references an unknown asset key.");
                if (shot.AssetKey is not null)
                    referencedAssetKeys.Add(shot.AssetKey);
                if (RequiresClaims(shot.FactualityMode, shot.ShotType) && shot.ClaimIds.Count == 0)
                    throw Invalid("Evidence-based, chart, and map shots require Claim references.");
                if (
                    shot.ShotType
                    is ProductionShotType.ArchivalImage
                        or ProductionShotType.ArchivalVideo
                )
                {
                    if (
                        shot.AssetKey is null
                        || assets[shot.AssetKey].AcquisitionMode
                            is not (
                                ProductionAcquisitionMode.SourceLicensed
                                or ProductionAcquisitionMode.SourcePublicDomain
                            )
                        || !assets[shot.AssetKey].RightsVerificationRequired
                    )
                        throw Invalid(
                            "Archival shots require a sourced asset with rights verification."
                        );
                }
                shotTiming[(scene.Sequence, shot.Sequence)] = allocatedShots[shot.Sequence - 1];
            }
            foreach (var text in scene.OnScreenText)
            {
                Required(text.Text, "on-screen text", 1_000);
                Required(text.TimingIntent, "on-screen text timing", 500);
                ValidateClaimRefs(text.ClaimIds, allowedClaims, "on-screen text");
                if (
                    text.Type
                        is ProductionOnScreenTextType.Name
                            or ProductionOnScreenTextType.Number
                            or ProductionOnScreenTextType.Date
                            or ProductionOnScreenTextType.Quote
                            or ProductionOnScreenTextType.Statistic
                    && text.ClaimIds.Count == 0
                )
                    throw Invalid("Factual on-screen text requires a Claim reference.");
            }
        }
        foreach (var asset in result.AssetRequirements)
        {
            Required(asset.AssetKey, "asset key", 100);
            Required(asset.CreativeBrief, "asset creative brief", 4_000);
            Required(asset.ReuseKey, "asset reuse key", 100);
            if (!referencedAssetKeys.Contains(asset.AssetKey))
                throw Invalid("Every asset requirement must be referenced by at least one shot.");
            if (
                asset.AcquisitionMode == ProductionAcquisitionMode.Generate
                && string.IsNullOrWhiteSpace(asset.GenerationPrompt)
            )
                throw Invalid("Generated assets require a generation prompt.");
            if (
                (
                    asset.AcquisitionMode
                    is ProductionAcquisitionMode.SourceLicensed
                        or ProductionAcquisitionMode.SourcePublicDomain
                ) && string.IsNullOrWhiteSpace(asset.SourceSearchBrief)
            )
                throw Invalid("Sourced assets require a source-search brief.");
            ValidateClaimRefs(
                asset.ClaimIds,
                context.Blocks.SelectMany(x => x.Claims).Select(x => x.Id).ToHashSet(),
                "asset"
            );
            if (RequiresClaims(asset.FactualityMode, asset.AssetType) && asset.ClaimIds.Count == 0)
                throw Invalid("Evidence-based, chart, and map assets require Claim references.");
            if (
                asset.AcquisitionMode
                    is ProductionAcquisitionMode.SourceLicensed
                        or ProductionAcquisitionMode.SourcePublicDomain
                && !asset.RightsVerificationRequired
            )
                throw Invalid("Sourced media must require rights verification.");
            if (
                asset.AcquisitionMode == ProductionAcquisitionMode.Generate
                && asset.AssetType == ProductionAssetType.Video
                && asset.FactualityMode == ProductionFactualityMode.EvidenceBasedDepiction
            )
                throw Invalid("Generated video cannot masquerade as archival evidence.");
        }
        return new(sceneTiming, shotTiming);
    }

    public static void ValidateAudit(
        ProductionGroundingAuditResult audit,
        ProductionPackageResult package,
        ProductionGenerationContext context
    )
    {
        if (audit.Issues is null || audit.Status == ProductionGroundingStatus.Pending)
            throw Invalid("Production grounding audit is incomplete.");
        var scenes = package.Scenes.ToDictionary(x => x.Sequence);
        var allClaims = context.Blocks.SelectMany(x => x.Claims).Select(x => x.Id).ToHashSet();
        foreach (var issue in audit.Issues)
        {
            if (!scenes.TryGetValue(issue.SceneSequence, out var scene))
                throw Invalid("Audit issue references an unknown scene.");
            if (issue.ShotSequence is int shot && scene.Shots.All(x => x.Sequence != shot))
                throw Invalid("Audit issue references an unknown shot.");
            if (
                issue.OnScreenTextSequence is int text
                && scene.OnScreenText.All(x => x.Sequence != text)
            )
                throw Invalid("Audit issue references unknown on-screen text.");
            if (
                issue.AssetKey is not null
                && package.AssetRequirements.All(x => x.AssetKey != issue.AssetKey)
            )
                throw Invalid("Audit issue references an unknown asset.");
            ValidateClaimRefs(issue.RelevantClaimIds, allClaims, "audit issue");
            Required(issue.ProblematicText, "audit problematic text", 4_000);
            Required(issue.Explanation, "audit explanation", 4_000);
        }
        var errors = audit.Issues.Any(x => x.Severity == ProductionGroundingIssueSeverity.Error);
        if (
            audit.Status == ProductionGroundingStatus.Passed && errors
            || audit.Status == ProductionGroundingStatus.Failed && !errors
        )
            throw Invalid("Production grounding status does not match its issues.");
    }

    private static bool RequiresClaims(ProductionFactualityMode mode, ProductionShotType type) =>
        mode
            is ProductionFactualityMode.EvidenceBasedDepiction
                or ProductionFactualityMode.DataVisualization
        || type is ProductionShotType.Chart or ProductionShotType.Map;

    private static bool RequiresClaims(ProductionFactualityMode mode, ProductionAssetType type) =>
        mode
            is ProductionFactualityMode.EvidenceBasedDepiction
                or ProductionFactualityMode.DataVisualization
        || type is ProductionAssetType.Chart or ProductionAssetType.Map;

    private static void ValidateClaimRefs(
        IReadOnlyList<Guid>? ids,
        HashSet<Guid> allowed,
        string owner
    )
    {
        if (
            ids is null
            || ids.Distinct().Count() != ids.Count
            || ids.Any(x => !allowed.Contains(x))
        )
            throw Invalid($"{owner} contains invalid or duplicate Claim references.");
    }

    private static void Contiguous(IEnumerable<int> values, string name)
    {
        var array = values.Order().ToArray();
        if (!array.SequenceEqual(Enumerable.Range(1, array.Length)))
            throw Invalid($"{name} sequences must be unique and contiguous from 1.");
    }

    private static void Required(string? value, string name, int max)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > max)
            throw Invalid($"Production {name} is required and must be at most {max} characters.");
    }

    private static void Optional(string? value, string name, int max)
    {
        if (value is not null && value.Length > max)
            throw Invalid($"Production {name} must be at most {max} characters.");
    }

    private static int[] Allocate(int total, int[] weights)
    {
        if (weights.Length == 0 || total < weights.Length)
            throw Invalid("Duration cannot be allocated to the requested plan.");
        var result = Enumerable.Repeat(1, weights.Length).ToArray();
        var remaining = total - weights.Length;
        var weightTotal = weights.Sum();
        var raw = weights.Select(x => remaining * (double)x / weightTotal).ToArray();
        for (var i = 0; i < result.Length; i++)
            result[i] += (int)Math.Floor(raw[i]);
        for (var i = 0; i < total - result.Sum(); i++)
            result[
                raw.Select((x, index) => (Fraction: x - Math.Floor(x), index))
                    .OrderByDescending(x => x.Fraction)
                    .ThenBy(x => x.index)
                    .ElementAt(i)
                    .index
            ]++;
        return result;
    }

    private static StructuredOutputException Invalid(string message) => new(message);

    public static void ValidateOptions(ProductionOptions value)
    {
        if (
            value.MaxJobRetries < 0
            || value.RunningJobLeaseSeconds < 15
            || value.MaxGenerationRetries < 0
            || value.MaxGroundingCorrectionAttempts < 0
            || value.MaxStructuredRepairAttempts < 0
            || value.MaxScenes < 1
            || value.MaxShotsPerScene < 1
            || value.MaxAssets < 0
            || value.MaxGeneratedMotionAssets < 0
            || value.MaxCharts < 0
            || value.MaxMaps < 0
            || value.MaxOnScreenTextPerScene < 0
        )
            throw new ApplicationValidationException(
                "ProductionPackage configuration contains invalid retry, lease, or collection limits."
            );
    }
}
