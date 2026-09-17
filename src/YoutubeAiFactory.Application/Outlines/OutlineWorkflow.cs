using System.Text.Json;
using Microsoft.Extensions.Logging;
using YoutubeAiFactory.Application.AI;
using YoutubeAiFactory.Application.Common;
using YoutubeAiFactory.Application.Persistence;
using YoutubeAiFactory.Application.Research;
using YoutubeAiFactory.Domain.AI;
using YoutubeAiFactory.Domain.Jobs;
using YoutubeAiFactory.Domain.Outlines;
using YoutubeAiFactory.Domain.Research;
using YoutubeAiFactory.Domain.Videos;

namespace YoutubeAiFactory.Application.Outlines;

public sealed class RunVideoOutlineHandler(IYoutubeAiFactoryStore store, OutlineGenerationContextBuilder contextBuilder,
    OutlineOptions options, TimeProvider timeProvider)
{
    public const string AlgorithmVersion = "outline-engine:v1";

    public async Task<RunVideoOutlineResult> HandleAsync(Guid projectId, Guid videoProjectId, CancellationToken cancellationToken)
    {
        OutlineGenerationContextBuilder.ValidateOptions(options);
        var videoProject = await store.GetVideoProjectAsync(projectId, videoProjectId, true, cancellationToken)
            ?? throw new ResourceNotFoundException("Video project was not found.");
        var active = await store.GetActiveVideoOutlineJobAsync(projectId, videoProjectId, cancellationToken);
        if (active is not null) return new RunVideoOutlineResult(active.Id, active.Status.ToString(), true);
        if (videoProject.Status is not (VideoProjectStatus.ResearchReady or VideoProjectStatus.OutlineReady))
            throw new ApplicationValidationException($"Outline generation cannot start while the VideoProject is {videoProject.Status}.");
        var project = await store.GetProjectAsync(projectId, cancellationToken)
            ?? throw new ResourceNotFoundException("Project was not found.");
        var source = await store.GetVideoProjectSourceAsync(projectId, videoProject.PilotId, videoProject.PilotVideoId, cancellationToken)
            ?? throw new ResourceNotFoundException("Video project source context was not found.");
        var research = await store.GetLatestResearchReportAsync(projectId, videoProjectId, cancellationToken)
            ?? throw new ApplicationValidationException("Complete evidence-backed research before generating an outline.");
        var context = contextBuilder.Build(project, videoProject, source, research);
        var previousStatus = videoProject.Status;
        var now = timeProvider.GetUtcNow();
        var payload = new OutlineJobPayload(projectId, videoProjectId, research.Report.Id, research.Report.Version,
            context.OutlineInputFingerprint, previousStatus.ToString());
        var job = new Job("outline-generation", JsonSerializer.Serialize(payload, OutlinePrompt.SerializerOptions), now,
            options.MaxJobRetries, projectId: projectId, videoProjectId: videoProjectId);
        videoProject.TransitionTo(VideoProjectStatus.OutlineGenerating, now);
        var persisted = await store.EnqueueVideoOutlineJobAsync(job, cancellationToken);
        return new RunVideoOutlineResult(persisted.Id, persisted.Status.ToString(), persisted.Id != job.Id);
    }
}

public sealed class GetVideoOutlineStatusHandler(IYoutubeAiFactoryStore store, OutlineGenerationContextBuilder contextBuilder)
{
    public async Task<OutlineStatusDto> HandleAsync(Guid projectId, Guid videoProjectId, CancellationToken cancellationToken)
    {
        var videoProject = await store.GetVideoProjectAsync(projectId, videoProjectId, false, cancellationToken)
            ?? throw new ResourceNotFoundException("Video project was not found.");
        var latest = await store.GetLatestVideoOutlineAsync(projectId, videoProjectId, false, cancellationToken);
        var active = await store.GetActiveVideoOutlineJobAsync(projectId, videoProjectId, cancellationToken);
        var latestJob = active ?? await store.GetLatestVideoOutlineJobAsync(projectId, videoProjectId, cancellationToken);
        var blockReason = active is not null ? "Outline generation is already in progress." :
            await GetBlockReasonAsync(store, contextBuilder, projectId, videoProject, cancellationToken);
        return new OutlineStatusDto(latest is null ? null : await OutlineDtoMapper.MapAsync(store, latest, videoProject, cancellationToken),
            ToJob(active), ToJob(latestJob), blockReason is null, blockReason);
    }

    internal static OutlineJobDto? ToJob(Job? job) => job is null ? null : new(job.Id, job.Status.ToString(), job.FailureReason);

