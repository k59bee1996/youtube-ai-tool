using System.Text.Json;
using Microsoft.Extensions.Logging;
using YoutubeAiFactory.Application.AI;
using YoutubeAiFactory.Application.Common;
using YoutubeAiFactory.Application.Competitors;
using YoutubeAiFactory.Application.Persistence;
using YoutubeAiFactory.Domain.AI;
using YoutubeAiFactory.Domain.Competitors;
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

public sealed record ArtifactLocalizationJobPayload(Guid ProjectId, Guid AnalysisId, int ArtifactVersion, string Locale,
    Guid? OpportunityReportId = null, Guid? ResearchReportId = null, Guid? VideoOutlineId = null,
    Guid? ProductionPackageId = null);
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
        if (cached is not null)
        {
            if (LocalizedCompetitorAnalysisCache.IsValid(analysis, cached)) return new(null, "Completed", true);
            await store.DeleteArtifactLocalizationAsync(cached.Id, cancellationToken);
        }
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
        var content = cached is not null && LocalizedCompetitorAnalysisCache.IsValid(analysis, cached)
            ? JsonSerializer.Deserialize<LocalizedCompetitorAnalysisContent>(cached.ContentJson, RequestCompetitorAnalysisLocalizationHandler.JsonOptions)
            : null;
        return new(content, active is null ? null : new(active.Id, active.Status.ToString(), active.FailureReason), latest is null ? null : new(latest.Id, latest.Status.ToString(), latest.FailureReason));
    }
}

