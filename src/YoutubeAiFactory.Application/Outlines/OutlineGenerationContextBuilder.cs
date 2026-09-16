using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using YoutubeAiFactory.Application.Common;
using YoutubeAiFactory.Application.Research;
using YoutubeAiFactory.Application.Videos;
using YoutubeAiFactory.Domain.Projects;
using YoutubeAiFactory.Domain.Research;
using YoutubeAiFactory.Domain.Videos;

namespace YoutubeAiFactory.Application.Outlines;

public sealed class OutlineGenerationContextBuilder(OutlineOptions options)
{
    public OutlineGenerationContext Build(Project project, VideoProject videoProject, VideoProjectSource source,
        ResearchReportWithDetails research)
    {
        ValidateOptions(options);
        if (project.Id != videoProject.ProjectId || research.Report.ProjectId != project.Id ||
            research.Report.VideoProjectId != videoProject.Id)
            throw new ApplicationValidationException("Outline inputs do not belong to the same project and VideoProject.");

        var payload = JsonSerializer.Deserialize<ResearchReportPayload>(research.Report.ResultJson, ResearchPrompts.SerializerOptions)
            ?? throw new ApplicationValidationException("The current ResearchReport is unreadable.");
        ValidateResearchOwnership(research, payload);
        var currentResearchFingerprint = ResearchBriefBuilder.CreateFingerprint(
            ResearchBriefBuilder.Build(project, videoProject, source.Opportunity.Name));
        if (!string.Equals(research.Report.InputFingerprint, currentResearchFingerprint, StringComparison.Ordinal))
            throw new ApplicationValidationException("The current research is based on outdated VideoProject context. Rerun research before generating a new outline.");
        if (payload.Confidence.UnsupportedCriticalClaimCount > 0)
            throw new ApplicationValidationException("The current research has unsupported critical claims. Resolve the central research gaps before generating an outline.");

        var selectedClaims = research.Claims
            .Where(item => item.SupportStatus != ResearchClaimSupportStatus.Unsupported)
            .OrderByDescending(item => item.IsCritical)
            .ThenBy(item => SupportPriority(item.SupportStatus))
            .ThenByDescending(item => item.Confidence)
            .Take(options.MaxClaimsForOutline)
            .ToArray();
        if (selectedClaims.Length == 0)
            throw new ApplicationValidationException("The current research contains no supported claims that can ground an outline.");

        var selectedClaimIds = selectedClaims.Select(item => item.Id).ToHashSet();
        var evidenceIds = selectedClaims
            .SelectMany(claim => research.ClaimEvidence.Where(item => item.ResearchClaimId == claim.Id)
                .OrderBy(item => item.ResearchEvidenceId))
            .Select(item => item.ResearchEvidenceId)
            .Distinct()
            .Take(options.MaxEvidenceExcerptsForOutline)
            .ToHashSet();
        var evidence = research.Evidence.Where(item => evidenceIds.Contains(item.Id)).ToArray();
        var evidenceById = evidence.ToDictionary(item => item.Id);
        var sourceById = research.Sources.ToDictionary(item => item.Id);
        var claimContexts = selectedClaims.Select(claim => new OutlineContextClaim(claim.Id, claim.Statement,
            claim.Type.ToString(), claim.SupportStatus.ToString(), claim.IsCritical, claim.Confidence,
            research.ClaimEvidence.Where(link => link.ResearchClaimId == claim.Id && evidenceById.ContainsKey(link.ResearchEvidenceId))
                .Select(link => link.ResearchEvidenceId).Distinct().ToArray())).ToArray();
        var evidenceContexts = evidence.Where(item => sourceById.ContainsKey(item.ResearchSourceId))
            .Select(item => new OutlineContextEvidence(item.Id, item.ResearchSourceId, item.Fact,
                item.SupportingExcerpt, item.SourceLocator, item.Type.ToString(), item.Confidence,
                sourceById[item.ResearchSourceId].Domain)).ToArray();
        var conflicts = research.Conflicts.Where(item => selectedClaimIds.Contains(item.ResearchClaimId))
            .Take(options.MaxConflictItemsForOutline)
            .Select(item => new OutlineContextConflict(item.Id, item.ResearchClaimId, item.Explanation, item.IsResolved))
            .ToArray();
        var gaps = payload.Synthesis.Gaps.Select((item, index) => new OutlineContextGap(index, item.Description, item.ClaimIds))
            .Take(options.MaxResearchGapsForOutline).ToArray();
        var findings = payload.Synthesis.KeyFindings.Take(options.MaxKeyFindingsForOutline)
            .Select(item => new OutlineContextFinding(item.Summary, item.Category, item.ClaimIds.Where(selectedClaimIds.Contains).ToArray()))
            .ToArray();

        var fingerprint = CreateFingerprint(project, videoProject, source, research.Report);
        return new OutlineGenerationContext(project.Id, videoProject.Id, research.Report.Id, research.Report.Version,
            research.Report.InputFingerprint, fingerprint, videoProject.WorkingTitle, videoProject.Topic,
            videoProject.Angle, videoProject.ContentFormat, videoProject.TargetAudience, videoProject.ViewerPromise,
            videoProject.HookConcept, project.Market.TargetLanguage, project.Market.TargetGeography,
            videoProject.ExperimentType, videoProject.PilotHypothesis, videoProject.VariableBeingTested,
            source.PilotVideo.ControlStrategy, videoProject.PrimaryMetric, videoProject.SuccessSignal,
            payload.Synthesis.ExecutiveSummary, findings, claimContexts, evidenceContexts, conflicts, gaps,
            payload.Synthesis.Warnings, payload.Synthesis.Limitations);
    }

