using System.Text.Json;
using Microsoft.Extensions.Logging;
using YoutubeAiFactory.Application.AI;
using YoutubeAiFactory.Application.Common;
using YoutubeAiFactory.Application.Persistence;
using YoutubeAiFactory.Domain.AI;
using YoutubeAiFactory.Domain.Ideas;
using YoutubeAiFactory.Domain.Jobs;

namespace YoutubeAiFactory.Application.Ideas;

public sealed class RunIdeaGenerationHandler(IYoutubeAiFactoryStore store, IdeaGenerationOptions options, TimeProvider timeProvider)
{
    public async Task<RunIdeaGenerationResult> HandleAsync(Guid projectId, Guid opportunityId, CancellationToken cancellationToken)
    {
        _ = await store.GetProjectAsync(projectId, cancellationToken) ?? throw new ResourceNotFoundException($"Project '{projectId}' was not found.");
        var opportunity = await store.GetOpportunityWithEvidenceAsync(projectId, opportunityId, false, cancellationToken) ?? throw new ResourceNotFoundException("Opportunity was not found.");
        if (opportunity.Candidate.DecisionStatus != Domain.Opportunities.OpportunityDecisionStatus.Approved) throw new ApplicationValidationException("Approve this opportunity before generating ideas.");
        var job = new Job("idea-generation", JsonSerializer.Serialize(new IdeaGenerationJobPayload(projectId, opportunityId), IdeaGenerationPrompt.SerializerOptions), timeProvider.GetUtcNow(), options.MaxJobRetries, projectId: projectId, opportunityId: opportunityId);
        var persisted = await store.EnqueueIdeaGenerationJobAsync(job, cancellationToken);
        return new(persisted.Id, persisted.Status.ToString(), persisted.Id != job.Id);
    }
}

public sealed class GetIdeaBankHandler(IYoutubeAiFactoryStore store)
{
    public async Task<IdeaBankDto> HandleAsync(Guid projectId, Guid opportunityId, CancellationToken cancellationToken)
    {
        _ = await store.GetOpportunityWithEvidenceAsync(projectId, opportunityId, false, cancellationToken) ?? throw new ResourceNotFoundException("Opportunity was not found.");
        var generations = await store.ListIdeaGenerationsAsync(projectId, opportunityId, cancellationToken);
        var allIdeas = await store.ListVideoIdeasAsync(projectId, opportunityId, cancellationToken);
        var latest = generations.Count == 0 ? null : generations[0]; var currentReport = await store.GetLatestOpportunityReportAsync(projectId, cancellationToken);
        var mapped = allIdeas.OrderByDescending(x => x.Idea.OverallScore).Select(ToDto).ToArray();
        return new(mapped, generations.Select(g => new IdeaGenerationSummaryDto(g.Id, g.Version, g.OpportunityReportVersion, g.CreatedAt, allIdeas.Count(i => i.Idea.GenerationId == g.Id))).ToArray(), latest is null ? null : new IdeaGenerationDto(latest.Id, latest.Version, latest.OpportunityId, latest.OpportunityReportVersion, latest.PromptKey, latest.PromptVersion, latest.Provider, latest.Model, latest.ScoringAlgorithmVersion, latest.CreatedAt, currentReport is not null && currentReport.Report.Version > latest.OpportunityReportVersion, mapped.Where(x => x.GenerationId == latest.Id).ToArray()), (await store.GetActiveIdeaGenerationJobAsync(projectId, opportunityId, cancellationToken))?.Status.ToString(), (await store.GetLatestIdeaGenerationJobAsync(projectId, opportunityId, cancellationToken))?.FailureReason);
    }
    internal static VideoIdeaDto ToDto(VideoIdeaWithEvidence source) { var x = source.Idea; return new(x.Id, x.OpportunityId, x.GenerationId, x.WorkingTitle, x.Topic, x.Angle, x.ContentFormat, x.TargetAudience, x.ViewerIntent, x.HookConcept, x.ThumbnailConcept, x.ViewerPromise, x.CoreQuestion, x.WhyViewerWouldCare, x.Hypothesis, new(x.OpportunityFit, x.ObservedDemandAlignment, x.Novelty, x.TitlePotential, x.ThumbnailPotential, x.StoryPotential, x.AudienceFit, x.EvidenceStrength, x.ProductionEase, x.CompetitionRisk, x.ResearchRisk, x.DuplicationPenalty, x.OverallScore), source.Evidence.Select(e => new IdeaEvidenceDto(e.Id, e.OpportunityEvidenceId, e.Summary)).ToArray(), JsonSerializer.Deserialize<string[]>(x.RisksJson, IdeaGenerationPrompt.SerializerOptions) ?? [], x.Confidence, x.DecisionStatus.ToString(), x.CreatedAt); }
}

