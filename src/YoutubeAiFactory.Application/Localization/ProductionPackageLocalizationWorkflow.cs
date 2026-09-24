using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using YoutubeAiFactory.Application.AI;
using YoutubeAiFactory.Application.Common;
using YoutubeAiFactory.Application.Competitors;
using YoutubeAiFactory.Application.Persistence;
using YoutubeAiFactory.Application.Production;
using YoutubeAiFactory.Domain.Jobs;
using YoutubeAiFactory.Domain.Localization;

namespace YoutubeAiFactory.Application.Localization;

public sealed record LocalizedProductionText(int Index, string Text);

public sealed record LocalizedProductionIssue(int Index, string Explanation);

public sealed record LocalizedProductionShot(Guid ShotId, string VisualDescription);

public sealed record LocalizedProductionScene(
    Guid SceneId,
    string NarrationSummary,
    string VisualStrategy,
    IReadOnlyList<LocalizedProductionShot> Shots
);

public sealed record LocalizedProductionAsset(
    Guid AssetId,
    string CreativeBrief,
    string SourceSearchBrief
);

public sealed record LocalizedProductionPackageContent(
    string CanonicalContentFingerprint,
    IReadOnlyList<LocalizedProductionText> Warnings,
    IReadOnlyList<LocalizedProductionIssue> GroundingIssues,
    IReadOnlyList<LocalizedProductionScene> Scenes,
    IReadOnlyList<LocalizedProductionAsset> Assets
);

public sealed record ProductionPackageLocalizationStatusDto(
    LocalizedProductionPackageContent? Content,
    AnalysisJobDto? ActiveJob,
    AnalysisJobDto? LatestJob
);

public sealed class RequestProductionPackageLocalizationHandler(
    IYoutubeAiFactoryStore store,
    ArtifactLocalizationOptions options,
    TimeProvider timeProvider
)
{
    public async Task<RequestArtifactLocalizationResult> HandleAsync(
        Guid projectId,
        Guid videoProjectId,
        Guid packageId,
        string locale,
        CancellationToken ct
    )
    {
        var normalized = RequestCompetitorAnalysisLocalizationHandler.NormalizeLocale(locale);
        var package =
            await store.GetProductionPackageAsync(projectId, videoProjectId, packageId, false, ct)
            ?? throw new ResourceNotFoundException("Production package was not found.");
        var cached = await store.GetArtifactLocalizationAsync(
            LocalizableArtifactTypes.ProductionPackage,
            packageId,
            package.Package.Version,
            normalized,
            ct
        );
        if (cached is not null)
        {
            if (LocalizedProductionPackageCache.IsValid(package, cached))
                return new(null, "Completed", true);
            await store.DeleteArtifactLocalizationAsync(cached.Id, ct);
        }

        var payload = new ArtifactLocalizationJobPayload(
            projectId,
            Guid.Empty,
            package.Package.Version,
            normalized,
            ProductionPackageId: packageId
        );
        var job = new Job(
            "artifact-localization",
            JsonSerializer.Serialize(
                payload,
                RequestCompetitorAnalysisLocalizationHandler.JsonOptions
            ),
            timeProvider.GetUtcNow(),
            options.MaxJobRetries,
            projectId: projectId,
            videoProjectId: videoProjectId,
            artifactType: LocalizableArtifactTypes.ProductionPackage,
            artifactId: packageId,
            artifactVersion: package.Package.Version,
            locale: normalized
        );
        var persisted = await store.EnqueueArtifactLocalizationJobAsync(job, ct);
        return new(persisted.Id, persisted.Status.ToString(), persisted.Id != job.Id);
    }
}

public sealed class GetProductionPackageLocalizationHandler(IYoutubeAiFactoryStore store)
{
    public async Task<ProductionPackageLocalizationStatusDto> HandleAsync(
        Guid projectId,
        Guid videoProjectId,
        Guid packageId,
        string locale,
        CancellationToken ct
    )
    {
        var normalized = RequestCompetitorAnalysisLocalizationHandler.NormalizeLocale(locale);
        var package =
            await store.GetProductionPackageAsync(projectId, videoProjectId, packageId, false, ct)
            ?? throw new ResourceNotFoundException("Production package was not found.");
        var version = package.Package.Version;
        var cached = await store.GetArtifactLocalizationAsync(
            LocalizableArtifactTypes.ProductionPackage,
            packageId,
            version,
            normalized,
            ct
        );
        var active = await store.GetActiveArtifactLocalizationJobAsync(
            LocalizableArtifactTypes.ProductionPackage,
            packageId,
            version,
            normalized,
            ct
        );
        var latest =
            active
            ?? await store.GetLatestArtifactLocalizationJobAsync(
                LocalizableArtifactTypes.ProductionPackage,
                packageId,
                version,
                normalized,
                ct
            );
        var content = cached is not null && LocalizedProductionPackageCache.IsValid(package, cached)
            ? JsonSerializer.Deserialize<LocalizedProductionPackageContent>(
                cached.ContentJson,
                RequestCompetitorAnalysisLocalizationHandler.JsonOptions
            )
            : null;
        return new(
            content,
            active is null
                ? null
                : new(active.Id, active.Status.ToString(), active.FailureReason),
            latest is null
                ? null
                : new(latest.Id, latest.Status.ToString(), latest.FailureReason)
        );
    }
}

