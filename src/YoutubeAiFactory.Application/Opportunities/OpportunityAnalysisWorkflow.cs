using System.Text.Json;
using Microsoft.Extensions.Logging;
using YoutubeAiFactory.Application.AI;
using YoutubeAiFactory.Application.Common;
using YoutubeAiFactory.Application.Persistence;
using YoutubeAiFactory.Domain.AI;
using YoutubeAiFactory.Domain.Jobs;
using YoutubeAiFactory.Domain.Opportunities;

namespace YoutubeAiFactory.Application.Opportunities;

public sealed class RunOpportunityAnalysisHandler(IYoutubeAiFactoryStore store, OpportunityAnalysisOptions options, TimeProvider timeProvider)
{
    public async Task<RunOpportunityAnalysisResult> HandleAsync(Guid projectId, CancellationToken cancellationToken)
    {
        _ = await store.GetProjectAsync(projectId, cancellationToken) ?? throw new ResourceNotFoundException($"Project '{projectId}' was not found.");
        if ((await store.GetCurrentCompetitorAnalysesForProjectAsync(projectId, cancellationToken)).Count == 0)
            throw new ApplicationValidationException("Analyze at least one competitor before generating opportunities.");
        var job = new Job("opportunity-analysis", JsonSerializer.Serialize(new OpportunityAnalysisJobPayload(projectId), OpportunityAnalysisPrompt.SerializerOptions), timeProvider.GetUtcNow(), options.MaxJobRetries, projectId: projectId);
        var persisted = await store.EnqueueOpportunityAnalysisJobAsync(job, cancellationToken);
        return new RunOpportunityAnalysisResult(persisted.Id, persisted.Status.ToString(), persisted.Id != job.Id);
    }
}

public sealed class GetOpportunityStatusHandler(IYoutubeAiFactoryStore store)
{
    public async Task<OpportunityStatusDto> HandleAsync(Guid projectId, CancellationToken cancellationToken)
    {
        _ = await store.GetProjectAsync(projectId, cancellationToken) ?? throw new ResourceNotFoundException($"Project '{projectId}' was not found.");
        var report = await store.GetLatestOpportunityReportAsync(projectId, cancellationToken);
        var active = await store.GetActiveOpportunityAnalysisJobAsync(projectId, cancellationToken);
        var latestJob = active ?? await store.GetLatestOpportunityAnalysisJobAsync(projectId, cancellationToken);
        var competitors = await store.ListCompetitorsAsync(projectId, cancellationToken);
        var analyses = await store.GetCurrentCompetitorAnalysesForProjectAsync(projectId, cancellationToken);
        return new OpportunityStatusDto(report is null ? null : ToDto(report, analyses), ToJob(active), ToJob(latestJob), competitors.Count, analyses.Count);
    }
    internal static OpportunityReportDto ToDto(OpportunityReportWithDetails report, IReadOnlyList<CurrentCompetitorAnalysis> current)
    {
        var stale = report.Sources.Any(source => current.FirstOrDefault(item => item.CompetitorId == source.CompetitorChannelId)?.Analysis.Version > source.CompetitorAnalysisVersion);
        return new OpportunityReportDto(report.Report.Id, report.Report.Version, report.Report.PromptKey, report.Report.PromptVersion, report.Report.Provider,
            report.Report.Model, report.Report.ScoringAlgorithmVersion, report.Report.CreatedAt, stale,
            report.Sources.Select(source => new OpportunityReportSourceDto(source.CompetitorChannelId, source.CompetitorAnalysisId, source.CompetitorAnalysisVersion)).ToArray(),
            Deserialize(report.Report.LimitationsJson), report.Candidates.Select(candidate => new OpportunityCandidateDto(candidate.Candidate.Id, candidate.Candidate.Name, candidate.Candidate.Description,
                candidate.Candidate.Audience, candidate.Candidate.Topic, candidate.Candidate.ContentFormat, candidate.Candidate.Angle, candidate.Candidate.WhyThisOpportunity,
                new OpportunityScoreBreakdown(candidate.Candidate.ObservedDemandSignal, candidate.Candidate.NoveltySignal, candidate.Candidate.CompetitionRiskSignal,
                    candidate.Candidate.AudienceFitSignal, candidate.Candidate.TransferabilitySignal, candidate.Candidate.EvidenceStrength, candidate.Candidate.StoryPotential,
                    candidate.Candidate.ProductionComplexity, candidate.Candidate.OverallScore), candidate.Candidate.Confidence,
                Deserialize(candidate.Candidate.RisksJson), Deserialize(candidate.Candidate.LimitationsJson), candidate.Candidate.DecisionStatus.ToString(),
                candidate.Evidence.Select(e => new OpportunityEvidenceDto(e.Id, e.CompetitorChannelId, e.CompetitorAnalysisId, e.CompetitorVideoId, e.EvidenceId, e.Summary)).ToArray())).OrderByDescending(item => item.Scores.OverallScore).ToArray());
    }
    internal static OpportunityJobDto? ToJob(Job? job) => job is null ? null : new OpportunityJobDto(job.Id, job.Status.ToString(), job.FailureReason);
    private static string[] Deserialize(string json) => JsonSerializer.Deserialize<string[]>(json, OpportunityAnalysisPrompt.SerializerOptions) ?? [];
}

