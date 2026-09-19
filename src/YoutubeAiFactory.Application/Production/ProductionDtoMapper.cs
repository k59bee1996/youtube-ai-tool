using System.Text.Json;
using YoutubeAiFactory.Application.Common;
using YoutubeAiFactory.Application.Persistence;
using YoutubeAiFactory.Domain.Production;
using YoutubeAiFactory.Domain.Videos;

namespace YoutubeAiFactory.Application.Production;

internal static class ProductionDtoMapper
{
    public static async Task<ProductionPackageDto> MapAsync(
        IYoutubeAiFactoryStore store,
        ProductionPackageWithDetails details,
        VideoProject videoProject,
        CancellationToken ct
    )
    {
        var script =
            await store.GetVideoScriptAsync(
                details.Package.ProjectId,
                details.Package.VideoProjectId,
                details.Package.VideoScriptId,
                false,
                ct
            ) ?? throw new ResourceNotFoundException("Production package Script was not found.");
        var research =
            await store.GetResearchReportAsync(
                details.Package.ProjectId,
                details.Package.VideoProjectId,
                details.Package.ResearchReportId,
                ct
            )
            ?? throw new ResourceNotFoundException(
                "Production package ResearchReport was not found."
            );
        var claims = research.Claims.ToDictionary(x => x.Id);
        var evidence = research.Evidence.ToDictionary(x => x.Id);
        var sources = research.Sources.ToDictionary(x => x.Id);
        var links = research
            .ClaimEvidence.GroupBy(x => x.ResearchClaimId)
            .ToDictionary(x => x.Key, x => x.ToArray());
        ProductionClaimDto Claim(Guid id)
        {
            var c = claims[id];
            return new(
                c.Id,
                c.Statement,
                c.SupportStatus.ToString(),
                links
                    .GetValueOrDefault(id, [])
                    .Where(x =>
                        evidence.ContainsKey(x.ResearchEvidenceId)
                        && sources.ContainsKey(evidence[x.ResearchEvidenceId].ResearchSourceId)
                    )
                    .Select(x =>
                        sources[evidence[x.ResearchEvidenceId].ResearchSourceId].CanonicalUrl
                    )
                    .Distinct()
                    .ToArray()
            );
        }
        var scriptBlocks = script.Blocks.ToDictionary(x => x.Id);
        var mapsByScene = details
            .SceneBlocks.GroupBy(x => x.ProductionSceneId)
            .ToDictionary(x => x.Key, x => x.OrderBy(y => y.Sequence).ToArray());
        var shotsByScene = details
            .Shots.GroupBy(x => x.ProductionSceneId)
            .ToDictionary(x => x.Key, x => x.OrderBy(y => y.Sequence).ToArray());
        var shotClaims = details
            .ShotClaims.GroupBy(x => x.ProductionShotId)
            .ToDictionary(x => x.Key, x => x.Select(y => y.ResearchClaimId).ToArray());
        var textByScene = details
            .OnScreenText.GroupBy(x => x.ProductionSceneId)
            .ToDictionary(x => x.Key, x => x.OrderBy(y => y.Sequence).ToArray());
        var textClaims = details
            .OnScreenTextClaims.GroupBy(x => x.ProductionOnScreenTextId)
            .ToDictionary(x => x.Key, x => x.Select(y => y.ResearchClaimId).ToArray());
        var assetClaims = details
            .AssetClaims.GroupBy(x => x.ProductionAssetRequirementId)
            .ToDictionary(x => x.Key, x => x.Select(y => y.ResearchClaimId).ToArray());
        var scenes = details
            .Scenes.OrderBy(x => x.Sequence)
            .Select(scene =>
            {
                var maps = mapsByScene.GetValueOrDefault(scene.Id, []);
                return new ProductionSceneDto(
                    scene.Id,
                    scene.Sequence,
                    scene.Purpose.ToString(),
                    string.Join(
                        "\n\n",
                        maps.Where(x => scriptBlocks.ContainsKey(x.ScriptBlockId))
                            .Select(x => scriptBlocks[x.ScriptBlockId].Text)
                    ),
                    scene.NarrationSummary,
                    scene.VisualStrategy,
                    scene.EstimatedDurationSeconds,
                    scene.Complexity.ToString(),
                    scene.TransitionIntent,
                    scene.MusicBrief,
                    scene.SoundEffectCue,
                    scene.VoiceDirection,
                    maps.Select(x => x.ScriptBlockId).ToArray(),
                    shotsByScene
                        .GetValueOrDefault(scene.Id, [])
                        .Select(x => new ProductionShotDto(
                            x.Id,
                            x.Sequence,
                            x.ShotType.ToString(),
                            x.VisualDescription,
                            x.Composition,
                            x.MotionSuggestion,
                            x.EstimatedDurationSeconds,
                            x.FactualityMode.ToString(),
                            x.AssetRequirementId,
                            shotClaims
                                .GetValueOrDefault(x.Id, [])
                                .Where(claims.ContainsKey)
                                .Select(Claim)
                                .ToArray(),
                            x.Notes
                        ))
                        .ToArray(),
                    textByScene
                        .GetValueOrDefault(scene.Id, [])
                        .Select(x => new ProductionOnScreenTextDto(
                            x.Id,
                            x.Sequence,
                            x.Text,
                            x.Type.ToString(),
                            x.TimingIntent,
                            textClaims
                                .GetValueOrDefault(x.Id, [])
                                .Where(claims.ContainsKey)
                                .Select(Claim)
                                .ToArray()
                        ))
                        .ToArray()
                );
            })
            .ToArray();
        var assets = details
            .Assets.OrderBy(x => x.AssetKey)
            .Select(x => new ProductionAssetDto(
                x.Id,
                x.AssetKey,
                x.AssetType.ToString(),
                x.AcquisitionMode.ToString(),
                x.CreativeBrief,
                x.GenerationPrompt,
                x.SourceSearchBrief,
                x.RightsVerificationRequired,
                x.FactualityMode.ToString(),
                x.ReuseKey,
                x.Complexity.ToString(),
                assetClaims
                    .GetValueOrDefault(x.Id, [])
                    .Where(claims.ContainsKey)
                    .Select(Claim)
                    .ToArray()
            ))
            .ToArray();
        var p = details.Package;
        var issues =
            JsonSerializer.Deserialize<ProductionGroundingIssueResult[]>(
                p.GroundingIssuesJson,
                ProductionPrompt.SerializerOptions
            ) ?? [];
        return new(
            p.Id,
            p.ProjectId,
            p.VideoProjectId,
            p.VideoScriptId,
            p.VideoScriptVersion,
            p.VideoOutlineId,
            p.VideoOutlineVersion,
            p.ResearchReportId,
            p.ResearchReportVersion,
            p.Version,
            p.Status.ToString(),
            p.GroundingStatus.ToString(),
            p.EngineVersion,
            p.PromptKey,
            p.PromptVersion,
            p.Provider,
            p.Model,
            p.ContentLanguage,
            p.EstimatedDurationSeconds,
            p.VisualDirection,
            p.PacingDirection,
            p.ColorDirection,
            p.TypographyDirection,
            p.AudioDirection,
            p.ExperimentProductionNotes,
            await IsStaleAsync(store, p, videoProject, ct),
            JsonSerializer.Deserialize<string[]>(p.WarningsJson, ProductionPrompt.SerializerOptions)
                ?? [],
            issues
                .Select(x => new ProductionGroundingIssueDto(
                    x.SceneSequence,
                    x.ShotSequence,
                    x.AssetKey,
                    x.OnScreenTextSequence,
                    x.IssueType.ToString(),
                    x.Severity.ToString(),
                    x.ProblematicText,
                    x.RelevantClaimIds,
                    x.Explanation
                ))
                .ToArray(),
            scenes,
            assets,
            p.CreatedAt,
            p.UpdatedAt,
            p.ApprovedAt
        );
    }

