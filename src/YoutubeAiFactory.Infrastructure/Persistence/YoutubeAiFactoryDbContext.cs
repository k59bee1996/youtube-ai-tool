using Microsoft.EntityFrameworkCore;
using YoutubeAiFactory.Domain.AI;
using YoutubeAiFactory.Domain.Competitors;
using YoutubeAiFactory.Domain.Ideas;
using YoutubeAiFactory.Domain.Jobs;
using YoutubeAiFactory.Domain.Localization;
using YoutubeAiFactory.Domain.Opportunities;
using YoutubeAiFactory.Domain.Outlines;
using YoutubeAiFactory.Domain.Pilots;
using YoutubeAiFactory.Domain.Projects;
using YoutubeAiFactory.Domain.Research;
using YoutubeAiFactory.Domain.Scripts;
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
    public DbSet<ResearchRun> ResearchRuns => Set<ResearchRun>();
    public DbSet<ResearchReport> ResearchReports => Set<ResearchReport>();
    public DbSet<ResearchSource> ResearchSources => Set<ResearchSource>();
    public DbSet<ResearchEvidence> ResearchEvidence => Set<ResearchEvidence>();
    public DbSet<ResearchClaim> ResearchClaims => Set<ResearchClaim>();
    public DbSet<ResearchClaimEvidence> ResearchClaimEvidence => Set<ResearchClaimEvidence>();
    public DbSet<ResearchConflict> ResearchConflicts => Set<ResearchConflict>();
    public DbSet<VideoOutline> VideoOutlines => Set<VideoOutline>();
    public DbSet<VideoOutlineSection> VideoOutlineSections => Set<VideoOutlineSection>();
    public DbSet<VideoOutlineSectionClaim> VideoOutlineSectionClaims => Set<VideoOutlineSectionClaim>();
    public DbSet<VideoOutlineSectionConflict> VideoOutlineSectionConflicts => Set<VideoOutlineSectionConflict>();
    public DbSet<VideoOutlineSectionGap> VideoOutlineSectionGaps => Set<VideoOutlineSectionGap>();
    public DbSet<VideoScript> VideoScripts => Set<VideoScript>();
    public DbSet<VideoScriptSection> VideoScriptSections => Set<VideoScriptSection>();
    public DbSet<VideoScriptBlock> VideoScriptBlocks => Set<VideoScriptBlock>();
    public DbSet<VideoScriptBlockClaim> VideoScriptBlockClaims => Set<VideoScriptBlockClaim>();
    public DbSet<VideoScriptBlockConflict> VideoScriptBlockConflicts => Set<VideoScriptBlockConflict>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("yaf");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(YoutubeAiFactoryDbContext).Assembly);
    }
}
