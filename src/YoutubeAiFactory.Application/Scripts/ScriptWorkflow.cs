using System.Text.Json;
using Microsoft.Extensions.Logging;
using YoutubeAiFactory.Application.AI;
using YoutubeAiFactory.Application.Common;
using YoutubeAiFactory.Application.Outlines;
using YoutubeAiFactory.Application.Persistence;
using YoutubeAiFactory.Application.Research;
using YoutubeAiFactory.Domain.AI;
using YoutubeAiFactory.Domain.Common;
using YoutubeAiFactory.Domain.Jobs;
using YoutubeAiFactory.Domain.Research;
using YoutubeAiFactory.Domain.Scripts;
using YoutubeAiFactory.Domain.Videos;

namespace YoutubeAiFactory.Application.Scripts;

public sealed class RunVideoScriptHandler(IYoutubeAiFactoryStore store,
    ScriptGenerationContextBuilder contextBuilder, ScriptOptions options, TimeProvider timeProvider)
{
    public const string EngineVersion = "script-engine:v1";

    public async Task<RunVideoScriptResult> HandleAsync(Guid projectId, Guid videoProjectId,
        CancellationToken cancellationToken)
    {
        ScriptGenerationContextBuilder.ValidateOptions(options);
        var videoProject = await store.GetVideoProjectAsync(projectId, videoProjectId, true, cancellationToken)
            ?? throw new ResourceNotFoundException("Video project was not found.");
        var active = await store.GetActiveVideoScriptJobAsync(projectId, videoProjectId, cancellationToken);
        if (active is not null) return ToRun(active, true);
        if (videoProject.Status is not (VideoProjectStatus.OutlineApproved or VideoProjectStatus.ScriptReady))
            throw new ApplicationValidationException(
                $"Script generation requires an approved Outline and cannot start while the VideoProject is {videoProject.Status}.");

        var context = await LoadContextAsync(store, contextBuilder, projectId, videoProject, cancellationToken);
        var payload = new ScriptJobPayload(projectId, videoProjectId, ScriptJobOperation.Generate,
            context.VideoOutlineId, context.VideoOutlineVersion, context.ResearchReportId,
            context.ResearchReportVersion, context.InputFingerprint, null, videoProject.Status.ToString());
        var job = new Job("script-workflow", JsonSerializer.Serialize(payload, ScriptPrompt.SerializerOptions),
            timeProvider.GetUtcNow(), options.MaxJobRetries, projectId: projectId, videoProjectId: videoProjectId);
        videoProject.TransitionTo(VideoProjectStatus.ScriptGenerating, timeProvider.GetUtcNow());
        return ToRun(await store.EnqueueVideoScriptJobAsync(job, cancellationToken), false, job.Id);
    }

    internal static async Task<ScriptGenerationContext> LoadContextAsync(IYoutubeAiFactoryStore store,
        ScriptGenerationContextBuilder contextBuilder, Guid projectId, VideoProject videoProject,
        CancellationToken cancellationToken)
    {
        var project = await store.GetProjectAsync(projectId, cancellationToken)
            ?? throw new ResourceNotFoundException("Project was not found.");
        var source = await store.GetVideoProjectSourceAsync(projectId, videoProject.PilotId,
            videoProject.PilotVideoId, cancellationToken)
            ?? throw new ResourceNotFoundException("Video project source context was not found.");
        var outline = await store.GetApprovedVideoOutlineAsync(projectId, videoProject.Id, cancellationToken)
            ?? throw new ApplicationValidationException("Approve a VideoOutline before generating the script.");
        var research = await store.GetLatestResearchReportAsync(projectId, videoProject.Id, cancellationToken)
            ?? throw new ApplicationValidationException("The approved outline's ResearchReport is unavailable.");
        if (research.Report.Id != outline.Outline.ResearchReportId ||
            research.Report.Version != outline.Outline.ResearchReportVersion)
            throw new ApplicationValidationException(
                "The approved outline is based on outdated research. Review or regenerate the outline before generating the script.");
        return contextBuilder.Build(project, videoProject, source, outline, research);
    }

    private static RunVideoScriptResult ToRun(Job job, bool existing, Guid? proposedId = null) =>
        new(job.Id, job.Status.ToString(), existing || proposedId is { } id && id != job.Id);
}

