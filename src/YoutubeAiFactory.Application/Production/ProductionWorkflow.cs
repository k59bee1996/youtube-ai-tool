using System.Text.Json;
using Microsoft.Extensions.Logging;
using YoutubeAiFactory.Application.AI;
using YoutubeAiFactory.Application.Common;
using YoutubeAiFactory.Application.Persistence;
using YoutubeAiFactory.Application.Scripts;
using YoutubeAiFactory.Domain.AI;
using YoutubeAiFactory.Domain.Common;
using YoutubeAiFactory.Domain.Jobs;
using YoutubeAiFactory.Domain.Production;
using YoutubeAiFactory.Domain.Videos;

namespace YoutubeAiFactory.Application.Production;

public sealed class RunProductionPackageHandler(
    IYoutubeAiFactoryStore store,
    ProductionContextBuilder contextBuilder,
    ProductionOptions options,
    TimeProvider timeProvider
)
{
    public const string EngineVersion = "production-package-engine:v1";

    public async Task<RunProductionPackageResult> HandleAsync(
        Guid projectId,
        Guid videoProjectId,
        CancellationToken ct
    )
    {
        ProductionValidator.ValidateOptions(options);
        var videoProject =
            await store.GetVideoProjectAsync(projectId, videoProjectId, true, ct)
            ?? throw new ResourceNotFoundException("Video project was not found.");
        var active = await store.GetActiveProductionPackageJobAsync(projectId, videoProjectId, ct);
        if (active is not null)
            return Run(active, true);
        if (
            videoProject.Status
            is not (VideoProjectStatus.ScriptApproved or VideoProjectStatus.Packaging)
        )
            throw new ApplicationValidationException(
                "Production package generation requires an approved Script."
            );
        var context = await LoadContextAsync(store, contextBuilder, projectId, videoProject, ct);
        var returnStatus = videoProject.Status.ToString();
        var payload = new ProductionJobPayload(
            projectId,
            videoProjectId,
            ProductionJobOperation.Generate,
            context.VideoScriptId,
            context.VideoScriptVersion,
            context.InputFingerprint,
            null,
            returnStatus
        );
        var job = new Job(
            "production-package",
            JsonSerializer.Serialize(payload, ProductionPrompt.SerializerOptions),
            timeProvider.GetUtcNow(),
            options.MaxJobRetries,
            projectId: projectId,
            videoProjectId: videoProjectId
        );
        if (videoProject.Status == VideoProjectStatus.ScriptApproved)
            videoProject.TransitionTo(VideoProjectStatus.Packaging, timeProvider.GetUtcNow());
        var persisted = await store.EnqueueProductionPackageJobAsync(job, ct);
        return Run(persisted, persisted.Id != job.Id);
    }

    internal static async Task<ProductionGenerationContext> LoadContextAsync(
        IYoutubeAiFactoryStore store,
        ProductionContextBuilder builder,
        Guid projectId,
        VideoProject vp,
        CancellationToken ct
    )
    {
        var project =
            await store.GetProjectAsync(projectId, ct)
            ?? throw new ResourceNotFoundException("Project was not found.");
        var source =
            await store.GetVideoProjectSourceAsync(projectId, vp.PilotId, vp.PilotVideoId, ct)
            ?? throw new ResourceNotFoundException("Video project source was not found.");
        var script =
            await store.GetApprovedVideoScriptAsync(projectId, vp.Id, ct)
            ?? throw new ApplicationValidationException(
                "Approve a Script before generating a production package."
            );
        if (await ScriptDtoMapper.IsStaleAsync(store, script.Script, vp, ct))
            throw new ApplicationValidationException(
                "The approved Script is stale. Regenerate the upstream artifact first."
            );
        var research =
            await store.GetResearchReportAsync(projectId, vp.Id, script.Script.ResearchReportId, ct)
            ?? throw new ApplicationValidationException(
                "The approved Script's ResearchReport is unavailable."
            );
        return builder.Build(project, vp, source, script, research);
    }

    private static RunProductionPackageResult Run(Job j, bool existing) =>
        new(j.Id, j.Status.ToString(), existing);
}