public sealed class ArtifactLocalizationJobProcessor(IYoutubeAiFactoryStore store, ILlmProvider provider, IAiModelResolver modelResolver, ArtifactLocalizationOptions options, TimeProvider timeProvider, ILogger<ArtifactLocalizationJobProcessor> logger)
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
            if (job.ArtifactType == LocalizableArtifactTypes.OpportunityReport)
            {
                await ProcessOpportunityReportAsync(job, payload, cancellationToken);
                return true;
            }
            if (job.ArtifactType == LocalizableArtifactTypes.ResearchReport)
            {
                await ProcessResearchReportAsync(job, payload, cancellationToken);
                return true;
            }
            if (job.ArtifactType == LocalizableArtifactTypes.VideoOutline)
            {
                await ProcessVideoOutlineAsync(job, payload, cancellationToken);
                return true;
            }
            if (job.ArtifactType == LocalizableArtifactTypes.ProductionPackage)
            {
                await ProcessProductionPackageAsync(job, payload, cancellationToken);
                return true;
            }
            if (job.ArtifactType != LocalizableArtifactTypes.CompetitorAnalysis)
                throw new ApplicationValidationException($"Artifact type '{job.ArtifactType}' is not supported for localization.");
            var analysis = await store.GetCompetitorAnalysisAsync(payload.ProjectId, payload.AnalysisId, cancellationToken)
                ?? throw new ResourceNotFoundException("The analysis for this localization job no longer exists.");
            if (analysis.Version != payload.ArtifactVersion) throw new ApplicationValidationException("The requested analysis version is no longer available.");
            var cached = await store.GetArtifactLocalizationAsync(LocalizableArtifactTypes.CompetitorAnalysis, analysis.Id, analysis.Version, payload.Locale, cancellationToken);
            if (cached is not null && LocalizedCompetitorAnalysisCache.IsValid(analysis, cached))
            {
                job.Complete(timeProvider.GetUtcNow());
                await store.SaveChangesAsync(cancellationToken);
                return true;
            }
            if (cached is not null)
            {
                await store.DeleteArtifactLocalizationAsync(cached.Id, cancellationToken);
            }
            var canonical = JsonSerializer.Deserialize<CompetitorAnalysisResult>(analysis.ResultJson, RequestCompetitorAnalysisHandlerJson)
                ?? throw new InvalidOperationException("Stored competitor analysis is unreadable.");
            var resolvedModel = modelResolver.Resolve(CompetitorAnalysisLocalizationPrompt.ModelProfile);
            run = new AiRun("ArtifactLocalization", payload.ProjectId, analysis.CompetitorChannelId, resolvedModel.Provider, resolvedModel.Model, CompetitorAnalysisLocalizationPrompt.Key, CompetitorAnalysisLocalizationPrompt.Version, timeProvider.GetUtcNow(), resolvedModel.Profile.ToString());
            store.AddAiRun(run);
            await store.SaveChangesAsync(cancellationToken);
            LlmResult<LocalizedCompetitorAnalysisContent>? answer = null;
            Exception? failure = null;
            for (var attempt = 0; attempt <= options.MaxStructuredOutputRetries; attempt++)
            {
                try
                {
                    answer = await provider.GenerateStructuredAsync<LocalizedCompetitorAnalysisContent>(CompetitorAnalysisLocalizationPrompt.Create(canonical, attempt > 0).WithResolvedModel(resolvedModel), cancellationToken);
                    LocalizedCompetitorAnalysisValidator.Validate(canonical, answer.Value);
                    break;
                }
                catch (StructuredOutputException exception)
                {
                    answer = null;
                    failure = exception;
                    if (attempt >= options.MaxStructuredOutputRetries) break;
                    run.RecordRetry();
                    await store.SaveChangesAsync(cancellationToken);
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

    private async Task ProcessOpportunityReportAsync(Job job, ArtifactLocalizationJobPayload payload, CancellationToken cancellationToken)
    {
        AiRun? run = null;
        try
        {
            if (payload.OpportunityReportId is not Guid reportId || reportId == Guid.Empty)
                throw new ApplicationValidationException("Opportunity localization job payload is invalid.");
            var report = await store.GetOpportunityReportAsync(payload.ProjectId, reportId, cancellationToken)
                ?? throw new ResourceNotFoundException("The opportunity report for this localization job no longer exists.");
            if (report.Report.Version != payload.ArtifactVersion)
                throw new ApplicationValidationException("The requested opportunity report version is no longer available.");
            var cached = await store.GetArtifactLocalizationAsync(LocalizableArtifactTypes.OpportunityReport, report.Report.Id, report.Report.Version, payload.Locale, cancellationToken);
            if (cached is not null && LocalizedOpportunityReportCache.IsValid(report, cached))
            {
                job.Complete(timeProvider.GetUtcNow());
                await store.SaveChangesAsync(cancellationToken);
                return;
            }
            if (cached is not null) await store.DeleteArtifactLocalizationAsync(cached.Id, cancellationToken);

            var resolvedModel = modelResolver.Resolve(OpportunityReportLocalizationPrompt.ModelProfile);
            run = new AiRun("ArtifactLocalization", payload.ProjectId, resolvedModel.Provider, resolvedModel.Model, OpportunityReportLocalizationPrompt.Key,
                OpportunityReportLocalizationPrompt.Version, timeProvider.GetUtcNow(), resolvedModel.Profile.ToString());
            store.AddAiRun(run);
            await store.SaveChangesAsync(cancellationToken);
            LlmResult<LocalizedOpportunityReportContent>? answer = null;
            Exception? failure = null;
            for (var attempt = 0; attempt <= options.MaxStructuredOutputRetries; attempt++)
            {
                try
                {
                    answer = await provider.GenerateStructuredAsync<LocalizedOpportunityReportContent>(OpportunityReportLocalizationPrompt.Create(report, attempt > 0).WithResolvedModel(resolvedModel), cancellationToken);
                    LocalizedOpportunityReportValidator.Validate(report, answer.Value);
                    break;
                }
                catch (StructuredOutputException exception)
                {
                    answer = null;
                    failure = exception;
                    if (attempt >= options.MaxStructuredOutputRetries) break;
                    run.RecordRetry();
                    await store.SaveChangesAsync(cancellationToken);
                }
                catch (Exception exception)
                {
                    failure = exception;
                    break;
                }
            }
            if (answer is null) throw failure ?? new StructuredOutputException("The provider did not return localized opportunity content.");
            run.RecordProvider(answer.Provider, answer.Model);
            store.AddArtifactLocalization(new ArtifactLocalization(LocalizableArtifactTypes.OpportunityReport, report.Report.Id, report.Report.Version, payload.Locale,
                JsonSerializer.Serialize(answer.Value, RequestCompetitorAnalysisLocalizationHandler.JsonOptions), run.Id, OpportunityReportLocalizationPrompt.Key,
                OpportunityReportLocalizationPrompt.Version, answer.Provider, answer.Model, timeProvider.GetUtcNow()));
            run.Complete(answer.InputTokens, answer.OutputTokens, null, timeProvider.GetUtcNow());
            job.Complete(timeProvider.GetUtcNow());
            await store.SaveChangesAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            var failedAt = timeProvider.GetUtcNow();
            var retryable = exception is ExternalServiceException { Failure: ExternalServiceFailure.QuotaExceeded or ExternalServiceFailure.Transient };
            await store.FailArtifactLocalizationJobAsync(job.Id, run?.Id,
                exception is YoutubeAiFactoryException ? exception.Message : "Opportunity translation could not be completed. Try again later.",
                retryable, failedAt, retryable ? failedAt.AddSeconds(Math.Pow(2, job.RetryCount + 1) * 5) : null, CancellationToken.None);
            LogFailed(logger, job.Id, exception);
        }
    }

    private async Task ProcessResearchReportAsync(Job job, ArtifactLocalizationJobPayload payload, CancellationToken cancellationToken)
    {
        AiRun? run = null;
        try
        {
            if (payload.ResearchReportId is not Guid reportId || reportId == Guid.Empty)
                throw new ApplicationValidationException("Research report localization job payload is invalid.");
            var report = await store.GetResearchReportAsync(payload.ProjectId, job.VideoProjectId ?? Guid.Empty, reportId, cancellationToken)
                ?? throw new ResourceNotFoundException("Research report for this localization job was not found.");
            if (report.Report.Version != payload.ArtifactVersion) throw new ApplicationValidationException("The requested research report version is no longer available.");
            var cached = await store.GetArtifactLocalizationAsync(LocalizableArtifactTypes.ResearchReport, report.Report.Id, report.Report.Version, payload.Locale, cancellationToken);
            if (cached is not null && LocalizedResearchReportCache.IsValid(report, cached))
            {
                job.Complete(timeProvider.GetUtcNow()); await store.SaveChangesAsync(cancellationToken); return;
            }
            if (cached is not null) await store.DeleteArtifactLocalizationAsync(cached.Id, cancellationToken);
            var resolvedModel = modelResolver.Resolve(ResearchReportLocalizationPrompt.ModelProfile);
            run = new AiRun("ArtifactLocalization", payload.ProjectId, resolvedModel.Provider, resolvedModel.Model,
                ResearchReportLocalizationPrompt.Key, ResearchReportLocalizationPrompt.Version, timeProvider.GetUtcNow(),
                resolvedModel.Profile.ToString(), report.Report.VideoProjectId, report.Report.ResearchRunId);
            store.AddAiRun(run); await store.SaveChangesAsync(cancellationToken);
            LlmResult<LocalizedResearchReportContent>? answer = null;
            Exception? failure = null;
            for (var attempt = 0; attempt <= options.MaxStructuredOutputRetries; attempt++)
            {
                try
                {
                    answer = await provider.GenerateStructuredAsync<LocalizedResearchReportContent>(ResearchReportLocalizationPrompt.Create(report, attempt > 0).WithResolvedModel(resolvedModel), cancellationToken);
                    LocalizedResearchReportValidator.Validate(report, answer.Value);
                    break;
                }
                catch (StructuredOutputException exception)
                {
                    failure = exception;
                    if (attempt == options.MaxStructuredOutputRetries) break;
                    run.RecordRetry(); await store.SaveChangesAsync(cancellationToken);
                }
                catch (Exception exception) { failure = exception; break; }
            }
            if (answer is null) throw failure ?? new StructuredOutputException("The provider did not return localized research content.");
            run.RecordProvider(answer.Provider, answer.Model);
            store.AddArtifactLocalization(new ArtifactLocalization(LocalizableArtifactTypes.ResearchReport, report.Report.Id, report.Report.Version,
                payload.Locale, JsonSerializer.Serialize(answer.Value, RequestCompetitorAnalysisLocalizationHandler.JsonOptions), run.Id,
                ResearchReportLocalizationPrompt.Key, ResearchReportLocalizationPrompt.Version, answer.Provider, answer.Model, timeProvider.GetUtcNow()));
            run.Complete(answer.InputTokens, answer.OutputTokens, null, timeProvider.GetUtcNow()); job.Complete(timeProvider.GetUtcNow());
            await store.SaveChangesAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception exception)
        {
            var failedAt = timeProvider.GetUtcNow();
            var retryable = exception is ExternalServiceException { Failure: ExternalServiceFailure.QuotaExceeded or ExternalServiceFailure.Transient };
            await store.FailArtifactLocalizationJobAsync(job.Id, run?.Id,
                exception is YoutubeAiFactoryException ? exception.Message : "Research report translation could not be completed. Try again later.",
                retryable, failedAt, retryable ? failedAt.AddSeconds(Math.Pow(2, job.RetryCount + 1) * 5) : null, CancellationToken.None);
            LogFailed(logger, job.Id, exception);
        }
    }

    private async Task ProcessVideoOutlineAsync(Job job, ArtifactLocalizationJobPayload payload,
        CancellationToken cancellationToken)
    {
        AiRun? run = null;
        try
        {
            if (payload.VideoOutlineId is not Guid outlineId || outlineId == Guid.Empty || job.VideoProjectId is not Guid videoProjectId)
                throw new ApplicationValidationException("Video outline localization job payload is invalid.");
            var outline = await store.GetVideoOutlineAsync(payload.ProjectId, videoProjectId, outlineId, false, cancellationToken)
                ?? throw new ResourceNotFoundException("Video outline for this localization job was not found.");
            if (outline.Outline.Version != payload.ArtifactVersion)
                throw new ApplicationValidationException("The requested video outline version is no longer available.");
            var cached = await store.GetArtifactLocalizationAsync(LocalizableArtifactTypes.VideoOutline, outlineId,
                outline.Outline.Version, payload.Locale, cancellationToken);
            if (cached is not null && LocalizedVideoOutlineCache.IsValid(outline, cached))
            {
                job.Complete(timeProvider.GetUtcNow());
                await store.SaveChangesAsync(cancellationToken);
                return;
            }
            if (cached is not null) await store.DeleteArtifactLocalizationAsync(cached.Id, cancellationToken);
            var resolved = modelResolver.Resolve(VideoOutlineLocalizationPrompt.ModelProfile);
            run = new AiRun("ArtifactLocalization", payload.ProjectId, resolved.Provider, resolved.Model,
                VideoOutlineLocalizationPrompt.Key, VideoOutlineLocalizationPrompt.Version, timeProvider.GetUtcNow(),
                resolved.Profile.ToString(), videoProjectId, researchReportId: outline.Outline.ResearchReportId);
            store.AddAiRun(run);
            await store.SaveChangesAsync(cancellationToken);
            LlmResult<LocalizedVideoOutlineContent>? answer = null;
            Exception? failure = null;
            for (var attempt = 0; attempt <= options.MaxStructuredOutputRetries; attempt++)
            {
                try
                {
                    answer = await provider.GenerateStructuredAsync<LocalizedVideoOutlineContent>(
                        VideoOutlineLocalizationPrompt.Create(outline, attempt > 0).WithResolvedModel(resolved), cancellationToken);
                    LocalizedVideoOutlineValidator.Validate(outline, answer.Value);
                    break;
                }
                catch (StructuredOutputException exception)
                {
                    answer = null;
                    failure = exception;
                    if (attempt >= options.MaxStructuredOutputRetries) break;
                    run.RecordRetry();
                    await store.SaveChangesAsync(cancellationToken);
                }
                catch (Exception exception) { failure = exception; break; }
            }
            if (answer is null)
                throw failure ?? new StructuredOutputException("The provider did not return localized outline content.");
            run.RecordProvider(answer.Provider, answer.Model);
            store.AddArtifactLocalization(new ArtifactLocalization(LocalizableArtifactTypes.VideoOutline, outlineId,
                outline.Outline.Version, payload.Locale,
                JsonSerializer.Serialize(answer.Value, RequestCompetitorAnalysisLocalizationHandler.JsonOptions), run.Id,
                VideoOutlineLocalizationPrompt.Key, VideoOutlineLocalizationPrompt.Version, answer.Provider, answer.Model,
                timeProvider.GetUtcNow()));
            run.Complete(answer.InputTokens, answer.OutputTokens, null, timeProvider.GetUtcNow());
            job.Complete(timeProvider.GetUtcNow());
            await store.SaveChangesAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception exception)
        {
            var failedAt = timeProvider.GetUtcNow();
            var retryable = exception is ExternalServiceException
            { Failure: ExternalServiceFailure.QuotaExceeded or ExternalServiceFailure.Transient };
            await store.FailArtifactLocalizationJobAsync(job.Id, run?.Id,
                exception is YoutubeAiFactoryException ? exception.Message : "Video outline translation could not be completed. Try again later.",
                retryable, failedAt, retryable ? failedAt.AddSeconds(Math.Pow(2, job.RetryCount + 1) * 5) : null,
                CancellationToken.None);
            LogFailed(logger, job.Id, exception);
        }
    }

    private async Task ProcessProductionPackageAsync(Job job, ArtifactLocalizationJobPayload payload,
        CancellationToken cancellationToken)
    {
        AiRun? run = null;
        try
        {
            if (payload.ProductionPackageId is not Guid packageId || packageId == Guid.Empty ||
                job.VideoProjectId is not Guid videoProjectId)
                throw new ApplicationValidationException("Production package localization job payload is invalid.");
            var package = await store.GetProductionPackageAsync(payload.ProjectId, videoProjectId, packageId, false,
                cancellationToken) ?? throw new ResourceNotFoundException(
                "Production package for this localization job was not found.");
            if (package.Package.Version != payload.ArtifactVersion)
                throw new ApplicationValidationException("The requested production package version is unavailable.");
            var cached = await store.GetArtifactLocalizationAsync(LocalizableArtifactTypes.ProductionPackage,
                packageId, package.Package.Version, payload.Locale, cancellationToken);
            if (cached is not null && LocalizedProductionPackageCache.IsValid(package, cached))
            {
                job.Complete(timeProvider.GetUtcNow());
                await store.SaveChangesAsync(cancellationToken);
                return;
            }
            if (cached is not null) await store.DeleteArtifactLocalizationAsync(cached.Id, cancellationToken);
            var resolved = modelResolver.Resolve(ProductionPackageLocalizationPrompt.ModelProfile);
            run = new AiRun("ArtifactLocalization", payload.ProjectId, resolved.Provider, resolved.Model,
                ProductionPackageLocalizationPrompt.Key, ProductionPackageLocalizationPrompt.Version,
                timeProvider.GetUtcNow(), resolved.Profile.ToString(), videoProjectId,
                researchReportId: package.Package.ResearchReportId);
            store.AddAiRun(run);
            await store.SaveChangesAsync(cancellationToken);
            LlmResult<LocalizedProductionPackageContent>? answer = null;
            Exception? failure = null;
            for (var attempt = 0; attempt <= options.MaxStructuredOutputRetries; attempt++)
            {
                try
                {
                    answer = await provider.GenerateStructuredAsync<LocalizedProductionPackageContent>(
                        ProductionPackageLocalizationPrompt.Create(package, attempt > 0).WithResolvedModel(resolved),
                        cancellationToken);
                    LocalizedProductionPackageValidator.Validate(package, answer.Value);
                    break;
                }
                catch (StructuredOutputException exception)
                {
                    answer = null;
                    failure = exception;
                    if (attempt >= options.MaxStructuredOutputRetries) break;
                    run.RecordRetry();
                    await store.SaveChangesAsync(cancellationToken);
                }
                catch (Exception exception)
                {
                    failure = exception;
                    break;
                }
            }
            if (answer is null)
                throw failure ?? new StructuredOutputException(
                    "The provider did not return localized production content.");
            run.RecordProvider(answer.Provider, answer.Model);
            store.AddArtifactLocalization(new ArtifactLocalization(LocalizableArtifactTypes.ProductionPackage,
                packageId, package.Package.Version, payload.Locale,
                JsonSerializer.Serialize(answer.Value, RequestCompetitorAnalysisLocalizationHandler.JsonOptions),
                run.Id, ProductionPackageLocalizationPrompt.Key, ProductionPackageLocalizationPrompt.Version,
                answer.Provider, answer.Model, timeProvider.GetUtcNow()));
            run.Complete(answer.InputTokens, answer.OutputTokens, null, timeProvider.GetUtcNow());
            job.Complete(timeProvider.GetUtcNow());
            await store.SaveChangesAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception exception)
        {
            var failedAt = timeProvider.GetUtcNow();
            var retryable = exception is ExternalServiceException
            { Failure: ExternalServiceFailure.QuotaExceeded or ExternalServiceFailure.Transient };
            await store.FailArtifactLocalizationJobAsync(job.Id, run?.Id,
                exception is YoutubeAiFactoryException ? exception.Message :
                    "Production package translation could not be completed. Try again later.",
                retryable, failedAt,
                retryable ? failedAt.AddSeconds(Math.Pow(2, job.RetryCount + 1) * 5) : null,
                CancellationToken.None);
            LogFailed(logger, job.Id, exception);
        }
    }

    private static readonly JsonSerializerOptions RequestCompetitorAnalysisHandlerJson = new(JsonSerializerDefaults.Web);
}

internal static class LocalizedCompetitorAnalysisCache
{
    public static bool IsValid(CompetitorAnalysis analysis, ArtifactLocalization localization)
    {
        try
        {
            var canonical = JsonSerializer.Deserialize<CompetitorAnalysisResult>(analysis.ResultJson, RequestCompetitorAnalysisLocalizationHandler.JsonOptions)
                ?? throw new InvalidOperationException("Stored competitor analysis is unreadable.");
            var localized = JsonSerializer.Deserialize<LocalizedCompetitorAnalysisContent>(localization.ContentJson, RequestCompetitorAnalysisLocalizationHandler.JsonOptions)
                ?? throw new StructuredOutputException("Stored localized analysis is empty.");
            LocalizedCompetitorAnalysisValidator.Validate(canonical, localized);
            return true;
        }
        catch (Exception exception) when (exception is JsonException or StructuredOutputException or InvalidOperationException)
        {
            return false;
        }
    }
}
