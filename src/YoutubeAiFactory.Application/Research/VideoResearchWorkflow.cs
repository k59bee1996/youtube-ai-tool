using System.Text.Json;
using Microsoft.Extensions.Logging;
using YoutubeAiFactory.Application.AI;
using YoutubeAiFactory.Application.Common;
using YoutubeAiFactory.Application.Persistence;
using YoutubeAiFactory.Domain.AI;
using YoutubeAiFactory.Domain.Jobs;
using YoutubeAiFactory.Domain.Research;
using YoutubeAiFactory.Domain.Videos;

namespace YoutubeAiFactory.Application.Research;

public sealed record ResearchJobPayload(Guid ProjectId, Guid VideoProjectId, Guid ResearchRunId);

public sealed class RunVideoResearchHandler(IYoutubeAiFactoryStore store, ResearchOptions options, TimeProvider timeProvider)
{
    public const string AlgorithmVersion = "research-engine:v1";

    public async Task<RunVideoResearchResult> HandleAsync(Guid projectId, Guid videoProjectId, CancellationToken cancellationToken)
    {
        ValidateOptions(options);
        var videoProject = await store.GetVideoProjectAsync(projectId, videoProjectId, true, cancellationToken)
            ?? throw new ResourceNotFoundException("Video project was not found.");
        var active = await store.GetActiveVideoResearchJobAsync(projectId, videoProjectId, cancellationToken);
        if (active is not null) return new RunVideoResearchResult(active.Id, active.Status.ToString(), true);
        if (videoProject.Status is not (VideoProjectStatus.Draft or VideoProjectStatus.ResearchReady or VideoProjectStatus.ResearchFailed))
            throw new ApplicationValidationException($"Research cannot start while the VideoProject is {videoProject.Status}.");
        var project = await store.GetProjectAsync(projectId, cancellationToken)
            ?? throw new ResourceNotFoundException("Project was not found.");
        var source = await store.GetVideoProjectSourceAsync(projectId, videoProject.PilotId, videoProject.PilotVideoId, cancellationToken)
            ?? throw new ResourceNotFoundException("Video project source context was not found.");
        var brief = ResearchBriefBuilder.Build(project, videoProject, source.Opportunity.Name);
        var now = timeProvider.GetUtcNow();
        var run = new ResearchRun(projectId, videoProjectId, AlgorithmVersion, ResearchBriefBuilder.CreateFingerprint(brief), now);
        var job = new Job("video-research", JsonSerializer.Serialize(new ResearchJobPayload(projectId, videoProjectId, run.Id), ResearchPrompts.SerializerOptions),
            now, options.MaxJobRetries, projectId: projectId, videoProjectId: videoProjectId);
        videoProject.TransitionTo(VideoProjectStatus.ResearchQueued, now);
        var persisted = await store.EnqueueVideoResearchJobAsync(job, run, cancellationToken);
        return new RunVideoResearchResult(persisted.Id, persisted.Status.ToString(), persisted.Id != job.Id);
    }

    internal static void ValidateOptions(ResearchOptions options)
    {
        if (options.MaxJobRetries is < 0 or > 5 || options.RunningJobLeaseSeconds is < 30 or > 3_600 ||
            options.MaxStructuredOutputRetries is < 0 or > 3 || options.MaxResearchQueries is < 1 or > 12 ||
            options.MaxSearchResultsPerQuery is < 1 or > 10 || options.MaxSourcesToFetch is < 1 or > 30 ||
            options.MaxRelevantSources is < 1 or > 20 || options.MaxRelevantSources > options.MaxSourcesToFetch ||
            options.MaxSourceCharacters is < 500 or > 50_000 || options.MaxEvidenceItemsPerSource is < 1 or > 20 ||
            options.MaxTotalEvidenceItems is < 1 or > 100 || options.MaxClaims is < 1 or > 80 ||
            options.MinUsableSources is < 1 || options.MinUsableSources > options.MaxSourcesToFetch ||
            options.MinEvidenceItems is < 1 || options.MinEvidenceItems > options.MaxTotalEvidenceItems ||
            options.MaxConcurrentSourceFetches is < 1 or > 10)
            throw new ApplicationValidationException("Research configuration contains an invalid bounded limit.");
    }
}

public sealed class GetVideoResearchStatusHandler(IYoutubeAiFactoryStore store)
{
    public async Task<ResearchStatusDto> HandleAsync(Guid projectId, Guid videoProjectId, CancellationToken cancellationToken)
    {
        var videoProject = await store.GetVideoProjectAsync(projectId, videoProjectId, false, cancellationToken)
            ?? throw new ResourceNotFoundException("Video project was not found.");
        var latest = await store.GetLatestResearchReportAsync(projectId, videoProjectId, cancellationToken);
        var active = await store.GetActiveVideoResearchJobAsync(projectId, videoProjectId, cancellationToken);
        var latestJob = active ?? await store.GetLatestVideoResearchJobAsync(projectId, videoProjectId, cancellationToken);
        var latestRun = await store.GetLatestResearchRunAsync(projectId, videoProjectId, cancellationToken);
        return new ResearchStatusDto(latest is null ? null : await ResearchDtoMapper.MapAsync(store, latest, videoProject, cancellationToken),
            ToJob(active), ToJob(latestJob), latestRun?.Status.ToString(), latestRun?.FailureReason);
    }

