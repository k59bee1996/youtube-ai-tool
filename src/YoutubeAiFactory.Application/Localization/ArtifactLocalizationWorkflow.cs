using System.Text.Json;
using Microsoft.Extensions.Logging;
using YoutubeAiFactory.Application.AI;
using YoutubeAiFactory.Application.Common;
using YoutubeAiFactory.Application.Competitors;
using YoutubeAiFactory.Application.Persistence;
using YoutubeAiFactory.Domain.AI;
using YoutubeAiFactory.Domain.Jobs;
using YoutubeAiFactory.Domain.Localization;

namespace YoutubeAiFactory.Application.Localization;

public sealed class ArtifactLocalizationOptions
{
    public const string SectionName = "ArtifactLocalization";
    public int MaxJobRetries { get; init; } = 2;
    public int MaxStructuredOutputRetries { get; init; } = 1;
    public int RunningJobLeaseSeconds { get; init; } = 300;
}

public sealed record ArtifactLocalizationJobPayload(Guid ProjectId, Guid AnalysisId, int ArtifactVersion, string Locale);
public sealed record ArtifactLocalizationStatusDto(LocalizedCompetitorAnalysisContent? Content, AnalysisJobDto? ActiveJob, AnalysisJobDto? LatestJob);
public sealed record RequestArtifactLocalizationResult(Guid? JobId, string Status, bool Existing);

public sealed class RequestCompetitorAnalysisLocalizationHandler(IYoutubeAiFactoryStore store, ArtifactLocalizationOptions options, TimeProvider timeProvider)
{
    public async Task<RequestArtifactLocalizationResult> HandleAsync(Guid projectId, Guid competitorId, Guid analysisId, string locale, CancellationToken cancellationToken)
    {
        var normalizedLocale = NormalizeLocale(locale);
        var analysis = await store.GetCompetitorAnalysisAsync(projectId, analysisId, cancellationToken)
            ?? throw new ResourceNotFoundException($"Analysis '{analysisId}' was not found in project '{projectId}'.");
        if (analysis.CompetitorChannelId != competitorId) throw new ResourceNotFoundException($"Analysis '{analysisId}' was not found for competitor '{competitorId}'.");
        var cached = await store.GetArtifactLocalizationAsync(LocalizableArtifactTypes.CompetitorAnalysis, analysis.Id, analysis.Version, normalizedLocale, cancellationToken);
        if (cached is not null) return new(null, "Completed", true);
        var payload = JsonSerializer.Serialize(new ArtifactLocalizationJobPayload(projectId, analysis.Id, analysis.Version, normalizedLocale), JsonOptions);
        var job = new Job("artifact-localization", payload, timeProvider.GetUtcNow(), options.MaxJobRetries, projectId: projectId,
            artifactType: LocalizableArtifactTypes.CompetitorAnalysis, artifactId: analysis.Id, artifactVersion: analysis.Version, locale: normalizedLocale);
        var persisted = await store.EnqueueArtifactLocalizationJobAsync(job, cancellationToken);
        return new(persisted.Id, persisted.Status.ToString(), persisted.Id != job.Id);
    }

    public static string NormalizeLocale(string locale) => locale.Trim().ToLowerInvariant() switch
    {
        "vi" => "vi",
        _ => throw new ApplicationValidationException("Only the Vietnamese analysis reading aid is available."),
    };

    internal static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
}

public sealed class GetCompetitorAnalysisLocalizationHandler(IYoutubeAiFactoryStore store)
{
    public async Task<ArtifactLocalizationStatusDto> HandleAsync(Guid projectId, Guid competitorId, Guid analysisId, string locale, CancellationToken cancellationToken)
    {
        var normalizedLocale = RequestCompetitorAnalysisLocalizationHandler.NormalizeLocale(locale);
        var analysis = await store.GetCompetitorAnalysisAsync(projectId, analysisId, cancellationToken)
            ?? throw new ResourceNotFoundException($"Analysis '{analysisId}' was not found in project '{projectId}'.");
        if (analysis.CompetitorChannelId != competitorId) throw new ResourceNotFoundException($"Analysis '{analysisId}' was not found for competitor '{competitorId}'.");
        var cached = await store.GetArtifactLocalizationAsync(LocalizableArtifactTypes.CompetitorAnalysis, analysis.Id, analysis.Version, normalizedLocale, cancellationToken);
        var active = await store.GetActiveArtifactLocalizationJobAsync(LocalizableArtifactTypes.CompetitorAnalysis, analysis.Id, analysis.Version, normalizedLocale, cancellationToken);
        var latest = active ?? await store.GetLatestArtifactLocalizationJobAsync(LocalizableArtifactTypes.CompetitorAnalysis, analysis.Id, analysis.Version, normalizedLocale, cancellationToken);
        var content = cached is null ? null : JsonSerializer.Deserialize<LocalizedCompetitorAnalysisContent>(cached.ContentJson, RequestCompetitorAnalysisLocalizationHandler.JsonOptions)
            ?? throw new InvalidOperationException("Stored localized analysis is unreadable.");
        return new(content, active is null ? null : new(active.Id, active.Status.ToString(), active.FailureReason), latest is null ? null : new(latest.Id, latest.Status.ToString(), latest.FailureReason));
    }
}