public sealed class SetIdeaDecisionHandler(IYoutubeAiFactoryStore store)
{
    public async Task<VideoIdeaDto> HandleAsync(Guid projectId, Guid ideaId, IdeaDecisionStatus status, CancellationToken cancellationToken)
    {
        var idea = await store.GetVideoIdeaAsync(projectId, ideaId, true, cancellationToken) ?? throw new ResourceNotFoundException("Idea was not found.");
        idea.Idea.SetDecision(status); await store.SaveChangesAsync(cancellationToken); return GetIdeaBankHandler.ToDto(idea);
    }
}

public sealed class GetIdeaGenerationHandler(IYoutubeAiFactoryStore store)
{
    public async Task<IdeaGenerationDto> HandleAsync(Guid projectId, Guid generationId, CancellationToken cancellationToken)
    {
        var generation = await store.GetIdeaGenerationAsync(projectId, generationId, cancellationToken) ?? throw new ResourceNotFoundException("Idea generation was not found.");
        var ideas = await store.ListVideoIdeasAsync(projectId, generation.OpportunityId, cancellationToken);
        var latestReport = await store.GetLatestOpportunityReportAsync(projectId, cancellationToken);
        return new(generation.Id, generation.Version, generation.OpportunityId, generation.OpportunityReportVersion, generation.PromptKey,
            generation.PromptVersion, generation.Provider, generation.Model, generation.ScoringAlgorithmVersion, generation.CreatedAt,
            latestReport is not null && latestReport.Report.Version > generation.OpportunityReportVersion,
            ideas.Where(item => item.Idea.GenerationId == generation.Id).OrderByDescending(item => item.Idea.OverallScore).Select(GetIdeaBankHandler.ToDto).ToArray());
    }
}