    internal static ResearchJobDto? ToJob(Job? job) => job is null ? null : new ResearchJobDto(job.Id, job.Status.ToString(), job.FailureReason);
}

public sealed class GetVideoResearchReportHandler(IYoutubeAiFactoryStore store)
{
    public async Task<ResearchReportDetails> HandleAsync(Guid projectId, Guid videoProjectId, Guid? reportId, CancellationToken cancellationToken)
    {
        var videoProject = await store.GetVideoProjectAsync(projectId, videoProjectId, false, cancellationToken)
            ?? throw new ResourceNotFoundException("Video project was not found.");
        var details = reportId is { } id
            ? await store.GetResearchReportAsync(projectId, videoProjectId, id, cancellationToken)
            : await store.GetLatestResearchReportAsync(projectId, videoProjectId, cancellationToken);
        if (details is null) throw new ResourceNotFoundException("Research report was not found.");
        var runs = await store.ListResearchRunsAsync(projectId, videoProjectId, cancellationToken);
        return new ResearchReportDetails(await ResearchDtoMapper.MapAsync(store, details, videoProject, cancellationToken), runs.Select(ToRun).ToArray());
    }

    private static ResearchRunDto ToRun(ResearchRun run) => new(run.Id, run.Status.ToString(), run.ResearchAlgorithmVersion,
        run.InputFingerprint, run.QueuedAt, run.StartedAt, run.CompletedAt, run.FailureReason, run.SearchQueryCount,
        run.SearchResultCount, run.FetchedSourceCount, run.RelevantSourceCount, run.EvidenceCount, run.ClaimCount,
        run.ConflictCount, run.SearchFailureCount, run.FetchFailureCount);
}

public sealed class ListVideoResearchReportsHandler(IYoutubeAiFactoryStore store)
{
    public async Task<IReadOnlyList<ResearchReportHistoryItemDto>> HandleAsync(Guid projectId, Guid videoProjectId, CancellationToken cancellationToken)
    {
        if (await store.GetVideoProjectAsync(projectId, videoProjectId, false, cancellationToken) is null)
            throw new ResourceNotFoundException("Video project was not found.");
        var reports = await store.ListResearchReportsAsync(projectId, videoProjectId, cancellationToken);
        return reports.Select(item => new ResearchReportHistoryItemDto(item.Id, item.Version, item.ResearchAlgorithmVersion, item.CreatedAt)).ToArray();
    }
}

