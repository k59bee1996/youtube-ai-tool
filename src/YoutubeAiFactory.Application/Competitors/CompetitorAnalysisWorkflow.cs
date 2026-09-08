using System.Text.Json;
using Microsoft.Extensions.Logging;
using YoutubeAiFactory.Application.AI;
using YoutubeAiFactory.Application.Common;
using YoutubeAiFactory.Application.Persistence;
using YoutubeAiFactory.Domain.AI;
using YoutubeAiFactory.Domain.Competitors;
using YoutubeAiFactory.Domain.Jobs;

namespace YoutubeAiFactory.Application.Competitors;

public sealed record CompetitorAnalysisJobPayload(Guid ProjectId, Guid CompetitorId);

public sealed class RunCompetitorAnalysisHandler(
    IYoutubeAiFactoryStore store,
    CompetitorAnalysisOptions options,
    TimeProvider timeProvider)
{
    private static readonly JsonSerializerOptions PayloadSerializerOptions = new(JsonSerializerDefaults.Web);
    public async Task<RunCompetitorAnalysisResult> HandleAsync(Guid projectId, Guid competitorId, CancellationToken cancellationToken)
    {
        var competitor = await store.GetCompetitorAsync(projectId, competitorId, false, cancellationToken)
            ?? throw new ResourceNotFoundException($"Competitor '{competitorId}' was not found in project '{projectId}'.");
        if (competitor.Videos.Count == 0) throw new ApplicationValidationException("Collect at least one competitor video before running AI analysis.");
        if (options.MaxJobRetries is < 0 or > 10)
            throw new ApplicationValidationException("CompetitorAnalysis:MaxJobRetries must be between 0 and 10.");
        var payload = JsonSerializer.Serialize(new CompetitorAnalysisJobPayload(projectId, competitorId), PayloadSerializerOptions);
        var job = new Job("competitor-analysis", payload, timeProvider.GetUtcNow(), options.MaxJobRetries, competitorId);
        var persisted = await store.EnqueueCompetitorAnalysisJobAsync(job, cancellationToken);
        return new RunCompetitorAnalysisResult(persisted.Id, persisted.Status.ToString(), persisted.Id != job.Id);
    }
}

public sealed class GetCompetitorAnalysisStatusHandler(IYoutubeAiFactoryStore store)
{
    public async Task<CompetitorAnalysisStatusDto> HandleAsync(Guid projectId, Guid competitorId, CancellationToken cancellationToken)
    {
        var competitor = await store.GetCompetitorAsync(projectId, competitorId, false, cancellationToken)
            ?? throw new ResourceNotFoundException($"Competitor '{competitorId}' was not found in project '{projectId}'.");
        var latest = await store.GetLatestCompetitorAnalysisAsync(projectId, competitorId, cancellationToken);
        var active = await store.GetActiveCompetitorAnalysisJobAsync(projectId, competitorId, cancellationToken);
        var latestJob = active ?? await store.GetLatestCompetitorAnalysisJobAsync(projectId, competitorId, cancellationToken);
        return new CompetitorAnalysisStatusDto(latest is null ? null : ToDto(latest, competitor.LastCollectedAt!.Value), ToJob(active), ToJob(latestJob));
    }

    internal static CompetitorAnalysisDto ToDto(CompetitorAnalysis analysis, DateTimeOffset lastCollectedAt) => new(
        analysis.Id, analysis.Version, analysis.PromptKey, analysis.PromptVersion, analysis.Provider, analysis.Model,
        analysis.SourceDataAsOf, analysis.AnalyzedVideoCount, analysis.CreatedAt, lastCollectedAt > analysis.SourceDataAsOf,
        JsonSerializer.Deserialize<CompetitorAnalysisResult>(analysis.ResultJson, CompetitorAnalysisPrompt.SerializerOptions)
            ?? throw new InvalidOperationException("Stored competitor analysis is unreadable."));
    internal static AnalysisJobDto? ToJob(Job? job) => job is null ? null : new AnalysisJobDto(job.Id, job.Status.ToString(), job.FailureReason);
}