public sealed class GetProductionPackageStatusHandler(
    IYoutubeAiFactoryStore store,
    ProductionContextBuilder builder
)
{
    public async Task<ProductionPackageStatusDto> HandleAsync(
        Guid projectId,
        Guid videoProjectId,
        CancellationToken ct
    )
    {
        var vp =
            await store.GetVideoProjectAsync(projectId, videoProjectId, false, ct)
            ?? throw new ResourceNotFoundException("Video project was not found.");
        var latest = await store.GetLatestProductionPackageAsync(
            projectId,
            videoProjectId,
            false,
            ct
        );
        var active = await store.GetActiveProductionPackageJobAsync(projectId, videoProjectId, ct);
        var last =
            active ?? await store.GetLatestProductionPackageJobAsync(projectId, videoProjectId, ct);
        string? reason = null;
        if (active is not null)
            reason = "A production package workflow is already in progress.";
        else if (vp.Status == VideoProjectStatus.ProductionReady)
            reason = "The approved production package is immutable.";
        else if (
            vp.Status is not (VideoProjectStatus.ScriptApproved or VideoProjectStatus.Packaging)
        )
            reason = "An approved, current Script is required.";
        else
            try
            {
                await RunProductionPackageHandler.LoadContextAsync(
                    store,
                    builder,
                    projectId,
                    vp,
                    ct
                );
            }
            catch (YoutubeAiFactoryException ex)
            {
                reason = ex.Message;
            }
        return new(
            latest is null ? null : await ProductionDtoMapper.MapAsync(store, latest, vp, ct),
            Job(active),
            Job(last),
            reason is null,
            reason
        );
    }

    internal static ProductionJobDto? Job(Job? j)
    {
        if (j is null)
            return null;
        var op =
            JsonSerializer
                .Deserialize<ProductionJobPayload>(j.Payload, ProductionPrompt.SerializerOptions)
                ?.Operation.ToString()
            ?? "Unknown";
        return new(j.Id, j.Status.ToString(), op, j.FailureReason);
    }
}

public sealed class GetProductionPackageHandler(IYoutubeAiFactoryStore store)
{
    public async Task<ProductionPackageDto> HandleAsync(
        Guid projectId,
        Guid videoProjectId,
        Guid? id,
        CancellationToken ct
    )
    {
        var vp =
            await store.GetVideoProjectAsync(projectId, videoProjectId, false, ct)
            ?? throw new ResourceNotFoundException("Video project was not found.");
        var p = id is Guid packageId
            ? await store.GetProductionPackageAsync(projectId, videoProjectId, packageId, false, ct)
            : await store.GetLatestProductionPackageAsync(projectId, videoProjectId, false, ct);
        return p is null
            ? throw new ResourceNotFoundException("Production package was not found.")
            : await ProductionDtoMapper.MapAsync(store, p, vp, ct);
    }
}

public sealed class ListProductionPackagesHandler(IYoutubeAiFactoryStore store)
{
    public async Task<IReadOnlyList<ProductionPackageHistoryItemDto>> HandleAsync(
        Guid projectId,
        Guid videoProjectId,
        CancellationToken ct
    )
    {
        var vp =
            await store.GetVideoProjectAsync(projectId, videoProjectId, false, ct)
            ?? throw new ResourceNotFoundException("Video project was not found.");
        var list = await store.ListProductionPackagesAsync(projectId, videoProjectId, ct);
        var result = new List<ProductionPackageHistoryItemDto>();
        foreach (var p in list)
            result.Add(
                new(
                    p.Id,
                    p.Version,
                    p.Status.ToString(),
                    p.GroundingStatus.ToString(),
                    p.VideoScriptVersion,
                    await ProductionDtoMapper.IsStaleAsync(store, p, vp, ct),
                    p.CreatedAt,
                    p.ApprovedAt
                )
            );
        return result;
    }
}