public sealed class GetVideoScriptStatusHandler(IYoutubeAiFactoryStore store,
    ScriptGenerationContextBuilder contextBuilder)
{
    public async Task<ScriptStatusDto> HandleAsync(Guid projectId, Guid videoProjectId,
        CancellationToken cancellationToken)
    {
        var videoProject = await store.GetVideoProjectAsync(projectId, videoProjectId, false, cancellationToken)
            ?? throw new ResourceNotFoundException("Video project was not found.");
        var latest = await store.GetLatestVideoScriptAsync(projectId, videoProjectId, false, cancellationToken);
        var active = await store.GetActiveVideoScriptJobAsync(projectId, videoProjectId, cancellationToken);
        var latestJob = active ?? await store.GetLatestVideoScriptJobAsync(projectId, videoProjectId, cancellationToken);
        var blockReason = active is not null ? "A Script workflow is already in progress." :
            await GetBlockReasonAsync(store, contextBuilder, projectId, videoProject, cancellationToken);
        return new ScriptStatusDto(latest is null ? null : await ScriptDtoMapper.MapAsync(store, latest,
            videoProject, cancellationToken), ToJob(active), ToJob(latestJob), blockReason is null, blockReason);
    }

    internal static ScriptJobDto? ToJob(Job? job)
    {
        if (job is null) return null;
        var operation = JsonSerializer.Deserialize<ScriptJobPayload>(job.Payload, ScriptPrompt.SerializerOptions)?.Operation
            .ToString() ?? "Unknown";
        return new(job.Id, job.Status.ToString(), operation, job.FailureReason);
    }

    private static async Task<string?> GetBlockReasonAsync(IYoutubeAiFactoryStore store,
        ScriptGenerationContextBuilder contextBuilder, Guid projectId, VideoProject videoProject,
        CancellationToken cancellationToken)
    {
        if (videoProject.Status == VideoProjectStatus.ScriptApproved)
            return "The approved Script is immutable.";
        if (videoProject.Status is not (VideoProjectStatus.OutlineApproved or VideoProjectStatus.ScriptReady))
            return "An approved, current VideoOutline is required before Script generation.";
        try
        {
            await RunVideoScriptHandler.LoadContextAsync(store, contextBuilder, projectId, videoProject, cancellationToken);
            return null;
        }
        catch (YoutubeAiFactoryException exception) { return exception.Message; }
    }
}

public sealed class GetVideoScriptHandler(IYoutubeAiFactoryStore store)
{
    public async Task<VideoScriptDto> HandleAsync(Guid projectId, Guid videoProjectId, Guid? scriptId,
        CancellationToken cancellationToken)
    {
        var videoProject = await store.GetVideoProjectAsync(projectId, videoProjectId, false, cancellationToken)
            ?? throw new ResourceNotFoundException("Video project was not found.");
        var script = scriptId is { } id
            ? await store.GetVideoScriptAsync(projectId, videoProjectId, id, false, cancellationToken)
            : await store.GetLatestVideoScriptAsync(projectId, videoProjectId, false, cancellationToken);
        if (script is null) throw new ResourceNotFoundException("Video script was not found.");
        return await ScriptDtoMapper.MapAsync(store, script, videoProject, cancellationToken);
    }
}

public sealed class ListVideoScriptsHandler(IYoutubeAiFactoryStore store)
{
    public async Task<IReadOnlyList<VideoScriptHistoryItemDto>> HandleAsync(Guid projectId, Guid videoProjectId,
        CancellationToken cancellationToken)
    {
        var videoProject = await store.GetVideoProjectAsync(projectId, videoProjectId, false, cancellationToken)
            ?? throw new ResourceNotFoundException("Video project was not found.");
        var scripts = await store.ListVideoScriptsAsync(projectId, videoProjectId, cancellationToken);
        var result = new List<VideoScriptHistoryItemDto>(scripts.Count);
        foreach (var script in scripts)
            result.Add(new(script.Id, script.Version, script.Status.ToString(), script.GroundingStatus.ToString(),
                script.VideoOutlineVersion, script.ResearchReportVersion,
                await ScriptDtoMapper.IsStaleAsync(store, script, videoProject, cancellationToken),
                script.CreatedAt, script.ApprovedAt));
        return result;
    }
}

public sealed class UpdateVideoScriptHandler(IYoutubeAiFactoryStore store,
    ScriptGenerationContextBuilder contextBuilder, ScriptOptions options, TimeProvider timeProvider)
{
    public async Task<VideoScriptDto> HandleAsync(Guid projectId, Guid videoProjectId, Guid scriptId,
        UpdateVideoScriptRequest request, CancellationToken cancellationToken)
    {
        var videoProject = await store.GetVideoProjectAsync(projectId, videoProjectId, false, cancellationToken)
            ?? throw new ResourceNotFoundException("Video project was not found.");
        if (videoProject.Status != VideoProjectStatus.ScriptReady)
            throw new ApplicationValidationException("Only a Ready Script can be edited.");
        var details = await store.GetVideoScriptAsync(projectId, videoProjectId, scriptId, true, cancellationToken)
            ?? throw new ResourceNotFoundException("Video script was not found.");
        var latest = await store.GetLatestVideoScriptAsync(projectId, videoProjectId, false, cancellationToken);
        if (latest?.Script.Id != scriptId)
            throw new ApplicationValidationException("Only the latest Script candidate can be edited.");
        if (details.Script.Status != VideoScriptStatus.Ready)
            throw new ApplicationValidationException("An approved Script is immutable.");
        if (request.Blocks is null || request.Blocks.Count == 0 ||
            request.Blocks.Select(item => item.BlockId).Distinct().Count() != request.Blocks.Count)
            throw new ApplicationValidationException("At least one unique Script block edit is required.");
        if (await ScriptDtoMapper.IsStaleAsync(store, details.Script, videoProject, cancellationToken))
            throw new ApplicationValidationException("This Script is outdated. Generate a new version from the current approved Outline.");

        var blocks = details.Blocks.ToDictionary(item => item.Id);
        foreach (var edit in request.Blocks)
        {
            if (!blocks.TryGetValue(edit.BlockId, out var block))
                throw new ApplicationValidationException("An edited block does not belong to this Script.");
            var words = ScriptMetrics.CountWords(edit.Text);
            try { block.UpdateText(edit.Text, words); }
            catch (DomainException exception) { throw new ApplicationValidationException(exception.Message); }
        }
        foreach (var section in details.Sections)
        {
            var words = details.Blocks.Where(item => item.VideoScriptSectionId == section.Id).Sum(item => item.WordCount);
            section.SetMetrics(words, ScriptMetrics.EstimateDurationSeconds(words, options.PlanningWordsPerMinute));
        }
        var context = await RunVideoScriptHandler.LoadContextAsync(store, contextBuilder, projectId, videoProject,
            cancellationToken);
        var total = details.Blocks.Sum(item => item.WordCount);
        var warnings = LengthWarnings(total, context);
        details.Script.RecordNarrationEdit(total,
            ScriptMetrics.EstimateDurationSeconds(total, options.PlanningWordsPerMinute),
            JsonSerializer.Serialize(warnings, ScriptPrompt.SerializerOptions), timeProvider.GetUtcNow());
        await store.SaveChangesAsync(cancellationToken);
        var refreshed = await store.GetVideoScriptAsync(projectId, videoProjectId, scriptId, false, cancellationToken)
            ?? throw new ResourceNotFoundException("Video script was not found after editing.");
        return await ScriptDtoMapper.MapAsync(store, refreshed, videoProject, cancellationToken);
    }

    internal static string[] LengthWarnings(int totalWords, ScriptGenerationContext context)
    {
        var warnings = new List<string>();
        if (totalWords < context.MinimumWordCount)
            warnings.Add($"Script is shorter than the configured target range ({totalWords} words; minimum {context.MinimumWordCount}).");
        if (totalWords > context.MaximumWordCount)
            warnings.Add($"Script is longer than the configured target range ({totalWords} words; maximum {context.MaximumWordCount}).");
        return warnings.ToArray();
    }
}

