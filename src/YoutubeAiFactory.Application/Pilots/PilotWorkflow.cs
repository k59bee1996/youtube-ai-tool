using System.Text.Json;
using Microsoft.Extensions.Logging;
using YoutubeAiFactory.Application.AI;
using YoutubeAiFactory.Application.Common;
using YoutubeAiFactory.Application.Persistence;
using YoutubeAiFactory.Domain.AI;
using YoutubeAiFactory.Domain.Jobs;
using YoutubeAiFactory.Domain.Pilots;

namespace YoutubeAiFactory.Application.Pilots;

public sealed class RunPilotGenerationHandler(IYoutubeAiFactoryStore store, TimeProvider timeProvider)
{
    public async Task<RunPilotGenerationResult> HandleAsync(Guid projectId, CancellationToken cancellationToken)
    {
        if (!await store.ProjectExistsAsync(projectId, cancellationToken)) throw new ResourceNotFoundException("Project was not found.");
        var ideas = await store.ListApprovedPilotIdeasAsync(projectId, cancellationToken);
        if (ideas.Count < PilotGenerationOptions.RequiredIdeaCount) throw new ApplicationValidationException($"Only {ideas.Count} approved ideas are available. At least 12 are required to generate a full pilot.");
        var active = await store.GetActivePilotGenerationJobAsync(projectId, cancellationToken);
        if (active is not null) return new RunPilotGenerationResult(active.Id, active.Status.ToString(), true);
        var job = new Job("pilot-generation", JsonSerializer.Serialize(new PilotGenerationJobPayload(projectId), PilotGenerationPrompt.SerializerOptions), timeProvider.GetUtcNow(), projectId: projectId);
        var persisted = await store.EnqueuePilotGenerationJobAsync(job, cancellationToken);
        return new RunPilotGenerationResult(persisted.Id, persisted.Status.ToString(), persisted.Id != job.Id);
    }
}

public sealed class GetPilotStatusHandler(IYoutubeAiFactoryStore store)
{
    public async Task<PilotStatusDto> HandleAsync(Guid projectId, CancellationToken cancellationToken)
    {
        if (!await store.ProjectExistsAsync(projectId, cancellationToken)) throw new ResourceNotFoundException("Project was not found.");
        var eligible = await store.ListApprovedPilotIdeasAsync(projectId, cancellationToken);
        var latest = await store.GetLatestPilotAsync(projectId, cancellationToken);
        var active = await store.GetActivePilotGenerationJobAsync(projectId, cancellationToken);
        var last = await store.GetLatestPilotGenerationJobAsync(projectId, cancellationToken);
        return new PilotStatusDto(latest is null ? null : await PilotDtoMapper.MapAsync(store, latest, cancellationToken), active?.Status.ToString(), last?.FailureReason, eligible.Count);
    }
}

public sealed class GetPilotHandler(IYoutubeAiFactoryStore store)
{
    public async Task<PilotDto> HandleAsync(Guid projectId, Guid pilotId, CancellationToken cancellationToken)
    {
        var pilot = await store.GetPilotAsync(projectId, pilotId, false, cancellationToken) ?? throw new ResourceNotFoundException("Pilot was not found.");
        return await PilotDtoMapper.MapAsync(store, pilot, cancellationToken);
    }
}

public sealed class ListPilotsHandler(IYoutubeAiFactoryStore store)
{
    public async Task<IReadOnlyList<PilotDto>> HandleAsync(Guid projectId, CancellationToken cancellationToken)
    {
        if (!await store.ProjectExistsAsync(projectId, cancellationToken)) throw new ResourceNotFoundException("Project was not found.");
        var pilots = await store.ListPilotsAsync(projectId, cancellationToken);
        var results = new List<PilotDto>(pilots.Count);
        foreach (var pilot in pilots) results.Add(await PilotDtoMapper.MapAsync(store, pilot, cancellationToken));
        return results;
    }
}