    public static async Task<bool> IsStaleAsync(
        IYoutubeAiFactoryStore store,
        ProductionPackage package,
        VideoProject videoProject,
        CancellationToken ct
    )
    {
        var script = await store.GetApprovedVideoScriptAsync(
            package.ProjectId,
            package.VideoProjectId,
            ct
        );
        var project = await store.GetProjectAsync(package.ProjectId, ct);
        return script is null
            || project is null
            || script.Script.Id != package.VideoScriptId
            || script.Script.Version != package.VideoScriptVersion
            || !string.Equals(
                package.InputFingerprint,
                ProductionContextBuilder.CreateFingerprint(project, videoProject, script.Script),
                StringComparison.Ordinal
            );
    }

    public static ProductionPackageResult ToResult(ProductionPackageWithDetails details)
    {
        var maps = details
            .SceneBlocks.GroupBy(x => x.ProductionSceneId)
            .ToDictionary(
                x => x.Key,
                x => x.OrderBy(y => y.Sequence).Select(y => y.ScriptBlockId).ToArray()
            );
        var assetsById = details.Assets.ToDictionary(x => x.Id);
        var sc = details
            .ShotClaims.GroupBy(x => x.ProductionShotId)
            .ToDictionary(x => x.Key, x => x.Select(y => y.ResearchClaimId).ToArray());
        var ac = details
            .AssetClaims.GroupBy(x => x.ProductionAssetRequirementId)
            .ToDictionary(x => x.Key, x => x.Select(y => y.ResearchClaimId).ToArray());
        var tc = details
            .OnScreenTextClaims.GroupBy(x => x.ProductionOnScreenTextId)
            .ToDictionary(x => x.Key, x => x.Select(y => y.ResearchClaimId).ToArray());
        var scenes = details
            .Scenes.OrderBy(x => x.Sequence)
            .Select(scene => new ProductionSceneResult(
                scene.Sequence,
                scene.Purpose,
                scene.NarrationSummary,
                scene.VisualStrategy,
                scene.Complexity,
                scene.TransitionIntent,
                scene.MusicBrief,
                scene.SoundEffectCue,
                scene.VoiceDirection,
                maps.GetValueOrDefault(scene.Id, []),
                details
                    .Shots.Where(x => x.ProductionSceneId == scene.Id)
                    .OrderBy(x => x.Sequence)
                    .Select(x => new ProductionShotResult(
                        x.Sequence,
                        x.ShotType,
                        x.VisualDescription,
                        x.Composition,
                        x.MotionSuggestion,
                        x.EstimatedDurationSeconds,
                        x.FactualityMode,
                        x.AssetRequirementId is Guid id && assetsById.TryGetValue(id, out var a)
                            ? a.AssetKey
                            : null,
                        sc.GetValueOrDefault(x.Id, []),
                        x.Notes
                    ))
                    .ToArray(),
                details
                    .OnScreenText.Where(x => x.ProductionSceneId == scene.Id)
                    .OrderBy(x => x.Sequence)
                    .Select(x => new ProductionOnScreenTextResult(
                        x.Sequence,
                        x.Text,
                        x.Type,
                        x.TimingIntent,
                        tc.GetValueOrDefault(x.Id, [])
                    ))
                    .ToArray()
            ))
            .ToArray();
        var p = details.Package;
        return new(
            p.VisualDirection,
            p.PacingDirection,
            p.ColorDirection,
            p.TypographyDirection,
            p.AudioDirection,
            p.ExperimentProductionNotes,
            scenes,
            details
                .Assets.Select(x => new ProductionAssetResult(
                    x.AssetKey,
                    x.AssetType,
                    x.AcquisitionMode,
                    x.CreativeBrief,
                    x.GenerationPrompt,
                    x.SourceSearchBrief,
                    x.RightsVerificationRequired,
                    x.FactualityMode,
                    x.ReuseKey,
                    x.Complexity,
                    ac.GetValueOrDefault(x.Id, [])
                ))
                .ToArray(),
            JsonSerializer.Deserialize<string[]>(p.WarningsJson, ProductionPrompt.SerializerOptions)
                ?? []
        );
    }
}
