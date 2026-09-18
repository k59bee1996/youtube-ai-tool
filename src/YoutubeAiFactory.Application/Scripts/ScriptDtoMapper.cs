using System.Text.Json;
using YoutubeAiFactory.Application.Common;
using YoutubeAiFactory.Application.Outlines;
using YoutubeAiFactory.Application.Persistence;
using YoutubeAiFactory.Application.Research;
using YoutubeAiFactory.Domain.Scripts;
using YoutubeAiFactory.Domain.Videos;

namespace YoutubeAiFactory.Application.Scripts;

internal static class ScriptDtoMapper
{
    public static async Task<VideoScriptDto> MapAsync(IYoutubeAiFactoryStore store,
        VideoScriptWithDetails details, VideoProject videoProject, CancellationToken cancellationToken)
    {
        var research = await store.GetResearchReportAsync(details.Script.ProjectId,
            details.Script.VideoProjectId, details.Script.ResearchReportId, cancellationToken)
            ?? throw new ResourceNotFoundException("The Script's source ResearchReport was not found.");
        var claims = research.Claims.ToDictionary(item => item.Id);
        var evidence = research.Evidence.ToDictionary(item => item.Id);
        var sources = research.Sources.ToDictionary(item => item.Id);
        var conflicts = research.Conflicts.ToDictionary(item => item.Id);
        var evidenceLinks = research.ClaimEvidence.GroupBy(item => item.ResearchClaimId)
            .ToDictionary(group => group.Key, group => group.ToArray());
        var blocksBySection = details.Blocks.GroupBy(item => item.VideoScriptSectionId)
            .ToDictionary(group => group.Key, group => group.OrderBy(item => item.Sequence).ToArray());
        var claimsByBlock = details.BlockClaims.GroupBy(item => item.ScriptBlockId)
            .ToDictionary(group => group.Key, group => group.ToArray());
        var conflictsByBlock = details.BlockConflicts.GroupBy(item => item.ScriptBlockId)
            .ToDictionary(group => group.Key, group => group.ToArray());

        var sections = details.Sections.OrderBy(item => item.Sequence).Select(section =>
            new VideoScriptSectionDto(section.Id, section.VideoOutlineSectionId, section.Sequence,
                section.Heading, section.WordCount, section.EstimatedDurationSeconds,
                blocksBySection.GetValueOrDefault(section.Id, []).Select(block =>
                {
                    var blockClaims = claimsByBlock.GetValueOrDefault(block.Id, [])
                        .Where(link => claims.ContainsKey(link.ResearchClaimId))
                        .Select(link => claims[link.ResearchClaimId])
                        .Select(claim => new ScriptClaimDto(claim.Id, claim.Statement, claim.Type.ToString(),
                            claim.SupportStatus.ToString(), claim.Confidence, claim.IsCritical,
                            evidenceLinks.GetValueOrDefault(claim.Id, [])
                                .Where(link => evidence.ContainsKey(link.ResearchEvidenceId))
                                .Select(link => evidence[link.ResearchEvidenceId])
                                .Where(item => sources.ContainsKey(item.ResearchSourceId))
                                .Select(item => new OutlineEvidenceDto(item.Id, item.Fact,
                                    item.SupportingExcerpt, item.SourceLocator, item.Type.ToString(),
                                    item.Confidence, new ResearchSourceDto(sources[item.ResearchSourceId].Id,
                                        sources[item.ResearchSourceId].Url,
                                        sources[item.ResearchSourceId].CanonicalUrl,
                                        sources[item.ResearchSourceId].Domain,
                                        sources[item.ResearchSourceId].Title,
                                        sources[item.ResearchSourceId].Publisher,
                                        sources[item.ResearchSourceId].PublishedAt,
                                        sources[item.ResearchSourceId].RetrievedAt,
                                        sources[item.ResearchSourceId].Category.ToString(),
                                        sources[item.ResearchSourceId].FetchStatus.ToString())))
                                .ToArray())).ToArray();
                    var blockConflicts = conflictsByBlock.GetValueOrDefault(block.Id, [])
                        .Where(link => conflicts.ContainsKey(link.ResearchConflictId))
                        .Select(link => conflicts[link.ResearchConflictId])
                        .Select(item => new ScriptConflictDto(item.Id, item.ResearchClaimId,
                            item.Explanation, item.IsResolved)).ToArray();
                    return new VideoScriptBlockDto(block.Id, block.Sequence, block.Type.ToString(), block.Text,
                        block.WordCount, blockClaims, blockConflicts);
                }).ToArray())).ToArray();

        var script = details.Script;
        var issues = JsonSerializer.Deserialize<ScriptGroundingIssueResult[]>(script.GroundingIssuesJson,
            ScriptPrompt.SerializerOptions) ?? [];
        return new VideoScriptDto(script.Id, script.ProjectId, script.VideoProjectId, script.VideoOutlineId,
            script.VideoOutlineVersion, script.ResearchReportId, script.ResearchReportVersion, script.Version,
            script.Status.ToString(), script.GroundingStatus.ToString(), script.ScriptEngineVersion,
            script.PromptKey, script.PromptVersion, script.Provider, script.Model, script.ContentLanguage,
            script.TotalWordCount, script.EstimatedDurationSeconds,
            await IsStaleAsync(store, script, videoProject, cancellationToken), DeserializeStrings(script.WarningsJson),
            issues.Select(item => new ScriptGroundingIssueDto(item.SectionSequence, item.BlockSequence,
                item.IssueType.ToString(), item.Severity.ToString(), item.ProblematicText,
                item.RelevantClaimIds, item.Explanation)).ToArray(), sections, script.CreatedAt,
            script.UpdatedAt, script.ApprovedAt);
    }

    public static async Task<bool> IsStaleAsync(IYoutubeAiFactoryStore store, VideoScript script,
        VideoProject videoProject, CancellationToken cancellationToken)
    {
        var outline = await store.GetApprovedVideoOutlineAsync(script.ProjectId, script.VideoProjectId,
            cancellationToken);
        var research = await store.GetLatestResearchReportAsync(script.ProjectId, script.VideoProjectId,
            cancellationToken);
        if (outline is null || research is null || outline.Outline.Id != script.VideoOutlineId ||
            outline.Outline.Version != script.VideoOutlineVersion ||
            research.Report.Id != script.ResearchReportId || research.Report.Version != script.ResearchReportVersion)
            return true;
        var project = await store.GetProjectAsync(script.ProjectId, cancellationToken);
        var source = await store.GetVideoProjectSourceAsync(script.ProjectId, videoProject.PilotId,
            videoProject.PilotVideoId, cancellationToken);
        return project is null || source is null ||
            !string.Equals(script.InputFingerprint,
                ScriptGenerationContextBuilder.CreateFingerprint(project, videoProject, outline.Outline,
                    research.Report), StringComparison.Ordinal);
    }

    private static string[] DeserializeStrings(string json) =>
        JsonSerializer.Deserialize<string[]>(json, ScriptPrompt.SerializerOptions) ?? [];
}
