using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using YoutubeAiFactory.Application.Common;
using YoutubeAiFactory.Application.Outlines;
using YoutubeAiFactory.Application.Research;
using YoutubeAiFactory.Application.Videos;
using YoutubeAiFactory.Domain.Outlines;
using YoutubeAiFactory.Domain.Projects;
using YoutubeAiFactory.Domain.Research;
using YoutubeAiFactory.Domain.Videos;

namespace YoutubeAiFactory.Application.Scripts;

public sealed class ScriptGenerationContextBuilder(ScriptOptions options)
{
    public ScriptGenerationContext Build(Project project, VideoProject videoProject, VideoProjectSource source,
        VideoOutlineWithDetails outline, ResearchReportWithDetails research)
    {
        ValidateOptions(options);
        if (project.Id != videoProject.ProjectId || outline.Outline.ProjectId != project.Id ||
            outline.Outline.VideoProjectId != videoProject.Id || research.Report.ProjectId != project.Id ||
            research.Report.VideoProjectId != videoProject.Id)
            throw new ApplicationValidationException("Script inputs do not belong to the same Project and VideoProject.");
        if (outline.Outline.Status != VideoOutlineStatus.Approved)
            throw new ApplicationValidationException("An explicitly approved VideoOutline is required before generating a script.");
        if (outline.Outline.ResearchReportId != research.Report.Id ||
            outline.Outline.ResearchReportVersion != research.Report.Version)
            throw new ApplicationValidationException("The approved outline does not reference the supplied ResearchReport.");

        OutlinePersistenceValidator.Validate(outline, research);
        var currentResearchFingerprint = ResearchBriefBuilder.CreateFingerprint(
            ResearchBriefBuilder.Build(project, videoProject, source.Opportunity.Name));
        if (!string.Equals(research.Report.InputFingerprint, currentResearchFingerprint, StringComparison.Ordinal) ||
            !string.Equals(outline.Outline.InputFingerprint,
                OutlineGenerationContextBuilder.CreateFingerprint(project, videoProject, source, research.Report),
                StringComparison.Ordinal))
            throw new ApplicationValidationException(
                "The approved outline is based on outdated research. Review or regenerate the outline before generating the script.");

        var researchPayload = JsonSerializer.Deserialize<ResearchReportPayload>(research.Report.ResultJson,
            ScriptPrompt.SerializerOptions) ?? throw new ApplicationValidationException("The source ResearchReport is unreadable.");
        var claims = research.Claims.ToDictionary(item => item.Id);
        var evidence = research.Evidence.ToDictionary(item => item.Id);
        var sources = research.Sources.ToDictionary(item => item.Id);
        var conflicts = research.Conflicts.ToDictionary(item => item.Id);
        var claimLinks = research.ClaimEvidence.GroupBy(item => item.ResearchClaimId)
            .ToDictionary(group => group.Key, group => group.ToArray());
        var claimsBySection = outline.SectionClaims.GroupBy(item => item.OutlineSectionId)
            .ToDictionary(group => group.Key, group => group.ToArray());
        var conflictsBySection = outline.SectionConflicts.GroupBy(item => item.OutlineSectionId)
            .ToDictionary(group => group.Key, group => group.ToArray());
        var gapsBySection = outline.SectionGaps.GroupBy(item => item.OutlineSectionId)
            .ToDictionary(group => group.Key, group => group.ToArray());
        var targetSeconds = outline.Outline.TotalEstimatedSeconds ?? options.DefaultTargetMinutes * 60;
        var targetWords = WordsForSeconds(targetSeconds);

        var sections = outline.Sections.OrderBy(item => item.Sequence).Select(section =>
        {
            var sectionClaims = claimsBySection.GetValueOrDefault(section.Id, [])
                .Select(link => claims.GetValueOrDefault(link.ResearchClaimId))
                .Where(claim => claim is not null && claim.SupportStatus != ResearchClaimSupportStatus.Unsupported)
                .Select(claim => new ScriptContextClaim(claim!.Id, claim.Statement, claim.Type.ToString(),
                    claim.SupportStatus.ToString(), claim.Confidence, claim.IsCritical,
                    claimLinks.GetValueOrDefault(claim.Id, [])
                        .Where(link => evidence.ContainsKey(link.ResearchEvidenceId))
                        .Select(link => (Link: link, Evidence: evidence[link.ResearchEvidenceId]))
                        .Where(item => sources.ContainsKey(item.Evidence.ResearchSourceId))
                        .Select(item => new ScriptContextEvidence(item.Evidence.Id, item.Link.Stance.ToString(),
                            item.Evidence.Fact, item.Evidence.SupportingExcerpt, item.Evidence.SourceLocator,
                            sources[item.Evidence.ResearchSourceId].Domain)).ToArray())).ToArray();
            var sectionConflicts = conflictsBySection.GetValueOrDefault(section.Id, [])
                .Where(link => conflicts.ContainsKey(link.ResearchConflictId))
                .Select(link => conflicts[link.ResearchConflictId])
                .Select(item => new ScriptContextConflict(item.Id, item.ResearchClaimId, item.Explanation, item.IsResolved))
                .ToArray();
            var sectionGaps = gapsBySection.GetValueOrDefault(section.Id, [])
                .Where(link => link.ResearchGapIndex >= 0 && link.ResearchGapIndex < researchPayload.Synthesis.Gaps.Count)
                .Select(link => (link.ResearchGapIndex, Gap: researchPayload.Synthesis.Gaps[link.ResearchGapIndex]))
                .Select(item => new ScriptContextGap(item.ResearchGapIndex, item.Gap.Description,
                    item.Gap.ClaimIds)).ToArray();
            return new ScriptContextSection(section.Id, section.Sequence, section.Heading, section.Purpose.ToString(),
                section.Objective, section.Summary, section.ViewerQuestion, section.TransitionIntent,
                section.EstimatedSeconds, WordsForSeconds(section.EstimatedSeconds ??
                    Math.Max(15, targetSeconds / outline.Sections.Count)), sectionClaims, sectionConflicts, sectionGaps);
        }).ToArray();

        return new ScriptGenerationContext(project.Id, videoProject.Id, outline.Outline.Id, outline.Outline.Version,
            research.Report.Id, research.Report.Version, CreateFingerprint(project, videoProject, outline.Outline,
                research.Report), videoProject.WorkingTitle, videoProject.Topic, videoProject.Angle,
            videoProject.ContentFormat, videoProject.TargetAudience, project.Audience.Description,
            videoProject.ViewerPromise, project.Market.TargetLanguage, project.Market.TargetGeography,
            outline.Outline.StructureType.ToString(), outline.Outline.CoreQuestion, outline.Outline.CoreTension,
            outline.Outline.OpeningHookConcept, outline.Outline.NarrativeProgression, outline.Outline.Payoff,
            outline.Outline.PacingStrategy, videoProject.ExperimentType.ToString(), videoProject.PilotHypothesis,
            videoProject.VariableBeingTested, source.PilotVideo.ControlStrategy,
            outline.Outline.HowOutlineImplementsExperiment, targetSeconds, targetWords,
            Math.Max(1, (int)Math.Floor(targetWords * options.MinimumLengthRatio)),
            Math.Max(1, (int)Math.Ceiling(targetWords * options.MaximumLengthRatio)), sections);
    }