public sealed class VideoResearchJobProcessor(
    IYoutubeAiFactoryStore store,
    ILlmProvider provider,
    IAiModelResolver modelResolver,
    IResearchSearchClient searchClient,
    IResearchContentFetcher contentFetcher,
    ResearchOptions options,
    TimeProvider timeProvider,
    ILogger<VideoResearchJobProcessor> logger)
{
    private static readonly Action<ILogger, Guid, Guid, int, int, int, Exception?> LogCompleted = LoggerMessage.Define<Guid, Guid, int, int, int>(
        LogLevel.Information, new EventId(1, nameof(LogCompleted)), "Research completed for project {ProjectId}, VideoProject {VideoProjectId}; sources {Sources}, evidence {Evidence}, claims {Claims}.");
    private static readonly Action<ILogger, Guid, Exception?> LogFailed = LoggerMessage.Define<Guid>(
        LogLevel.Warning, new EventId(2, nameof(LogFailed)), "Video research job {JobId} failed.");

    public async Task<bool> ProcessNextAsync(CancellationToken cancellationToken)
    {
        RunVideoResearchHandler.ValidateOptions(options);
        var now = timeProvider.GetUtcNow();
        var job = await store.TryClaimNextVideoResearchJobAsync(now, now.AddSeconds(-options.RunningJobLeaseSeconds), cancellationToken);
        if (job is null) return false;
        var payload = JsonSerializer.Deserialize<ResearchJobPayload>(job.Payload, ResearchPrompts.SerializerOptions)
            ?? throw new ApplicationValidationException("Video research job payload is invalid.");
        AiRun? activeAiRun = null;
        try
        {
            var videoProject = await store.GetVideoProjectAsync(payload.ProjectId, payload.VideoProjectId, true, cancellationToken)
                ?? throw new ResourceNotFoundException("The VideoProject for this research job no longer exists.");
            var researchRun = await store.GetResearchRunAsync(payload.ProjectId, payload.VideoProjectId, payload.ResearchRunId, true, cancellationToken)
                ?? throw new ResourceNotFoundException("The research run for this job no longer exists.");
            if (researchRun.Status == ResearchRunStatus.Queued) researchRun.Start(now);
            if (videoProject.Status == VideoProjectStatus.ResearchQueued) videoProject.TransitionTo(VideoProjectStatus.Researching, now);
            if (videoProject.Status != VideoProjectStatus.Researching || researchRun.Status != ResearchRunStatus.Running)
                throw new ApplicationValidationException("Video research job state is no longer runnable.");
            var project = await store.GetProjectAsync(payload.ProjectId, cancellationToken)
                ?? throw new ResourceNotFoundException("Project was not found.");
            var sourceContext = await store.GetVideoProjectSourceAsync(payload.ProjectId, videoProject.PilotId, videoProject.PilotVideoId, cancellationToken)
                ?? throw new ResourceNotFoundException("Video project source context was not found.");
            var brief = ResearchBriefBuilder.Build(project, videoProject, sourceContext.Opportunity.Name);
            var inputFingerprint = ResearchBriefBuilder.CreateFingerprint(brief);
            if (!string.Equals(researchRun.InputFingerprint, inputFingerprint, StringComparison.Ordinal))
                throw new ApplicationValidationException("Video project research inputs changed after this run was queued. Start a new research run from the current brief.");

            var planAnswer = await GenerateAsync<ResearchQueryPlanResult>("ResearchQueryPlanning", ResearchPrompts.QueryPlan(brief),
                value => ValidatePlan(value), payload, cancellationToken);
            activeAiRun = planAnswer.AiRun;
            var plan = new ResearchPlan(planAnswer.Result.Objective, planAnswer.Result.Questions, planAnswer.Result.Queries,
                planAnswer.Result.PriorityFactAreas, planAnswer.Result.KnownRisks);

            var searchOutcome = await DiscoverAsync(plan, brief.TargetLanguage, cancellationToken);
            var fetched = await FetchSourcesAsync(searchOutcome.Results, researchRun.Id, cancellationToken);
            foreach (var item in fetched.AllSources) store.AddResearchSource(item.Source);
            researchRun.RecordMetrics(ToRunMetrics(plan, searchOutcome, fetched, 0, 0, 0, 0));
            await store.SaveChangesAsync(cancellationToken);

            var relevant = new List<FetchedSource>();
            foreach (var item in fetched.UsableSources)
            {
                var relevance = await GenerateAsync<ResearchSourceRelevanceResult>("ResearchSourceRelevance",
                    ResearchPrompts.Relevance(brief, plan, item.Source.Title ?? item.Source.Domain, item.Source.CanonicalUrl, item.Text),
                    ValidateRelevance, payload, cancellationToken);
                activeAiRun = relevance.AiRun;
                if (relevance.Result.Relevance is ResearchSourceRelevance.Relevant or ResearchSourceRelevance.PossiblyRelevant)
                {
                    relevant.Add(item);
                    if (relevant.Count == options.MaxRelevantSources) break;
                }
            }
            if (relevant.Count < options.MinUsableSources)
                throw new ApplicationValidationException("Research found no usable relevant sources. Refine the video premise or retry later.");

            var evidence = new List<ResearchEvidence>();
            foreach (var item in relevant)
            {
                if (evidence.Count >= options.MaxTotalEvidenceItems) break;
                var extracted = await GenerateAsync<ResearchEvidenceExtractionResult>("ResearchEvidenceExtraction",
                    ResearchPrompts.Evidence(brief, plan, item.Source.Id, item.Source.Title ?? item.Source.Domain, item.Source.CanonicalUrl, item.Text),
                    value => ValidateEvidenceExtraction(value, item.Text), payload, cancellationToken);
                activeAiRun = extracted.AiRun;
                foreach (var candidate in extracted.Result.Evidence.Take(options.MaxEvidenceItemsPerSource))
                {
                    if (evidence.Count == options.MaxTotalEvidenceItems) break;
                    if (evidence.Any(existing => existing.ResearchSourceId == item.Source.Id &&
                        string.Equals(ResearchSupportAnalyzer.NormalizeStatement(existing.Fact), ResearchSupportAnalyzer.NormalizeStatement(candidate.Fact), StringComparison.Ordinal))) continue;
                    var itemEvidence = new ResearchEvidence(researchRun.Id, item.Source.Id, candidate.Type, candidate.Fact,
                        candidate.SupportingExcerpt, candidate.SourceLocator, candidate.Confidence, timeProvider.GetUtcNow());
                    evidence.Add(itemEvidence);
                    store.AddResearchEvidence(itemEvidence);
                }
            }
            if (evidence.Count < options.MinEvidenceItems)
                throw new ApplicationValidationException("Research found sources but could not extract enough source-bound evidence.");
            researchRun.RecordMetrics(ToRunMetrics(plan, searchOutcome, fetched, relevant.Count, evidence.Count, 0, 0));
            await store.SaveChangesAsync(cancellationToken);

            var reportId = Guid.NewGuid();
            var claims = BuildClaims(reportId, evidence, timeProvider.GetUtcNow(), options.MaxClaims);
            var claimEvidence = claims.SelectMany(item => item.EvidenceIds.Select(evidenceId =>
                new ResearchClaimEvidence(item.Claim.Id, evidenceId, ResearchEvidenceStance.Support))).ToList();
            var evidenceById = evidence.ToDictionary(item => item.Id);
            var sourceById = relevant.Select(item => item.Source).ToDictionary(item => item.Id);

            var conflicts = new List<ResearchConflict>();
            if (claims.Count > 0 && evidence.Count > 1)
            {
                var contradictionAnswer = await GenerateAsync<ResearchContradictionAnalysisResult>("ResearchContradictionAnalysis",
                    ResearchPrompts.Contradictions(brief, ToPromptClaims(claims, claimEvidence), ToPromptEvidence(evidence, sourceById)),
                    value => ValidateConflicts(value, claims.Select(item => item.Claim.Id), evidence.Select(item => item.Id), claimEvidence), payload, cancellationToken);
                activeAiRun = contradictionAnswer.AiRun;
                foreach (var conflict in contradictionAnswer.Result.Conflicts.DistinctBy(item => (item.ClaimId, item.SupportingEvidenceId, item.ContradictingEvidenceId)))
                {
                    if (!claimEvidence.Any(link => link.ResearchClaimId == conflict.ClaimId && link.ResearchEvidenceId == conflict.ContradictingEvidenceId && link.Stance == ResearchEvidenceStance.Contradict))
                        claimEvidence.Add(new ResearchClaimEvidence(conflict.ClaimId, conflict.ContradictingEvidenceId, ResearchEvidenceStance.Contradict));
                    conflicts.Add(new ResearchConflict(reportId, conflict.ClaimId, conflict.SupportingEvidenceId,
                        conflict.ContradictingEvidenceId, conflict.Explanation, conflict.IsResolved, timeProvider.GetUtcNow()));
                }
            }
            foreach (var claim in claims.Select(item => item.Claim))
                claim.SetSupportStatus(ResearchSupportAnalyzer.GetStatus(claimEvidence.Where(link => link.ResearchClaimId == claim.Id), evidenceById, sourceById));
            researchRun.RecordMetrics(ToRunMetrics(plan, searchOutcome, fetched, relevant.Count, evidence.Count, claims.Count, conflicts.Count));
            var deterministicGaps = BuildDeterministicGaps(claims.Select(item => item.Claim), fetched.FetchFailureCount);
            var synthesisAnswer = await GenerateAsync<ResearchSynthesisResult>("ResearchSynthesis",
                ResearchPrompts.Synthesis(brief, plan, ToPromptClaims(claims, claimEvidence), ToPromptEvidence(evidence, sourceById),
                    conflicts.Select(item => new ConflictForPrompt(item.ResearchClaimId, item.SupportingEvidenceId, item.ContradictingEvidenceId, item.Explanation, item.IsResolved)).ToArray(), deterministicGaps),
                value => ValidateSynthesis(value, claims.Select(item => item.Claim), claimEvidence, evidenceById), payload, cancellationToken);
            activeAiRun = synthesisAnswer.AiRun;
            var mergedSynthesis = synthesisAnswer.Result with
            {
                Gaps = MergeGaps(deterministicGaps, synthesisAnswer.Result.Gaps),
                Limitations = MergeStrings(synthesisAnswer.Result.Limitations, BuildLimitations(searchOutcome.SearchFailureCount, fetched.FetchFailureCount, claims.Select(item => item.Claim))),
            };
            var metrics = BuildMetrics(plan, searchOutcome, fetched, evidence, claims.Select(item => item.Claim), conflicts, sourceById);
            var confidence = BuildConfidence(metrics, claims.Select(item => item.Claim));
            var payloadJson = JsonSerializer.Serialize(new ResearchReportPayload(mergedSynthesis, confidence, metrics, plan), ResearchPrompts.SerializerOptions);
            var version = await store.GetNextResearchReportVersionAsync(payload.ProjectId, payload.VideoProjectId, cancellationToken);
            var report = new ResearchReport(payload.ProjectId, payload.VideoProjectId, researchRun.Id, version,
                RunVideoResearchHandler.AlgorithmVersion, researchRun.InputFingerprint, payloadJson, synthesisAnswer.AiRun.Id, timeProvider.GetUtcNow(), reportId);
            store.AddResearchReport(report);
            foreach (var claim in claims.Select(item => item.Claim)) store.AddResearchClaim(claim);
            foreach (var link in claimEvidence) store.AddResearchClaimEvidence(link);
            foreach (var conflict in conflicts) store.AddResearchConflict(conflict);
            researchRun.Complete(report.Id, ToRunMetrics(plan, searchOutcome, fetched, relevant.Count, evidence.Count, claims.Count, conflicts.Count), timeProvider.GetUtcNow());
            videoProject.TransitionTo(VideoProjectStatus.ResearchReady, timeProvider.GetUtcNow());
            job.Complete(timeProvider.GetUtcNow());
            await store.SaveChangesAsync(cancellationToken);
            LogCompleted(logger, payload.ProjectId, payload.VideoProjectId, fetched.UsableSources.Count, evidence.Count, claims.Count, null);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            await store.RequeueVideoResearchJobAsync(job.Id, CancellationToken.None);
            throw;
        }
        catch (Exception exception)
        {
            var failed = timeProvider.GetUtcNow();
            var retryable = exception is ExternalServiceException { Failure: ExternalServiceFailure.QuotaExceeded or ExternalServiceFailure.Transient };
            await store.FailVideoResearchJobAsync(job.Id, activeAiRun?.Id,
                exception is YoutubeAiFactoryException ? exception.Message : "Research could not be completed. Try again later.",
                retryable, failed, retryable ? failed.AddSeconds(Math.Pow(2, job.RetryCount + 1) * 5) : null, CancellationToken.None);
            LogFailed(logger, job.Id, exception);
        }
        return true;
    }

    private async Task<(T Result, AiRun AiRun)> GenerateAsync<T>(string workflow, LlmRequest request, Action<T> validate,
        ResearchJobPayload payload, CancellationToken cancellationToken)
    {
        var resolvedModel = modelResolver.Resolve(request.ModelProfile);
        var run = new AiRun(workflow, payload.ProjectId, resolvedModel.Provider, resolvedModel.Model, request.PromptKey,
            request.PromptVersion, timeProvider.GetUtcNow(), resolvedModel.Profile.ToString(), payload.VideoProjectId, payload.ResearchRunId);
        store.AddAiRun(run);
        await store.SaveChangesAsync(cancellationToken);
        string? diagnostic = null;
        try
        {
            for (var attempt = 0; attempt <= options.MaxStructuredOutputRetries; attempt++)
            {
                try
                {
                    var answer = await provider.GenerateStructuredAsync<T>(request.WithResolvedModel(resolvedModel), cancellationToken);
                    validate(answer.Value);
                    run.RecordProvider(answer.Provider, answer.Model);
                    run.Complete(answer.InputTokens, answer.OutputTokens, null, timeProvider.GetUtcNow());
                    await store.SaveChangesAsync(cancellationToken);
                    return (answer.Value, run);
                }
                catch (Exception exception) when (exception is StructuredOutputException or ApplicationValidationException)
                {
                    diagnostic = exception.Message;
                    if (attempt == options.MaxStructuredOutputRetries) throw;
                    run.RecordRetry();
                }
            }
            throw new InvalidOperationException(diagnostic);
        }
        catch (Exception exception)
        {
            if (run.Status == AiRunStatus.Running)
            {
                run.Fail(exception is YoutubeAiFactoryException ? exception.Message : "Research AI stage failed.", timeProvider.GetUtcNow());
                await store.SaveChangesAsync(CancellationToken.None);
            }
            throw;
        }
    }

    private async Task<SearchOutcome> DiscoverAsync(ResearchPlan plan, string language, CancellationToken cancellationToken)
    {
        var results = new List<ResearchSearchResult>();
        var failures = 0;
        foreach (var query in plan.Queries.Take(options.MaxResearchQueries))
        {
            try
            {
                var response = await searchClient.SearchAsync(new ResearchSearchRequest(query.Query, language, options.MaxSearchResultsPerQuery), cancellationToken);
                results.AddRange(response.Results.Take(options.MaxSearchResultsPerQuery));
            }
            catch (ExternalServiceException) { failures++; }
        }
        var unique = new List<ResearchSearchResult>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var result in results)
        {
            try
            {
                if (seen.Add(ResearchUrlCanonicalizer.Canonicalize(result.Url))) unique.Add(result);
            }
            catch (ArgumentException) { }
        }
        if (unique.Count == 0) throw new ApplicationValidationException("Research search returned no usable public source URLs.");
        return new SearchOutcome(unique, results.Count, failures);
    }

    private async Task<FetchOutcome> FetchSourcesAsync(IReadOnlyList<ResearchSearchResult> results, Guid researchRunId, CancellationToken cancellationToken)
    {
        var allSources = new List<FetchedSource>();
        var usable = new List<FetchedSource>();
        var fetchFailures = 0;
        var seenFinalUrls = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var seenHashes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var candidates = results.Take(options.MaxSourcesToFetch).ToArray();
        var fetchedResponses = new ResearchContentFetchResult[candidates.Length];
        await Parallel.ForEachAsync(Enumerable.Range(0, candidates.Length), new ParallelOptions
        {
            MaxDegreeOfParallelism = options.MaxConcurrentSourceFetches,
            CancellationToken = cancellationToken,
        }, async (index, token) =>
        {
            var requestedUrl = ResearchUrlCanonicalizer.Canonicalize(candidates[index].Url);
            try
            {
                fetchedResponses[index] = await contentFetcher.FetchAsync(
                    new ResearchContentFetchRequest(requestedUrl, options.MaxSourceCharacters), token);
            }
            catch (ExternalServiceException)
            {
                fetchedResponses[index] = new ResearchContentFetchResult(ResearchSourceFetchStatus.Failed, requestedUrl,
                    null, null, null, null, "Source could not be retrieved.", 0);
            }
        });
        for (var index = 0; index < candidates.Length; index++)
        {
            var result = candidates[index];
            var requestedCanonical = ResearchUrlCanonicalizer.Canonicalize(result.Url);
            var fetched = fetchedResponses[index];
            var finalUrl = fetched.FinalUrl ?? requestedCanonical;
            string canonical;
            try { canonical = ResearchUrlCanonicalizer.Canonicalize(finalUrl); }
            catch (ArgumentException) { canonical = requestedCanonical; }
            var text = fetched.ExtractedText?.Trim();
            var hash = string.IsNullOrWhiteSpace(text) ? null : Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(text)));
            var status = fetched.Status;
            if (status == ResearchSourceFetchStatus.Fetched && (string.IsNullOrWhiteSpace(text) || text.Length < 150)) status = ResearchSourceFetchStatus.Empty;
            if (status == ResearchSourceFetchStatus.Fetched && !seenFinalUrls.Add(canonical)) continue;
            if (status == ResearchSourceFetchStatus.Fetched && hash is not null && !seenHashes.Add(hash)) status = ResearchSourceFetchStatus.Duplicate;
            if (status is not ResearchSourceFetchStatus.Fetched) fetchFailures++;
            var domain = Uri.TryCreate(canonical, UriKind.Absolute, out var uri) ? uri.Host : "unknown";
            var source = new ResearchSource(researchRunId, requestedCanonical, canonical, domain, Trim(fetched.Title ?? result.Title, 1_000),
                domain, result.PublishedAt, timeProvider.GetUtcNow(), Categorize(domain), status, hash, null, fetched.FailureReason);
            var item = new FetchedSource(source, status == ResearchSourceFetchStatus.Fetched ? text! : string.Empty);
            allSources.Add(item);
            if (status == ResearchSourceFetchStatus.Fetched) usable.Add(item);
        }
        return new FetchOutcome(allSources, usable, fetchFailures);
    }

    private static ResearchSourceCategory Categorize(string domain) => domain.EndsWith(".gov", StringComparison.OrdinalIgnoreCase) ? ResearchSourceCategory.GovernmentOfficial :
        domain.EndsWith(".edu", StringComparison.OrdinalIgnoreCase) ? ResearchSourceCategory.Academic : ResearchSourceCategory.Unknown;

    private static string? Trim(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var trimmed = value.Trim();
        return trimmed[..Math.Min(maxLength, trimmed.Length)];
    }

    private void ValidatePlan(ResearchQueryPlanResult value)
    {
        if (string.IsNullOrWhiteSpace(value.Objective) || value.Questions.Count == 0 || value.Queries.Count == 0 ||
            value.Questions.Count > 20 || value.Queries.Count > options.MaxResearchQueries ||
            value.PriorityFactAreas.Count > 20 || value.KnownRisks.Count > 20 ||
            value.Queries.Any(item => string.IsNullOrWhiteSpace(item.Query) || string.IsNullOrWhiteSpace(item.Purpose)))
            throw new ApplicationValidationException("Research query planning did not return a usable bounded plan.");
    }

    private static void ValidateRelevance(ResearchSourceRelevanceResult value)
    {
        if (string.IsNullOrWhiteSpace(value.Reasoning)) throw new ApplicationValidationException("Source relevance output is incomplete.");
    }

    private static void ValidateEvidenceExtraction(ResearchEvidenceExtractionResult value, string sourceText)
    {
        if (value.Evidence.Count == 0) throw new ApplicationValidationException("Evidence extraction did not return source-bound evidence.");
        var normalizedSource = NormalizeSpace(sourceText);
        foreach (var item in value.Evidence)
        {
            if (string.IsNullOrWhiteSpace(item.Fact) || string.IsNullOrWhiteSpace(item.SupportingExcerpt) ||
                string.IsNullOrWhiteSpace(item.SourceLocator) || item.Fact.Length > 4_000 || item.SupportingExcerpt.Length > 2_000 ||
                item.SourceLocator.Length > 500 || item.Confidence is < 0 or > 100 ||
                !normalizedSource.Contains(NormalizeSpace(item.SupportingExcerpt), StringComparison.OrdinalIgnoreCase))
                throw new ApplicationValidationException("Evidence extraction returned an unsupported or invalid excerpt.");
        }
    }

    private static void ValidateConflicts(ResearchContradictionAnalysisResult value, IEnumerable<Guid> claimIds,
        IEnumerable<Guid> evidenceIds, IReadOnlyList<ResearchClaimEvidence> claimEvidence)
    {
        var claims = claimIds.ToHashSet(); var evidence = evidenceIds.ToHashSet();
        if (value.Conflicts.Any(item => !claims.Contains(item.ClaimId) || !evidence.Contains(item.SupportingEvidenceId) ||
            !evidence.Contains(item.ContradictingEvidenceId) || item.SupportingEvidenceId == item.ContradictingEvidenceId ||
            !claimEvidence.Any(link => link.ResearchClaimId == item.ClaimId && link.ResearchEvidenceId == item.SupportingEvidenceId && link.Stance == ResearchEvidenceStance.Support) ||
            string.IsNullOrWhiteSpace(item.Explanation) || item.Explanation.Length > 2_000))
            throw new ApplicationValidationException("Contradiction analysis referenced evidence outside this research run.");
    }

    private static void ValidateSynthesis(ResearchSynthesisResult value, IEnumerable<ResearchClaim> claims,
        IReadOnlyList<ResearchClaimEvidence> links, Dictionary<Guid, ResearchEvidence> evidence)
    {
        if (string.IsNullOrWhiteSpace(value.ExecutiveSummary) || value.ExecutiveSummary.Length > 5_000) throw new ApplicationValidationException("Research synthesis is incomplete.");
        var claimMap = claims.ToDictionary(item => item.Id);
        foreach (var finding in value.KeyFindings)
        {
            if (string.IsNullOrWhiteSpace(finding.Summary) || finding.ClaimIds.Count == 0 || finding.EvidenceIds.Count == 0 || finding.ClaimIds.Any(id => !claimMap.ContainsKey(id)) ||
                finding.EvidenceIds.Any(id => !evidence.ContainsKey(id)) || finding.ClaimIds.Any(id => claimMap[id].SupportStatus == ResearchClaimSupportStatus.Unsupported))
                throw new ApplicationValidationException("Research synthesis referenced an invalid or unsupported claim.");
            var permittedEvidence = links.Where(link => finding.ClaimIds.Contains(link.ResearchClaimId)).Select(link => link.ResearchEvidenceId).ToHashSet();
            if (finding.EvidenceIds.Any(id => !permittedEvidence.Contains(id)))
                throw new ApplicationValidationException("Research synthesis cited evidence that does not support its claim.");
        }
        if (value.Gaps.Any(gap => string.IsNullOrWhiteSpace(gap.Description) || gap.ClaimIds.Any(id => !claimMap.ContainsKey(id))))
            throw new ApplicationValidationException("Research synthesis referenced an invalid research gap claim.");
    }

    private static string NormalizeSpace(string value) => string.Join(' ', value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

    private static List<ClaimBuildItem> BuildClaims(Guid reportId, List<ResearchEvidence> evidence, DateTimeOffset createdAt, int maxClaims)
    {
        return evidence.GroupBy(item => ResearchSupportAnalyzer.NormalizeStatement(item.Fact), StringComparer.Ordinal)
            .Where(group => !string.IsNullOrEmpty(group.Key)).Take(maxClaims).Select(group =>
            {
                var representative = group.OrderByDescending(item => item.Confidence).First();
                var type = representative.Type switch
                {
                    ResearchEvidenceType.Statistic => ResearchClaimType.Numerical,
                    ResearchEvidenceType.Date or ResearchEvidenceType.TimelineEvent => ResearchClaimType.Historical,
                    ResearchEvidenceType.Mechanism => ResearchClaimType.Mechanism,
                    ResearchEvidenceType.Example => ResearchClaimType.Anecdotal,
                    ResearchEvidenceType.Interpretation => ResearchClaimType.Interpretive,
                    _ => ResearchClaimType.Factual,
                };
                var critical = type is ResearchClaimType.Numerical or ResearchClaimType.Historical || representative.Type == ResearchEvidenceType.Fact;
                var confidence = decimal.Round(group.Average(item => item.Confidence), 2);
                return new ClaimBuildItem(new ResearchClaim(reportId, representative.Fact, type, confidence, critical, createdAt), group.Select(item => item.Id).ToArray());
            }).ToList();
    }

    private static ClaimForPrompt[] ToPromptClaims(IReadOnlyList<ClaimBuildItem> claims, IReadOnlyList<ResearchClaimEvidence> links) =>
        claims.Select(item => new ClaimForPrompt(item.Claim.Id, item.Claim.Statement, item.Claim.Type.ToString(), item.Claim.SupportStatus.ToString(),
            item.Claim.IsCritical, item.Claim.Confidence, links.Where(link => link.ResearchClaimId == item.Claim.Id).Select(link => link.ResearchEvidenceId).Distinct().ToArray())).ToArray();

    private static EvidenceForPrompt[] ToPromptEvidence(List<ResearchEvidence> evidence, Dictionary<Guid, ResearchSource> sourceById) =>
        evidence.Select(item => new EvidenceForPrompt(item.Id, item.ResearchSourceId, item.Fact, item.SupportingExcerpt, item.Type.ToString(), item.Confidence,
            sourceById[item.ResearchSourceId].Domain)).ToArray();

    private static List<ResearchGap> BuildDeterministicGaps(IEnumerable<ResearchClaim> claims, int fetchFailures)
    {
        var gaps = new List<ResearchGap>();
        foreach (var claim in claims.Where(item => item.SupportStatus == ResearchClaimSupportStatus.Unsupported))
            gaps.Add(new ResearchGap("No validated supporting evidence was retained for this claim.", [claim.Id]));
        foreach (var claim in claims.Where(item => item.SupportStatus == ResearchClaimSupportStatus.Conflicted))
            gaps.Add(new ResearchGap("Available evidence contains unresolved disagreement; do not state this claim as definitive.", [claim.Id]));
        if (fetchFailures > 0) gaps.Add(new ResearchGap("Some discovered sources could not be fetched or used in this run.", []));
        return gaps;
    }

    private static ResearchGap[] MergeGaps(IReadOnlyList<ResearchGap> first, IReadOnlyList<ResearchGap> second) =>
        first.Concat(second).GroupBy(item => (item.Description, string.Join(',', item.ClaimIds.Order()))).Select(group => group.First()).ToArray();

    private static List<string> BuildLimitations(int searchFailures, int fetchFailures, IEnumerable<ResearchClaim> claims)
    {
        var limitations = new List<string>();
        if (searchFailures > 0) limitations.Add("Some planned search queries failed, so coverage is not exhaustive.");
        if (fetchFailures > 0) limitations.Add("Some discovered sources could not be retrieved or parsed.");
        if (claims.Any(item => item.SupportStatus == ResearchClaimSupportStatus.Supported)) limitations.Add("Some claims have support from only one independent source.");
        if (claims.Any(item => item.SupportStatus == ResearchClaimSupportStatus.Conflicted)) limitations.Add("Some collected evidence remains disputed.");
        return limitations;
    }

    private static string[] MergeStrings(IEnumerable<string> first, IEnumerable<string> second) => first.Concat(second)
        .Where(item => !string.IsNullOrWhiteSpace(item)).Distinct(StringComparer.Ordinal).ToArray();

    private static ResearchMetricsDto BuildMetrics(ResearchPlan plan, SearchOutcome search, FetchOutcome fetch,
        List<ResearchEvidence> evidence, IEnumerable<ResearchClaim> claims, List<ResearchConflict> conflicts,
        Dictionary<Guid, ResearchSource> sources)
    {
        var materialized = claims.ToArray();
        return new ResearchMetricsDto(plan.Queries.Count, search.SearchResultCount, fetch.UsableSources.Count,
            sources.Count, sources.Values.Select(item => item.Domain).Distinct(StringComparer.OrdinalIgnoreCase).Count(), evidence.Count,
            materialized.Length, materialized.Count(item => item.SupportStatus == ResearchClaimSupportStatus.Corroborated),
            materialized.Count(item => item.SupportStatus == ResearchClaimSupportStatus.Supported),
            materialized.Count(item => item.SupportStatus == ResearchClaimSupportStatus.Conflicted),
            materialized.Count(item => item.SupportStatus == ResearchClaimSupportStatus.Unsupported), conflicts.Count,
            search.SearchFailureCount, fetch.FetchFailureCount);
    }

    private static ResearchConfidenceSummary BuildConfidence(ResearchMetricsDto metrics, IEnumerable<ResearchClaim> claims)
    {
        var unsupportedCritical = claims.Count(item => item.IsCritical && item.SupportStatus == ResearchClaimSupportStatus.Unsupported);
        var level = metrics.RelevantSourceCount >= 3 && metrics.CorroboratedClaimCount > 0 && metrics.ConflictedClaimCount == 0 ? "High" :
            metrics.EvidenceCount > 0 ? "Medium" : "Low";
        return new ResearchConfidenceSummary(level, metrics.FetchedSourceCount, metrics.RelevantSourceCount, metrics.ClaimCount,
            metrics.CorroboratedClaimCount, metrics.SingleSourceClaimCount, metrics.ConflictedClaimCount, unsupportedCritical);
    }

    private static ResearchRunMetrics ToRunMetrics(ResearchPlan plan, SearchOutcome search, FetchOutcome fetch,
        int relevantSourceCount, int evidenceCount, int claimCount, int conflictCount) => new(plan.Queries.Count, search.SearchResultCount,
        fetch.UsableSources.Count, relevantSourceCount, evidenceCount, claimCount, conflictCount,
        search.SearchFailureCount, fetch.FetchFailureCount);

    private sealed record FetchedSource(ResearchSource Source, string Text);
    private sealed record SearchOutcome(IReadOnlyList<ResearchSearchResult> Results, int SearchResultCount, int SearchFailureCount);
    private sealed record FetchOutcome(IReadOnlyList<FetchedSource> AllSources, IReadOnlyList<FetchedSource> UsableSources, int FetchFailureCount);
    private sealed record ClaimBuildItem(ResearchClaim Claim, IReadOnlyList<Guid> EvidenceIds);
}