public sealed class ArtifactLocalizationJobProcessor(IYoutubeAiFactoryStore store, ILlmProvider provider, ArtifactLocalizationOptions options, TimeProvider timeProvider, ILogger<ArtifactLocalizationJobProcessor> logger)
{
    private static readonly Action<ILogger, Guid, Exception?> LogFailed = LoggerMessage.Define<Guid>(LogLevel.Warning, new EventId(1, nameof(LogFailed)), "Artifact localization job {JobId} failed.");
    public async Task<bool> ProcessNextAsync(CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var job = await store.TryClaimNextArtifactLocalizationJobAsync(now, now.AddSeconds(-options.RunningJobLeaseSeconds), cancellationToken);
        if (job is null) return false;
        AiRun? run = null;
        try
        {
            var payload = JsonSerializer.Deserialize<ArtifactLocalizationJobPayload>(job.Payload, RequestCompetitorAnalysisLocalizationHandler.JsonOptions)
                ?? throw new InvalidOperationException("Artifact localization job payload is invalid.");
            var analysis = await store.GetCompetitorAnalysisAsync(payload.ProjectId, payload.AnalysisId, cancellationToken)
                ?? throw new ResourceNotFoundException("The analysis for this localization job no longer exists.");
            if (analysis.Version != payload.ArtifactVersion) throw new ApplicationValidationException("The requested analysis version is no longer available.");
            if (await store.GetArtifactLocalizationAsync(LocalizableArtifactTypes.CompetitorAnalysis, analysis.Id, analysis.Version, payload.Locale, cancellationToken) is not null)
            {
                job.Complete(timeProvider.GetUtcNow());
                await store.SaveChangesAsync(cancellationToken);
                return true;
            }
            var canonical = JsonSerializer.Deserialize<CompetitorAnalysisResult>(analysis.ResultJson, RequestCompetitorAnalysisHandlerJson)
                ?? throw new InvalidOperationException("Stored competitor analysis is unreadable.");
            run = new AiRun("ArtifactLocalization", payload.ProjectId, analysis.CompetitorChannelId, "pending", "pending", CompetitorAnalysisLocalizationPrompt.Key, CompetitorAnalysisLocalizationPrompt.Version, timeProvider.GetUtcNow());
            store.AddAiRun(run);
            await store.SaveChangesAsync(cancellationToken);
            LlmResult<LocalizedCompetitorAnalysisContent>? answer = null;
            Exception? failure = null;
            for (var attempt = 0; attempt <= options.MaxStructuredOutputRetries; attempt++)
            {
                try
                {
                    answer = await provider.GenerateStructuredAsync<LocalizedCompetitorAnalysisContent>(CompetitorAnalysisLocalizationPrompt.Create(canonical, attempt > 0), cancellationToken);
                    LocalizedCompetitorAnalysisValidator.Validate(canonical, answer.Value);
                    break;
                }
                catch (StructuredOutputException exception) when (attempt < options.MaxStructuredOutputRetries)
                {
                    run.RecordRetry(); failure = exception; await store.SaveChangesAsync(cancellationToken);
                }
                catch (Exception exception) { failure = exception; break; }
            }
            if (answer is null) throw failure ?? new StructuredOutputException("The provider did not return localized analysis content.");
            run.RecordProvider(answer.Provider, answer.Model);
            store.AddArtifactLocalization(new ArtifactLocalization(LocalizableArtifactTypes.CompetitorAnalysis, analysis.Id, analysis.Version, payload.Locale,
                JsonSerializer.Serialize(answer.Value, RequestCompetitorAnalysisLocalizationHandler.JsonOptions), run.Id, CompetitorAnalysisLocalizationPrompt.Key,
                CompetitorAnalysisLocalizationPrompt.Version, answer.Provider, answer.Model, timeProvider.GetUtcNow()));
            run.Complete(answer.InputTokens, answer.OutputTokens, null, timeProvider.GetUtcNow());
            job.Complete(timeProvider.GetUtcNow());
            await store.SaveChangesAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            await store.RequeueArtifactLocalizationJobAsync(job.Id, CancellationToken.None); throw;
        }
        catch (Exception exception)
        {
            var failedAt = timeProvider.GetUtcNow();
            var retryable = exception is ExternalServiceException { Failure: ExternalServiceFailure.QuotaExceeded or ExternalServiceFailure.Transient };
            await store.FailArtifactLocalizationJobAsync(job.Id, run?.Id, exception is YoutubeAiFactoryException ? exception.Message : "Analysis translation could not be completed. Try again later.", retryable, failedAt, retryable ? failedAt.AddSeconds(Math.Pow(2, job.RetryCount + 1) * 5) : null, CancellationToken.None);
            LogFailed(logger, job.Id, exception);
        }
        return true;
    }

    private static readonly JsonSerializerOptions RequestCompetitorAnalysisHandlerJson = new(JsonSerializerDefaults.Web);
}