public sealed class ValidateVideoScriptHandler(IYoutubeAiFactoryStore store,
    ScriptGenerationContextBuilder contextBuilder, ScriptOptions options, TimeProvider timeProvider)
{
    public async Task<RunVideoScriptResult> HandleAsync(Guid projectId, Guid videoProjectId, Guid scriptId,
        CancellationToken cancellationToken)
    {
        var videoProject = await store.GetVideoProjectAsync(projectId, videoProjectId, false, cancellationToken)
            ?? throw new ResourceNotFoundException("Video project was not found.");
        if (videoProject.Status != VideoProjectStatus.ScriptReady)
            throw new ApplicationValidationException("Only a Ready Script can be validated.");
        var active = await store.GetActiveVideoScriptJobAsync(projectId, videoProjectId, cancellationToken);
        if (active is not null) return new(active.Id, active.Status.ToString(), true);
        var script = await store.GetVideoScriptAsync(projectId, videoProjectId, scriptId, false, cancellationToken)
            ?? throw new ResourceNotFoundException("Video script was not found.");
        var latest = await store.GetLatestVideoScriptAsync(projectId, videoProjectId, false, cancellationToken);
        if (latest?.Script.Id != scriptId || script.Script.Status != VideoScriptStatus.Ready)
            throw new ApplicationValidationException("Only the latest Ready Script can be validated.");
        var context = await RunVideoScriptHandler.LoadContextAsync(store, contextBuilder, projectId, videoProject,
            cancellationToken);
        if (!string.Equals(script.Script.InputFingerprint, context.InputFingerprint, StringComparison.Ordinal))
            throw new ApplicationValidationException("This Script is outdated. Generate a new version from the current approved Outline.");
        var payload = new ScriptJobPayload(projectId, videoProjectId, ScriptJobOperation.Validate,
            context.VideoOutlineId, context.VideoOutlineVersion, context.ResearchReportId,
            context.ResearchReportVersion, context.InputFingerprint, scriptId, videoProject.Status.ToString());
        var job = new Job("script-workflow", JsonSerializer.Serialize(payload, ScriptPrompt.SerializerOptions),
            timeProvider.GetUtcNow(), options.MaxJobRetries, projectId: projectId, videoProjectId: videoProjectId);
        var persisted = await store.EnqueueVideoScriptJobAsync(job, cancellationToken);
        return new(persisted.Id, persisted.Status.ToString(), persisted.Id != job.Id);
    }
}

public sealed class ApproveVideoScriptHandler(IYoutubeAiFactoryStore store,
    ScriptGenerationContextBuilder contextBuilder, TimeProvider timeProvider)
{
    public async Task<VideoScriptDto> HandleAsync(Guid projectId, Guid videoProjectId, Guid scriptId,
        CancellationToken cancellationToken)
    {
        var videoProject = await store.GetVideoProjectAsync(projectId, videoProjectId, true, cancellationToken)
            ?? throw new ResourceNotFoundException("Video project was not found.");
        if (videoProject.Status != VideoProjectStatus.ScriptReady)
            throw new ApplicationValidationException("Only a ScriptReady VideoProject can approve a Script.");
        var details = await store.GetVideoScriptAsync(projectId, videoProjectId, scriptId, true, cancellationToken)
            ?? throw new ResourceNotFoundException("Video script was not found.");
        var latest = await store.GetLatestVideoScriptAsync(projectId, videoProjectId, false, cancellationToken);
        if (latest?.Script.Id != scriptId)
            throw new ApplicationValidationException("Only the latest applicable Script version can be approved.");
        var context = await RunVideoScriptHandler.LoadContextAsync(store, contextBuilder, projectId, videoProject,
            cancellationToken);
        if (!string.Equals(details.Script.InputFingerprint, context.InputFingerprint, StringComparison.Ordinal))
            throw new ApplicationValidationException("This Script is outdated. Generate a new version before approval.");
        ScriptPersistenceValidator.Validate(details, context);
        var now = timeProvider.GetUtcNow();
        try { details.Script.Approve(now); }
        catch (DomainException exception) { throw new ApplicationValidationException(exception.Message); }
        videoProject.TransitionTo(VideoProjectStatus.ScriptApproved, now);
        await store.SaveChangesAsync(cancellationToken);
        var refreshed = await store.GetVideoScriptAsync(projectId, videoProjectId, scriptId, false, cancellationToken)
            ?? throw new ResourceNotFoundException("Approved Script was not found.");
        return await ScriptDtoMapper.MapAsync(store, refreshed, videoProject, cancellationToken);
    }
}