public sealed class IdeaGenerationJobProcessor(IYoutubeAiFactoryStore store, ILlmProvider provider, IdeaGenerationContextBuilder contextBuilder, IdeaGenerationOptions options, IdeaScoringEngine scoringEngine, TimeProvider timeProvider, ILogger<IdeaGenerationJobProcessor> logger)
{
    private static readonly Action<ILogger, Guid, Guid, int, int, Exception?> LogCompleted = LoggerMessage.Define<Guid, Guid, int, int>(LogLevel.Information, new EventId(1, nameof(LogCompleted)), "Idea generation completed for project {ProjectId}, opportunity {OpportunityId}; accepted {AcceptedCount} of {GeneratedCount} candidates.");
    private static readonly Action<ILogger, Guid, Exception?> LogFailed = LoggerMessage.Define<Guid>(LogLevel.Warning, new EventId(2, nameof(LogFailed)), "Idea generation job {JobId} failed.");
    public async Task<bool> ProcessNextAsync(CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow(); var job = await store.TryClaimNextIdeaGenerationJobAsync(now, now.AddSeconds(-options.RunningJobLeaseSeconds), cancellationToken); if (job is null) return false; AiRun? run = null;
        try
        {
            var payload = JsonSerializer.Deserialize<IdeaGenerationJobPayload>(job.Payload, IdeaGenerationPrompt.SerializerOptions) ?? throw new ApplicationValidationException("Idea generation job payload is invalid.");
            var project = await store.GetProjectAsync(payload.ProjectId, cancellationToken) ?? throw new ResourceNotFoundException("The project for this idea job no longer exists.");
            var opportunity = await store.GetOpportunityWithEvidenceAsync(payload.ProjectId, payload.OpportunityId, false, cancellationToken) ?? throw new ResourceNotFoundException("Approved opportunity was not found.");
            var existing = await store.ListExistingIdeaContextAsync(payload.ProjectId, cancellationToken); var titles = await store.ListCompetitorTitlesAsync(payload.ProjectId, cancellationToken); var context = contextBuilder.Build(project, opportunity, existing, titles);
            run = new AiRun("IdeaGeneration", payload.ProjectId, "pending", "pending", IdeaGenerationPrompt.Key, IdeaGenerationPrompt.Version, now); store.AddAiRun(run); await store.SaveChangesAsync(cancellationToken);
            var accepted = new List<VideoIdeaCandidateResult>(); var generatedCount = 0; var totalInputTokens = 0; var totalOutputTokens = 0; var hasInputTokens = false; var hasOutputTokens = false;
            for (var replacement = 0; replacement <= options.MaxReplacementAttempts && accepted.Count < options.MinIdeaCount; replacement++)
            {
                var requestCount = replacement == 0 ? options.TargetIdeaCount : options.MinIdeaCount - accepted.Count;
                LlmResult<IdeaGenerationResult>? answer = null; Exception? failure = null;
                for (var attempt = 0; attempt <= options.MaxStructuredOutputRetries; attempt++)
                { try { answer = await provider.GenerateStructuredAsync<IdeaGenerationResult>(IdeaGenerationPrompt.Create(context, requestCount, attempt > 0, replacement > 0 ? accepted : null), cancellationToken); break; } catch (StructuredOutputException ex) when (attempt < options.MaxStructuredOutputRetries) { run.RecordRetry(); failure = ex; } catch (Exception ex) { failure = ex; break; } }
                if (answer is null) throw failure ?? new StructuredOutputException("The provider did not return idea output.");
                run.RecordProvider(answer.Provider, answer.Model); if (answer.InputTokens is { } inputTokens) { totalInputTokens += inputTokens; hasInputTokens = true; }
                if (answer.OutputTokens is { } outputTokens) { totalOutputTokens += outputTokens; hasOutputTokens = true; }
                if (answer.Value.Ideas is null || answer.Value.Ideas.Count > options.MaxGeneratedCandidates) throw new StructuredOutputException("The provider returned an invalid number of idea candidates."); generatedCount += answer.Value.Ideas.Count;
                foreach (var candidate in answer.Value.Ideas)
                {
                    try { IdeaGenerationValidator.Validate(candidate, context); } catch (StructuredOutputException) { continue; }
                    if (IdeaDuplicateDetector.IsNearCompetitorCopy(candidate, context.CompetitorTitles, options.CompetitorTitleThreshold) || accepted.Any(other => IdeaDuplicateDetector.IsNearDuplicate(candidate, other, options.NearDuplicateThreshold)) || context.ExistingIdeas.Any(other => IdeaDuplicateDetector.IsNearDuplicate(candidate, other, options.NearDuplicateThreshold))) continue;
                    accepted.Add(candidate);
                }
            }
            if (accepted.Count < options.MinIdeaCount) throw new StructuredOutputException($"Only {accepted.Count} unique evidence-backed ideas were valid; at least {options.MinIdeaCount} are required.");
            var answerProvider = run.Provider; var answerModel = run.Model; var generation = new IdeaGeneration(payload.ProjectId, payload.OpportunityId, context.OpportunityReportId, context.OpportunityReportVersion, await store.GetNextIdeaGenerationVersionAsync(payload.OpportunityId, cancellationToken), run.Id, IdeaGenerationPrompt.Key, IdeaGenerationPrompt.Version, answerProvider, answerModel, IdeaScoringEngine.AlgorithmVersion, timeProvider.GetUtcNow()); store.AddIdeaGeneration(generation);
            foreach (var candidate in accepted.Take(options.MaxGeneratedCandidates)) { var score = scoringEngine.Calculate(candidate, context, 0); var idea = new VideoIdea(payload.ProjectId, payload.OpportunityId, generation.Id, candidate.WorkingTitle, candidate.Topic, candidate.Angle, candidate.ContentFormat, candidate.TargetAudience, candidate.ViewerIntent, candidate.HookConcept, candidate.ThumbnailConcept, candidate.ViewerPromise, candidate.CoreQuestion, candidate.WhyViewerWouldCare, candidate.Hypothesis, score.OpportunityFit, score.ObservedDemandAlignment, score.Novelty, score.TitlePotential, score.ThumbnailPotential, score.StoryPotential, score.AudienceFit, score.EvidenceStrength, score.ProductionEase, score.CompetitionRisk, score.ResearchRisk, candidate.Confidence, score.OverallScore, score.DuplicationPenalty, IdeaScoringEngine.AlgorithmVersion, JsonSerializer.Serialize(candidate.Risks, IdeaGenerationPrompt.SerializerOptions), timeProvider.GetUtcNow()); store.AddVideoIdea(idea); foreach (var evidenceId in candidate.EvidenceIds.Distinct()) { var evidence = context.Evidence.Single(x => x.OpportunityEvidenceId == evidenceId); store.AddIdeaEvidence(new IdeaEvidence(idea.Id, evidenceId, evidence.Summary)); } }
            run.Complete(hasInputTokens ? totalInputTokens : null, hasOutputTokens ? totalOutputTokens : null, null, timeProvider.GetUtcNow()); job.Complete(timeProvider.GetUtcNow()); await store.SaveChangesAsync(cancellationToken); LogCompleted(logger, payload.ProjectId, payload.OpportunityId, accepted.Count, generatedCount, null);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { await store.RequeueIdeaGenerationJobAsync(job.Id, CancellationToken.None); throw; }
        catch (Exception ex) { var failed = timeProvider.GetUtcNow(); var retryable = ex is ExternalServiceException { Failure: ExternalServiceFailure.QuotaExceeded or ExternalServiceFailure.Transient }; await store.FailIdeaGenerationJobAsync(job.Id, run?.Id, ex is YoutubeAiFactoryException ? ex.Message : "Idea generation could not be completed. Try again later.", retryable, failed, retryable ? failed.AddSeconds(Math.Pow(2, job.RetryCount + 1) * 5) : null, CancellationToken.None); LogFailed(logger, job.Id, ex); }
        return true;
    }
}