public static class ProductionPackageLocalizationPrompt
{
    public const string Key = "production-package-localization";
    public const int Version = 1;
    public static AiModelProfile ModelProfile => AiWorkflowProfiles.ArtifactLocalization;

    public static LlmRequest Create(
        ProductionPackageWithDetails package,
        bool correcting
    ) =>
        new(
            Key,
            Version,
            "Translate only the supplied reader-facing production explanations into Vietnamese. Echo canonicalContentFingerprint and every ID/index exactly. Preserve list order, count, and empty-field topology. Do not translate or alter Script narration, generation prompts, on-screen copy, composition/motion instructions, IDs, claims, URLs, enums, statuses, versions, metrics, or export data. Do not add facts or production instructions. Return JSON only.",
            $"Canonical reader-facing production content:\n{JsonSerializer.Serialize(LocalizedProductionPackageValidator.Source(package), RequestCompetitorAnalysisLocalizationHandler.JsonOptions)}"
                + (correcting ? "\nPreserve all identities and list topology exactly." : string.Empty),
            new Dictionary<string, string> { ["max_output_tokens"] = "8000" },
            Schema(),
            ModelProfile
        );

    private static JsonNode Schema() =>
        JsonNode
            .Parse(
                """
                {"type":"object","additionalProperties":false,"properties":{
                  "canonicalContentFingerprint":{"type":"string","minLength":64,"maxLength":64},
                  "warnings":{"type":"array","items":{"$ref":"#/$defs/indexedText"}},
                  "groundingIssues":{"type":"array","items":{"type":"object","additionalProperties":false,"properties":{"index":{"type":"integer","minimum":0},"explanation":{"type":"string"}},"required":["index","explanation"]}},
                  "scenes":{"type":"array","items":{"type":"object","additionalProperties":false,"properties":{"sceneId":{"type":"string","format":"uuid"},"narrationSummary":{"type":"string"},"visualStrategy":{"type":"string"},"shots":{"type":"array","items":{"type":"object","additionalProperties":false,"properties":{"shotId":{"type":"string","format":"uuid"},"visualDescription":{"type":"string"}},"required":["shotId","visualDescription"]}}},"required":["sceneId","narrationSummary","visualStrategy","shots"]}},
                  "assets":{"type":"array","items":{"type":"object","additionalProperties":false,"properties":{"assetId":{"type":"string","format":"uuid"},"creativeBrief":{"type":"string"},"sourceSearchBrief":{"type":"string"}},"required":["assetId","creativeBrief","sourceSearchBrief"]}}
                },"required":["canonicalContentFingerprint","warnings","groundingIssues","scenes","assets"],
                "$defs":{"indexedText":{"type":"object","additionalProperties":false,"properties":{"index":{"type":"integer","minimum":0},"text":{"type":"string"}},"required":["index","text"]}}}
                """
            )!
            .DeepClone();
}

public static class LocalizedProductionPackageValidator
{
    public static void Validate(
        ProductionPackageWithDetails canonical,
        LocalizedProductionPackageContent localized
    )
    {
        var source = Source(canonical);
        if (
            !string.Equals(
                localized.CanonicalContentFingerprint,
                source.CanonicalContentFingerprint,
                StringComparison.Ordinal
            )
            || !PreservesIndexed(localized.Warnings, source.Warnings.Count)
            || localized.GroundingIssues.Count != source.GroundingIssues.Count
            || !localized.GroundingIssues.Select(x => x.Index).SequenceEqual(
                Enumerable.Range(0, source.GroundingIssues.Count)
            )
            || localized.GroundingIssues.Any(x => string.IsNullOrWhiteSpace(x.Explanation))
            || localized.Scenes.Count != source.Scenes.Count
            || !localized.Scenes.Select(x => x.SceneId).SequenceEqual(
                source.Scenes.Select(x => x.SceneId)
            )
            || localized.Assets.Count != source.Assets.Count
            || !localized.Assets.Select(x => x.AssetId).SequenceEqual(
                source.Assets.Select(x => x.AssetId)
            )
        )
            throw new StructuredOutputException(
                "Localized production package does not preserve canonical identities and topology."
            );

        for (var index = 0; index < source.Scenes.Count; index++)
        {
            var original = source.Scenes[index];
            var translated = localized.Scenes[index];
            if (
                string.IsNullOrWhiteSpace(translated.NarrationSummary)
                || string.IsNullOrWhiteSpace(translated.VisualStrategy)
                || translated.Shots.Count != original.Shots.Count
                || !translated.Shots.Select(x => x.ShotId).SequenceEqual(
                    original.Shots.Select(x => x.ShotId)
                )
                || translated.Shots.Any(x => string.IsNullOrWhiteSpace(x.VisualDescription))
            )
                throw new StructuredOutputException(
                    "Localized production scene content is incomplete or reordered."
                );
        }

        for (var index = 0; index < source.Assets.Count; index++)
        {
            var original = source.Assets[index];
            var translated = localized.Assets[index];
            if (
                string.IsNullOrWhiteSpace(translated.CreativeBrief)
                || !PreservesOptional(original.SourceSearchBrief, translated.SourceSearchBrief)
            )
                throw new StructuredOutputException(
                    "Localized production asset content is incomplete."
                );
        }
    }

