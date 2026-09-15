using System.Text.Json;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using YoutubeAiFactory.Application.Common;
using YoutubeAiFactory.Application.Ideas;
using YoutubeAiFactory.Application.Opportunities;
using YoutubeAiFactory.Application.Persistence;
using YoutubeAiFactory.Application.Pilots;
using YoutubeAiFactory.Application.Research;
using YoutubeAiFactory.Application.Videos;
using YoutubeAiFactory.Domain.AI;
using YoutubeAiFactory.Domain.Competitors;
using YoutubeAiFactory.Domain.Ideas;
using YoutubeAiFactory.Domain.Jobs;
using YoutubeAiFactory.Domain.Localization;
using YoutubeAiFactory.Domain.Opportunities;
using YoutubeAiFactory.Domain.Pilots;
using YoutubeAiFactory.Domain.Projects;
using YoutubeAiFactory.Domain.Research;
using YoutubeAiFactory.Domain.Videos;

namespace YoutubeAiFactory.Infrastructure.Persistence;

internal sealed class YoutubeAiFactoryStore(YoutubeAiFactoryDbContext dbContext)
    : IYoutubeAiFactoryStore
{
    private static readonly JsonSerializerOptions ResearchPayloadSerializerOptions = new(JsonSerializerDefaults.Web);
    public Task<bool> ProjectExistsAsync(Guid projectId, CancellationToken cancellationToken) =>
        dbContext.Projects.AnyAsync(project => project.Id == projectId, cancellationToken);

    public Task<Project?> GetProjectAsync(Guid projectId, CancellationToken cancellationToken) =>
        dbContext.Projects
            .AsNoTracking()
            .SingleOrDefaultAsync(project => project.Id == projectId, cancellationToken);

    public async Task<IReadOnlyList<Project>> ListProjectsAsync(CancellationToken cancellationToken) =>
        await dbContext.Projects
            .AsNoTracking()
            .OrderByDescending(project => project.CreatedAt)
            .ToListAsync(cancellationToken);

    public void AddProject(Project project) => dbContext.Projects.Add(project);

    public async Task<IReadOnlyList<CompetitorChannel>> ListCompetitorsAsync(
        Guid projectId,
        CancellationToken cancellationToken) =>
        await dbContext.CompetitorChannels
            .AsNoTracking()
            .Where(channel => channel.ProjectId == projectId)
            .OrderBy(channel => channel.Title)
            .ToListAsync(cancellationToken);

    public Task<CompetitorChannel?> GetCompetitorAsync(
        Guid projectId,
        Guid competitorId,
        bool forUpdate,
        CancellationToken cancellationToken)
    {
        var query = dbContext.CompetitorChannels
            .Include(channel => channel.Videos)
            .Where(channel => channel.ProjectId == projectId && channel.Id == competitorId);

        return (forUpdate ? query : query.AsNoTracking())
            .SingleOrDefaultAsync(cancellationToken);
    }

    public Task<CompetitorChannel?> FindCompetitorByYoutubeChannelIdAsync(
        Guid projectId,
        string youtubeChannelId,
        CancellationToken cancellationToken) =>
        dbContext.CompetitorChannels
            .Include(channel => channel.Videos)
            .SingleOrDefaultAsync(
                channel => channel.ProjectId == projectId &&
                    channel.YoutubeChannelId == youtubeChannelId,
                cancellationToken);

    public void AddCompetitor(CompetitorChannel competitor) =>
        dbContext.CompetitorChannels.Add(competitor);

    public Task<CompetitorAnalysis?> GetLatestCompetitorAnalysisAsync(
        Guid projectId, Guid competitorId, CancellationToken cancellationToken) =>
        dbContext.CompetitorAnalyses.AsNoTracking()
            .Where(analysis => analysis.CompetitorChannelId == competitorId &&
                dbContext.CompetitorChannels.Any(channel => channel.Id == competitorId && channel.ProjectId == projectId))
            .OrderByDescending(analysis => analysis.Version)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<int> GetNextCompetitorAnalysisVersionAsync(Guid competitorId, CancellationToken cancellationToken) =>
        (await dbContext.CompetitorAnalyses.Where(analysis => analysis.CompetitorChannelId == competitorId)
            .Select(analysis => (int?)analysis.Version).MaxAsync(cancellationToken) ?? 0) + 1;

    public Task<CompetitorAnalysis?> GetCompetitorAnalysisAsync(Guid projectId, Guid analysisId, CancellationToken cancellationToken) =>
        dbContext.CompetitorAnalyses.AsNoTracking().Where(analysis => analysis.Id == analysisId)
            .Join(dbContext.CompetitorChannels.AsNoTracking().Where(channel => channel.ProjectId == projectId), analysis => analysis.CompetitorChannelId, channel => channel.Id, (analysis, _) => analysis)
            .SingleOrDefaultAsync(cancellationToken);

    public Task<ArtifactLocalization?> GetArtifactLocalizationAsync(string artifactType, Guid artifactId, int artifactVersion, string locale, CancellationToken cancellationToken) =>
        dbContext.ArtifactLocalizations.AsNoTracking().SingleOrDefaultAsync(item => item.ArtifactType == artifactType && item.ArtifactId == artifactId && item.ArtifactVersion == artifactVersion && item.Locale == locale, cancellationToken);

    public Task<Job?> GetActiveArtifactLocalizationJobAsync(string artifactType, Guid artifactId, int artifactVersion, string locale, CancellationToken cancellationToken) =>
        FindArtifactLocalizationJobAsync(artifactType, artifactId, artifactVersion, locale, true, cancellationToken);

    public Task<Job?> GetLatestArtifactLocalizationJobAsync(string artifactType, Guid artifactId, int artifactVersion, string locale, CancellationToken cancellationToken) =>
        FindArtifactLocalizationJobAsync(artifactType, artifactId, artifactVersion, locale, false, cancellationToken);

    public async Task<Job> EnqueueArtifactLocalizationJobAsync(Job job, CancellationToken cancellationToken)
    {
        dbContext.Jobs.Add(job);
        try { await dbContext.SaveChangesAsync(cancellationToken); return job; }
        catch (DbUpdateException exception) when (IsUniqueConstraintViolation(exception))
        {
            dbContext.ChangeTracker.Clear();
            return await dbContext.Jobs.AsNoTracking().SingleAsync(candidate => candidate.Type == "artifact-localization" && candidate.ArtifactType == job.ArtifactType && candidate.ArtifactId == job.ArtifactId && candidate.ArtifactVersion == job.ArtifactVersion && candidate.Locale == job.Locale && (candidate.Status == JobStatus.Queued || candidate.Status == JobStatus.Running || candidate.Status == JobStatus.Retrying), cancellationToken);
        }
    }

    public Task<Job?> TryClaimNextArtifactLocalizationJobAsync(DateTimeOffset now, DateTimeOffset staleRunningBefore, CancellationToken cancellationToken) =>
        TryClaimJobAsync("artifact-localization", now, staleRunningBefore, cancellationToken);
    public Task RequeueArtifactLocalizationJobAsync(Guid jobId, CancellationToken cancellationToken) => RequeueJobAsync(jobId, cancellationToken);
    public Task FailArtifactLocalizationJobAsync(Guid jobId, Guid? aiRunId, string reason, bool retryable, DateTimeOffset failedAt, DateTimeOffset? retryAt, CancellationToken cancellationToken) =>
        FailJobAsync(jobId, aiRunId, reason, retryable, failedAt, retryAt, cancellationToken);
    public void AddArtifactLocalization(ArtifactLocalization localization) => dbContext.ArtifactLocalizations.Add(localization);
    public async Task DeleteArtifactLocalizationAsync(Guid localizationId, CancellationToken cancellationToken)
    {
        var localization = await dbContext.ArtifactLocalizations.SingleAsync(item => item.Id == localizationId, cancellationToken);
        dbContext.ArtifactLocalizations.Remove(localization);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<CurrentCompetitorAnalysis>> GetCurrentCompetitorAnalysesForProjectAsync(Guid projectId, CancellationToken cancellationToken)
    {
        var analyses = await dbContext.CompetitorAnalyses.AsNoTracking()
            .Join(dbContext.CompetitorChannels.AsNoTracking().Where(channel => channel.ProjectId == projectId), analysis => analysis.CompetitorChannelId, channel => channel.Id,
                (analysis, channel) => new { analysis, channel })
            .ToListAsync(cancellationToken);
        return analyses.GroupBy(item => item.channel.Id).Select(group => group.OrderByDescending(item => item.analysis.Version).First())
            .Select(item => new CurrentCompetitorAnalysis(item.channel.Id, item.channel.Title!, item.analysis)).ToArray();
    }

    public async Task<OpportunityReportWithDetails?> GetLatestOpportunityReportAsync(Guid projectId, CancellationToken cancellationToken)
    {
        var report = await dbContext.OpportunityReports.AsNoTracking().Where(item => item.ProjectId == projectId).OrderByDescending(item => item.Version).FirstOrDefaultAsync(cancellationToken);
        if (report is null) return null;
        var sources = await dbContext.OpportunityReportSources.AsNoTracking().Where(item => item.ReportId == report.Id).ToListAsync(cancellationToken);
        var candidates = await dbContext.OpportunityCandidates.AsNoTracking().Where(item => item.ReportId == report.Id).ToListAsync(cancellationToken);
        var candidateIds = candidates.Select(item => item.Id).ToArray();
        var evidence = await dbContext.OpportunityEvidence.AsNoTracking().Where(item => candidateIds.Contains(item.CandidateId)).ToListAsync(cancellationToken);
        return new OpportunityReportWithDetails(report, sources, candidates.Select(item => new OpportunityCandidateWithEvidence(item, evidence.Where(e => e.CandidateId == item.Id).ToArray())).ToArray(), []);
    }

    public async Task<OpportunityReportWithDetails?> GetOpportunityReportAsync(Guid projectId, Guid reportId, CancellationToken cancellationToken)
    {
        var report = await dbContext.OpportunityReports.AsNoTracking().SingleOrDefaultAsync(item => item.ProjectId == projectId && item.Id == reportId, cancellationToken);
        if (report is null) return null;
        var sources = await dbContext.OpportunityReportSources.AsNoTracking().Where(item => item.ReportId == report.Id).ToListAsync(cancellationToken);
        var candidates = await dbContext.OpportunityCandidates.AsNoTracking().Where(item => item.ReportId == report.Id).ToListAsync(cancellationToken);
        var ids = candidates.Select(item => item.Id).ToArray();
        var evidence = await dbContext.OpportunityEvidence.AsNoTracking().Where(item => ids.Contains(item.CandidateId)).ToListAsync(cancellationToken);
        return new OpportunityReportWithDetails(report, sources, candidates.Select(item => new OpportunityCandidateWithEvidence(item, evidence.Where(e => e.CandidateId == item.Id).ToArray())).ToArray(), []);
    }

    public async Task<int> GetNextOpportunityReportVersionAsync(Guid projectId, CancellationToken cancellationToken) =>
        (await dbContext.OpportunityReports.Where(item => item.ProjectId == projectId).Select(item => (int?)item.Version).MaxAsync(cancellationToken) ?? 0) + 1;

    public Task<Job?> GetActiveOpportunityAnalysisJobAsync(Guid projectId, CancellationToken cancellationToken) => FindOpportunityJobAsync(projectId, true, cancellationToken);
    public Task<Job?> GetLatestOpportunityAnalysisJobAsync(Guid projectId, CancellationToken cancellationToken) => FindOpportunityJobAsync(projectId, false, cancellationToken);

    public async Task<Job> EnqueueOpportunityAnalysisJobAsync(Job job, CancellationToken cancellationToken)
    {
        dbContext.Jobs.Add(job);
        try { await dbContext.SaveChangesAsync(cancellationToken); return job; }
        catch (DbUpdateException exception) when (IsUniqueConstraintViolation(exception))
        {
            dbContext.ChangeTracker.Clear();
            return await dbContext.Jobs.AsNoTracking().SingleAsync(item => item.Type == "opportunity-analysis" && item.ProjectId == job.ProjectId && (item.Status == JobStatus.Queued || item.Status == JobStatus.Running || item.Status == JobStatus.Retrying), cancellationToken);
        }
    }

    public Task<Job?> TryClaimNextOpportunityAnalysisJobAsync(DateTimeOffset now, DateTimeOffset staleRunningBefore, CancellationToken cancellationToken) =>
        TryClaimJobAsync("opportunity-analysis", now, staleRunningBefore, cancellationToken);

    public Task RequeueOpportunityAnalysisJobAsync(Guid jobId, CancellationToken cancellationToken) => RequeueJobAsync(jobId, cancellationToken);
    public Task FailOpportunityAnalysisJobAsync(Guid jobId, Guid? aiRunId, string reason, bool retryable, DateTimeOffset failedAt, DateTimeOffset? retryAt, CancellationToken cancellationToken) =>
        FailJobAsync(jobId, aiRunId, reason, retryable, failedAt, retryAt, cancellationToken);
    public void AddOpportunityReport(OpportunityReport report) => dbContext.OpportunityReports.Add(report);
    public void AddOpportunityReportSource(OpportunityReportSource source) => dbContext.OpportunityReportSources.Add(source);
    public void AddOpportunityCandidate(OpportunityCandidate candidate) => dbContext.OpportunityCandidates.Add(candidate);
    public void AddOpportunityEvidence(OpportunityEvidence evidence) => dbContext.OpportunityEvidence.Add(evidence);
    public async Task<ApprovedOpportunityWithEvidence?> GetOpportunityWithEvidenceAsync(Guid projectId, Guid opportunityId, bool forUpdate, CancellationToken cancellationToken)
    {
        var candidates = dbContext.OpportunityCandidates.Join(dbContext.OpportunityReports.Where(x => x.ProjectId == projectId), candidate => candidate.ReportId, report => report.Id, (candidate, report) => new { candidate, report }).Where(x => x.candidate.Id == opportunityId);
        var found = await (forUpdate ? candidates : candidates.AsNoTracking()).SingleOrDefaultAsync(cancellationToken);
        if (found is null) return null;
        var evidenceQuery = dbContext.OpportunityEvidence.Where(x => x.CandidateId == opportunityId);
        var evidence = await (forUpdate ? evidenceQuery : evidenceQuery.AsNoTracking()).ToListAsync(cancellationToken);
        return new ApprovedOpportunityWithEvidence(found.candidate, found.report, evidence);
    }
    public async Task<IReadOnlyList<ExistingIdeaContext>> ListExistingIdeaContextAsync(Guid projectId, CancellationToken cancellationToken) =>
        await dbContext.VideoIdeas.AsNoTracking().Where(x => x.ProjectId == projectId && x.DecisionStatus != IdeaDecisionStatus.Rejected).OrderByDescending(x => x.CreatedAt).Select(x => new ExistingIdeaContext(x.Id, x.WorkingTitle, x.Topic, x.Angle, x.ContentFormat)).ToListAsync(cancellationToken);
    public async Task<IReadOnlyList<string>> ListCompetitorTitlesAsync(Guid projectId, CancellationToken cancellationToken) =>
        await dbContext.CompetitorVideos.AsNoTracking().Where(x => dbContext.CompetitorChannels.Any(c => c.Id == x.CompetitorChannelId && c.ProjectId == projectId)).Select(x => x.Title).Where(x => x != null).Cast<string>().ToListAsync(cancellationToken);
    public async Task<IReadOnlyList<IdeaGeneration>> ListIdeaGenerationsAsync(Guid projectId, Guid opportunityId, CancellationToken cancellationToken) =>
        await dbContext.IdeaGenerations.AsNoTracking().Where(x => x.ProjectId == projectId && x.OpportunityId == opportunityId).OrderByDescending(x => x.Version).ToListAsync(cancellationToken);

    public Task<IdeaGeneration?> GetIdeaGenerationAsync(Guid projectId, Guid generationId, CancellationToken cancellationToken) =>
        dbContext.IdeaGenerations.AsNoTracking().SingleOrDefaultAsync(x => x.ProjectId == projectId && x.Id == generationId, cancellationToken);
    public async Task<IReadOnlyList<VideoIdeaWithEvidence>> ListVideoIdeasAsync(Guid projectId, Guid opportunityId, CancellationToken cancellationToken)
    {
        var ideas = await dbContext.VideoIdeas.AsNoTracking().Where(x => x.ProjectId == projectId && x.OpportunityId == opportunityId).ToListAsync(cancellationToken); var ids = ideas.Select(x => x.Id).ToArray(); var evidence = await dbContext.IdeaEvidence.AsNoTracking().Where(x => ids.Contains(x.IdeaId)).ToListAsync(cancellationToken); return ideas.Select(x => new VideoIdeaWithEvidence(x, evidence.Where(e => e.IdeaId == x.Id).ToArray())).ToArray();
    }
    public async Task<VideoIdeaWithEvidence?> GetVideoIdeaAsync(Guid projectId, Guid ideaId, bool forUpdate, CancellationToken cancellationToken)
    {
        var query = dbContext.VideoIdeas.Where(x => x.ProjectId == projectId && x.Id == ideaId); var idea = await (forUpdate ? query : query.AsNoTracking()).SingleOrDefaultAsync(cancellationToken); if (idea is null) return null; var evidenceQuery = dbContext.IdeaEvidence.Where(x => x.IdeaId == ideaId); var evidence = await (forUpdate ? evidenceQuery : evidenceQuery.AsNoTracking()).ToListAsync(cancellationToken); return new VideoIdeaWithEvidence(idea, evidence);
    }
    public async Task<int> GetNextIdeaGenerationVersionAsync(Guid opportunityId, CancellationToken cancellationToken) => (await dbContext.IdeaGenerations.Where(x => x.OpportunityId == opportunityId).Select(x => (int?)x.Version).MaxAsync(cancellationToken) ?? 0) + 1;
    public Task<Job?> GetActiveIdeaGenerationJobAsync(Guid projectId, Guid opportunityId, CancellationToken cancellationToken) => FindIdeaJobAsync(projectId, opportunityId, true, cancellationToken);
    public Task<Job?> GetLatestIdeaGenerationJobAsync(Guid projectId, Guid opportunityId, CancellationToken cancellationToken) => FindIdeaJobAsync(projectId, opportunityId, false, cancellationToken);
    public async Task<Job> EnqueueIdeaGenerationJobAsync(Job job, CancellationToken cancellationToken) { dbContext.Jobs.Add(job); try { await dbContext.SaveChangesAsync(cancellationToken); return job; } catch (DbUpdateException exception) when (IsUniqueConstraintViolation(exception)) { dbContext.ChangeTracker.Clear(); return await dbContext.Jobs.AsNoTracking().SingleAsync(x => x.Type == "idea-generation" && x.OpportunityId == job.OpportunityId && (x.Status == JobStatus.Queued || x.Status == JobStatus.Running || x.Status == JobStatus.Retrying), cancellationToken); } }
    public Task<Job?> TryClaimNextIdeaGenerationJobAsync(DateTimeOffset now, DateTimeOffset staleRunningBefore, CancellationToken cancellationToken) => TryClaimJobAsync("idea-generation", now, staleRunningBefore, cancellationToken);
    public Task RequeueIdeaGenerationJobAsync(Guid jobId, CancellationToken cancellationToken) => RequeueJobAsync(jobId, cancellationToken);
    public Task FailIdeaGenerationJobAsync(Guid jobId, Guid? aiRunId, string reason, bool retryable, DateTimeOffset failedAt, DateTimeOffset? retryAt, CancellationToken cancellationToken) => FailJobAsync(jobId, aiRunId, reason, retryable, failedAt, retryAt, cancellationToken);
    public void AddIdeaGeneration(IdeaGeneration generation) => dbContext.IdeaGenerations.Add(generation); public void AddVideoIdea(VideoIdea idea) => dbContext.VideoIdeas.Add(idea); public void AddIdeaEvidence(IdeaEvidence evidence) => dbContext.IdeaEvidence.Add(evidence);

    public Task<IReadOnlyList<PilotIdeaContext>> ListApprovedPilotIdeasAsync(Guid projectId, CancellationToken cancellationToken) => ListPilotIdeaContextInternalAsync(projectId, true, cancellationToken);
    public Task<IReadOnlyList<PilotIdeaContext>> ListPilotIdeaContextAsync(Guid projectId, CancellationToken cancellationToken) => ListPilotIdeaContextInternalAsync(projectId, false, cancellationToken);
    private async Task<IReadOnlyList<PilotIdeaContext>> ListPilotIdeaContextInternalAsync(Guid projectId, bool approvedOnly, CancellationToken cancellationToken)
    {
        var ideaQuery = dbContext.VideoIdeas.AsNoTracking().Where(x => x.ProjectId == projectId);
        if (approvedOnly) ideaQuery = ideaQuery.Where(x => x.DecisionStatus == IdeaDecisionStatus.Approved);
        var ideas = await ideaQuery
            .Join(dbContext.OpportunityCandidates.AsNoTracking(), idea => idea.OpportunityId, opportunity => opportunity.Id, (idea, opportunity) => new { idea, opportunity })
            .Where(x => !approvedOnly || x.opportunity.DecisionStatus == OpportunityDecisionStatus.Approved)
            .ToListAsync(cancellationToken);
        var topicCounts = ideas.GroupBy(x => x.idea.Topic, StringComparer.OrdinalIgnoreCase).ToDictionary(x => x.Key, x => x.Count(), StringComparer.OrdinalIgnoreCase);
        var formatCounts = ideas.GroupBy(x => x.idea.ContentFormat, StringComparer.OrdinalIgnoreCase).ToDictionary(x => x.Key, x => x.Count(), StringComparer.OrdinalIgnoreCase);
        var opportunityCounts = ideas.GroupBy(x => x.idea.OpportunityId).ToDictionary(x => x.Key, x => x.Count());
        return ideas.Select(x => new PilotIdeaContext(x.idea.Id, x.idea.OpportunityId, x.opportunity.Name, x.idea.WorkingTitle, x.idea.Topic, x.idea.Angle, x.idea.ContentFormat, x.idea.TargetAudience, x.idea.HookConcept, x.idea.ThumbnailConcept, x.idea.ViewerPromise, x.idea.Hypothesis, x.idea.OverallScore, x.idea.EvidenceStrength, x.idea.ProductionEase, x.idea.Novelty, x.idea.StoryPotential, x.idea.DecisionStatus, x.opportunity.DecisionStatus, topicCounts[x.idea.Topic], formatCounts[x.idea.ContentFormat], opportunityCounts[x.idea.OpportunityId])).ToArray();
    }
    public Task<Pilot?> GetLatestPilotAsync(Guid projectId, CancellationToken cancellationToken) => dbContext.Pilots.AsNoTracking().Where(x => x.ProjectId == projectId).OrderByDescending(x => x.Version).FirstOrDefaultAsync(cancellationToken);
    public Task<Pilot?> GetPilotAsync(Guid projectId, Guid pilotId, bool forUpdate, CancellationToken cancellationToken)
    {
        var query = dbContext.Pilots.Where(x => x.ProjectId == projectId && x.Id == pilotId);
        return (forUpdate ? query : query.AsNoTracking()).SingleOrDefaultAsync(cancellationToken);
    }
    public async Task<IReadOnlyList<Pilot>> ListPilotsAsync(Guid projectId, CancellationToken cancellationToken) => await dbContext.Pilots.AsNoTracking().Where(x => x.ProjectId == projectId).OrderByDescending(x => x.Version).ToListAsync(cancellationToken);
    public async Task<IReadOnlyList<PilotVideo>> ListPilotVideosAsync(Guid pilotId, bool forUpdate, CancellationToken cancellationToken)
    {
        var query = dbContext.PilotVideos.Where(x => x.PilotId == pilotId); return await (forUpdate ? query : query.AsNoTracking()).OrderBy(x => x.Sequence).ToListAsync(cancellationToken);
    }
    public async Task<int> GetNextPilotVersionAsync(Guid projectId, CancellationToken cancellationToken) => (await dbContext.Pilots.Where(x => x.ProjectId == projectId).Select(x => (int?)x.Version).MaxAsync(cancellationToken) ?? 0) + 1;
    public Task<Job?> GetActivePilotGenerationJobAsync(Guid projectId, CancellationToken cancellationToken) => FindPilotJobAsync(projectId, true, cancellationToken);
    public Task<Job?> GetLatestPilotGenerationJobAsync(Guid projectId, CancellationToken cancellationToken) => FindPilotJobAsync(projectId, false, cancellationToken);
    public async Task<Job> EnqueuePilotGenerationJobAsync(Job job, CancellationToken cancellationToken)
    {
        dbContext.Jobs.Add(job); try { await dbContext.SaveChangesAsync(cancellationToken); return job; }
        catch (DbUpdateException exception) when (IsUniqueConstraintViolation(exception)) { dbContext.ChangeTracker.Clear(); return await dbContext.Jobs.AsNoTracking().SingleAsync(x => x.Type == "pilot-generation" && x.ProjectId == job.ProjectId && (x.Status == JobStatus.Queued || x.Status == JobStatus.Running || x.Status == JobStatus.Retrying), cancellationToken); }
    }
    public Task<Job?> TryClaimNextPilotGenerationJobAsync(DateTimeOffset now, DateTimeOffset staleRunningBefore, CancellationToken cancellationToken) => TryClaimJobAsync("pilot-generation", now, staleRunningBefore, cancellationToken);
    public Task RequeuePilotGenerationJobAsync(Guid jobId, CancellationToken cancellationToken) => RequeueJobAsync(jobId, cancellationToken);
    public Task FailPilotGenerationJobAsync(Guid jobId, Guid? aiRunId, string reason, bool retryable, DateTimeOffset failedAt, DateTimeOffset? retryAt, CancellationToken cancellationToken) => FailJobAsync(jobId, aiRunId, reason, retryable, failedAt, retryAt, cancellationToken);
    public void AddPilot(Pilot pilot) => dbContext.Pilots.Add(pilot);
    public void AddPilotVideo(PilotVideo pilotVideo) => dbContext.PilotVideos.Add(pilotVideo);

    public Task<VideoProject?> GetVideoProjectByPilotVideoAsync(Guid projectId, Guid pilotVideoId, CancellationToken cancellationToken) =>
        dbContext.VideoProjects.AsNoTracking().SingleOrDefaultAsync(x => x.ProjectId == projectId && x.PilotVideoId == pilotVideoId, cancellationToken);

    public Task<VideoProject?> GetVideoProjectAsync(Guid projectId, Guid videoProjectId, bool forUpdate, CancellationToken cancellationToken)
    {
        var query = dbContext.VideoProjects.Where(x => x.ProjectId == projectId && x.Id == videoProjectId);
        return (forUpdate ? query : query.AsNoTracking()).SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<VideoProject>> ListVideoProjectsAsync(Guid projectId, CancellationToken cancellationToken) =>
        await dbContext.VideoProjects.AsNoTracking().Where(x => x.ProjectId == projectId).OrderByDescending(x => x.CreatedAt).ToListAsync(cancellationToken);

    public async Task<VideoProjectSource?> GetVideoProjectSourceAsync(Guid projectId, Guid pilotId, Guid pilotVideoId, CancellationToken cancellationToken)
    {
        var source = await dbContext.PilotVideos.AsNoTracking()
            .Where(video => video.Id == pilotVideoId && video.PilotId == pilotId)
            .Join(dbContext.Pilots.AsNoTracking().Where(pilot => pilot.ProjectId == projectId), video => video.PilotId, pilot => pilot.Id, (video, pilot) => new { video, pilot })
            .Join(dbContext.VideoIdeas.AsNoTracking().Where(idea => idea.ProjectId == projectId), item => item.video.VideoIdeaId, idea => idea.Id, (item, idea) => new { item.video, item.pilot, idea })
            .Join(
                dbContext.OpportunityCandidates.AsNoTracking()
                    .Join(dbContext.OpportunityReports.AsNoTracking().Where(report => report.ProjectId == projectId), candidate => candidate.ReportId, report => report.Id, (candidate, _) => candidate),
                item => item.video.OpportunityId, opportunity => opportunity.Id,
                (item, opportunity) => new VideoProjectSource(item.pilot, item.video, item.idea, opportunity))
            .SingleOrDefaultAsync(cancellationToken);
        return source;
    }

    public async Task<VideoProject> CreateVideoProjectIfAbsentAsync(VideoProject project, CancellationToken cancellationToken)
    {
        dbContext.VideoProjects.Add(project);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return project;
        }
        catch (DbUpdateException exception) when (IsUniqueConstraintViolation(exception))
        {
            dbContext.ChangeTracker.Clear();
            return await dbContext.VideoProjects.AsNoTracking().SingleAsync(x => x.PilotVideoId == project.PilotVideoId, cancellationToken);
        }
    }

    public Task<Job?> GetActiveVideoResearchJobAsync(Guid projectId, Guid videoProjectId, CancellationToken cancellationToken) =>
        FindVideoResearchJobAsync(projectId, videoProjectId, true, cancellationToken);

    public Task<Job?> GetLatestVideoResearchJobAsync(Guid projectId, Guid videoProjectId, CancellationToken cancellationToken) =>
        FindVideoResearchJobAsync(projectId, videoProjectId, false, cancellationToken);

    public async Task<Job> EnqueueVideoResearchJobAsync(Job job, ResearchRun run, CancellationToken cancellationToken)
    {
        dbContext.Jobs.Add(job);
        dbContext.ResearchRuns.Add(run);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return job;
        }
        catch (DbUpdateException exception) when (IsUniqueConstraintViolation(exception))
        {
            dbContext.ChangeTracker.Clear();
            return await dbContext.Jobs.AsNoTracking().SingleAsync(candidate =>
                candidate.Type == "video-research" && candidate.VideoProjectId == job.VideoProjectId &&
                (candidate.Status == JobStatus.Queued || candidate.Status == JobStatus.Running || candidate.Status == JobStatus.Retrying), cancellationToken);
        }
    }

    public Task<Job?> TryClaimNextVideoResearchJobAsync(DateTimeOffset now, DateTimeOffset staleRunningBefore, CancellationToken cancellationToken) =>
        TryClaimJobAsync("video-research", now, staleRunningBefore, cancellationToken);

    public Task RequeueVideoResearchJobAsync(Guid jobId, CancellationToken cancellationToken) => RequeueJobAsync(jobId, cancellationToken);

    public async Task FailVideoResearchJobAsync(Guid jobId, Guid? aiRunId, string reason, bool retryable,
        DateTimeOffset failedAt, DateTimeOffset? retryAt, CancellationToken cancellationToken)
    {
        dbContext.ChangeTracker.Clear();
        var job = await dbContext.Jobs.SingleAsync(candidate => candidate.Id == jobId, cancellationToken);
        var payload = JsonSerializer.Deserialize<ResearchJobPayload>(job.Payload, ResearchPayloadSerializerOptions)
            ?? throw new InvalidOperationException("Video research job payload is invalid.");
        if (aiRunId is { } aiRunIdValue)
        {
            var aiRun = await dbContext.AiRuns.SingleOrDefaultAsync(candidate => candidate.Id == aiRunIdValue, cancellationToken);
            if (aiRun?.Status == AiRunStatus.Running) aiRun.Fail(reason, failedAt);
        }

        var willRetry = retryable && job.RetryCount < job.MaxRetries;
        if (!willRetry)
        {
            var run = await dbContext.ResearchRuns.SingleOrDefaultAsync(candidate => candidate.Id == payload.ResearchRunId && candidate.ProjectId == payload.ProjectId && candidate.VideoProjectId == payload.VideoProjectId, cancellationToken);
            if (run is not null && run.Status is ResearchRunStatus.Queued or ResearchRunStatus.Running)
                run.Fail(reason, new ResearchRunMetrics(run.SearchQueryCount, run.SearchResultCount, run.FetchedSourceCount,
                    run.RelevantSourceCount, run.EvidenceCount, run.ClaimCount, run.ConflictCount, run.SearchFailureCount, run.FetchFailureCount), failedAt);
            var videoProject = await dbContext.VideoProjects.SingleOrDefaultAsync(candidate => candidate.Id == payload.VideoProjectId && candidate.ProjectId == payload.ProjectId, cancellationToken);
            if (videoProject?.Status is VideoProjectStatus.ResearchQueued or VideoProjectStatus.Researching)
                videoProject.TransitionTo(VideoProjectStatus.ResearchFailed, failedAt);
        }

        if (job.Status == JobStatus.Running) job.Fail(reason, retryable, failedAt, retryAt);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<ResearchRun?> GetResearchRunAsync(Guid projectId, Guid videoProjectId, Guid researchRunId, bool forUpdate, CancellationToken cancellationToken)
    {
        var query = dbContext.ResearchRuns.Where(x => x.Id == researchRunId && x.ProjectId == projectId && x.VideoProjectId == videoProjectId);
        return (forUpdate ? query : query.AsNoTracking()).SingleOrDefaultAsync(cancellationToken);
    }

    public Task<ResearchRun?> GetLatestResearchRunAsync(Guid projectId, Guid videoProjectId, CancellationToken cancellationToken) =>
        dbContext.ResearchRuns.AsNoTracking().Where(x => x.ProjectId == projectId && x.VideoProjectId == videoProjectId)
            .OrderByDescending(x => x.QueuedAt).FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<ResearchRun>> ListResearchRunsAsync(Guid projectId, Guid videoProjectId, CancellationToken cancellationToken) =>
        await dbContext.ResearchRuns.AsNoTracking().Where(x => x.ProjectId == projectId && x.VideoProjectId == videoProjectId)
            .OrderByDescending(x => x.QueuedAt).ToListAsync(cancellationToken);

    public async Task<int> GetNextResearchReportVersionAsync(Guid projectId, Guid videoProjectId, CancellationToken cancellationToken) =>
        (await dbContext.ResearchReports.Where(x => x.ProjectId == projectId && x.VideoProjectId == videoProjectId)
            .Select(x => (int?)x.Version).MaxAsync(cancellationToken) ?? 0) + 1;

    public Task<ResearchReportWithDetails?> GetLatestResearchReportAsync(Guid projectId, Guid videoProjectId, CancellationToken cancellationToken) =>
        GetResearchReportInternalAsync(dbContext.ResearchReports.AsNoTracking().Where(x => x.ProjectId == projectId && x.VideoProjectId == videoProjectId)
            .OrderByDescending(x => x.Version).Select(x => x.Id), cancellationToken);

    public Task<ResearchReportWithDetails?> GetResearchReportAsync(Guid projectId, Guid videoProjectId, Guid reportId, CancellationToken cancellationToken) =>
        GetResearchReportInternalAsync(dbContext.ResearchReports.AsNoTracking().Where(x => x.ProjectId == projectId && x.VideoProjectId == videoProjectId && x.Id == reportId)
            .Select(x => x.Id), cancellationToken);

    public async Task<IReadOnlyList<ResearchReport>> ListResearchReportsAsync(Guid projectId, Guid videoProjectId, CancellationToken cancellationToken) =>
        await dbContext.ResearchReports.AsNoTracking().Where(x => x.ProjectId == projectId && x.VideoProjectId == videoProjectId)
            .OrderByDescending(x => x.Version).ToListAsync(cancellationToken);

    private async Task<ResearchReportWithDetails?> GetResearchReportInternalAsync(IQueryable<Guid> reportIds, CancellationToken cancellationToken)
    {
        var reportId = await reportIds.FirstOrDefaultAsync(cancellationToken);
        if (reportId == Guid.Empty) return null;
        var report = await dbContext.ResearchReports.AsNoTracking().SingleAsync(x => x.Id == reportId, cancellationToken);
        var sources = await dbContext.ResearchSources.AsNoTracking().Where(x => x.ResearchRunId == report.ResearchRunId).ToListAsync(cancellationToken);
        var evidence = await dbContext.ResearchEvidence.AsNoTracking().Where(x => x.ResearchRunId == report.ResearchRunId).ToListAsync(cancellationToken);
        var claims = await dbContext.ResearchClaims.AsNoTracking().Where(x => x.ResearchReportId == report.Id).ToListAsync(cancellationToken);
        var claimIds = claims.Select(x => x.Id).ToArray();
        var links = await dbContext.ResearchClaimEvidence.AsNoTracking().Where(x => claimIds.Contains(x.ResearchClaimId)).ToListAsync(cancellationToken);
        var conflicts = await dbContext.ResearchConflicts.AsNoTracking().Where(x => x.ResearchReportId == report.Id).ToListAsync(cancellationToken);
        return new ResearchReportWithDetails(report, sources, evidence, claims, links, conflicts);
    }

    public void AddResearchSource(ResearchSource source) => dbContext.ResearchSources.Add(source);
    public void AddResearchEvidence(ResearchEvidence evidence) => dbContext.ResearchEvidence.Add(evidence);
    public void AddResearchReport(ResearchReport report) => dbContext.ResearchReports.Add(report);
    public void AddResearchClaim(ResearchClaim claim) => dbContext.ResearchClaims.Add(claim);
    public void AddResearchClaimEvidence(ResearchClaimEvidence claimEvidence) => dbContext.ResearchClaimEvidence.Add(claimEvidence);
    public void AddResearchConflict(ResearchConflict conflict) => dbContext.ResearchConflicts.Add(conflict);

    public Task<Job?> GetActiveCompetitorAnalysisJobAsync(Guid projectId, Guid competitorId, CancellationToken cancellationToken) =>
        FindAnalysisJobAsync(projectId, competitorId, activeOnly: true, cancellationToken);

    public Task<Job?> GetLatestCompetitorAnalysisJobAsync(Guid projectId, Guid competitorId, CancellationToken cancellationToken) =>
        FindAnalysisJobAsync(projectId, competitorId, activeOnly: false, cancellationToken);

    public async Task<Job> EnqueueCompetitorAnalysisJobAsync(Job job, CancellationToken cancellationToken)
    {
        dbContext.Jobs.Add(job);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return job;
        }
        catch (DbUpdateException exception) when (IsUniqueConstraintViolation(exception))
        {
            dbContext.ChangeTracker.Clear();
            return await dbContext.Jobs.AsNoTracking().SingleAsync(candidate =>
                candidate.CompetitorChannelId == job.CompetitorChannelId &&
                candidate.Type == "competitor-analysis" &&
                (candidate.Status == JobStatus.Queued || candidate.Status == JobStatus.Running || candidate.Status == JobStatus.Retrying), cancellationToken);
        }
    }

    public async Task<Job?> TryClaimNextCompetitorAnalysisJobAsync(
        DateTimeOffset now,
        DateTimeOffset staleRunningBefore,
        CancellationToken cancellationToken)
    {
        return await TryClaimJobAsync("competitor-analysis", now, staleRunningBefore, cancellationToken);
    }

    public async Task RequeueCompetitorAnalysisJobAsync(Guid jobId, CancellationToken cancellationToken)
    {
        await RequeueJobAsync(jobId, cancellationToken);
    }

    private async Task RequeueJobAsync(Guid jobId, CancellationToken cancellationToken)
    {
        dbContext.ChangeTracker.Clear();
        var job = await dbContext.Jobs.SingleAsync(candidate => candidate.Id == jobId, cancellationToken);
        if (job.Status == JobStatus.Running)
        {
            job.Requeue(DateTimeOffset.UtcNow);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task FailCompetitorAnalysisJobAsync(
        Guid jobId,
        Guid? aiRunId,
        string reason,
        bool retryable,
        DateTimeOffset failedAt,
        DateTimeOffset? retryAt,
        CancellationToken cancellationToken)
    {
        await FailJobAsync(jobId, aiRunId, reason, retryable, failedAt, retryAt, cancellationToken);
    }

    private async Task FailJobAsync(
        Guid jobId, Guid? aiRunId, string reason, bool retryable, DateTimeOffset failedAt, DateTimeOffset? retryAt, CancellationToken cancellationToken)
    {
        dbContext.ChangeTracker.Clear();
        var job = await dbContext.Jobs.SingleAsync(candidate => candidate.Id == jobId, cancellationToken);
        if (aiRunId is { } runId)
        {
            var run = await dbContext.AiRuns.SingleOrDefaultAsync(candidate => candidate.Id == runId, cancellationToken);
            if (run?.Status == AiRunStatus.Running) run.Fail(reason, failedAt);
        }
        if (job.Status == JobStatus.Running) job.Fail(reason, retryable, failedAt, retryAt);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public void AddCompetitorAnalysis(CompetitorAnalysis analysis) => dbContext.CompetitorAnalyses.Add(analysis);

    public void AddAiRun(AiRun aiRun) => dbContext.AiRuns.Add(aiRun);

    public void AddJob(Job job) => dbContext.Jobs.Add(job);

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ResourceConflictException("The pilot was changed by another request. Refresh it and try again.");
        }
        catch (DbUpdateException exception) when (IsUniqueConstraintViolation(exception))
        {
            throw new ResourceConflictException(
                "The project already contains this YouTube channel or video.");
        }
    }

    private Task<Job?> FindAnalysisJobAsync(Guid projectId, Guid competitorId, bool activeOnly, CancellationToken cancellationToken)
    {
        var query = dbContext.Jobs.AsNoTracking().Where(job => job.Type == "competitor-analysis" && job.CompetitorChannelId == competitorId &&
            dbContext.CompetitorChannels.Any(channel => channel.Id == competitorId && channel.ProjectId == projectId));
        if (activeOnly) query = query.Where(job => job.Status == JobStatus.Queued || job.Status == JobStatus.Running || job.Status == JobStatus.Retrying);
        return query.OrderByDescending(job => job.CreatedAt).FirstOrDefaultAsync(cancellationToken);
    }

    private Task<Job?> FindArtifactLocalizationJobAsync(string artifactType, Guid artifactId, int artifactVersion, string locale, bool activeOnly, CancellationToken cancellationToken)
    {
        var query = dbContext.Jobs.AsNoTracking().Where(job => job.Type == "artifact-localization" && job.ArtifactType == artifactType && job.ArtifactId == artifactId && job.ArtifactVersion == artifactVersion && job.Locale == locale);
        if (activeOnly) query = query.Where(job => job.Status == JobStatus.Queued || job.Status == JobStatus.Running || job.Status == JobStatus.Retrying);
        return query.OrderByDescending(job => job.CreatedAt).FirstOrDefaultAsync(cancellationToken);
    }

    private Task<Job?> FindOpportunityJobAsync(Guid projectId, bool activeOnly, CancellationToken cancellationToken)
    {
        var query = dbContext.Jobs.AsNoTracking().Where(job => job.Type == "opportunity-analysis" && job.ProjectId == projectId);
        if (activeOnly) query = query.Where(job => job.Status == JobStatus.Queued || job.Status == JobStatus.Running || job.Status == JobStatus.Retrying);
        return query.OrderByDescending(job => job.CreatedAt).FirstOrDefaultAsync(cancellationToken);
    }
    private Task<Job?> FindPilotJobAsync(Guid projectId, bool activeOnly, CancellationToken cancellationToken)
    {
        var query = dbContext.Jobs.AsNoTracking().Where(job => job.Type == "pilot-generation" && job.ProjectId == projectId);
        if (activeOnly) query = query.Where(job => job.Status == JobStatus.Queued || job.Status == JobStatus.Running || job.Status == JobStatus.Retrying);
        return query.OrderByDescending(job => job.CreatedAt).FirstOrDefaultAsync(cancellationToken);
    }
    private Task<Job?> FindIdeaJobAsync(Guid projectId, Guid opportunityId, bool activeOnly, CancellationToken cancellationToken)
    {
        var query = dbContext.Jobs.AsNoTracking().Where(job => job.Type == "idea-generation" && job.ProjectId == projectId && job.OpportunityId == opportunityId);
        if (activeOnly) query = query.Where(job => job.Status == JobStatus.Queued || job.Status == JobStatus.Running || job.Status == JobStatus.Retrying); return query.OrderByDescending(job => job.CreatedAt).FirstOrDefaultAsync(cancellationToken);
    }
    private Task<Job?> FindVideoResearchJobAsync(Guid projectId, Guid videoProjectId, bool activeOnly, CancellationToken cancellationToken)
    {
        var query = dbContext.Jobs.AsNoTracking().Where(job => job.Type == "video-research" && job.VideoProjectId == videoProjectId &&
            dbContext.VideoProjects.Any(videoProject => videoProject.Id == videoProjectId && videoProject.ProjectId == projectId));
        if (activeOnly) query = query.Where(job => job.Status == JobStatus.Queued || job.Status == JobStatus.Running || job.Status == JobStatus.Retrying);
        return query.OrderByDescending(job => job.CreatedAt).FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<Job?> TryClaimJobAsync(string type, DateTimeOffset now, DateTimeOffset staleRunningBefore, CancellationToken cancellationToken)
    {
        var strategy = dbContext.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            var job = await dbContext.Jobs.FromSqlRaw("""
                SELECT TOP (1) * FROM [yaf].[jobs] WITH (UPDLOCK, READPAST, ROWLOCK)
                WHERE type = {0}
                  AND (status IN ('Queued', 'Retrying') OR (status = 'Running' AND started_at <= {2}))
                  AND available_at <= {1}
                ORDER BY available_at, created_at
                """, type, now, staleRunningBefore).FirstOrDefaultAsync(cancellationToken);

            if (job is null)
            {
                return null;
            }

            if (job.Status == JobStatus.Running)
            {
                job.Requeue(now);
            }

            job.Start(now);
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return job;
        });
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException exception) =>
        exception.InnerException is SqlException { Number: 2601 or 2627 };
}