    private static async Task<string?> GetBlockReasonAsync(IYoutubeAiFactoryStore store,
        OutlineGenerationContextBuilder contextBuilder, Guid projectId, VideoProject videoProject,
        CancellationToken cancellationToken)
    {
        if (videoProject.Status is not (VideoProjectStatus.ResearchReady or VideoProjectStatus.OutlineReady))
            return videoProject.Status == VideoProjectStatus.OutlineApproved
                ? "The approved outline is immutable."
                : "A current ResearchReport is required before outline generation.";
        var research = await store.GetLatestResearchReportAsync(projectId, videoProject.Id, cancellationToken);
        if (research is null) return "Complete evidence-backed research before generating an outline.";
        var project = await store.GetProjectAsync(projectId, cancellationToken);
        var source = await store.GetVideoProjectSourceAsync(projectId, videoProject.PilotId, videoProject.PilotVideoId, cancellationToken);
        if (project is null || source is null) return "Video project source context is unavailable.";
        try { contextBuilder.Build(project, videoProject, source, research); return null; }
        catch (ApplicationValidationException exception) { return exception.Message; }
    }
}

public sealed class GetVideoOutlineHandler(IYoutubeAiFactoryStore store)
{
    public async Task<VideoOutlineDto> HandleAsync(Guid projectId, Guid videoProjectId, Guid? outlineId,
        CancellationToken cancellationToken)
    {
        var videoProject = await store.GetVideoProjectAsync(projectId, videoProjectId, false, cancellationToken)
            ?? throw new ResourceNotFoundException("Video project was not found.");
        var outline = outlineId is { } id
            ? await store.GetVideoOutlineAsync(projectId, videoProjectId, id, false, cancellationToken)
            : await store.GetLatestVideoOutlineAsync(projectId, videoProjectId, false, cancellationToken);
        if (outline is null) throw new ResourceNotFoundException("Video outline was not found.");
        return await OutlineDtoMapper.MapAsync(store, outline, videoProject, cancellationToken);
    }
}

public sealed class ListVideoOutlinesHandler(IYoutubeAiFactoryStore store)
{
    public async Task<IReadOnlyList<VideoOutlineHistoryItemDto>> HandleAsync(Guid projectId, Guid videoProjectId,
        CancellationToken cancellationToken)
    {
        var videoProject = await store.GetVideoProjectAsync(projectId, videoProjectId, false, cancellationToken)
            ?? throw new ResourceNotFoundException("Video project was not found.");
        var outlines = await store.ListVideoOutlinesAsync(projectId, videoProjectId, cancellationToken);
        var result = new List<VideoOutlineHistoryItemDto>(outlines.Count);
        foreach (var outline in outlines)
        {
            var stale = await OutlineDtoMapper.IsStaleAsync(store, outline, videoProject, cancellationToken);
            result.Add(new(outline.Id, outline.Version, outline.Status.ToString(), outline.ResearchReportVersion,
                outline.StructureType.ToString(), stale, outline.CreatedAt, outline.ApprovedAt));
        }
        return result;
    }
}

public sealed class UpdateVideoOutlineHandler(IYoutubeAiFactoryStore store, TimeProvider timeProvider)
{
    public async Task<VideoOutlineDto> HandleAsync(Guid projectId, Guid videoProjectId, Guid outlineId,
        UpdateVideoOutlineRequest request, CancellationToken cancellationToken)
    {
        var videoProject = await store.GetVideoProjectAsync(projectId, videoProjectId, false, cancellationToken)
            ?? throw new ResourceNotFoundException("Video project was not found.");
        if (videoProject.Status != VideoProjectStatus.OutlineReady)
            throw new ApplicationValidationException("Only a Ready outline can be edited.");
        var details = await store.GetVideoOutlineAsync(projectId, videoProjectId, outlineId, true, cancellationToken)
            ?? throw new ResourceNotFoundException("Video outline was not found.");
        if (details.Outline.Status != VideoOutlineStatus.Ready)
            throw new ApplicationValidationException("An approved outline is immutable.");
        if (request.Sections is null || request.Sections.Count == 0 ||
            request.Sections.Select(item => item.SectionId).Distinct().Count() != request.Sections.Count)
            throw new ApplicationValidationException("At least one unique outline section edit is required.");
        var sections = details.Sections.ToDictionary(item => item.Id);
        foreach (var edit in request.Sections)
        {
            if (!sections.TryGetValue(edit.SectionId, out var section))
                throw new ApplicationValidationException("An edited section does not belong to this outline.");
            section.UpdatePlanningContent(edit.Heading, edit.Objective, edit.Summary, edit.ViewerQuestion,
                edit.TransitionIntent, edit.EstimatedSeconds);
        }
        int? totalEstimatedSeconds = details.Sections.Any(item => item.EstimatedSeconds.HasValue)
            ? details.Sections.Sum(item => item.EstimatedSeconds ?? 0) : null;
        details.Outline.RecordEdit(timeProvider.GetUtcNow(), totalEstimatedSeconds);
        await store.SaveChangesAsync(cancellationToken);
        var refreshed = await store.GetVideoOutlineAsync(projectId, videoProjectId, outlineId, false, cancellationToken)
            ?? throw new ResourceNotFoundException("Video outline was not found after editing.");
        return await OutlineDtoMapper.MapAsync(store, refreshed, videoProject, cancellationToken);
    }
}

