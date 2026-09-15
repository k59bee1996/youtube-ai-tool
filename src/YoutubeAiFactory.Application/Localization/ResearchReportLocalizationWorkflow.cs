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

public sealed record LocalizedResearchFinding(int Index, string Summary, string Category);
public sealed record LocalizedResearchGap(int Index, string Description);
public sealed record LocalizedResearchText(int Index, string Text);
public sealed record LocalizedResearchReportContent(string ExecutiveSummary, IReadOnlyList<LocalizedResearchFinding> KeyFindings,
    IReadOnlyList<LocalizedResearchGap> Gaps, IReadOnlyList<LocalizedResearchText> Warnings, IReadOnlyList<LocalizedResearchText> Limitations);
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
        "Translate only reader-facing explanatory research content into Vietnamese. Keep every supplied index, list order, and list count exactly. Do not translate or alter IDs, claim statuses, source IDs, evidence IDs, URLs, source excerpts, dates/numbers, scores, metrics, model metadata, prompt metadata, or workflow states. Do not add facts or improve claims. Return JSON only.",
        $"Canonical dynamic research content:\n{JsonSerializer.Serialize(LocalizedResearchReportValidator.Source(report), RequestCompetitorAnalysisLocalizationHandler.JsonOptions)}" +
        (correcting ? "\nPreserve list topology exactly." : string.Empty), new Dictionary<string, string> { ["max_output_tokens"] = "5000" }, Schema(), ModelProfile);

    private static JsonNode Schema() => JsonNode.Parse("""
    {"type":"object","additionalProperties":false,"properties":{"executiveSummary":{"type":"string"},"keyFindings":{"type":"array","items":{"type":"object","additionalProperties":false,"properties":{"index":{"type":"integer","minimum":0},"summary":{"type":"string"},"category":{"type":"string"}},"required":["index","summary","category"]}},"gaps":{"type":"array","items":{"type":"object","additionalProperties":false,"properties":{"index":{"type":"integer","minimum":0},"description":{"type":"string"}},"required":["index","description"]}},"warnings":{"type":"array","items":{"type":"object","additionalProperties":false,"properties":{"index":{"type":"integer","minimum":0},"text":{"type":"string"}},"required":["index","text"]}},"limitations":{"type":"array","items":{"type":"object","additionalProperties":false,"properties":{"index":{"type":"integer","minimum":0},"text":{"type":"string"}},"required":["index","text"]}}},"required":["executiveSummary","keyFindings","gaps","warnings","limitations"]}
    """)!.DeepClone();
}

public static class LocalizedResearchReportValidator
{
    public static void Validate(ResearchReportWithDetails canonical, LocalizedResearchReportContent localized)
    {
        var source = Source(canonical);
        if (string.IsNullOrWhiteSpace(localized.ExecutiveSummary) || !PreservesIndexSequence(localized.KeyFindings.Select(item => item.Index), source.KeyFindings.Count) ||
            !PreservesIndexSequence(localized.Gaps.Select(item => item.Index), source.Gaps.Count) ||
            !PreservesIndexSequence(localized.Warnings.Select(item => item.Index), source.Warnings.Count) || !PreservesIndexSequence(localized.Limitations.Select(item => item.Index), source.Limitations.Count) ||
            localized.KeyFindings.Any(item => string.IsNullOrWhiteSpace(item.Summary) || string.IsNullOrWhiteSpace(item.Category)) ||
            localized.Gaps.Any(item => string.IsNullOrWhiteSpace(item.Description)) || localized.Warnings.Any(item => string.IsNullOrWhiteSpace(item.Text)) || localized.Limitations.Any(item => string.IsNullOrWhiteSpace(item.Text)))
            throw new StructuredOutputException("Localized research content does not preserve the canonical report topology.");
    }

    internal static ResearchLocalizationSource Source(ResearchReportWithDetails report)
    {
        var payload = JsonSerializer.Deserialize<ResearchReportPayload>(report.Report.ResultJson, RequestCompetitorAnalysisLocalizationHandler.JsonOptions)
            ?? throw new InvalidOperationException("Stored research report is unreadable.");
        return new(payload.Synthesis.ExecutiveSummary, payload.Synthesis.KeyFindings.Select((item, index) => new LocalizedResearchFinding(index, item.Summary, item.Category)).ToArray(),
            payload.Synthesis.Gaps.Select((item, index) => new LocalizedResearchGap(index, item.Description)).ToArray(),
            payload.Synthesis.Warnings.Select((item, index) => new LocalizedResearchText(index, item)).ToArray(),
            payload.Synthesis.Limitations.Select((item, index) => new LocalizedResearchText(index, item)).ToArray());
    }

    private static bool PreservesIndexSequence(IEnumerable<int> indexes, int expectedCount) => indexes.SequenceEqual(Enumerable.Range(0, expectedCount));
}

internal sealed record ResearchLocalizationSource(string ExecutiveSummary, IReadOnlyList<LocalizedResearchFinding> KeyFindings,
    IReadOnlyList<LocalizedResearchGap> Gaps, IReadOnlyList<LocalizedResearchText> Warnings, IReadOnlyList<LocalizedResearchText> Limitations);

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
