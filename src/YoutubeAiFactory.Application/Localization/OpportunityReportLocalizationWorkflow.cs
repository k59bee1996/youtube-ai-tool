using System.Text.Json;
using YoutubeAiFactory.Application.Common;
using YoutubeAiFactory.Application.Opportunities;
using YoutubeAiFactory.Application.Persistence;
using YoutubeAiFactory.Domain.Jobs;
using YoutubeAiFactory.Domain.Localization;

namespace YoutubeAiFactory.Application.Localization;

public sealed record OpportunityReportLocalizationStatusDto(
    LocalizedOpportunityReportContent? Content,
    OpportunityJobDto? ActiveJob,
    OpportunityJobDto? LatestJob);

public sealed class RequestOpportunityReportLocalizationHandler(
    IYoutubeAiFactoryStore store,
    ArtifactLocalizationOptions options,
    TimeProvider timeProvider)
{
    public async Task<RequestArtifactLocalizationResult> HandleAsync(
        Guid projectId,
        Guid reportId,
        string locale,
        CancellationToken cancellationToken)
    {
        var normalizedLocale = RequestCompetitorAnalysisLocalizationHandler.NormalizeLocale(locale);
        var report = await store.GetOpportunityReportAsync(projectId, reportId, cancellationToken)
            ?? throw new ResourceNotFoundException($"Opportunity report '{reportId}' was not found in project '{projectId}'.");
        var cached = await store.GetArtifactLocalizationAsync(
            LocalizableArtifactTypes.OpportunityReport,
            report.Report.Id,
            report.Report.Version,
            normalizedLocale,
            cancellationToken);
        if (cached is not null)
        {
            if (LocalizedOpportunityReportCache.IsValid(report, cached)) return new(null, "Completed", true);
            await store.DeleteArtifactLocalizationAsync(cached.Id, cancellationToken);
        }

        var payload = JsonSerializer.Serialize(
            new ArtifactLocalizationJobPayload(projectId, Guid.Empty, report.Report.Version, normalizedLocale, report.Report.Id),
            RequestCompetitorAnalysisLocalizationHandler.JsonOptions);
        var job = new Job(
            "artifact-localization",
            payload,
            timeProvider.GetUtcNow(),
            options.MaxJobRetries,
            projectId: projectId,
            artifactType: LocalizableArtifactTypes.OpportunityReport,
            artifactId: report.Report.Id,
            artifactVersion: report.Report.Version,
            locale: normalizedLocale);
        var persisted = await store.EnqueueArtifactLocalizationJobAsync(job, cancellationToken);
        return new(persisted.Id, persisted.Status.ToString(), persisted.Id != job.Id);
    }
}

public sealed class GetOpportunityReportLocalizationHandler(IYoutubeAiFactoryStore store)
{
    public async Task<OpportunityReportLocalizationStatusDto> HandleAsync(
        Guid projectId,
        Guid reportId,
        string locale,
        CancellationToken cancellationToken)
    {
        var normalizedLocale = RequestCompetitorAnalysisLocalizationHandler.NormalizeLocale(locale);
        var report = await store.GetOpportunityReportAsync(projectId, reportId, cancellationToken)
            ?? throw new ResourceNotFoundException($"Opportunity report '{reportId}' was not found in project '{projectId}'.");
        var cached = await store.GetArtifactLocalizationAsync(LocalizableArtifactTypes.OpportunityReport, report.Report.Id, report.Report.Version, normalizedLocale, cancellationToken);
        var active = await store.GetActiveArtifactLocalizationJobAsync(LocalizableArtifactTypes.OpportunityReport, report.Report.Id, report.Report.Version, normalizedLocale, cancellationToken);
        var latest = active ?? await store.GetLatestArtifactLocalizationJobAsync(LocalizableArtifactTypes.OpportunityReport, report.Report.Id, report.Report.Version, normalizedLocale, cancellationToken);
        var content = cached is not null && LocalizedOpportunityReportCache.IsValid(report, cached)
            ? JsonSerializer.Deserialize<LocalizedOpportunityReportContent>(cached.ContentJson, RequestCompetitorAnalysisLocalizationHandler.JsonOptions)
            : null;
        return new(
            content,
            active is null ? null : new OpportunityJobDto(active.Id, active.Status.ToString(), active.FailureReason),
            latest is null ? null : new OpportunityJobDto(latest.Id, latest.Status.ToString(), latest.FailureReason));
    }
}

internal static class LocalizedOpportunityReportCache
{
    public static bool IsValid(OpportunityReportWithDetails report, ArtifactLocalization localization)
    {
        try
        {
            var content = JsonSerializer.Deserialize<LocalizedOpportunityReportContent>(
                localization.ContentJson,
                RequestCompetitorAnalysisLocalizationHandler.JsonOptions)
                ?? throw new StructuredOutputException("Stored localized opportunity report is empty.");
            LocalizedOpportunityReportValidator.Validate(report, content);
            return true;
        }
        catch (Exception exception) when (exception is JsonException or StructuredOutputException or InvalidOperationException)
        {
            return false;
        }
    }
}