    internal static LocalizedProductionPackageContent Source(
        ProductionPackageWithDetails package
    )
    {
        var warnings = JsonSerializer.Deserialize<string[]>(
                package.Package.WarningsJson,
                RequestCompetitorAnalysisLocalizationHandler.JsonOptions
            ) ?? [];
        var issues = JsonSerializer.Deserialize<ProductionGroundingIssueResult[]>(
                package.Package.GroundingIssuesJson,
                ProductionPrompt.SerializerOptions
            ) ?? [];
        var shots = package
            .Shots.GroupBy(x => x.ProductionSceneId)
            .ToDictionary(x => x.Key, x => x.OrderBy(y => y.Sequence).ToArray());
        var scenes = package
            .Scenes.OrderBy(x => x.Sequence)
            .Select(scene => new LocalizedProductionScene(
                scene.Id,
                scene.NarrationSummary,
                scene.VisualStrategy,
                shots
                    .GetValueOrDefault(scene.Id, [])
                    .Select(shot => new LocalizedProductionShot(
                        shot.Id,
                        shot.VisualDescription
                    ))
                    .ToArray()
            ))
            .ToArray();
        var assets = package
            .Assets.OrderBy(x => x.AssetKey)
            .Select(asset => new LocalizedProductionAsset(
                asset.Id,
                asset.CreativeBrief,
                asset.SourceSearchBrief
            ))
            .ToArray();
        var fingerprint = CreateFingerprint(warnings, issues, scenes, assets);
        return new(
            fingerprint,
            warnings.Select((text, index) => new LocalizedProductionText(index, text)).ToArray(),
            issues
                .Select((issue, index) => new LocalizedProductionIssue(index, issue.Explanation))
                .ToArray(),
            scenes,
            assets
        );
    }

    public static string CreateFingerprint(ProductionPackageWithDetails package)
    {
        var source = Source(package);
        return source.CanonicalContentFingerprint;
    }

    private static string CreateFingerprint(
        IReadOnlyList<string> warnings,
        IReadOnlyList<ProductionGroundingIssueResult> issues,
        IReadOnlyList<LocalizedProductionScene> scenes,
        IReadOnlyList<LocalizedProductionAsset> assets
    )
    {
        var material = JsonSerializer.Serialize(
            new
            {
                Warnings = warnings,
                Issues = issues.Select(x => x.Explanation),
                Scenes = scenes,
                Assets = assets,
            },
            RequestCompetitorAnalysisLocalizationHandler.JsonOptions
        );
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(material)));
    }

    private static bool PreservesIndexed(
        IReadOnlyList<LocalizedProductionText> values,
        int count
    ) =>
        values.Count == count
        && values.Select(x => x.Index).SequenceEqual(Enumerable.Range(0, count))
        && values.All(x => !string.IsNullOrWhiteSpace(x.Text));

    private static bool PreservesOptional(string original, string translated) =>
        string.IsNullOrWhiteSpace(original)
            ? string.IsNullOrWhiteSpace(translated)
            : !string.IsNullOrWhiteSpace(translated);
}

public static class LocalizedProductionPackageCache
{
    public static bool IsValid(
        ProductionPackageWithDetails package,
        ArtifactLocalization localization
    )
    {
        try
        {
            var content = JsonSerializer.Deserialize<LocalizedProductionPackageContent>(
                    localization.ContentJson,
                    RequestCompetitorAnalysisLocalizationHandler.JsonOptions
                )
                ?? throw new StructuredOutputException(
                    "Stored localized production package is empty."
                );
            LocalizedProductionPackageValidator.Validate(package, content);
            return true;
        }
        catch (Exception ex)
            when (ex
                is JsonException
                    or StructuredOutputException
                    or InvalidOperationException)
        {
            return false;
        }
    }
}