public sealed class UpdateProductionPackageHandler(
    IYoutubeAiFactoryStore store,
    TimeProvider timeProvider
)
{
    public async Task<ProductionPackageDto> HandleAsync(
        Guid projectId,
        Guid videoProjectId,
        Guid packageId,
        UpdateProductionPackageRequest request,
        CancellationToken ct
    )
    {
        var vp =
            await store.GetVideoProjectAsync(projectId, videoProjectId, false, ct)
            ?? throw new ResourceNotFoundException("Video project was not found.");
        if (vp.Status != VideoProjectStatus.Packaging)
            throw new ApplicationValidationException(
                "Only a Ready production package can be edited."
            );
        if (
            await store.GetActiveProductionPackageJobAsync(projectId, videoProjectId, ct)
            is not null
        )
            throw new ResourceConflictException(
                "Wait for the active production package workflow to finish."
            );
        var p =
            await store.GetProductionPackageAsync(projectId, videoProjectId, packageId, true, ct)
            ?? throw new ResourceNotFoundException("Production package was not found.");
        var latest = await store.GetLatestProductionPackageAsync(
            projectId,
            videoProjectId,
            false,
            ct
        );
        if (latest?.Package.Id != packageId || p.Package.Status != ProductionPackageStatus.Ready)
            throw new ApplicationValidationException(
                "Only the latest Ready production package can be edited."
            );
        if (await ProductionDtoMapper.IsStaleAsync(store, p.Package, vp, ct))
            throw new ApplicationValidationException("This production package is stale.");
        try
        {
            var scenes = p.Scenes.ToDictionary(x => x.Id);
            foreach (var e in request.Scenes ?? [])
            {
                if (!scenes.TryGetValue(e.SceneId, out var x))
                    throw new ApplicationValidationException(
                        "Edited scene does not belong to the package."
                    );
                x.Update(
                    e.NarrationSummary,
                    e.VisualStrategy,
                    e.TransitionIntent,
                    e.MusicBrief,
                    e.SoundEffectCue,
                    e.VoiceDirection
                );
            }
            var shots = p.Shots.ToDictionary(x => x.Id);
            foreach (var e in request.Shots ?? [])
            {
                if (!shots.TryGetValue(e.ShotId, out var x))
                    throw new ApplicationValidationException(
                        "Edited shot does not belong to the package."
                    );
                x.Update(e.VisualDescription, e.Composition, e.MotionSuggestion, e.Notes);
            }
            var assets = p.Assets.ToDictionary(x => x.Id);
            foreach (var e in request.Assets ?? [])
            {
                if (!assets.TryGetValue(e.AssetId, out var x))
                    throw new ApplicationValidationException(
                        "Edited asset does not belong to the package."
                    );
                x.Update(e.CreativeBrief, e.GenerationPrompt, e.SourceSearchBrief);
            }
            var texts = p.OnScreenText.ToDictionary(x => x.Id);
            foreach (var e in request.OnScreenText ?? [])
            {
                if (!texts.TryGetValue(e.OnScreenTextId, out var x))
                    throw new ApplicationValidationException(
                        "Edited on-screen text does not belong to the package."
                    );
                x.Update(e.Text, e.TimingIntent);
            }
            p.Package.RecordEdit(
                request.VisualDirection,
                request.PacingDirection,
                request.ColorDirection,
                request.TypographyDirection,
                request.AudioDirection,
                request.ExperimentProductionNotes,
                timeProvider.GetUtcNow()
            );
        }
        catch (DomainException ex)
        {
            throw new ApplicationValidationException(ex.Message);
        }
        await store.SaveChangesAsync(ct);
        return await ProductionDtoMapper.MapAsync(store, p, vp, ct);
    }
}

public sealed class ValidateProductionPackageHandler(
    IYoutubeAiFactoryStore store,
    ProductionContextBuilder builder,
    ProductionOptions options,
    TimeProvider timeProvider
)
{
    public async Task<RunProductionPackageResult> HandleAsync(
        Guid projectId,
        Guid videoProjectId,
        Guid packageId,
        CancellationToken ct
    )
    {
        var vp =
            await store.GetVideoProjectAsync(projectId, videoProjectId, false, ct)
            ?? throw new ResourceNotFoundException("Video project was not found.");
        if (vp.Status != VideoProjectStatus.Packaging)
            throw new ApplicationValidationException(
                "Only a package in Packaging can be validated."
            );
        var active = await store.GetActiveProductionPackageJobAsync(projectId, videoProjectId, ct);
        if (active is not null)
            return new(active.Id, active.Status.ToString(), true);
        var p =
            await store.GetProductionPackageAsync(projectId, videoProjectId, packageId, false, ct)
            ?? throw new ResourceNotFoundException("Production package was not found.");
        var latest = await store.GetLatestProductionPackageAsync(
            projectId,
            videoProjectId,
            false,
            ct
        );
        if (latest?.Package.Id != packageId || p.Package.Status != ProductionPackageStatus.Ready)
            throw new ApplicationValidationException(
                "Only the latest Ready package can be validated."
            );
        var context = await RunProductionPackageHandler.LoadContextAsync(
            store,
            builder,
            projectId,
            vp,
            ct
        );
        if (p.Package.InputFingerprint != context.InputFingerprint)
            throw new ApplicationValidationException("This production package is stale.");
        var payload = new ProductionJobPayload(
            projectId,
            videoProjectId,
            ProductionJobOperation.Validate,
            context.VideoScriptId,
            context.VideoScriptVersion,
            context.InputFingerprint,
            packageId,
            vp.Status.ToString()
        );
        var job = new Job(
            "production-package",
            JsonSerializer.Serialize(payload, ProductionPrompt.SerializerOptions),
            timeProvider.GetUtcNow(),
            options.MaxJobRetries,
            projectId: projectId,
            videoProjectId: videoProjectId
        );
        var persisted = await store.EnqueueProductionPackageJobAsync(job, ct);
        return new(persisted.Id, persisted.Status.ToString(), persisted.Id != job.Id);
    }
}