public sealed partial class VideoScriptJobProcessor(IYoutubeAiFactoryStore store, ILlmProvider provider,
    IAiModelResolver modelResolver, ScriptGenerationContextBuilder contextBuilder, ScriptValidator validator,
    ScriptOptions options, TimeProvider timeProvider, ILogger<VideoScriptJobProcessor> logger,
    IVideoScriptJobLeaseRenewer? leaseRenewer = null)
{
    private static readonly Action<ILogger, Guid, Exception?> LogFailed = LoggerMessage.Define<Guid>(LogLevel.Warning,
        new EventId(2, nameof(LogFailed)), "Video Script job {JobId} failed.");
    private static readonly Action<ILogger, Guid, Exception?> LogLeaseLost = LoggerMessage.Define<Guid>(LogLevel.Error,
        new EventId(3, nameof(LogLeaseLost)), "Video Script job {JobId} lost ownership before completion.");

    [LoggerMessage(EventId = 1, Level = LogLevel.Information,
        Message = "Script workflow completed for Project {ProjectId}, VideoProject {VideoProjectId}, Outline {OutlineId}, Script {ScriptId}; operation {Operation}, version {ScriptVersion}, engine {EngineVersion}, words {WordCount}, estimated seconds {EstimatedSeconds}, grounding {GroundingStatus}, corrections {CorrectionAttempts}, duration {DurationMs} ms.")]
    private static partial void LogCompleted(ILogger logger, Guid projectId, Guid videoProjectId, Guid outlineId,
        Guid scriptId, string operation, int scriptVersion, string engineVersion, int wordCount,
        int estimatedSeconds, string groundingStatus, int correctionAttempts, long durationMs);

    public async Task<bool> ProcessNextAsync(CancellationToken cancellationToken)
    {
        ScriptGenerationContextBuilder.ValidateOptions(options);
        var now = timeProvider.GetUtcNow();
        var job = await store.TryClaimNextVideoScriptJobAsync(now,
            now.AddSeconds(-options.RunningJobLeaseSeconds), cancellationToken);
        if (job is null) return false;
        var payload = JsonSerializer.Deserialize<ScriptJobPayload>(job.Payload, ScriptPrompt.SerializerOptions)
            ?? throw new ApplicationValidationException("Video Script job payload is invalid.");
        var leaseId = job.LeaseId ?? throw new ApplicationValidationException("Video Script job has no ownership lease.");
        using var leaseLostCancellation = new CancellationTokenSource();
        using var heartbeatCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        using var executionCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken,
            leaseLostCancellation.Token);
        var heartbeat = leaseRenewer is null ? Task.CompletedTask : MaintainLeaseAsync(job.Id, leaseId,
            leaseLostCancellation, heartbeatCancellation.Token);
        AiRun? lastAiRun = null;
        try
        {
            var videoProject = await store.GetVideoProjectAsync(payload.ProjectId, payload.VideoProjectId, true,
                executionCancellation.Token)
                ?? throw new ResourceNotFoundException("The VideoProject for this Script job no longer exists.");
            var context = await RunVideoScriptHandler.LoadContextAsync(store, contextBuilder, payload.ProjectId,
                videoProject, executionCancellation.Token);
            ValidatePayload(payload, context);

            ProcessedScript processed;
            VideoScriptWithDetails? persisted = null;
            if (payload.Operation == ScriptJobOperation.Generate)
            {
                if (videoProject.Status != VideoProjectStatus.ScriptGenerating)
                    throw new ApplicationValidationException("Script generation job state is no longer runnable.");
                processed = await GenerateAndGroundAsync(context, job.Id, executionCancellation.Token);
                lastAiRun = processed.GroundingRun;
                persisted = await PersistGeneratedAsync(payload, videoProject, context, processed,
                    executionCancellation.Token);
            }
            else
            {
                if (videoProject.Status != VideoProjectStatus.ScriptReady || payload.VideoScriptId is null)
                    throw new ApplicationValidationException("Script validation job state is no longer runnable.");
                persisted = await store.GetVideoScriptAsync(payload.ProjectId, payload.VideoProjectId,
                    payload.VideoScriptId.Value, true, executionCancellation.Token)
                    ?? throw new ResourceNotFoundException("The Script selected for grounding validation no longer exists.");
                if (persisted.Script.Status != VideoScriptStatus.Ready ||
                    !string.Equals(persisted.Script.InputFingerprint, context.InputFingerprint, StringComparison.Ordinal))
                    throw new ApplicationValidationException("The Script selected for grounding validation is no longer current.");
                var result = ToResult(persisted);
                validator.ValidateGenerated(result, context);
                var audit = await AuditAsync(context, job.Id, result, executionCancellation.Token);
                lastAiRun = audit.Run;
                persisted.Script.RecordGroundingResult(audit.Result.Status, audit.Run.Id,
                    JsonSerializer.Serialize(audit.Result.Issues, ScriptPrompt.SerializerOptions), timeProvider.GetUtcNow());
                processed = new(result, validator.ValidateGenerated(result, context), audit.Run, audit.Result, 0,
                    persisted.Script.GenerationAiRunId, persisted.Script.Provider, persisted.Script.Model);
            }

            var completedAt = timeProvider.GetUtcNow();
            if (leaseRenewer is null)
            {
                job.Complete(completedAt);
                await store.SaveChangesAsync(executionCancellation.Token);
            }
            else if (!await store.CompleteVideoScriptJobAsync(job.Id, leaseId, completedAt,
                executionCancellation.Token))
            {
                leaseLostCancellation.Cancel();
                LogLeaseLost(logger, job.Id, null);
                return true;
            }

            if (logger.IsEnabled(LogLevel.Information))
            {
                var duration = job.StartedAt is null ? 0 : (long)(completedAt - job.StartedAt.Value).TotalMilliseconds;
                var operationName = payload.Operation.ToString();
                var groundingStatus = persisted.Script.GroundingStatus.ToString();
                LogCompleted(logger, payload.ProjectId, payload.VideoProjectId, payload.VideoOutlineId,
                    persisted.Script.Id, operationName, persisted.Script.Version,
                    RunVideoScriptHandler.EngineVersion, persisted.Script.TotalWordCount,
                    persisted.Script.EstimatedDurationSeconds, groundingStatus,
                    processed.CorrectionAttempts, duration);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            await store.RequeueVideoScriptJobAsync(job.Id, CancellationToken.None);
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
            await store.FailVideoScriptJobAsync(job.Id, lastAiRun?.Id,
                exception is YoutubeAiFactoryException ? exception.Message : "Script workflow could not be completed. Try again later.",
                retryable, failedAt,
                retryable ? failedAt.AddSeconds(Math.Pow(2, job.RetryCount + 1) * 5) : null,
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

    private async Task<ProcessedScript> GenerateAndGroundAsync(ScriptGenerationContext context, Guid jobId,
        CancellationToken cancellationToken)
    {
        var generated = await GenerateAsync(context, jobId, cancellationToken);
        var result = generated.Result;
        var contentRun = generated.Run;
        var contentProvider = generated.Provider;
        var contentModel = generated.Model;
        var metrics = validator.ValidateGenerated(result, context);
        var lengthCorrectionAttempts = 0;
        if (metrics.Warnings.Count > 0)
        {
            if (lengthCorrectionAttempts >= options.MaxLengthCorrectionAttempts)
                throw new StructuredOutputException("Script length is outside the configured target range.");
            var corrected = await CorrectAsync(context, jobId, result, [], string.Join(" ", metrics.Warnings),
                cancellationToken);
            result = corrected.Result;
            contentRun = corrected.Run;
            contentProvider = corrected.Provider;
            contentModel = corrected.Model;
            lengthCorrectionAttempts++;
            metrics = validator.ValidateGenerated(result, context);
            if (metrics.Warnings.Count > 0)
                throw new StructuredOutputException("Script remained outside the configured target range after bounded correction.");
        }

        var audit = await AuditAsync(context, jobId, result, cancellationToken);
        var groundingCorrectionAttempts = 0;
        while (audit.Result.Status == ScriptGroundingStatus.Failed &&
               groundingCorrectionAttempts < options.MaxGroundingCorrectionAttempts)
        {
            var corrected = await CorrectAsync(context, jobId, result, audit.Result.Issues, null, cancellationToken);
            result = corrected.Result;
            contentRun = corrected.Run;
            contentProvider = corrected.Provider;
            contentModel = corrected.Model;
            groundingCorrectionAttempts++;
            metrics = validator.ValidateGenerated(result, context);
            if (metrics.Warnings.Count > 0)
                throw new StructuredOutputException("Grounding correction moved the Script outside the configured length range.");
            audit = await AuditAsync(context, jobId, result, cancellationToken);
        }
        if (audit.Result.Status != ScriptGroundingStatus.Passed)
            throw new StructuredOutputException("Script grounding failed after the configured bounded correction attempts.");
        return new(result, metrics, audit.Run, audit.Result,
            lengthCorrectionAttempts + groundingCorrectionAttempts, contentRun.Id,
            contentProvider, contentModel);
    }

    private async Task<GeneratedScript> GenerateAsync(ScriptGenerationContext context, Guid jobId,
        CancellationToken cancellationToken)
    {
        var resolved = modelResolver.Resolve(AiWorkflowProfiles.ScriptGeneration);
        var run = StartRun("ScriptGeneration", context, jobId, resolved, ScriptPrompt.GenerationKey,
            ScriptPrompt.GenerationVersion);
        await store.SaveChangesAsync(cancellationToken);
        string? diagnostic = null;
        VideoScriptResult? previous = null;
        var repairAttempts = 0;
        try
        {
            for (var attempt = 0; attempt <= options.MaxGenerationRetries; attempt++)
            {
                try
                {
                    var answer = await provider.GenerateStructuredAsync<VideoScriptResult>(
                        ScriptPrompt.CreateGeneration(context, diagnostic, previous).WithResolvedModel(resolved),
                        cancellationToken);
                    previous = answer.Value;
                    validator.ValidateGenerated(answer.Value, context);
                    CompleteRun(run, answer);
                    await store.SaveChangesAsync(cancellationToken);
                    return new(answer.Value, run, answer.Provider, answer.Model);
                }
                catch (StructuredOutputException exception) when (!string.IsNullOrWhiteSpace(exception.RawOutput) &&
                    repairAttempts < options.MaxStructuredRepairAttempts)
                {
                    repairAttempts++;
                    try
                    {
                        var repaired = await RepairAsync(context, jobId, exception.RawOutput!, exception.Message,
                            cancellationToken);
                        validator.ValidateGenerated(repaired.Result, context);
                        run.RecordProvider(resolved.Provider, resolved.Model);
                        run.Complete(null, null, null, timeProvider.GetUtcNow());
                        await store.SaveChangesAsync(cancellationToken);
                        return repaired;
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
                    throw new StructuredOutputException(diagnostic ?? "Script output did not satisfy the approved Outline contract.");
                run.RecordRetry();
                await store.SaveChangesAsync(cancellationToken);
            }
            throw new StructuredOutputException("Script output did not satisfy the required contract.");
        }
        catch (Exception exception)
        {
            await FailRunAsync(run, exception, "Script generation AI stage failed.");
            throw;
        }
    }

    private async Task<GeneratedScript> CorrectAsync(ScriptGenerationContext context, Guid jobId, VideoScriptResult script,
        IReadOnlyList<ScriptGroundingIssueResult> issues, string? lengthDiagnostic,
        CancellationToken cancellationToken)
    {
        var resolved = modelResolver.Resolve(AiWorkflowProfiles.ScriptGroundingCorrection);
        var run = StartRun("ScriptGroundingCorrection", context, jobId, resolved, ScriptPrompt.CorrectionKey,
            ScriptPrompt.CorrectionVersion);
        await store.SaveChangesAsync(cancellationToken);
        try
        {
            var answer = await provider.GenerateStructuredAsync<VideoScriptResult>(
                ScriptPrompt.CreateCorrection(context, script, issues, lengthDiagnostic).WithResolvedModel(resolved),
                cancellationToken);
            validator.ValidateGenerated(answer.Value, context);
            CompleteRun(run, answer);
            await store.SaveChangesAsync(cancellationToken);
            return new(answer.Value, run, answer.Provider, answer.Model);
        }
        catch (Exception exception)
        {
            await FailRunAsync(run, exception, "Script grounding correction failed.");
            throw;
        }
    }

    private async Task<AuditedScript> AuditAsync(ScriptGenerationContext context, Guid jobId, VideoScriptResult script,
        CancellationToken cancellationToken)
    {
        var resolved = modelResolver.Resolve(AiWorkflowProfiles.ScriptGroundingAudit);
        var run = StartRun("ScriptGroundingAudit", context, jobId, resolved, ScriptPrompt.AuditKey,
            ScriptPrompt.AuditVersion);
        await store.SaveChangesAsync(cancellationToken);
        try
        {
            var answer = await provider.GenerateStructuredAsync<ScriptGroundingAuditResult>(
                ScriptPrompt.CreateAudit(context, script).WithResolvedModel(resolved), cancellationToken);
            ScriptValidator.ValidateAudit(answer.Value, script, context);
            CompleteRun(run, answer);
            await store.SaveChangesAsync(cancellationToken);
            return new(answer.Value, run);
        }
        catch (Exception exception)
        {
            await FailRunAsync(run, exception, "Script grounding audit failed.");
            throw;
        }
    }

    private async Task<GeneratedScript> RepairAsync(ScriptGenerationContext context, Guid jobId,
        string malformedOutput, string diagnostic, CancellationToken cancellationToken)
    {
        var resolved = modelResolver.Resolve(AiWorkflowProfiles.StructuredOutputRepair);
        var run = StartRun("StructuredOutputRepair", context, jobId, resolved, ScriptPrompt.RepairKey,
            ScriptPrompt.RepairVersion);
        await store.SaveChangesAsync(cancellationToken);
        try
        {
            var answer = await provider.GenerateStructuredAsync<VideoScriptResult>(
                ScriptPrompt.CreateRepair(malformedOutput, diagnostic).WithResolvedModel(resolved), cancellationToken);
            validator.ValidateGenerated(answer.Value, context);
            CompleteRun(run, answer);
            await store.SaveChangesAsync(cancellationToken);
            return new(answer.Value, run, answer.Provider, answer.Model);
        }
        catch (Exception exception)
        {
            await FailRunAsync(run, exception, "Structured Script repair failed.");
            throw;
        }
    }

    private AiRun StartRun(string workflow, ScriptGenerationContext context, Guid jobId, ResolvedAiModel resolved,
        string promptKey, int promptVersion)
    {
        var run = new AiRun(workflow, context.ProjectId, resolved.Provider, resolved.Model, promptKey,
            promptVersion, timeProvider.GetUtcNow(), resolved.Profile.ToString(), context.VideoProjectId,
            researchReportId: context.ResearchReportId, jobId: jobId, workflowStage: workflow);
        store.AddAiRun(run);
        return run;
    }

    private void CompleteRun<T>(AiRun run, LlmResult<T> answer)
    {
        run.RecordProvider(answer.Provider, answer.Model);
        run.CompleteFrom(answer, timeProvider.GetUtcNow());
    }

    private async Task FailRunAsync(AiRun run, Exception exception, string fallback)
    {
        if (run.Status == AiRunStatus.Running)
        {
            run.Fail(exception is YoutubeAiFactoryException ? exception.Message : fallback,
                timeProvider.GetUtcNow());
            await store.SaveChangesAsync(CancellationToken.None);
        }
    }

    private async Task<VideoScriptWithDetails> PersistGeneratedAsync(ScriptJobPayload payload,
        VideoProject videoProject, ScriptGenerationContext context, ProcessedScript processed,
        CancellationToken cancellationToken)
    {
        var version = await store.GetNextVideoScriptVersionAsync(payload.ProjectId, payload.VideoProjectId,
            cancellationToken);
        var script = new VideoScript(payload.ProjectId, payload.VideoProjectId, payload.VideoOutlineId,
            payload.VideoOutlineVersion, payload.ResearchReportId, payload.ResearchReportVersion, version,
            processed.GenerationRunId, processed.GroundingRun.Id, RunVideoScriptHandler.EngineVersion,
            ScriptPrompt.GenerationKey, ScriptPrompt.GenerationVersion, context.InputFingerprint,
            processed.Provider, processed.Model, context.ContentLanguage, processed.Metrics.TotalWordCount,
            processed.Metrics.EstimatedDurationSeconds,
            JsonSerializer.Serialize(processed.Metrics.Warnings, ScriptPrompt.SerializerOptions),
            JsonSerializer.Serialize(processed.Audit.Issues, ScriptPrompt.SerializerOptions),
            timeProvider.GetUtcNow());
        store.AddVideoScript(script);
        var sections = new List<VideoScriptSection>();
        var blocks = new List<VideoScriptBlock>();
        var claims = new List<VideoScriptBlockClaim>();
        var conflicts = new List<VideoScriptBlockConflict>();
        var contextSections = context.Sections.ToDictionary(item => item.Sequence);
        foreach (var generatedSection in processed.Result.Sections.OrderBy(item => item.Sequence))
        {
            var contextSection = contextSections[generatedSection.Sequence];
            var sectionWords = processed.Metrics.SectionWordCounts[generatedSection.Sequence];
            var section = new VideoScriptSection(script.Id, generatedSection.OutlineSectionId,
                generatedSection.Sequence, contextSection.Heading, sectionWords,
                ScriptMetrics.EstimateDurationSeconds(sectionWords, options.PlanningWordsPerMinute));
            store.AddVideoScriptSection(section);
            sections.Add(section);
            foreach (var generatedBlock in generatedSection.Blocks.OrderBy(item => item.Sequence))
            {
                var block = new VideoScriptBlock(section.Id, generatedBlock.Sequence, generatedBlock.Type,
                    generatedBlock.Text, processed.Metrics.BlockWordCounts[(generatedSection.Sequence,
                        generatedBlock.Sequence)]);
                store.AddVideoScriptBlock(block);
                blocks.Add(block);
                foreach (var claimId in generatedBlock.ClaimIds)
                {
                    var link = new VideoScriptBlockClaim(block.Id, claimId);
                    store.AddVideoScriptBlockClaim(link);
                    claims.Add(link);
                }
                foreach (var conflictId in generatedBlock.ConflictIds)
                {
                    var link = new VideoScriptBlockConflict(block.Id, conflictId);
                    store.AddVideoScriptBlockConflict(link);
                    conflicts.Add(link);
                }
            }
        }
        videoProject.TransitionTo(VideoProjectStatus.ScriptReady, timeProvider.GetUtcNow());
        return new(script, sections, blocks, claims, conflicts);
    }

    private async Task MaintainLeaseAsync(Guid jobId, Guid leaseId,
        CancellationTokenSource leaseLostCancellation, CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(Math.Max(5,
            options.RunningJobLeaseSeconds / 3d)));
        try
        {
            while (await timer.WaitForNextTickAsync(cancellationToken))
            {
                if (await leaseRenewer!.RenewAsync(jobId, leaseId, timeProvider.GetUtcNow(), cancellationToken))
                    continue;
                leaseLostCancellation.Cancel();
                LogLeaseLost(logger, jobId, null);
                return;
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
        catch (Exception exception)
        {
            leaseLostCancellation.Cancel();
            LogLeaseLost(logger, jobId, exception);
        }
    }

    private static void ValidatePayload(ScriptJobPayload payload, ScriptGenerationContext context)
    {
        if (payload.VideoOutlineId != context.VideoOutlineId ||
            payload.VideoOutlineVersion != context.VideoOutlineVersion ||
            payload.ResearchReportId != context.ResearchReportId ||
            payload.ResearchReportVersion != context.ResearchReportVersion ||
            !string.Equals(payload.InputFingerprint, context.InputFingerprint, StringComparison.Ordinal))
            throw new ApplicationValidationException(
                "Script inputs changed after the job was queued. Queue a new Script from the current approved Outline.");
    }

    private static VideoScriptResult ToResult(VideoScriptWithDetails details)
    {
        var blocksBySection = details.Blocks.GroupBy(item => item.VideoScriptSectionId)
            .ToDictionary(group => group.Key, group => group.OrderBy(item => item.Sequence).ToArray());
        var claimsByBlock = details.BlockClaims.GroupBy(item => item.ScriptBlockId)
            .ToDictionary(group => group.Key, group => group.Select(item => item.ResearchClaimId).ToArray());
        var conflictsByBlock = details.BlockConflicts.GroupBy(item => item.ScriptBlockId)
            .ToDictionary(group => group.Key, group => group.Select(item => item.ResearchConflictId).ToArray());
        var sections = details.Sections.OrderBy(item => item.Sequence).Select(section =>
            new ScriptSectionResult(section.VideoOutlineSectionId, section.Sequence,
                blocksBySection.GetValueOrDefault(section.Id, []).Select(block => new ScriptBlockResult(
                    block.Sequence, block.Type, block.Text, claimsByBlock.GetValueOrDefault(block.Id, []),
                    conflictsByBlock.GetValueOrDefault(block.Id, []))).ToArray())).ToArray();
        var lastBlocks = sections.Length == 0 ? null : sections[^1].Blocks;
        var closing = lastBlocks is null || lastBlocks.Count == 0 ? "Narrative payoff." : lastBlocks[^1].Text;
        return new(sections, closing);
    }

    private sealed record GeneratedScript(VideoScriptResult Result, AiRun Run, string Provider, string Model);
    private sealed record AuditedScript(ScriptGroundingAuditResult Result, AiRun Run);
    private sealed record ProcessedScript(VideoScriptResult Result, ScriptValidationMetrics Metrics,
        AiRun GroundingRun, ScriptGroundingAuditResult Audit, int CorrectionAttempts,
        Guid GenerationRunId, string Provider, string Model);
}

internal static class ScriptPersistenceValidator
{
    public static void Validate(VideoScriptWithDetails details, ScriptGenerationContext context)
    {
        var script = details.Script;
        if (script.ProjectId != context.ProjectId || script.VideoProjectId != context.VideoProjectId ||
            script.VideoOutlineId != context.VideoOutlineId || script.VideoOutlineVersion != context.VideoOutlineVersion ||
            script.ResearchReportId != context.ResearchReportId ||
            script.ResearchReportVersion != context.ResearchReportVersion)
            throw new ApplicationValidationException("Script source lineage does not match its approved inputs.");
        if (details.Sections.Count != context.Sections.Count)
            throw new ApplicationValidationException("Script must retain every approved Outline section.");
        if (!details.Sections.Select(item => item.Sequence).Order().SequenceEqual(
            Enumerable.Range(1, details.Sections.Count)))
            throw new ApplicationValidationException("Script section order is invalid.");
        var expected = context.Sections.ToDictionary(item => item.Sequence);
        var claimLinks = details.BlockClaims.GroupBy(item => item.ScriptBlockId)
            .ToDictionary(group => group.Key, group => group.Select(item => item.ResearchClaimId).ToArray());
        var conflictLinks = details.BlockConflicts.GroupBy(item => item.ScriptBlockId)
            .ToDictionary(group => group.Key, group => group.Select(item => item.ResearchConflictId).ToArray());
        var totalWords = 0;
        foreach (var section in details.Sections)
        {
            if (!expected.TryGetValue(section.Sequence, out var expectedSection) ||
                expectedSection.OutlineSectionId != section.VideoOutlineSectionId)
                throw new ApplicationValidationException("Script section lineage is invalid.");
            var blocks = details.Blocks.Where(item => item.VideoScriptSectionId == section.Id)
                .OrderBy(item => item.Sequence).ToArray();
            if (blocks.Length == 0 ||
                !blocks.Select(item => item.Sequence).SequenceEqual(Enumerable.Range(1, blocks.Length)))
                throw new ApplicationValidationException("Script block order is invalid.");
            var expectedClaims = expectedSection.Claims.ToDictionary(item => item.Id);
            var expectedConflicts = expectedSection.Conflicts.ToDictionary(item => item.Id);
            var sectionWords = 0;
            foreach (var block in blocks)
            {
                var blockClaims = claimLinks.GetValueOrDefault(block.Id, []);
                var blockConflicts = conflictLinks.GetValueOrDefault(block.Id, []);
                if (block.Type is ScriptBlockType.FactualNarration or ScriptBlockType.ConflictExplanation or
                        ScriptBlockType.Quote && blockClaims.Length == 0)
                    throw new ApplicationValidationException("Factual Script blocks must retain Claim traceability.");
                foreach (var claimId in blockClaims)
                {
                    if (!expectedClaims.TryGetValue(claimId, out var claim) ||
                        claim.SupportStatus == ResearchClaimSupportStatus.Unsupported.ToString())
                        throw new ApplicationValidationException(
                            "Script contains a Claim outside its approved Outline section.");
                    if (claim.SupportStatus == ResearchClaimSupportStatus.Conflicted.ToString() &&
                        !blockConflicts.Any(id => expectedConflicts.TryGetValue(id, out var conflict) &&
                            conflict.ClaimId == claimId))
                        throw new ApplicationValidationException(
                            "A conflicted Claim must retain its conflict traceability.");
                }
                if (blockConflicts.Any(id => !expectedConflicts.TryGetValue(id, out var conflict) ||
                    !blockClaims.Contains(conflict.ClaimId)))
                    throw new ApplicationValidationException("Script contains an invalid conflict relationship.");
                var actualWords = ScriptMetrics.CountWords(block.Text);
                if (actualWords != block.WordCount)
                    throw new ApplicationValidationException("Script block word metrics are inconsistent.");
                sectionWords += actualWords;
            }
            if (sectionWords != section.WordCount)
                throw new ApplicationValidationException("Script section word metrics are inconsistent.");
            totalWords += sectionWords;
        }
        if (totalWords != script.TotalWordCount)
            throw new ApplicationValidationException("Script total word metrics are inconsistent.");
        if (script.GroundingStatus != ScriptGroundingStatus.Passed)
            throw new ApplicationValidationException("Script grounding must pass before approval.");
    }
}
