using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using YoutubeAiFactory.Application.Common;
using YoutubeAiFactory.Application.Research;
using YoutubeAiFactory.Application.Scripts;
using YoutubeAiFactory.Application.Videos;
using YoutubeAiFactory.Domain.Projects;
using YoutubeAiFactory.Domain.Research;
using YoutubeAiFactory.Domain.Scripts;
using YoutubeAiFactory.Domain.Videos;

namespace YoutubeAiFactory.Application.Production;

public sealed class ProductionContextBuilder(ProductionOptions options)
{
    public ProductionGenerationContext Build(
        Project project,
        VideoProject videoProject,
        VideoProjectSource source,
        VideoScriptWithDetails script,
        ResearchReportWithDetails research
    )
    {
        ProductionValidator.ValidateOptions(options);
        if (
            script.Script.Status != VideoScriptStatus.Approved
            || script.Script.GroundingStatus != ScriptGroundingStatus.Passed
        )
            throw new ApplicationValidationException(
                "An explicitly approved, grounding-passed Script is required."
            );
        if (
            script.Script.ProjectId != project.Id
            || script.Script.VideoProjectId != videoProject.Id
            || research.Report.Id != script.Script.ResearchReportId
            || research.Report.Version != script.Script.ResearchReportVersion
        )
            throw new ApplicationValidationException(
                "Production inputs do not share exact Script and Research lineage."
            );

        var claims = research.Claims.ToDictionary(x => x.Id);
        var evidence = research.Evidence.ToDictionary(x => x.Id);
        var sources = research.Sources.ToDictionary(x => x.Id);
        var evidenceByClaim = research
            .ClaimEvidence.GroupBy(x => x.ResearchClaimId)
            .ToDictionary(x => x.Key, x => x.ToArray());
        var conflicts = research.Conflicts.ToDictionary(x => x.Id);
        var claimIdsByBlock = script
            .BlockClaims.GroupBy(x => x.ScriptBlockId)
            .ToDictionary(x => x.Key, x => x.Select(y => y.ResearchClaimId).ToArray());
        var conflictIdsByBlock = script
            .BlockConflicts.GroupBy(x => x.ScriptBlockId)
            .ToDictionary(x => x.Key, x => x.Select(y => y.ResearchConflictId).ToArray());
        var sectionSeq = script.Sections.ToDictionary(x => x.Id, x => x.Sequence);
        var global = 0;
        var blocks = script
            .Blocks.OrderBy(x => sectionSeq[x.VideoScriptSectionId])
            .ThenBy(x => x.Sequence)
            .Select(block =>
            {
                var blockClaims = claimIdsByBlock
                    .GetValueOrDefault(block.Id, [])
                    .Where(claims.ContainsKey)
                    .Select(id => claims[id])
                    .Where(x => x.SupportStatus != ResearchClaimSupportStatus.Unsupported)
                    .Select(claim => new ProductionContextClaim(
                        claim.Id,
                        claim.Statement,
                        claim.SupportStatus.ToString(),
                        claim.IsCritical,
                        evidenceByClaim
                            .GetValueOrDefault(claim.Id, [])
                            .Where(x => evidence.ContainsKey(x.ResearchEvidenceId))
                            .Select(x => evidence[x.ResearchEvidenceId].SupportingExcerpt)
                            .ToArray(),
                        evidenceByClaim
                            .GetValueOrDefault(claim.Id, [])
                            .Where(x =>
                                evidence.ContainsKey(x.ResearchEvidenceId)
                                && sources.ContainsKey(
                                    evidence[x.ResearchEvidenceId].ResearchSourceId
                                )
                            )
                            .Select(x => evidence[x.ResearchEvidenceId])
                            .Select(x => sources[x.ResearchSourceId].CanonicalUrl)
                            .Distinct()
                            .ToArray()
                    ))
                    .ToArray();
                var blockConflicts = conflictIdsByBlock
                    .GetValueOrDefault(block.Id, [])
                    .Where(conflicts.ContainsKey)
                    .Select(id => conflicts[id])
                    .Select(x => new ProductionContextConflict(
                        x.Id,
                        x.ResearchClaimId,
                        x.Explanation,
                        x.IsResolved
                    ))
                    .ToArray();
                return new ProductionContextBlock(
                    block.Id,
                    ++global,
                    sectionSeq[block.VideoScriptSectionId],
                    block.Sequence,
                    block.Type,
                    block.Text,
                    block.WordCount,
                    blockClaims,
                    blockConflicts
                );
            })
            .ToArray();
        return new(
            project.Id,
            videoProject.Id,
            script.Script.Id,
            script.Script.Version,
            script.Script.VideoOutlineId,
            script.Script.VideoOutlineVersion,
            script.Script.ResearchReportId,
            script.Script.ResearchReportVersion,
            CreateFingerprint(project, videoProject, script.Script),
            videoProject.WorkingTitle,
            videoProject.ViewerPromise,
            videoProject.ContentFormat,
            script.Script.ContentLanguage,
            project.Market.TargetGeography,
            videoProject.TargetAudience,
            project.Audience.Description,
            videoProject.ExperimentType.ToString(),
            videoProject.PilotHypothesis,
            videoProject.VariableBeingTested,
            source.PilotVideo.ControlStrategy,
            videoProject.PrimaryMetric,
            videoProject.SuccessSignal,
            script.Script.EstimatedDurationSeconds,
            blocks
        );
    }

    public static string CreateFingerprint(
        Project project,
        VideoProject videoProject,
        VideoScript script
    )
    {
        var json = JsonSerializer.Serialize(
            new
            {
                ProjectId = project.Id,
                ContentLanguage = project.Market.TargetLanguage,
                TargetGeography = project.Market.TargetGeography,
                VideoProjectId = videoProject.Id,
                videoProject.WorkingTitle,
                videoProject.ViewerPromise,
                videoProject.ContentFormat,
                videoProject.TargetAudience,
                project.Audience.Description,
                videoProject.ExperimentType,
                videoProject.PilotHypothesis,
                videoProject.VariableBeingTested,
                videoProject.PrimaryMetric,
                videoProject.SuccessSignal,
                ScriptId = script.Id,
                script.Version,
                script.InputFingerprint,
                script.EstimatedDurationSeconds,
            }
        );
        return Convert
            .ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json)))
            .ToLowerInvariant();
    }
}