public sealed class ApproveProductionPackageHandler(
    IYoutubeAiFactoryStore store,
    ProductionContextBuilder builder,
    ProductionValidator validator,
    TimeProvider timeProvider
)
{
    public async Task<ProductionPackageDto> HandleAsync(
        Guid projectId,
        Guid videoProjectId,
        Guid packageId,
        CancellationToken ct
    )
    {
        var vp =
            await store.GetVideoProjectAsync(projectId, videoProjectId, true, ct)
            ?? throw new ResourceNotFoundException("Video project was not found.");
        if (vp.Status != VideoProjectStatus.Packaging)
            throw new ApplicationValidationException(
                "Only a Packaging VideoProject can approve a package."
            );
        if (
            await store.GetActiveProductionPackageJobAsync(projectId, videoProjectId, ct)
            is not null
        )
            throw new ResourceConflictException(
                "Wait for the active production package workflow to finish."
            );
        var p =
            await store.GetProductionPackageAsync(projectId, videoProjectId, packageId, true, ct)
            ?? throw new ResourceNotFoundException("Production package was not found.");
        var latest = await store.GetLatestProductionPackageAsync(
            projectId,
            videoProjectId,
            false,
            ct
        );
        if (latest?.Package.Id != packageId)
            throw new ApplicationValidationException("Only the latest package can be approved.");
        var context = await RunProductionPackageHandler.LoadContextAsync(
            store,
            builder,
            projectId,
            vp,
            ct
        );
        if (p.Package.InputFingerprint != context.InputFingerprint)
            throw new ApplicationValidationException("This production package is stale.");
        ProductionPersistenceValidator.Validate(p, context, validator);
        if (p.Package.GroundingStatus != ProductionGroundingStatus.Passed)
            throw new ApplicationValidationException(
                "Production grounding must pass before approval."
            );
        var now = timeProvider.GetUtcNow();
        p.Package.Approve(now);
        vp.TransitionTo(VideoProjectStatus.ProductionReady, now);
        await store.SaveChangesAsync(ct);
        return await ProductionDtoMapper.MapAsync(store, p, vp, ct);
    }
}

public sealed class ExportProductionPackageHandler(
    IYoutubeAiFactoryStore store,
    TimeProvider timeProvider
)
{
    public async Task<ProductionExport> HandleAsync(
        Guid projectId,
        Guid videoProjectId,
        CancellationToken ct
    )
    {
        var vp =
            await store.GetVideoProjectAsync(projectId, videoProjectId, false, ct)
            ?? throw new ResourceNotFoundException("Video project was not found.");
        var p =
            await store.GetApprovedProductionPackageAsync(projectId, videoProjectId, ct)
            ?? throw new ApplicationValidationException(
                "Approve a production package before export."
            );
        var dto = await ProductionDtoMapper.MapAsync(store, p, vp, ct);
        if (dto.IsStale)
            throw new ApplicationValidationException(
                "The approved production package is stale and cannot be exported."
            );
        return new(
            "production-package-export:v1",
            timeProvider.GetUtcNow(),
            projectId,
            videoProjectId,
            dto.Id,
            dto.Version,
            dto.VideoScriptId,
            dto.VideoScriptVersion,
            dto.ContentLanguage,
            dto.EstimatedDurationSeconds,
            dto.VisualDirection,
            dto.PacingDirection,
            dto.ColorDirection,
            dto.TypographyDirection,
            dto.AudioDirection,
            dto.ExperimentProductionNotes,
            dto.Scenes,
            dto.AssetRequirements
        );
    }
}