public sealed class ListPilotCandidatesHandler(IYoutubeAiFactoryStore store)
{
    public async Task<IReadOnlyList<PilotCandidateDto>> HandleAsync(Guid projectId, CancellationToken cancellationToken)
    {
        if (!await store.ProjectExistsAsync(projectId, cancellationToken)) throw new ResourceNotFoundException("Project was not found.");
        return (await store.ListApprovedPilotIdeasAsync(projectId, cancellationToken)).Select(x => new PilotCandidateDto(x.VideoIdeaId, x.OpportunityId, x.OpportunityName, x.WorkingTitle, x.Topic, x.ContentFormat, x.OverallScore)).ToArray();
    }
}

public sealed class ApprovePilotHandler(IYoutubeAiFactoryStore store, TimeProvider timeProvider)
{
    public async Task<PilotDto> HandleAsync(Guid projectId, Guid pilotId, CancellationToken cancellationToken)
    {
        var pilot = await store.GetPilotAsync(projectId, pilotId, true, cancellationToken) ?? throw new ResourceNotFoundException("Pilot was not found.");
        var dto = await PilotDtoMapper.MapAsync(store, pilot, cancellationToken);
        if (dto.RequiresReview) throw new ApplicationValidationException("This pilot requires review because an included idea is no longer approved.");
        pilot.Approve(timeProvider.GetUtcNow()); await store.SaveChangesAsync(cancellationToken);
        return await PilotDtoMapper.MapAsync(store, pilot, cancellationToken);
    }
}

public sealed class ReplacePilotSlotHandler(IYoutubeAiFactoryStore store)
{
    public async Task<PilotDto> HandleAsync(Guid projectId, Guid pilotId, int sequence, ReplacePilotSlotRequest request, CancellationToken cancellationToken)
    {
        if (sequence is < 1 or > 12) throw new ApplicationValidationException("Pilot sequence must be between 1 and 12.");
        var pilot = await store.GetPilotAsync(projectId, pilotId, true, cancellationToken) ?? throw new ResourceNotFoundException("Pilot was not found.");
        if (pilot.Status != PilotStatus.Draft) throw new ApplicationValidationException("Only draft pilots can be changed.");
        var videos = await store.ListPilotVideosAsync(pilotId, true, cancellationToken);
        var slot = videos.SingleOrDefault(x => x.Sequence == sequence) ?? throw new ResourceNotFoundException("Pilot slot was not found.");
        if (videos.Any(x => x.VideoIdeaId == request.VideoIdeaId)) throw new ApplicationValidationException("An idea can appear only once in a pilot.");
        var idea = (await store.ListApprovedPilotIdeasAsync(projectId, cancellationToken)).SingleOrDefault(x => x.VideoIdeaId == request.VideoIdeaId) ?? throw new ApplicationValidationException("Replacement ideas must be approved ideas from this project.");
        var type = PilotPlanValidator.ExpectedType(sequence); var metric = type switch { PilotExperimentType.Topic => "Views relative to channel baseline", PilotExperimentType.Packaging => "CTR", _ => "Audience retention" };
        var variable = type switch { PilotExperimentType.Topic => idea.Topic, PilotExperimentType.Packaging => idea.HookConcept, _ => idea.ContentFormat };
        var control = type switch { PilotExperimentType.Topic => "Keep packaging intensity and format roughly consistent.", PilotExperimentType.Packaging => "Keep topic attractiveness and production quality roughly consistent.", _ => "Keep topic strength and packaging quality roughly consistent." };
        slot.Replace(idea.VideoIdeaId, idea.OpportunityId, idea.Hypothesis, variable, control, metric, $"Compare this {type} result with comparable pilot videos rather than a universal threshold.", $"Manual replacement preserves the {type} experiment block using an approved, unused idea.");
        await store.SaveChangesAsync(cancellationToken); return await PilotDtoMapper.MapAsync(store, pilot, cancellationToken);
    }
}

