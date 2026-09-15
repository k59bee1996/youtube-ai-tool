using System.Text.Json;
using System.Text.Json.Nodes;
using YoutubeAiFactory.Application.AI;
using YoutubeAiFactory.Application.Common;
using YoutubeAiFactory.Application.Competitors;
using YoutubeAiFactory.Application.Persistence;
using YoutubeAiFactory.Application.Research;
using YoutubeAiFactory.Domain.Jobs;
using YoutubeAiFactory.Domain.Localization;

namespace YoutubeAiFactory.Application.Localization;

public sealed record LocalizedResearchFinding(string Summary, string Category);
public sealed record LocalizedResearchGap(string Description);
public sealed record LocalizedResearchReportContent(string ExecutiveSummary, IReadOnlyList<LocalizedResearchFinding> KeyFindings,
    IReadOnlyList<LocalizedResearchGap> Gaps, IReadOnlyList<string> Warnings, IReadOnlyList<string> Limitations);
public sealed record ResearchReportLocalizationStatusDto(LocalizedResearchReportContent? Content, AnalysisJobDto? ActiveJob, AnalysisJobDto? LatestJob);

public sealed class RequestResearchReportLocalizationHandler(IYoutubeAiFactoryStore store, ArtifactLocalizationOptions options, TimeProvider timeProvider)
{
    public async Task<RequestArtifactLocalizationResult> HandleAsync(Guid projectId, Guid videoProjectId, Guid reportId, string locale, CancellationToken cancellationToken)
    {
        var normalizedLocale = RequestCompetitorAnalysisLocalizationHandler.NormalizeLocale(locale);
        var report = await store.GetResearchReportAsync(projectId, videoProjectId, reportId, cancellationToken)
            ?? throw new ResourceNotFoundException("Research report was not found.");
        var cached = await store.GetArtifactLocalizationAsync(LocalizableArtifactTypes.ResearchReport, report.Report.Id, report.Report.Version, normalizedLocale, cancellationToken);
        if (cached is not null)
        {
            if (LocalizedResearchReportCache.IsValid(report, cached)) return new(null, "Completed", true);
            await store.DeleteArtifactLocalizationAsync(cached.Id, cancellationToken);
        }
        var payload = JsonSerializer.Serialize(new ArtifactLocalizationJobPayload(projectId, Guid.Empty, report.Report.Version, normalizedLocale,
            ResearchReportId: report.Report.Id), RequestCompetitorAnalysisLocalizationHandler.JsonOptions);
        var job = new Job("artifact-localization", payload, timeProvider.GetUtcNow(), options.MaxJobRetries, projectId: projectId,
            videoProjectId: videoProjectId, artifactType: LocalizableArtifactTypes.ResearchReport, artifactId: report.Report.Id,
            artifactVersion: report.Report.Version, locale: normalizedLocale);
        var persisted = await store.EnqueueArtifactLocalizationJobAsync(job, cancellationToken);
        return new(persisted.Id, persisted.Status.ToString(), persisted.Id != job.Id);
    }
}

public sealed class GetResearchReportLocalizationHandler(IYoutubeAiFactoryStore store)
{
    public async Task<ResearchReportLocalizationStatusDto> HandleAsync(Guid projectId, Guid videoProjectId, Guid reportId, string locale, CancellationToken cancellationToken)
    {
        var normalizedLocale = RequestCompetitorAnalysisLocalizationHandler.NormalizeLocale(locale);
        var report = await store.GetResearchReportAsync(projectId, videoProjectId, reportId, cancellationToken)
            ?? throw new ResourceNotFoundException("Research report was not found.");
        var cached = await store.GetArtifactLocalizationAsync(LocalizableArtifactTypes.ResearchReport, report.Report.Id, report.Report.Version, normalizedLocale, cancellationToken);
        var active = await store.GetActiveArtifactLocalizationJobAsync(LocalizableArtifactTypes.ResearchReport, report.Report.Id, report.Report.Version, normalizedLocale, cancellationToken);
        var latest = active ?? await store.GetLatestArtifactLocalizationJobAsync(LocalizableArtifactTypes.ResearchReport, report.Report.Id, report.Report.Version, normalizedLocale, cancellationToken);
        var content = cached is not null && LocalizedResearchReportCache.IsValid(report, cached)
            ? JsonSerializer.Deserialize<LocalizedResearchReportContent>(cached.ContentJson, RequestCompetitorAnalysisLocalizationHandler.JsonOptions) : null;
        return new(content, active is null ? null : new(active.Id, active.Status.ToString(), active.FailureReason),
            latest is null ? null : new(latest.Id, latest.Status.ToString(), latest.FailureReason));
    }
}