public sealed class ReorderVideoOutlineSectionsHandler(IYoutubeAiFactoryStore store, TimeProvider timeProvider)
{
    public async Task<VideoOutlineDto> HandleAsync(Guid projectId, Guid videoProjectId, Guid outlineId,
        ReorderVideoOutlineSectionsRequest request, CancellationToken cancellationToken)
    {
        var videoProject = await store.GetVideoProjectAsync(projectId, videoProjectId, false, cancellationToken)
            ?? throw new ResourceNotFoundException("Video project was not found.");
        if (videoProject.Status != VideoProjectStatus.OutlineReady)
            throw new ApplicationValidationException("Only a Ready outline can be reordered.");
        var details = await store.GetVideoOutlineAsync(projectId, videoProjectId, outlineId, true, cancellationToken)
            ?? throw new ResourceNotFoundException("Video outline was not found.");
        if (details.Outline.Status != VideoOutlineStatus.Ready)
            throw new ApplicationValidationException("An approved outline is immutable.");
        var expected = details.Sections.Select(item => item.Id).ToHashSet();
        if (request.SectionIds is null || request.SectionIds.Count != expected.Count ||
            request.SectionIds.Distinct().Count() != expected.Count || request.SectionIds.Any(id => !expected.Contains(id)))
            throw new ApplicationValidationException("Section order must contain every outline section exactly once.");
        details.Outline.RecordReorder(timeProvider.GetUtcNow());
        await store.ReorderVideoOutlineSectionsAsync(details.Outline, request.SectionIds, cancellationToken);
        var refreshed = await store.GetVideoOutlineAsync(projectId, videoProjectId, outlineId, false, cancellationToken)
            ?? throw new ResourceNotFoundException("Video outline was not found after reordering.");
        return await OutlineDtoMapper.MapAsync(store, refreshed, videoProject, cancellationToken);
    }
}

public sealed class ApproveVideoOutlineHandler(IYoutubeAiFactoryStore store,
    OutlineGenerationContextBuilder contextBuilder, TimeProvider timeProvider)
{
    public async Task<VideoOutlineDto> HandleAsync(Guid projectId, Guid videoProjectId, Guid outlineId,
        CancellationToken cancellationToken)
    {
        var videoProject = await store.GetVideoProjectAsync(projectId, videoProjectId, true, cancellationToken)
            ?? throw new ResourceNotFoundException("Video project was not found.");
        if (videoProject.Status != VideoProjectStatus.OutlineReady)
            throw new ApplicationValidationException("Only an OutlineReady VideoProject can approve an outline.");
        var details = await store.GetVideoOutlineAsync(projectId, videoProjectId, outlineId, true, cancellationToken)
            ?? throw new ResourceNotFoundException("Video outline was not found.");
        var latest = await store.GetLatestVideoOutlineAsync(projectId, videoProjectId, false, cancellationToken);
        if (latest?.Outline.Id != outlineId)
            throw new ApplicationValidationException("Only the latest applicable outline version can be approved.");
        var research = await store.GetLatestResearchReportAsync(projectId, videoProjectId, cancellationToken)
            ?? throw new ApplicationValidationException("The source ResearchReport is no longer available.");
        if (research.Report.Id != details.Outline.ResearchReportId || research.Report.Version != details.Outline.ResearchReportVersion)
            throw new ApplicationValidationException("This outline is outdated because a newer ResearchReport is current. Generate a new outline before approval.");
        var project = await store.GetProjectAsync(projectId, cancellationToken)
            ?? throw new ResourceNotFoundException("Project was not found.");
        var source = await store.GetVideoProjectSourceAsync(projectId, videoProject.PilotId, videoProject.PilotVideoId, cancellationToken)
            ?? throw new ResourceNotFoundException("Video project source context was not found.");
        var context = contextBuilder.Build(project, videoProject, source, research);
        if (!string.Equals(context.OutlineInputFingerprint, details.Outline.InputFingerprint, StringComparison.Ordinal))
            throw new ApplicationValidationException("This outline is outdated because its source inputs changed. Generate a new outline before approval.");
        OutlinePersistenceValidator.Validate(details, research);
        var now = timeProvider.GetUtcNow();
        try { details.Outline.Approve(now); }
        catch (YoutubeAiFactory.Domain.Common.DomainException exception) { throw new ApplicationValidationException(exception.Message); }
        videoProject.TransitionTo(VideoProjectStatus.OutlineApproved, now);
        await store.SaveChangesAsync(cancellationToken);
        var refreshed = await store.GetVideoOutlineAsync(projectId, videoProjectId, outlineId, false, cancellationToken)
            ?? throw new ResourceNotFoundException("Approved outline was not found.");
        return await OutlineDtoMapper.MapAsync(store, refreshed, videoProject, cancellationToken);
    }
}