public sealed class MovePilotSlotHandler(IYoutubeAiFactoryStore store)
{
    public async Task<PilotDto> HandleAsync(Guid projectId, Guid pilotId, int sequence, MovePilotSlotRequest request, CancellationToken cancellationToken)
    {
        if (sequence is < 1 or > 12) throw new ApplicationValidationException("Pilot sequence must be between 1 and 12.");
        var pilot = await store.GetPilotAsync(projectId, pilotId, true, cancellationToken) ?? throw new ResourceNotFoundException("Pilot was not found.");
        if (pilot.Status != PilotStatus.Draft) throw new ApplicationValidationException("Only draft pilots can be changed.");
        var delta = request.Direction.Equals("up", StringComparison.OrdinalIgnoreCase) ? -1 : request.Direction.Equals("down", StringComparison.OrdinalIgnoreCase) ? 1 : throw new ApplicationValidationException("Move direction must be 'up' or 'down'.");
        var target = sequence + delta; if (target is < 1 or > 12 || PilotPlanValidator.ExpectedType(target) != PilotPlanValidator.ExpectedType(sequence)) throw new ApplicationValidationException("Pilot slots can only move within their experiment block.");
        var videos = await store.ListPilotVideosAsync(pilotId, true, cancellationToken); var current = videos.Single(x => x.Sequence == sequence); var destination = videos.Single(x => x.Sequence == target); current.SwapContentsWith(destination);
        await store.SaveChangesAsync(cancellationToken); return await PilotDtoMapper.MapAsync(store, pilot, cancellationToken);
    }
}