public sealed class OpportunityAnalysisJobProcessor(IYoutubeAiFactoryStore store, ILlmProvider provider, OpportunityAnalysisContextBuilder contextBuilder,
    OpportunityAnalysisOptions options, TimeProvider timeProvider, ILogger<OpportunityAnalysisJobProcessor> logger)
{
    private static readonly Action<ILogger, Guid, Exception?> LogFailed = LoggerMessage.Define<Guid>(LogLevel.Warning, new EventId(2, nameof(LogFailed)), "Opportunity analysis job {JobId} failed.");
    private static readonly Action<ILogger, Guid, Guid, Exception?> LogCompleted = LoggerMessage.Define<Guid, Guid>(LogLevel.Information, new EventId(1, nameof(LogCompleted)), "Opportunity analysis completed for project {ProjectId}; report {ReportId}.");
    public async Task<bool> ProcessNextAsync(CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var job = await store.TryClaimNextOpportunityAnalysisJobAsync(now, now.AddSeconds(-options.RunningJobLeaseSeconds), cancellationToken);
        if (job is null) return false;
        AiRun? run = null;
        try
        {
            var payload = JsonSerializer.Deserialize<OpportunityAnalysisJobPayload>(job.Payload, OpportunityAnalysisPrompt.SerializerOptions) ?? throw new ApplicationValidationException("Opportunity job payload is invalid.");
            var project = await store.GetProjectAsync(payload.ProjectId, cancellationToken) ?? throw new ResourceNotFoundException("The project for this opportunity job no longer exists.");
            var analyses = await store.GetCurrentCompetitorAnalysesForProjectAsync(project.Id, cancellationToken);
            var context = contextBuilder.Build(project, analyses);
            run = new AiRun("OpportunityAnalysis", project.Id, "pending", "pending", OpportunityAnalysisPrompt.Key, OpportunityAnalysisPrompt.Version, now);
            store.AddAiRun(run); await store.SaveChangesAsync(cancellationToken);
            LlmResult<OpportunityAnalysisResult>? answer = null; Exception? failure = null;
            for (var attempt = 0; attempt <= options.MaxStructuredOutputRetries; attempt++)
            {
                try { answer = await provider.GenerateStructuredAsync<OpportunityAnalysisResult>(OpportunityAnalysisPrompt.Create(context, attempt > 0), cancellationToken); OpportunityAnalysisValidator.Validate(answer.Value, context, options.MaxCandidates); break; }
                catch (StructuredOutputException ex) when (attempt < options.MaxStructuredOutputRetries) { run.RecordRetry(); failure = ex; await store.SaveChangesAsync(cancellationToken); }
                catch (Exception ex) { failure = ex; answer = null; break; }
            }
            if (answer is null) throw failure ?? new StructuredOutputException("The provider did not return opportunity output.");
            run.RecordProvider(answer.Provider, answer.Model);
            var reportLimitations = answer.Value.Limitations.Concat(context.Limitations).Where(item => !string.IsNullOrWhiteSpace(item)).Distinct(StringComparer.Ordinal).ToArray();
            var report = new OpportunityReport(project.Id, await store.GetNextOpportunityReportVersionAsync(project.Id, cancellationToken), run.Id,
                OpportunityAnalysisPrompt.Key, OpportunityAnalysisPrompt.Version, answer.Provider, answer.Model, OpportunityScoringEngine.AlgorithmVersion,
                context.Sources.Count, JsonSerializer.Serialize(reportLimitations, OpportunityAnalysisPrompt.SerializerOptions), timeProvider.GetUtcNow());
            store.AddOpportunityReport(report);
            foreach (var source in context.Sources) store.AddOpportunityReportSource(new OpportunityReportSource(report.Id, source.CompetitorId, source.CompetitorAnalysisId, source.CompetitorAnalysisVersion));
            foreach (var result in answer.Value.Opportunities)
            {
                var score = OpportunityScoringEngine.Calculate(result, context);
                var candidate = new OpportunityCandidate(report.Id, result.Name, result.Description, result.Audience, result.Topic, result.ContentFormat, result.Angle,
                    result.WhyThisOpportunity, score.ObservedDemandSignal, score.NoveltySignal, score.CompetitionRiskSignal, score.AudienceFitSignal, score.TransferabilitySignal,
                    score.EvidenceStrength, score.StoryPotential, score.ProductionComplexity, result.Confidence, score.OverallScore,
                    JsonSerializer.Serialize(result.Risks, OpportunityAnalysisPrompt.SerializerOptions), JsonSerializer.Serialize(result.Limitations, OpportunityAnalysisPrompt.SerializerOptions), timeProvider.GetUtcNow());
                store.AddOpportunityCandidate(candidate);
                foreach (var evidenceId in result.EvidenceIds.Distinct(StringComparer.Ordinal))
                {
                    var evidence = context.Evidence.Single(item => item.Id == evidenceId);
                    store.AddOpportunityEvidence(new OpportunityEvidence(candidate.Id, evidence.CompetitorId, evidence.CompetitorAnalysisId, evidence.VideoId, evidence.Id, evidence.Summary));
                }
            }
            run.Complete(answer.InputTokens, answer.OutputTokens, null, timeProvider.GetUtcNow()); job.Complete(timeProvider.GetUtcNow());
            await store.SaveChangesAsync(cancellationToken);
            LogCompleted(logger, project.Id, report.Id, null);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { await store.RequeueOpportunityAnalysisJobAsync(job.Id, CancellationToken.None); throw; }
        catch (Exception ex)
        {
            var failedAt = timeProvider.GetUtcNow(); var retryable = ex is ExternalServiceException { Failure: ExternalServiceFailure.QuotaExceeded or ExternalServiceFailure.Transient };
            await store.FailOpportunityAnalysisJobAsync(job.Id, run?.Id, ex is YoutubeAiFactoryException ? ex.Message : "Opportunity analysis could not be completed. Try again later.", retryable, failedAt, retryable ? failedAt.AddSeconds(Math.Pow(2, job.RetryCount + 1) * 5) : null, CancellationToken.None);
            LogFailed(logger, job.Id, ex);
        }
        return true;
    }
}