    public static string CreateFingerprint(Project project, VideoProject videoProject, VideoOutline outline,
        ResearchReport research)
    {
        var material = JsonSerializer.Serialize(new
        {
            OutlineId = outline.Id,
            OutlineVersion = outline.Version,
            ResearchReportId = research.Id,
            ResearchReportVersion = research.Version,
            project.Market.TargetLanguage,
            videoProject.TargetAudience,
            project.Audience.Description,
            videoProject.ContentFormat,
            videoProject.ViewerPromise,
            outline.InputFingerprint,
        });
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(material)));
    }

    public static void ValidateOptions(ScriptOptions value)
    {
        if (value.MaxJobRetries is < 0 or > 5 || value.RunningJobLeaseSeconds is < 30 or > 3_600 ||
            value.MaxGenerationRetries is < 0 or > 3 || value.MaxLengthCorrectionAttempts is < 0 or > 3 ||
            value.MaxGroundingCorrectionAttempts is < 0 or > 3 ||
            value.MaxStructuredRepairAttempts is < 0 or > 1 || value.PlanningWordsPerMinute is < 80 or > 250 ||
            value.DefaultTargetMinutes is < 1 or > 60 || value.MinimumLengthRatio is < 0.25m or >= 1m ||
            value.MaximumLengthRatio is <= 1m or > 3m || value.MaxBlocksPerSection is < 1 or > 30 ||
            value.MaxNarrationCharactersPerBlock is < 100 or > 100_000)
            throw new ApplicationValidationException("Script configuration contains an invalid bounded limit.");
    }

    private int WordsForSeconds(int seconds) => Math.Max(1,
        (int)Math.Round(seconds * options.PlanningWordsPerMinute / 60d, MidpointRounding.AwayFromZero));
}