public static class ResearchReportLocalizationPrompt
{
    public const string Key = "research-report-localization";
    public const int Version = 1;
    public static AiModelProfile ModelProfile => AiWorkflowProfiles.ArtifactLocalization;
    public static LlmRequest Create(ResearchReportWithDetails report, bool correcting) => new(Key, Version,
        "Translate only reader-facing explanatory research content into Vietnamese. Keep the exact key-findings and gaps list order/count. Do not translate or alter IDs, claim statuses, source IDs, evidence IDs, URLs, source excerpts, dates/numbers, scores, metrics, model metadata, prompt metadata, or workflow states. Do not add facts or improve claims. Return JSON only.",
        $"Canonical dynamic research content:\n{JsonSerializer.Serialize(LocalizedResearchReportValidator.Source(report), RequestCompetitorAnalysisLocalizationHandler.JsonOptions)}" +
        (correcting ? "\nPreserve list topology exactly." : string.Empty), new Dictionary<string, string> { ["max_output_tokens"] = "5000" }, Schema(), ModelProfile);

    private static JsonNode Schema() => JsonNode.Parse("""
    {"type":"object","additionalProperties":false,"properties":{"executiveSummary":{"type":"string"},"keyFindings":{"type":"array","items":{"type":"object","additionalProperties":false,"properties":{"summary":{"type":"string"},"category":{"type":"string"}},"required":["summary","category"]}},"gaps":{"type":"array","items":{"type":"object","additionalProperties":false,"properties":{"description":{"type":"string"}},"required":["description"]}},"warnings":{"type":"array","items":{"type":"string"}},"limitations":{"type":"array","items":{"type":"string"}}},"required":["executiveSummary","keyFindings","gaps","warnings","limitations"]}
    """)!.DeepClone();
}

internal static class LocalizedResearchReportValidator
{
    public static void Validate(ResearchReportWithDetails canonical, LocalizedResearchReportContent localized)
    {
        var source = Source(canonical);
        if (string.IsNullOrWhiteSpace(localized.ExecutiveSummary) || localized.KeyFindings.Count != source.KeyFindings.Count ||
            localized.Gaps.Count != source.Gaps.Count || localized.Warnings.Count != source.Warnings.Count || localized.Limitations.Count != source.Limitations.Count ||
            localized.KeyFindings.Any(item => string.IsNullOrWhiteSpace(item.Summary) || string.IsNullOrWhiteSpace(item.Category)) ||
            localized.Gaps.Any(item => string.IsNullOrWhiteSpace(item.Description)) || localized.Warnings.Any(string.IsNullOrWhiteSpace) || localized.Limitations.Any(string.IsNullOrWhiteSpace))
            throw new StructuredOutputException("Localized research content does not preserve the canonical report topology.");
    }

    public static ResearchLocalizationSource Source(ResearchReportWithDetails report)
    {
        var payload = JsonSerializer.Deserialize<ResearchReportPayload>(report.Report.ResultJson, RequestCompetitorAnalysisLocalizationHandler.JsonOptions)
            ?? throw new InvalidOperationException("Stored research report is unreadable.");
        return new(payload.Synthesis.ExecutiveSummary, payload.Synthesis.KeyFindings.Select(item => new LocalizedResearchFinding(item.Summary, item.Category)).ToArray(),
            payload.Synthesis.Gaps.Select(item => new LocalizedResearchGap(item.Description)).ToArray(), payload.Synthesis.Warnings, payload.Synthesis.Limitations);
    }
}

internal sealed record ResearchLocalizationSource(string ExecutiveSummary, IReadOnlyList<LocalizedResearchFinding> KeyFindings,
    IReadOnlyList<LocalizedResearchGap> Gaps, IReadOnlyList<string> Warnings, IReadOnlyList<string> Limitations);

internal static class LocalizedResearchReportCache
{
    public static bool IsValid(ResearchReportWithDetails report, ArtifactLocalization localization)
    {
        try
        {
            var content = JsonSerializer.Deserialize<LocalizedResearchReportContent>(localization.ContentJson, RequestCompetitorAnalysisLocalizationHandler.JsonOptions)
                ?? throw new StructuredOutputException("Stored localized research report is empty.");
            LocalizedResearchReportValidator.Validate(report, content);
            return true;
        }
        catch (Exception exception) when (exception is JsonException or StructuredOutputException or InvalidOperationException) { return false; }
    }
}
