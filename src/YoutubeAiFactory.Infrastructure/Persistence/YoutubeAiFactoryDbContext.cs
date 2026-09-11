using Microsoft.EntityFrameworkCore;
using YoutubeAiFactory.Domain.AI;
using YoutubeAiFactory.Domain.Competitors;
using YoutubeAiFactory.Domain.Ideas;
using YoutubeAiFactory.Domain.Jobs;
using YoutubeAiFactory.Domain.Opportunities;
using YoutubeAiFactory.Domain.Projects;

namespace YoutubeAiFactory.Infrastructure.Persistence;

public sealed class YoutubeAiFactoryDbContext(DbContextOptions<YoutubeAiFactoryDbContext> options)
    : DbContext(options)
{
    public DbSet<Project> Projects => Set<Project>();

    public DbSet<CompetitorChannel> CompetitorChannels => Set<CompetitorChannel>();

    public DbSet<CompetitorVideo> CompetitorVideos => Set<CompetitorVideo>();

    public DbSet<CompetitorAnalysis> CompetitorAnalyses => Set<CompetitorAnalysis>();

    public DbSet<Job> Jobs => Set<Job>();

    public DbSet<AiRun> AiRuns => Set<AiRun>();
    public DbSet<OpportunityReport> OpportunityReports => Set<OpportunityReport>();
    public DbSet<OpportunityReportSource> OpportunityReportSources => Set<OpportunityReportSource>();
    public DbSet<OpportunityCandidate> OpportunityCandidates => Set<OpportunityCandidate>();
    public DbSet<OpportunityEvidence> OpportunityEvidence => Set<OpportunityEvidence>();
    public DbSet<IdeaGeneration> IdeaGenerations => Set<IdeaGeneration>();
    public DbSet<VideoIdea> VideoIdeas => Set<VideoIdea>();
    public DbSet<IdeaEvidence> IdeaEvidence => Set<IdeaEvidence>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("yaf");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(YoutubeAiFactoryDbContext).Assembly);
    }
}
