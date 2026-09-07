using Microsoft.EntityFrameworkCore;
using YoutubeAiFactory.Domain.AI;
using YoutubeAiFactory.Domain.Competitors;
using YoutubeAiFactory.Domain.Jobs;
using YoutubeAiFactory.Domain.Projects;

namespace YoutubeAiFactory.Infrastructure.Persistence;

public sealed class YoutubeAiFactoryDbContext(DbContextOptions<YoutubeAiFactoryDbContext> options)
    : DbContext(options)
{
    public DbSet<Project> Projects => Set<Project>();

    public DbSet<CompetitorChannel> CompetitorChannels => Set<CompetitorChannel>();

    public DbSet<CompetitorVideo> CompetitorVideos => Set<CompetitorVideo>();

    public DbSet<Job> Jobs => Set<Job>();

    public DbSet<AiRun> AiRuns => Set<AiRun>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("yaf");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(YoutubeAiFactoryDbContext).Assembly);
    }
}