    public static string CreateFingerprint(Project project, VideoProject videoProject, VideoProjectSource source,
        ResearchReport report)
    {
        var material = JsonSerializer.Serialize(new
        {
            videoProject.WorkingTitle,
            videoProject.Topic,
            videoProject.Angle,
            videoProject.ContentFormat,
            videoProject.TargetAudience,
            videoProject.ViewerPromise,
            videoProject.HookConcept,
            project.Market.TargetLanguage,
            project.Market.TargetGeography,
            ExperimentType = videoProject.ExperimentType.ToString(),
            videoProject.PilotHypothesis,
            videoProject.VariableBeingTested,
            source.PilotVideo.ControlStrategy,
            videoProject.PrimaryMetric,
            videoProject.SuccessSignal,
            ResearchReportId = report.Id,
            ResearchReportVersion = report.Version,
            report.InputFingerprint,
        });
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(material)));
    }

    public static void ValidateOptions(OutlineOptions value)
    {
        if (value.MaxJobRetries is < 0 or > 5 || value.RunningJobLeaseSeconds is < 30 or > 3_600 ||
            value.MaxGenerationRetries is < 0 or > 3 || value.MaxStructuredRepairAttempts is < 0 or > 1 ||
            value.MinOutlineSections is < 2 or > 20 || value.MaxOutlineSections < value.MinOutlineSections ||
            value.MaxOutlineSections > 30 || value.MaxClaimsForOutline is < 1 or > 80 ||
            value.MaxConflictItemsForOutline is < 0 or > 30 || value.MaxResearchGapsForOutline is < 0 or > 30 ||
            value.MaxEvidenceExcerptsForOutline is < 1 or > 100 || value.MaxKeyFindingsForOutline is < 0 or > 40)
            throw new ApplicationValidationException("Outline configuration contains an invalid bounded limit.");
    }

    private static int SupportPriority(ResearchClaimSupportStatus status) => status switch
    {
        ResearchClaimSupportStatus.Corroborated => 0,
        ResearchClaimSupportStatus.Conflicted => 1,
        ResearchClaimSupportStatus.Supported => 2,
        _ => 3,
    };

    private static void ValidateResearchOwnership(ResearchReportWithDetails research, ResearchReportPayload payload)
    {
        var reportId = research.Report.Id;
        var runId = research.Report.ResearchRunId;
        var claims = research.Claims.ToDictionary(item => item.Id);
        var evidence = research.Evidence.ToDictionary(item => item.Id);
        var sources = research.Sources.ToDictionary(item => item.Id);
        var invalid = research.Claims.Any(item => item.ResearchReportId != reportId) ||
            research.Sources.Any(item => item.ResearchRunId != runId) ||
            research.Evidence.Any(item => item.ResearchRunId != runId || !sources.ContainsKey(item.ResearchSourceId)) ||
            research.ClaimEvidence.Any(item => !claims.ContainsKey(item.ResearchClaimId) || !evidence.ContainsKey(item.ResearchEvidenceId)) ||
            research.Conflicts.Any(item => item.ResearchReportId != reportId || !claims.ContainsKey(item.ResearchClaimId) ||
                !evidence.ContainsKey(item.SupportingEvidenceId) || !evidence.ContainsKey(item.ContradictingEvidenceId)) ||
            payload.Synthesis.KeyFindings.SelectMany(item => item.ClaimIds).Any(id => !claims.ContainsKey(id)) ||
            payload.Synthesis.KeyFindings.SelectMany(item => item.EvidenceIds).Any(id => !evidence.ContainsKey(id)) ||
            payload.Synthesis.Gaps.SelectMany(item => item.ClaimIds).Any(id => !claims.ContainsKey(id));
        if (invalid)
            throw new ApplicationValidationException("The current ResearchReport contains invalid cross-report research relationships.");
    }
}