public sealed class ProductionPackageJobProcessor(
    IYoutubeAiFactoryStore store,
    ILlmProvider provider,
    IAiModelResolver resolver,
    ProductionContextBuilder builder,
    ProductionValidator validator,
    ProductionOptions options,
    TimeProvider timeProvider,
    ILogger<ProductionPackageJobProcessor> logger,
    IProductionPackageJobLeaseRenewer? leaseRenewer = null
)
{
    private static readonly Action<ILogger, Guid, Exception?> LogLeaseLost =
        LoggerMessage.Define<Guid>(
            LogLevel.Warning,
            new EventId(1, "ProductionLeaseLost"),
            "Production package job {JobId} lost its lease."
        );
    private static readonly Action<ILogger, Guid, Exception?> LogJobFailed =
        LoggerMessage.Define<Guid>(
            LogLevel.Warning,
            new EventId(2, "ProductionJobFailed"),
            "Production package job {JobId} failed."
        );
    private static readonly Action<ILogger, Exception?> LogLeaseRenewalFailed =
        LoggerMessage.Define(
            LogLevel.Error,
            new EventId(3, "ProductionLeaseRenewalFailed"),
            "Production package lease renewal failed."
        );

    public async Task<bool> ProcessNextAsync(CancellationToken ct)
    {
        ProductionValidator.ValidateOptions(options);
        var now = timeProvider.GetUtcNow();
        var job = await store.TryClaimNextProductionPackageJobAsync(
            now,
            now.AddSeconds(-options.RunningJobLeaseSeconds),
            ct
        );
        if (job is null)
            return false;
        var payload =
            JsonSerializer.Deserialize<ProductionJobPayload>(
                job.Payload,
                ProductionPrompt.SerializerOptions
            )
            ?? throw new ApplicationValidationException(
                "Production package job payload is invalid."
            );
        var lease =
            job.LeaseId
            ?? throw new ApplicationValidationException("Production package job has no lease.");
        using var leaseLost = new CancellationTokenSource();
        using var heartbeatCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        using var execution = CancellationTokenSource.CreateLinkedTokenSource(ct, leaseLost.Token);
        var heartbeat = leaseRenewer is null
            ? Task.CompletedTask
            : MaintainLeaseAsync(job.Id, lease, leaseLost, heartbeatCts.Token);
        AiRun? last = null;
        try
        {
            var vp =
                await store.GetVideoProjectAsync(
                    payload.ProjectId,
                    payload.VideoProjectId,
                    true,
                    execution.Token
                ) ?? throw new ResourceNotFoundException("Video project no longer exists.");
            var context = await RunProductionPackageHandler.LoadContextAsync(
                store,
                builder,
                payload.ProjectId,
                vp,
                execution.Token
            );
            if (
                payload.VideoScriptId != context.VideoScriptId
                || payload.VideoScriptVersion != context.VideoScriptVersion
                || payload.InputFingerprint != context.InputFingerprint
            )
                throw new ApplicationValidationException(
                    "Production inputs changed after the job was queued."
                );
            if (payload.Operation == ProductionJobOperation.Generate)
            {
                if (vp.Status != VideoProjectStatus.Packaging)
                    throw new ApplicationValidationException(
                        "Production package generation state is no longer runnable."
                    );
                var generated = await GenerateAsync(context, execution.Token);
                last = generated.GroundingRun;
                await PersistAsync(payload, context, generated, execution.Token);
            }
            else
            {
                if (payload.ProductionPackageId is null)
                    throw new ApplicationValidationException("Validation job has no package.");
                var package =
                    await store.GetProductionPackageAsync(
                        payload.ProjectId,
                        payload.VideoProjectId,
                        payload.ProductionPackageId.Value,
                        true,
                        execution.Token
                    )
                    ?? throw new ResourceNotFoundException("Production package no longer exists.");
                var result = ProductionDtoMapper.ToResult(package);
                ProductionPersistenceValidator.Validate(package, context, validator);
                var audit = await AuditAsync(context, result, execution.Token);
                last = audit.Run;
                package.Package.RecordGroundingResult(
                    audit.Result.Status,
                    audit.Run.Id,
                    JsonSerializer.Serialize(
                        audit.Result.Issues,
                        ProductionPrompt.SerializerOptions
                    ),
                    timeProvider.GetUtcNow()
                );
            }
            if (leaseRenewer is null)
            {
                job.Complete(timeProvider.GetUtcNow());
                await store.SaveChangesAsync(execution.Token);
            }
            else if (
                !await store.CompleteProductionPackageJobAsync(
                    job.Id,
                    lease,
                    timeProvider.GetUtcNow(),
                    execution.Token
                )
            )
            {
                leaseLost.Cancel();
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            await store.RequeueProductionPackageJobAsync(job.Id, CancellationToken.None);
            throw;
        }
        catch (Exception ex) when (leaseLost.IsCancellationRequested)
        {
            LogLeaseLost(logger, job.Id, ex);
        }
        catch (Exception ex)
        {
            var failed = timeProvider.GetUtcNow();
            var retryable =
                ex is ExternalServiceException
                {
                    Failure: ExternalServiceFailure.Transient
                        or ExternalServiceFailure.QuotaExceeded
                };
            await store.FailProductionPackageJobAsync(
                job.Id,
                last?.Id,
                ex is YoutubeAiFactoryException
                    ? ex.Message
                    : "Production package workflow could not be completed.",
                retryable,
                failed,
                retryable ? failed.AddSeconds(Math.Pow(2, job.RetryCount + 1) * 5) : null,
                CancellationToken.None
            );
            LogJobFailed(logger, job.Id, ex);
        }
        finally
        {
            heartbeatCts.Cancel();
            try
            {
                await heartbeat;
            }
            catch (OperationCanceledException) { }
        }
        return true;
    }

    private async Task<Processed> GenerateAsync(
        ProductionGenerationContext context,
        CancellationToken ct
    )
    {
        var generated = await GenerateCandidateAsync(context, ct);
        var result = generated.Result;
        var timing = validator.ValidateGenerated(result, context);
        var audit = await AuditAsync(context, result, ct);
        var corrections = 0;
        while (
            audit.Result.Status == ProductionGroundingStatus.Failed
            && corrections < options.MaxGroundingCorrectionAttempts
        )
        {
            var corrected = await CorrectAsync(context, result, audit.Result.Issues, ct);
            result = corrected.Result;
            generated = corrected;
            timing = validator.ValidateGenerated(result, context);
            audit = await AuditAsync(context, result, ct);
            corrections++;
        }
        if (audit.Result.Status != ProductionGroundingStatus.Passed)
            throw new StructuredOutputException(
                "Production grounding failed after bounded correction."
            );
        return new(
            result,
            timing,
            generated.Run,
            generated.Provider,
            generated.Model,
            audit.Run,
            audit.Result
        );
    }

    private async Task<Generated> GenerateCandidateAsync(
        ProductionGenerationContext context,
        CancellationToken ct
    )
    {
        var resolved = resolver.Resolve(AiWorkflowProfiles.ProductionPackageGeneration);
        var run = Start(
            "ProductionPackageGeneration",
            context,
            resolved,
            ProductionPrompt.GenerationKey,
            ProductionPrompt.GenerationVersion
        );
        await store.SaveChangesAsync(ct);
        string? diagnostic = null;
        ProductionPackageResult? prior = null;
        var repairs = 0;
        try
        {
            for (var attempt = 0; attempt <= options.MaxGenerationRetries; attempt++)
            {
                try
                {
                    var answer = await provider.GenerateStructuredAsync<ProductionPackageResult>(
                        ProductionPrompt
                            .Generate(context, diagnostic, prior)
                            .WithResolvedModel(resolved),
                        ct
                    );
                    prior = answer.Value;
                    validator.ValidateGenerated(answer.Value, context);
                    Complete(run, answer);
                    await store.SaveChangesAsync(ct);
                    return new(answer.Value, run, answer.Provider, answer.Model);
                }
                catch (StructuredOutputException ex)
                    when (ex.RawOutput is not null && repairs < options.MaxStructuredRepairAttempts)
                {
                    repairs++;
                    var repaired = await RepairAsync(context, ex.RawOutput, ex.Message, ct);
                    run.Complete(null, null, null, timeProvider.GetUtcNow());
                    return repaired;
                }
                catch (Exception ex)
                    when (ex is StructuredOutputException or ApplicationValidationException)
                {
                    diagnostic = ex.Message;
                }
                if (attempt == options.MaxGenerationRetries)
                    throw new StructuredOutputException(
                        diagnostic ?? "Production output failed validation."
                    );
                run.RecordRetry();
                await store.SaveChangesAsync(ct);
            }
            throw new StructuredOutputException("Production output failed validation.");
        }
        catch (Exception ex)
        {
            await FailRun(run, ex, "Production generation failed.");
            throw;
        }
    }

    private async Task<Generated> CorrectAsync(
        ProductionGenerationContext context,
        ProductionPackageResult package,
        IReadOnlyList<ProductionGroundingIssueResult> issues,
        CancellationToken ct
    )
    {
        var m = resolver.Resolve(AiWorkflowProfiles.ProductionPackageCorrection);
        var run = Start(
            "ProductionPackageCorrection",
            context,
            m,
            ProductionPrompt.CorrectionKey,
            ProductionPrompt.CorrectionVersion
        );
        await store.SaveChangesAsync(ct);
        try
        {
            var a = await provider.GenerateStructuredAsync<ProductionPackageResult>(
                ProductionPrompt.Correct(context, package, issues).WithResolvedModel(m),
                ct
            );
            validator.ValidateGenerated(a.Value, context);
            Complete(run, a);
            await store.SaveChangesAsync(ct);
            return new(a.Value, run, a.Provider, a.Model);
        }
        catch (Exception ex)
        {
            await FailRun(run, ex, "Production correction failed.");
            throw;
        }
    }

    private async Task<Audited> AuditAsync(
        ProductionGenerationContext context,
        ProductionPackageResult package,
        CancellationToken ct
    )
    {
        var m = resolver.Resolve(AiWorkflowProfiles.ProductionGroundingAudit);
        var run = Start(
            "ProductionGroundingAudit",
            context,
            m,
            ProductionPrompt.AuditKey,
            ProductionPrompt.AuditVersion
        );
        await store.SaveChangesAsync(ct);
        try
        {
            var a = await provider.GenerateStructuredAsync<ProductionGroundingAuditResult>(
                ProductionPrompt.Audit(context, package).WithResolvedModel(m),
                ct
            );
            ProductionValidator.ValidateAudit(a.Value, package, context);
            Complete(run, a);
            await store.SaveChangesAsync(ct);
            return new(a.Value, run);
        }
        catch (Exception ex)
        {
            await FailRun(run, ex, "Production grounding audit failed.");
            throw;
        }
    }

    private async Task<Generated> RepairAsync(
        ProductionGenerationContext context,
        string raw,
        string diagnostic,
        CancellationToken ct
    )
    {
        var m = resolver.Resolve(AiWorkflowProfiles.StructuredOutputRepair);
        var run = Start(
            "StructuredOutputRepair",
            context,
            m,
            ProductionPrompt.RepairKey,
            ProductionPrompt.RepairVersion
        );
        await store.SaveChangesAsync(ct);
        try
        {
            var a = await provider.GenerateStructuredAsync<ProductionPackageResult>(
                ProductionPrompt.Repair(raw, diagnostic).WithResolvedModel(m),
                ct
            );
            validator.ValidateGenerated(a.Value, context);
            Complete(run, a);
            await store.SaveChangesAsync(ct);
            return new(a.Value, run, a.Provider, a.Model);
        }
        catch (Exception ex)
        {
            await FailRun(run, ex, "Structured production repair failed.");
            throw;
        }
    }

    private AiRun Start(
        string workflow,
        ProductionGenerationContext c,
        ResolvedAiModel m,
        string key,
        int version
    )
    {
        var run = new AiRun(
            workflow,
            c.ProjectId,
            m.Provider,
            m.Model,
            key,
            version,
            timeProvider.GetUtcNow(),
            m.Profile.ToString(),
            c.VideoProjectId,
            researchReportId: c.ResearchReportId
        );
        store.AddAiRun(run);
        return run;
    }

    private void Complete<T>(AiRun run, LlmResult<T> result)
    {
        run.RecordProvider(result.Provider, result.Model);
        run.Complete(result.InputTokens, result.OutputTokens, null, timeProvider.GetUtcNow());
    }

    private async Task FailRun(AiRun run, Exception ex, string fallback)
    {
        if (run.Status == AiRunStatus.Running)
        {
            run.Fail(
                ex is YoutubeAiFactoryException ? ex.Message : fallback,
                timeProvider.GetUtcNow()
            );
            await store.SaveChangesAsync(CancellationToken.None);
        }
    }

    private async Task PersistAsync(
        ProductionJobPayload payload,
        ProductionGenerationContext context,
        Processed processed,
        CancellationToken ct
    )
    {
        var version = await store.GetNextProductionPackageVersionAsync(
            payload.ProjectId,
            payload.VideoProjectId,
            ct
        );
        var r = processed.Result;
        var p = new ProductionPackage(
            payload.ProjectId,
            payload.VideoProjectId,
            context.VideoScriptId,
            context.VideoScriptVersion,
            context.VideoOutlineId,
            context.VideoOutlineVersion,
            context.ResearchReportId,
            context.ResearchReportVersion,
            version,
            processed.GenerationRun.Id,
            processed.GroundingRun.Id,
            RunProductionPackageHandler.EngineVersion,
            ProductionPrompt.GenerationKey,
            ProductionPrompt.GenerationVersion,
            context.InputFingerprint,
            processed.Provider,
            processed.Model,
            context.ContentLanguage,
            context.EstimatedDurationSeconds,
            r.VisualDirection,
            r.PacingDirection,
            r.ColorDirection,
            r.TypographyDirection,
            r.AudioDirection,
            r.ExperimentProductionNotes,
            JsonSerializer.Serialize(r.Warnings, ProductionPrompt.SerializerOptions),
            JsonSerializer.Serialize(processed.Audit.Issues, ProductionPrompt.SerializerOptions),
            timeProvider.GetUtcNow()
        );
        store.AddProductionPackage(p);
        var assets = new Dictionary<string, ProductionAssetRequirement>(StringComparer.Ordinal);
        foreach (var a in r.AssetRequirements)
        {
            var e = new ProductionAssetRequirement(
                p.Id,
                a.AssetKey,
                a.AssetType,
                a.AcquisitionMode,
                a.CreativeBrief,
                a.GenerationPrompt,
                a.SourceSearchBrief,
                a.RightsVerificationRequired,
                a.FactualityMode,
                a.ReuseKey,
                a.Complexity
            );
            assets.Add(a.AssetKey, e);
            store.AddProductionAsset(e);
            foreach (var claim in a.ClaimIds)
                store.AddProductionAssetClaim(new(e.Id, claim));
        }
        foreach (var s in r.Scenes.OrderBy(x => x.Sequence))
        {
            var scene = new ProductionScene(
                p.Id,
                s.Sequence,
                s.Purpose,
                s.NarrationSummary,
                s.VisualStrategy,
                processed.Timing.SceneSeconds[s.Sequence],
                s.Complexity,
                s.TransitionIntent,
                s.MusicBrief,
                s.SoundEffectCue,
                s.VoiceDirection
            );
            store.AddProductionScene(scene);
            for (var i = 0; i < s.ScriptBlockIds.Count; i++)
                store.AddProductionSceneScriptBlock(
                    new(p.Id, scene.Id, s.ScriptBlockIds[i], i + 1)
                );
            foreach (var shot in s.Shots)
            {
                var e = new ProductionShot(
                    scene.Id,
                    shot.Sequence,
                    shot.ShotType,
                    shot.VisualDescription,
                    shot.Composition,
                    shot.MotionSuggestion,
                    processed.Timing.ShotSeconds[(s.Sequence, shot.Sequence)],
                    shot.FactualityMode,
                    shot.AssetKey is null ? null : assets[shot.AssetKey].Id,
                    shot.Notes
                );
                store.AddProductionShot(e);
                foreach (var claim in shot.ClaimIds)
                    store.AddProductionShotClaim(new(e.Id, claim));
            }
            foreach (var text in s.OnScreenText)
            {
                var e = new ProductionOnScreenText(
                    scene.Id,
                    text.Sequence,
                    text.Text,
                    text.Type,
                    text.TimingIntent
                );
                store.AddProductionOnScreenText(e);
                foreach (var claim in text.ClaimIds)
                    store.AddProductionOnScreenTextClaim(new(e.Id, claim));
            }
        }
    }

    private async Task MaintainLeaseAsync(
        Guid jobId,
        Guid lease,
        CancellationTokenSource lost,
        CancellationToken ct
    )
    {
        using var timer = new PeriodicTimer(
            TimeSpan.FromSeconds(Math.Max(5, options.RunningJobLeaseSeconds / 3d))
        );
        try
        {
            while (await timer.WaitForNextTickAsync(ct))
            {
                if (await leaseRenewer!.RenewAsync(jobId, lease, timeProvider.GetUtcNow(), ct))
                    continue;
                lost.Cancel();
                return;
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { }
        catch (Exception ex)
        {
            LogLeaseRenewalFailed(logger, ex);
            lost.Cancel();
        }
    }

    private sealed record Generated(
        ProductionPackageResult Result,
        AiRun Run,
        string Provider,
        string Model
    );

    private sealed record Audited(ProductionGroundingAuditResult Result, AiRun Run);

    private sealed record Processed(
        ProductionPackageResult Result,
        ProductionTimingPlan Timing,
        AiRun GenerationRun,
        string Provider,
        string Model,
        AiRun GroundingRun,
        ProductionGroundingAuditResult Audit
    );
}

internal static class ProductionPersistenceValidator
{
    public static void Validate(
        ProductionPackageWithDetails details,
        ProductionGenerationContext context,
        ProductionValidator validator
    )
    {
        var p = details.Package;
        if (
            p.ProjectId != context.ProjectId
            || p.VideoProjectId != context.VideoProjectId
            || p.VideoScriptId != context.VideoScriptId
            || p.VideoScriptVersion != context.VideoScriptVersion
            || p.VideoOutlineId != context.VideoOutlineId
            || p.VideoOutlineVersion != context.VideoOutlineVersion
            || p.ResearchReportId != context.ResearchReportId
            || p.ResearchReportVersion != context.ResearchReportVersion
            || p.EstimatedDurationSeconds != context.EstimatedDurationSeconds
        )
            throw new ApplicationValidationException(
                "Production package lineage or runtime does not match the approved Script."
            );
        var timing = validator.ValidateGenerated(ProductionDtoMapper.ToResult(details), context);
        var scenes = details.Scenes.ToDictionary(x => x.Id);
        var assets = details.Assets.ToDictionary(x => x.Id);
        foreach (var scene in details.Scenes)
        {
            if (
                scene.ProductionPackageId != p.Id
                || timing.SceneSeconds[scene.Sequence] != scene.EstimatedDurationSeconds
            )
                throw new ApplicationValidationException(
                    "Persisted production scene timing or ownership is inconsistent."
                );
        }
        foreach (var link in details.SceneBlocks)
        {
            if (link.ProductionPackageId != p.Id || !scenes.ContainsKey(link.ProductionSceneId))
                throw new ApplicationValidationException(
                    "Persisted ScriptBlock mapping ownership is inconsistent."
                );
        }
        foreach (var asset in details.Assets)
            if (asset.ProductionPackageId != p.Id)
                throw new ApplicationValidationException(
                    "Persisted asset ownership is inconsistent."
                );
        foreach (var shot in details.Shots)
        {
            if (
                !scenes.TryGetValue(shot.ProductionSceneId, out var scene)
                || timing.ShotSeconds[(scene.Sequence, shot.Sequence)]
                    != shot.EstimatedDurationSeconds
                || shot.AssetRequirementId is Guid assetId && !assets.ContainsKey(assetId)
            )
                throw new ApplicationValidationException(
                    "Persisted shot timing, scene, or asset ownership is inconsistent."
                );
        }
        if (details.OnScreenText.Any(x => !scenes.ContainsKey(x.ProductionSceneId)))
            throw new ApplicationValidationException(
                "Persisted on-screen text ownership is inconsistent."
            );
    }
}