public sealed class PilotGenerationJobProcessor(IYoutubeAiFactoryStore store, ILlmProvider provider, PilotGenerationContextBuilder contextBuilder,
    PilotGenerationOptions options, TimeProvider timeProvider, ILogger<PilotGenerationJobProcessor> logger)
{
    private static readonly Action<ILogger, Guid, Guid, int, Exception?> LogCompleted = LoggerMessage.Define<Guid, Guid, int>(LogLevel.Information, new EventId(1, nameof(LogCompleted)), "Pilot generation completed for project {ProjectId}, pilot {PilotId}, eligible ideas {EligibleIdeaCount}.");
    private static readonly Action<ILogger, Guid, Exception?> LogFailed = LoggerMessage.Define<Guid>(LogLevel.Warning, new EventId(2, nameof(LogFailed)), "Pilot generation job {JobId} failed.");
    public async Task<bool> ProcessNextAsync(CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow(); var job = await store.TryClaimNextPilotGenerationJobAsync(now, now.AddSeconds(-options.RunningJobLeaseSeconds), cancellationToken); if (job is null) return false; AiRun? run = null;
        try
        {
            var payload = JsonSerializer.Deserialize<PilotGenerationJobPayload>(job.Payload, PilotGenerationPrompt.SerializerOptions) ?? throw new ApplicationValidationException("Pilot generation job payload is invalid.");
            var project = await store.GetProjectAsync(payload.ProjectId, cancellationToken) ?? throw new ResourceNotFoundException("The project for this pilot job no longer exists.");
            var context = contextBuilder.Build(project, await store.ListApprovedPilotIdeasAsync(payload.ProjectId, cancellationToken));
            run = new AiRun("PilotGeneration", payload.ProjectId, "pending", "pending", PilotGenerationPrompt.Key, PilotGenerationPrompt.Version, now); store.AddAiRun(run); await store.SaveChangesAsync(cancellationToken);
            LlmResult<PilotPlanResult>? answer = null; Exception? failure = null;
            for (var attempt = 0; attempt <= options.MaxStructuredOutputRetries; attempt++)
            {
                try { answer = await provider.GenerateStructuredAsync<PilotPlanResult>(PilotGenerationPrompt.Create(context, attempt > 0), cancellationToken); PilotPlanValidator.Validate(answer.Value, context); break; }
                catch (StructuredOutputException ex) when (attempt < options.MaxStructuredOutputRetries) { run.RecordRetry(); failure = ex; }
                catch (Exception ex) { failure = ex; break; }
            }
            if (answer is null) throw failure ?? new StructuredOutputException("The provider did not return a valid pilot plan.");
            run.RecordProvider(answer.Provider, answer.Model); var plan = answer.Value; var warnings = PilotBalanceAnalyzer.Analyze(plan, context);
            var pilot = new Pilot(payload.ProjectId, await store.GetNextPilotVersionAsync(payload.ProjectId, cancellationToken), run.Id, PilotGenerationPrompt.Key, PilotGenerationPrompt.Version, answer.Provider, answer.Model, "pilot-planning:v1", plan.Name, plan.Objective, JsonSerializer.Serialize(plan.Assumptions, PilotGenerationPrompt.SerializerOptions), JsonSerializer.Serialize(plan.Limitations, PilotGenerationPrompt.SerializerOptions), JsonSerializer.Serialize(warnings, PilotGenerationPrompt.SerializerOptions), context.EligibleIdeaCount, timeProvider.GetUtcNow());
            store.AddPilot(pilot); foreach (var video in plan.Videos.OrderBy(x => x.Sequence)) store.AddPilotVideo(new PilotVideo(pilot.Id, video.VideoIdeaId, video.OpportunityId, video.Sequence, video.ExperimentType, video.Hypothesis, video.VariableBeingTested, video.ControlStrategy, video.PrimaryMetric, video.SuccessSignal, video.Rationale, JsonSerializer.Serialize(video.SecondaryMetrics ?? [], PilotGenerationPrompt.SerializerOptions), video.Notes));
            run.Complete(answer.InputTokens, answer.OutputTokens, null, timeProvider.GetUtcNow()); job.Complete(timeProvider.GetUtcNow()); await store.SaveChangesAsync(cancellationToken); LogCompleted(logger, payload.ProjectId, pilot.Id, context.EligibleIdeaCount, null);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { await store.RequeuePilotGenerationJobAsync(job.Id, CancellationToken.None); throw; }
        catch (Exception ex) { var failed = timeProvider.GetUtcNow(); var retryable = ex is ExternalServiceException { Failure: ExternalServiceFailure.QuotaExceeded or ExternalServiceFailure.Transient }; await store.FailPilotGenerationJobAsync(job.Id, run?.Id, ex is YoutubeAiFactoryException ? ex.Message : "Pilot generation could not be completed. Try again later.", retryable, failed, retryable ? failed.AddSeconds(Math.Pow(2, job.RetryCount + 1) * 5) : null, CancellationToken.None); LogFailed(logger, job.Id, ex); }
        return true;
    }
}

internal static class PilotDtoMapper
{
    public static async Task<PilotDto> MapAsync(IYoutubeAiFactoryStore store, Pilot pilot, CancellationToken cancellationToken)
    {
        var ideas = (await store.ListPilotIdeaContextAsync(pilot.ProjectId, cancellationToken)).ToDictionary(x => x.VideoIdeaId);
        var videos = await store.ListPilotVideosAsync(pilot.Id, false, cancellationToken);
        var requiresReview = videos.Any(x => !ideas.TryGetValue(x.VideoIdeaId, out var idea) || idea.DecisionStatus != Domain.Ideas.IdeaDecisionStatus.Approved);
        var mapped = videos.Select(x => { ideas.TryGetValue(x.VideoIdeaId, out var idea); return new PilotVideoDto(x.Id, x.Sequence, x.VideoIdeaId, x.OpportunityId, idea?.WorkingTitle ?? "Unavailable idea", idea?.OpportunityName ?? "Unavailable opportunity", idea?.OverallScore ?? 0, x.ExperimentType.ToString(), x.Hypothesis, x.VariableBeingTested, x.ControlStrategy, x.PrimaryMetric, x.SuccessSignal, x.Rationale, Parse(x.SecondaryMetricsJson), x.Notes); }).ToArray();
        return new PilotDto(pilot.Id, pilot.Version, pilot.Name, pilot.Objective, pilot.Status.ToString(), pilot.CreatedAt, pilot.ApprovedAt, pilot.EligibleIdeaCount, pilot.PromptKey, pilot.PromptVersion, pilot.Provider, pilot.Model, pilot.PlanningAlgorithmVersion, Parse(pilot.AssumptionsJson), Parse(pilot.LimitationsJson), Parse(pilot.WarningsJson), requiresReview, mapped);
    }
    private static string[] Parse(string json) => JsonSerializer.Deserialize<string[]>(json, PilotGenerationPrompt.SerializerOptions) ?? [];
}