public sealed partial class VideoOutlineJobProcessor(IYoutubeAiFactoryStore store, ILlmProvider provider,
    IAiModelResolver modelResolver, OutlineGenerationContextBuilder contextBuilder, OutlineValidator validator,
    OutlineOptions options, TimeProvider timeProvider, ILogger<VideoOutlineJobProcessor> logger,
    IVideoOutlineJobLeaseRenewer? leaseRenewer = null)
{
    private static readonly Action<ILogger, Guid, Exception?> LogFailed = LoggerMessage.Define<Guid>(LogLevel.Warning,
        new EventId(2, nameof(LogFailed)), "Video outline job {JobId} failed.");
    private static readonly Action<ILogger, Guid, Exception?> LogLeaseLost = LoggerMessage.Define<Guid>(LogLevel.Error,
        new EventId(3, nameof(LogLeaseLost)), "Video outline job {JobId} lost ownership before completion.");
    private static readonly Action<ILogger, Guid, Exception?> LogLeaseRenewalFailed = LoggerMessage.Define<Guid>(LogLevel.Error,
        new EventId(4, nameof(LogLeaseRenewalFailed)), "Video outline job {JobId} lease renewal failed.");

    [LoggerMessage(EventId = 1, Level = LogLevel.Information,
        Message = "Outline completed for Project {ProjectId}, VideoProject {VideoProjectId}, ResearchReport {ResearchReportId}, Outline {OutlineId}; version {OutlineVersion}, algorithm {OutlineAlgorithmVersion}, prompt {PromptVersion}, profile {ModelProfile}, sections {SectionCount}, referenced claims {ClaimCount}, conflicts {ConflictReferenceCount}, gaps {GapReferenceCount}, estimated seconds {TotalEstimatedSeconds}, duration {DurationMs} ms, status {Status}.")]
    private static partial void LogCompleted(ILogger logger, Guid projectId, Guid videoProjectId,
        Guid researchReportId, Guid outlineId, int outlineVersion, string outlineAlgorithmVersion,
        int promptVersion, string modelProfile, int sectionCount, int claimCount, int conflictReferenceCount,
        int gapReferenceCount, int? totalEstimatedSeconds, long durationMs, string status);

    public async Task<bool> ProcessNextAsync(CancellationToken cancellationToken)
    {
        OutlineGenerationContextBuilder.ValidateOptions(options);
        var now = timeProvider.GetUtcNow();
        var job = await store.TryClaimNextVideoOutlineJobAsync(now, now.AddSeconds(-options.RunningJobLeaseSeconds), cancellationToken);
        if (job is null) return false;
        var payload = JsonSerializer.Deserialize<OutlineJobPayload>(job.Payload, OutlinePrompt.SerializerOptions)
            ?? throw new ApplicationValidationException("Video outline job payload is invalid.");
        var leaseId = job.LeaseId ?? throw new ApplicationValidationException("Video outline job has no ownership lease.");
        using var leaseLostCancellation = new CancellationTokenSource();
        using var heartbeatCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        using var executionCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, leaseLostCancellation.Token);
        var executionToken = executionCancellation.Token;
        var heartbeat = leaseRenewer is null
            ? Task.CompletedTask
            : MaintainLeaseAsync(job.Id, leaseId, leaseLostCancellation, heartbeatCancellation.Token);
        AiRun? aiRun = null;
        try
        {
            var videoProject = await store.GetVideoProjectAsync(payload.ProjectId, payload.VideoProjectId, true, executionToken)
                ?? throw new ResourceNotFoundException("The VideoProject for this outline job no longer exists.");
            if (videoProject.Status != VideoProjectStatus.OutlineGenerating)
                throw new ApplicationValidationException("Video outline job state is no longer runnable.");
            var research = await store.GetResearchReportAsync(payload.ProjectId, payload.VideoProjectId,
                payload.ResearchReportId, executionToken)
                ?? throw new ApplicationValidationException("The source ResearchReport is no longer available.");
            var latestResearch = await store.GetLatestResearchReportAsync(payload.ProjectId, payload.VideoProjectId, executionToken);
            if (latestResearch?.Report.Id != payload.ResearchReportId || research.Report.Version != payload.ResearchReportVersion)
                throw new ApplicationValidationException("A newer ResearchReport became current. Queue a new outline from the current research.");
            var project = await store.GetProjectAsync(payload.ProjectId, executionToken)
                ?? throw new ResourceNotFoundException("Project was not found.");
            var source = await store.GetVideoProjectSourceAsync(payload.ProjectId, videoProject.PilotId,
                videoProject.PilotVideoId, executionToken)
                ?? throw new ResourceNotFoundException("Video project source context was not found.");
            var context = contextBuilder.Build(project, videoProject, source, research);
            if (!string.Equals(context.OutlineInputFingerprint, payload.InputFingerprint, StringComparison.Ordinal))
                throw new ApplicationValidationException("Outline inputs changed after the job was queued. Queue a new outline from the current inputs.");

            var generated = await GenerateAsync(context, executionToken);
            aiRun = generated.AiRun;
            var warnings = validator.ValidateGenerated(generated.Result, context);
            executionToken.ThrowIfCancellationRequested();
            var version = await store.GetNextVideoOutlineVersionAsync(payload.ProjectId, payload.VideoProjectId, executionToken);
            int? totalSeconds = generated.Result.Sections.Any(item => item.EstimatedSeconds.HasValue)
                ? generated.Result.Sections.Sum(item => item.EstimatedSeconds ?? 0) : null;
            var outline = new VideoOutline(payload.ProjectId, payload.VideoProjectId, research.Report.Id,
                research.Report.Version, version, generated.AiRun.Id, RunVideoOutlineHandler.AlgorithmVersion,
                OutlinePrompt.Key, OutlinePrompt.Version, context.OutlineInputFingerprint, generated.Provider,
                generated.Model, generated.Result.NarrativeStrategy.StructureType,
                generated.Result.NarrativeStrategy.CoreQuestion, generated.Result.NarrativeStrategy.CoreTension,
                generated.Result.NarrativeStrategy.OpeningHookConcept, videoProject.ViewerPromise,
                generated.Result.NarrativeStrategy.NarrativeProgression, generated.Result.NarrativeStrategy.Payoff,
                generated.Result.NarrativeStrategy.PacingStrategy, videoProject.ExperimentType,
                videoProject.VariableBeingTested, source.PilotVideo.ControlStrategy,
                generated.Result.ExperimentAlignment.HowOutlineImplementsExperiment,
                JsonSerializer.Serialize(generated.Result.ExperimentAlignment.RisksToExperimentIntegrity, OutlinePrompt.SerializerOptions),
                JsonSerializer.Serialize(warnings, OutlinePrompt.SerializerOptions), totalSeconds, timeProvider.GetUtcNow());
            store.AddVideoOutline(outline);
            foreach (var generatedSection in generated.Result.Sections.OrderBy(item => item.Sequence))
            {
                var section = new VideoOutlineSection(outline.Id, generatedSection.Sequence, generatedSection.Heading,
                    generatedSection.Purpose, generatedSection.Objective, generatedSection.Summary,
                    generatedSection.ViewerQuestion, generatedSection.TransitionIntent, generatedSection.EstimatedSeconds);
                store.AddVideoOutlineSection(section);
                foreach (var reference in generatedSection.ClaimReferences)
                    store.AddVideoOutlineSectionClaim(new(section.Id, reference.ClaimId, reference.UsageRole));
                foreach (var conflictId in generatedSection.ConflictIds)
                    store.AddVideoOutlineSectionConflict(new(section.Id, conflictId));
                foreach (var gapIndex in generatedSection.ResearchGapIndexes)
                    store.AddVideoOutlineSectionGap(new(section.Id, gapIndex));
            }
            var completedAt = timeProvider.GetUtcNow();
            videoProject.TransitionTo(VideoProjectStatus.OutlineReady, completedAt);
            if (leaseRenewer is null)
            {
                job.Complete(completedAt);
                await store.SaveChangesAsync(executionToken);
            }
            else if (!await store.CompleteVideoOutlineJobAsync(job.Id, leaseId, completedAt, executionToken))
            {
                leaseLostCancellation.Cancel();
                LogLeaseLost(logger, job.Id, null);
                return true;
            }
            if (logger.IsEnabled(LogLevel.Information))
            {
                var modelProfile = AiWorkflowProfiles.OutlineGeneration.ToString();
                var claimCount = generated.Result.Sections.SelectMany(item => item.ClaimReferences)
                    .Select(item => item.ClaimId).Distinct().Count();
                var conflictCount = generated.Result.Sections.SelectMany(item => item.ConflictIds).Distinct().Count();
                var gapCount = generated.Result.Sections.SelectMany(item => item.ResearchGapIndexes).Distinct().Count();
                var durationMs = job.StartedAt is null ? 0 : (long)(completedAt - job.StartedAt.Value).TotalMilliseconds;
                var status = job.Status.ToString();
                LogCompleted(logger, payload.ProjectId, payload.VideoProjectId, research.Report.Id, outline.Id, version,
                    RunVideoOutlineHandler.AlgorithmVersion, OutlinePrompt.Version, modelProfile,
                    generated.Result.Sections.Count, claimCount, conflictCount, gapCount, totalSeconds, durationMs, status);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            await store.RequeueVideoOutlineJobAsync(job.Id, CancellationToken.None);
            throw;
        }
        catch (OperationCanceledException) when (leaseLostCancellation.IsCancellationRequested)
        {
            LogLeaseLost(logger, job.Id, null);
        }
        catch (Exception exception) when (leaseLostCancellation.IsCancellationRequested)
        {
            LogLeaseLost(logger, job.Id, exception);
        }
        catch (Exception exception)
        {
            var failedAt = timeProvider.GetUtcNow();
            var retryable = exception is ExternalServiceException
            { Failure: ExternalServiceFailure.QuotaExceeded or ExternalServiceFailure.Transient };
            await store.FailVideoOutlineJobAsync(job.Id, aiRun?.Id,
                exception is YoutubeAiFactoryException ? exception.Message : "Outline could not be completed. Try again later.",
                retryable, failedAt, retryable ? failedAt.AddSeconds(Math.Pow(2, job.RetryCount + 1) * 5) : null,
                CancellationToken.None);
            LogFailed(logger, job.Id, exception);
        }
        finally
        {
            heartbeatCancellation.Cancel();
            try { await heartbeat; }
            catch (OperationCanceledException) { }
        }
        return true;
    }

    private async Task MaintainLeaseAsync(Guid jobId, Guid leaseId, CancellationTokenSource leaseLostCancellation,
        CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(Math.Max(5, options.RunningJobLeaseSeconds / 3d)));
        try
        {
            while (await timer.WaitForNextTickAsync(cancellationToken))
            {
                if (!await leaseRenewer!.RenewAsync(jobId, leaseId, timeProvider.GetUtcNow(), cancellationToken))
                {
                    leaseLostCancellation.Cancel();
                    LogLeaseLost(logger, jobId, null);
                    return;
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
        catch (Exception exception)
        {
            leaseLostCancellation.Cancel();
            LogLeaseRenewalFailed(logger, jobId, exception);
        }
    }

    private async Task<GeneratedOutline> GenerateAsync(OutlineGenerationContext context, CancellationToken cancellationToken)
    {
        var resolved = modelResolver.Resolve(AiWorkflowProfiles.OutlineGeneration);
        var run = new AiRun("OutlineGeneration", context.ProjectId, resolved.Provider, resolved.Model,
            OutlinePrompt.Key, OutlinePrompt.Version, timeProvider.GetUtcNow(), resolved.Profile.ToString(),
            context.VideoProjectId, researchReportId: context.ResearchReportId);
        store.AddAiRun(run);
        await store.SaveChangesAsync(cancellationToken);
        string? diagnostic = null;
        OutlineGenerationResult? previous = null;
        var structuredRepairAttempts = 0;
        try
        {
            for (var attempt = 0; attempt <= options.MaxGenerationRetries; attempt++)
            {
                try
                {
                    var answer = await provider.GenerateStructuredAsync<OutlineGenerationResult>(
                        OutlinePrompt.Create(context, diagnostic, previous).WithResolvedModel(resolved), cancellationToken);
                    previous = answer.Value;
                    var warnings = validator.ValidateGenerated(answer.Value, context);
                    _ = warnings;
                    run.RecordProvider(answer.Provider, answer.Model);
                    run.Complete(answer.InputTokens, answer.OutputTokens, null, timeProvider.GetUtcNow());
                    await store.SaveChangesAsync(cancellationToken);
                    return new(answer.Value, run, answer.Provider, answer.Model);
                }
                catch (StructuredOutputException exception) when (!string.IsNullOrWhiteSpace(exception.RawOutput) &&
                    structuredRepairAttempts < options.MaxStructuredRepairAttempts)
                {
                    structuredRepairAttempts++;
                    try
                    {
                        var repaired = await RepairAsync(context, exception.RawOutput!, exception.Message, cancellationToken);
                        validator.ValidateGenerated(repaired.Value, context);
                        run.RecordProvider(resolved.Provider, resolved.Model);
                        run.Complete(null, null, null, timeProvider.GetUtcNow());
                        await store.SaveChangesAsync(cancellationToken);
                        return new(repaired.Value, run, resolved.Provider, resolved.Model);
                    }
                    catch (Exception repairFailure) when (repairFailure is StructuredOutputException or ApplicationValidationException)
                    {
                        diagnostic = repairFailure.Message;
                    }
                }
                catch (Exception exception) when (exception is StructuredOutputException or ApplicationValidationException)
                {
                    diagnostic = exception.Message;
                }
                if (attempt == options.MaxGenerationRetries)
                    throw new StructuredOutputException(diagnostic ?? "Outline output did not satisfy the evidence and experiment constraints.");
                run.RecordRetry();
                await store.SaveChangesAsync(cancellationToken);
            }
            throw new StructuredOutputException("Outline output did not satisfy the required contract.");
        }
        catch (Exception exception)
        {
            if (run.Status == AiRunStatus.Running)
            {
                run.Fail(exception is YoutubeAiFactoryException ? exception.Message : "Outline AI stage failed.", timeProvider.GetUtcNow());
                await store.SaveChangesAsync(CancellationToken.None);
            }
            throw;
        }
    }

    private async Task<LlmResult<OutlineGenerationResult>> RepairAsync(OutlineGenerationContext context,
        string malformedOutput, string diagnostic, CancellationToken cancellationToken)
    {
        var resolved = modelResolver.Resolve(AiWorkflowProfiles.StructuredOutputRepair);
        var run = new AiRun("StructuredOutputRepair", context.ProjectId, resolved.Provider, resolved.Model,
            OutlinePrompt.RepairKey, OutlinePrompt.RepairVersion, timeProvider.GetUtcNow(), resolved.Profile.ToString(),
            context.VideoProjectId, researchReportId: context.ResearchReportId);
        store.AddAiRun(run);
        await store.SaveChangesAsync(cancellationToken);
        try
        {
            var answer = await provider.GenerateStructuredAsync<OutlineGenerationResult>(
                OutlinePrompt.CreateRepair(malformedOutput, diagnostic).WithResolvedModel(resolved), cancellationToken);
            validator.ValidateGenerated(answer.Value, context);
            run.RecordProvider(answer.Provider, answer.Model);
            run.Complete(answer.InputTokens, answer.OutputTokens, null, timeProvider.GetUtcNow());
            await store.SaveChangesAsync(cancellationToken);
            return answer;
        }
        catch (Exception exception)
        {
            if (run.Status == AiRunStatus.Running)
            {
                run.Fail(exception is YoutubeAiFactoryException ? exception.Message : "Structured outline repair failed.",
                    timeProvider.GetUtcNow());
                await store.SaveChangesAsync(CancellationToken.None);
            }
            throw;
        }
    }

    private sealed record GeneratedOutline(OutlineGenerationResult Result, AiRun AiRun, string Provider, string Model);
}

internal static class OutlinePersistenceValidator
{
    public static void Validate(VideoOutlineWithDetails outline, ResearchReportWithDetails research)
    {
        if (outline.Outline.ResearchReportId != research.Report.Id ||
            outline.Outline.ProjectId != research.Report.ProjectId || outline.Outline.VideoProjectId != research.Report.VideoProjectId)
            throw new ApplicationValidationException("Outline and ResearchReport ownership do not match.");
        if (!outline.Sections.Select(item => item.Sequence).Order().SequenceEqual(Enumerable.Range(1, outline.Sections.Count)))
            throw new ApplicationValidationException("Outline section order is invalid.");
        var sectionIds = outline.Sections.Select(item => item.Id).ToHashSet();
        var claims = research.Claims.ToDictionary(item => item.Id);
        var conflicts = research.Conflicts.ToDictionary(item => item.Id);
        var payload = JsonSerializer.Deserialize<ResearchReportPayload>(research.Report.ResultJson, OutlinePrompt.SerializerOptions)
            ?? throw new ApplicationValidationException("The source ResearchReport is unreadable.");
        if (outline.SectionClaims.Any(link => !sectionIds.Contains(link.OutlineSectionId) ||
            !claims.TryGetValue(link.ResearchClaimId, out var claim) || claim.SupportStatus == ResearchClaimSupportStatus.Unsupported))
            throw new ApplicationValidationException("Outline contains an invalid or unsupported ResearchClaim reference.");
        if (outline.SectionConflicts.Any(link => !sectionIds.Contains(link.OutlineSectionId) || !conflicts.ContainsKey(link.ResearchConflictId)))
            throw new ApplicationValidationException("Outline contains an invalid ResearchConflict reference.");
        if (outline.SectionGaps.Any(link => !sectionIds.Contains(link.OutlineSectionId) ||
            link.ResearchGapIndex < 0 || link.ResearchGapIndex >= payload.Synthesis.Gaps.Count))
            throw new ApplicationValidationException("Outline contains an invalid ResearchGap reference.");
        foreach (var link in outline.SectionClaims.Where(link => claims[link.ResearchClaimId].SupportStatus == ResearchClaimSupportStatus.Conflicted))
        {
            if (!outline.SectionConflicts.Any(conflictLink => conflictLink.OutlineSectionId == link.OutlineSectionId &&
                conflicts[conflictLink.ResearchConflictId].ResearchClaimId == link.ResearchClaimId))
                throw new ApplicationValidationException("A conflicted claim lost its conflict traceability.");
        }
    }
}

internal static class OutlineDtoMapper
{
    public static async Task<VideoOutlineDto> MapAsync(IYoutubeAiFactoryStore store, VideoOutlineWithDetails details,
        VideoProject videoProject, CancellationToken cancellationToken)
    {
        var research = await store.GetResearchReportAsync(details.Outline.ProjectId, details.Outline.VideoProjectId,
            details.Outline.ResearchReportId, cancellationToken)
            ?? throw new ResourceNotFoundException("The outline's source ResearchReport was not found.");
        var payload = JsonSerializer.Deserialize<ResearchReportPayload>(research.Report.ResultJson, OutlinePrompt.SerializerOptions)
            ?? throw new InvalidOperationException("Stored ResearchReport is unreadable.");
        var claims = research.Claims.ToDictionary(item => item.Id);
        var evidence = research.Evidence.ToDictionary(item => item.Id);
        var sources = research.Sources.ToDictionary(item => item.Id);
        var evidenceLinks = research.ClaimEvidence.GroupBy(item => item.ResearchClaimId)
            .ToDictionary(group => group.Key, group => group.ToArray());
        var conflicts = research.Conflicts.ToDictionary(item => item.Id);
        var claimsBySection = details.SectionClaims.GroupBy(item => item.OutlineSectionId)
            .ToDictionary(group => group.Key, group => group.ToArray());
        var conflictsBySection = details.SectionConflicts.GroupBy(item => item.OutlineSectionId)
            .ToDictionary(group => group.Key, group => group.ToArray());
        var gapsBySection = details.SectionGaps.GroupBy(item => item.OutlineSectionId)
            .ToDictionary(group => group.Key, group => group.ToArray());
        var sectionDtos = details.Sections.OrderBy(item => item.Sequence).Select(section =>
        {
            var sectionClaims = claimsBySection.GetValueOrDefault(section.Id, []).Where(link => claims.ContainsKey(link.ResearchClaimId))
                .Select(link =>
                {
                    var claim = claims[link.ResearchClaimId];
                    var claimEvidence = evidenceLinks.GetValueOrDefault(claim.Id, []).Where(item => evidence.ContainsKey(item.ResearchEvidenceId))
                        .Select(item => evidence[item.ResearchEvidenceId]).Where(item => sources.ContainsKey(item.ResearchSourceId))
                        .Select(item => new OutlineEvidenceDto(item.Id, item.Fact, item.SupportingExcerpt, item.SourceLocator,
                            item.Type.ToString(), item.Confidence, new ResearchSourceDto(sources[item.ResearchSourceId].Id,
                                sources[item.ResearchSourceId].Url, sources[item.ResearchSourceId].CanonicalUrl,
                                sources[item.ResearchSourceId].Domain, sources[item.ResearchSourceId].Title,
                                sources[item.ResearchSourceId].Publisher, sources[item.ResearchSourceId].PublishedAt,
                                sources[item.ResearchSourceId].RetrievedAt, sources[item.ResearchSourceId].Category.ToString(),
                                sources[item.ResearchSourceId].FetchStatus.ToString()))).ToArray();
                    return new OutlineClaimDto(claim.Id, claim.Statement, claim.Type.ToString(), claim.SupportStatus.ToString(),
                        claim.Confidence, claim.IsCritical, link.UsageRole.ToString(), claimEvidence);
                }).ToArray();
            var sectionConflicts = conflictsBySection.GetValueOrDefault(section.Id, []).Where(link => conflicts.ContainsKey(link.ResearchConflictId))
                .Select(link => conflicts[link.ResearchConflictId])
                .Select(item => new OutlineConflictDto(item.Id, item.ResearchClaimId, item.Explanation, item.IsResolved)).ToArray();
            var sectionGaps = gapsBySection.GetValueOrDefault(section.Id, [])
                .Where(item => item.ResearchGapIndex >= 0 && item.ResearchGapIndex < payload.Synthesis.Gaps.Count)
                .Select(item => new OutlineGapDto(item.ResearchGapIndex, payload.Synthesis.Gaps[item.ResearchGapIndex].Description,
                    payload.Synthesis.Gaps[item.ResearchGapIndex].ClaimIds)).ToArray();
            return new VideoOutlineSectionDto(section.Id, section.Sequence, section.Heading, section.Purpose.ToString(),
                section.Objective, section.Summary, section.ViewerQuestion, section.TransitionIntent,
                section.EstimatedSeconds, sectionClaims, sectionConflicts, sectionGaps);
        }).ToArray();
        var outline = details.Outline;
        var stale = await IsStaleAsync(store, outline, videoProject, cancellationToken);
        return new VideoOutlineDto(outline.Id, outline.ProjectId, outline.VideoProjectId, outline.ResearchReportId,
            outline.ResearchReportVersion, outline.Version, outline.Status.ToString(), outline.OutlineAlgorithmVersion,
            outline.PromptKey, outline.PromptVersion, outline.Provider, outline.Model, outline.StructureType.ToString(),
            outline.CoreQuestion, outline.CoreTension, outline.OpeningHookConcept, outline.ViewerPromise,
            outline.NarrativeProgression, outline.Payoff, outline.PacingStrategy,
            new ExperimentAlignmentDto(outline.ExperimentType.ToString(), outline.VariableBeingTested,
                outline.ControlStrategy, outline.HowOutlineImplementsExperiment,
                DeserializeStrings(outline.ExperimentRisksJson)), outline.TotalEstimatedSeconds,
            outline.TransitionsRequireReview, stale, DeserializeStrings(outline.WarningsJson), sectionDtos,
            outline.CreatedAt, outline.UpdatedAt, outline.ApprovedAt);
    }

    public static async Task<bool> IsStaleAsync(IYoutubeAiFactoryStore store, VideoOutline outline,
        VideoProject videoProject, CancellationToken cancellationToken)
    {
        var latest = await store.GetLatestResearchReportAsync(outline.ProjectId, outline.VideoProjectId, cancellationToken);
        if (latest is null || latest.Report.Id != outline.ResearchReportId || latest.Report.Version != outline.ResearchReportVersion)
            return true;
        var project = await store.GetProjectAsync(outline.ProjectId, cancellationToken);
        var source = await store.GetVideoProjectSourceAsync(outline.ProjectId, videoProject.PilotId,
            videoProject.PilotVideoId, cancellationToken);
        return project is null || source is null ||
            !string.Equals(latest.Report.InputFingerprint, ResearchBriefBuilder.CreateFingerprint(
                ResearchBriefBuilder.Build(project, videoProject, source.Opportunity.Name)), StringComparison.Ordinal) ||
            !string.Equals(outline.InputFingerprint,
                OutlineGenerationContextBuilder.CreateFingerprint(project, videoProject, source, latest.Report),
                StringComparison.Ordinal);
    }

    private static string[] DeserializeStrings(string json) =>
        JsonSerializer.Deserialize<string[]>(json, OutlinePrompt.SerializerOptions) ?? [];
}