internal static class ResearchDtoMapper
{
    public static async Task<ResearchReportDto> MapAsync(IYoutubeAiFactoryStore store, ResearchReportWithDetails details,
        VideoProject videoProject, CancellationToken cancellationToken)
    {
        var result = JsonSerializer.Deserialize<ResearchReportPayload>(details.Report.ResultJson, ResearchPrompts.SerializerOptions)
            ?? throw new InvalidOperationException("Stored research report is unreadable.");
        var project = await store.GetProjectAsync(details.Report.ProjectId, cancellationToken)
            ?? throw new ResourceNotFoundException("Project was not found.");
        var sourceContext = await store.GetVideoProjectSourceAsync(details.Report.ProjectId, videoProject.PilotId, videoProject.PilotVideoId, cancellationToken)
            ?? throw new ResourceNotFoundException("Video project source context was not found.");
        var isStale = !string.Equals(details.Report.InputFingerprint,
            ResearchBriefBuilder.CreateFingerprint(ResearchBriefBuilder.Build(project, videoProject, sourceContext.Opportunity.Name)), StringComparison.Ordinal);
        var linksByClaim = details.ClaimEvidence.GroupBy(item => item.ResearchClaimId).ToDictionary(group => group.Key, group => group.ToArray());
        return new ResearchReportDto(details.Report.Id, details.Report.Version, details.Report.ResearchAlgorithmVersion, details.Report.CreatedAt,
            isStale, result.Synthesis, result.Confidence, result.Metrics,
            details.Sources.Select(item => new ResearchSourceDto(item.Id, item.Url, item.CanonicalUrl, item.Domain, item.Title, item.Publisher,
                item.PublishedAt, item.RetrievedAt, item.Category.ToString(), item.FetchStatus.ToString())).ToArray(),
            details.Evidence.Select(item => new ResearchEvidenceDto(item.Id, item.ResearchSourceId, item.Type.ToString(), item.Fact,
                item.SupportingExcerpt, item.SourceLocator, item.Confidence)).ToArray(),
            details.Claims.Select(item => new ResearchClaimDto(item.Id, item.Statement, item.Type.ToString(), item.SupportStatus.ToString(),
                item.Confidence, item.IsCritical, linksByClaim.TryGetValue(item.Id, out var links)
                    ? links.Select(link => new ResearchClaimEvidenceDto(link.ResearchEvidenceId, link.Stance.ToString())).ToArray() : [])).ToArray(),
            details.Conflicts.Select(item => new ResearchConflictDto(item.Id, item.ResearchClaimId, item.SupportingEvidenceId,
                item.ContradictingEvidenceId, item.Explanation, item.IsResolved)).ToArray());
    }
}