public sealed class CompetitorAnalysisJobProcessor(
    IYoutubeAiFactoryStore store,
    ILlmProvider provider,
    CompetitorAnalysisContextBuilder contextBuilder,
    CompetitorAnalysisOptions options,
    TimeProvider timeProvider,
    ILogger<CompetitorAnalysisJobProcessor> logger)
{
    private static readonly Action<ILogger, Guid, int, int, int, Exception?> LogCompleted =
        LoggerMessage.Define<Guid, int, int, int>(LogLevel.Information, new EventId(1, nameof(LogCompleted)), "Competitor analysis completed for {CompetitorId}; version {Version}, {Videos} videos, retries {Retries}.");
    private static readonly Action<ILogger, Guid, Exception?> LogFailed =
        LoggerMessage.Define<Guid>(LogLevel.Warning, new EventId(2, nameof(LogFailed)), "Competitor analysis job {JobId} failed.");
    public async Task<bool> ProcessNextAsync(CancellationToken cancellationToken)
    {
        if (options.RunningJobLeaseSeconds is < 30 or > 3_600)
            throw new ApplicationValidationException("CompetitorAnalysis:RunningJobLeaseSeconds must be between 30 and 3600.");
        var now = timeProvider.GetUtcNow();
        var job = await store.TryClaimNextCompetitorAnalysisJobAsync(
            now,
            now.AddSeconds(-options.RunningJobLeaseSeconds),
            cancellationToken);
        if (job is null) return false;
        var payload = JsonSerializer.Deserialize<CompetitorAnalysisJobPayload>(job.Payload, CompetitorAnalysisPrompt.SerializerOptions)
            ?? throw new InvalidOperationException("Competitor analysis job payload is invalid.");
        AiRun? run = null;
        try
        {
            var competitor = await store.GetCompetitorAsync(payload.ProjectId, payload.CompetitorId, false, cancellationToken)
                ?? throw new ResourceNotFoundException("The competitor for this analysis job no longer exists.");
            var context = contextBuilder.Build(competitor);
            run = new AiRun("CompetitorAnalysis", payload.ProjectId, payload.CompetitorId, "pending", "pending", CompetitorAnalysisPrompt.Key, CompetitorAnalysisPrompt.Version, timeProvider.GetUtcNow());
            store.AddAiRun(run);
            await store.SaveChangesAsync(cancellationToken);
            LlmResult<CompetitorAnalysisResult>? answer = null;
            Exception? finalFailure = null;
            for (var attempt = 0; attempt <= options.MaxStructuredOutputRetries; attempt++)
            {
                try
                {
                    answer = await provider.GenerateStructuredAsync<CompetitorAnalysisResult>(CompetitorAnalysisPrompt.Create(context, attempt > 0), cancellationToken);
                    CompetitorAnalysisValidator.Validate(answer.Value, context);
                    break;
                }
                catch (StructuredOutputException exception) when (attempt < options.MaxStructuredOutputRetries)
                {
                    run.RecordRetry();
                    finalFailure = exception;
                    await store.SaveChangesAsync(cancellationToken);
                }
                catch (Exception exception)
                {
                    answer = null;
                    finalFailure = exception;
                    break;
                }
            }
            if (answer is null) throw finalFailure ?? new StructuredOutputException("The provider did not return analysis output.");
            run.RecordProvider(answer.Provider, answer.Model);
            var version = await store.GetNextCompetitorAnalysisVersionAsync(competitor.Id, cancellationToken);
            var persisted = new CompetitorAnalysis(competitor.Id, version, run.Id, CompetitorAnalysisPrompt.Key, CompetitorAnalysisPrompt.Version,
                answer.Provider, answer.Model, context.SourceDataAsOf, context.AnalyzedVideoCount,
                JsonSerializer.Serialize(answer.Value, CompetitorAnalysisPrompt.SerializerOptions), timeProvider.GetUtcNow());
            store.AddCompetitorAnalysis(persisted);
            run.Complete(answer.InputTokens, answer.OutputTokens, null, timeProvider.GetUtcNow());
            job.Complete(timeProvider.GetUtcNow());
            await store.SaveChangesAsync(cancellationToken);
            LogCompleted(logger, competitor.Id, version, context.AnalyzedVideoCount, run.RetryCount, null);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            await store.RequeueCompetitorAnalysisJobAsync(job.Id, CancellationToken.None);
            throw;
        }
        catch (Exception exception)
        {
            var failedAt = timeProvider.GetUtcNow();
            var retryable = exception is ExternalServiceException
            {
                Failure: ExternalServiceFailure.QuotaExceeded or ExternalServiceFailure.Transient,
            };
            DateTimeOffset? retryAt = retryable ? failedAt.AddSeconds(Math.Pow(2, job.RetryCount + 1) * 5) : null;
            await store.FailCompetitorAnalysisJobAsync(
                job.Id,
                run?.Id,
                SafeFailure(exception),
                retryable,
                failedAt,
                retryAt,
                CancellationToken.None);
            LogFailed(logger, job.Id, exception);
        }
        return true;
    }

    private static string SafeFailure(Exception exception) => exception is YoutubeAiFactoryException ? exception.Message : "AI analysis could not be completed. Try again later.";
}
