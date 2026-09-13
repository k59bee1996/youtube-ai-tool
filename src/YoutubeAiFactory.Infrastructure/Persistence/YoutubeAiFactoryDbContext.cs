using Microsoft.EntityFrameworkCore;
using YoutubeAiFactory.Domain.AI;
using YoutubeAiFactory.Domain.Competitors;
using YoutubeAiFactory.Domain.Ideas;
using YoutubeAiFactory.Domain.Jobs;
using YoutubeAiFactory.Domain.Localization;
using YoutubeAiFactory.Domain.Opportunities;
using YoutubeAiFactory.Domain.Pilots;
using YoutubeAiFactory.Domain.Projects;
using YoutubeAiFactory.Domain.Videos;

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
    public DbSet<ArtifactLocalization> ArtifactLocalizations => Set<ArtifactLocalization>();
    public DbSet<OpportunityReport> OpportunityReports => Set<OpportunityReport>();
    public DbSet<OpportunityReportSource> OpportunityReportSources => Set<OpportunityReportSource>();
    public DbSet<OpportunityCandidate> OpportunityCandidates => Set<OpportunityCandidate>();
    public DbSet<OpportunityEvidence> OpportunityEvidence => Set<OpportunityEvidence>();
    public DbSet<IdeaGeneration> IdeaGenerations => Set<IdeaGeneration>();
    public DbSet<VideoIdea> VideoIdeas => Set<VideoIdea>();
    public DbSet<IdeaEvidence> IdeaEvidence => Set<IdeaEvidence>();
    public DbSet<Pilot> Pilots => Set<Pilot>();
    public DbSet<PilotVideo> PilotVideos => Set<PilotVideo>();
    public DbSet<VideoProject> VideoProjects => Set<VideoProject>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("yaf");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(YoutubeAiFactoryDbContext).Assembly);
    }
}
