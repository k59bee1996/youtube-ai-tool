using Microsoft.EntityFrameworkCore;
using Npgsql;
using YoutubeAiFactory.Application.Common;
using YoutubeAiFactory.Application.Persistence;
using YoutubeAiFactory.Domain.AI;
using YoutubeAiFactory.Domain.Competitors;
using YoutubeAiFactory.Domain.Jobs;
using YoutubeAiFactory.Domain.Projects;

namespace YoutubeAiFactory.Infrastructure.Persistence;

internal sealed class YoutubeAiFactoryStore(YoutubeAiFactoryDbContext dbContext)
    : IYoutubeAiFactoryStore
{
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
        catch (DbUpdateException exception) when (IsActiveAnalysisJobConflict(exception))
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
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var job = await dbContext.Jobs.FromSqlRaw("""
            SELECT * FROM yaf.jobs
            WHERE type = 'competitor-analysis'
              AND (status IN ('Queued', 'Retrying') OR (status = 'Running' AND started_at <= {1}))
              AND available_at <= {0}
            ORDER BY available_at, created_at
            LIMIT 1 FOR UPDATE SKIP LOCKED
            """, now, staleRunningBefore).FirstOrDefaultAsync(cancellationToken);
        if (job is null) return null;
        if (job.Status == JobStatus.Running) job.Requeue(now);
        job.Start(now);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return job;
    }

    public async Task RequeueCompetitorAnalysisJobAsync(Guid jobId, CancellationToken cancellationToken)
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
        catch (DbUpdateException exception)
            when (exception.InnerException is PostgresException
            {
                SqlState: PostgresErrorCodes.UniqueViolation,
            })
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

    private static bool IsActiveAnalysisJobConflict(DbUpdateException exception) =>
        exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation, ConstraintName: "ux_jobs_active_competitor_analysis" };
}
