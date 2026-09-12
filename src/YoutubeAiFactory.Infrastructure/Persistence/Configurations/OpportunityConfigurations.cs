using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using YoutubeAiFactory.Domain.Competitors;
using YoutubeAiFactory.Domain.Opportunities;
using YoutubeAiFactory.Domain.Projects;

namespace YoutubeAiFactory.Infrastructure.Persistence.Configurations;

internal sealed class OpportunityReportConfiguration : IEntityTypeConfiguration<OpportunityReport>
{
    public void Configure(EntityTypeBuilder<OpportunityReport> builder)
    {
        builder.ToTable("opportunity_reports"); builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id"); builder.Property(x => x.ProjectId).HasColumnName("project_id").IsRequired();
        builder.Property(x => x.Version).HasColumnName("version").IsRequired(); builder.Property(x => x.AiRunId).HasColumnName("ai_run_id").IsRequired();
        builder.Property(x => x.PromptKey).HasColumnName("prompt_key").HasMaxLength(100).IsRequired(); builder.Property(x => x.PromptVersion).HasColumnName("prompt_version");
        builder.Property(x => x.Provider).HasColumnName("provider").HasMaxLength(100).IsRequired(); builder.Property(x => x.Model).HasColumnName("model").HasMaxLength(100).IsRequired();
        builder.Property(x => x.ScoringAlgorithmVersion).HasColumnName("scoring_algorithm_version").HasMaxLength(100).IsRequired(); builder.Property(x => x.SourceAnalysisCount).HasColumnName("source_analysis_count"); builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.LimitationsJson).HasColumnName("limitations_json").HasColumnType("nvarchar(max)").IsRequired();
        builder.HasIndex(x => new { x.ProjectId, x.Version }).IsUnique(); builder.HasIndex(x => new { x.ProjectId, x.CreatedAt });
        builder.HasOne<Project>().WithMany().HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Cascade);
    }
}
internal sealed class OpportunityReportSourceConfiguration : IEntityTypeConfiguration<OpportunityReportSource>
{
    public void Configure(EntityTypeBuilder<OpportunityReportSource> builder)
    {
        builder.ToTable("opportunity_report_sources"); builder.HasKey(x => x.Id); builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.ReportId).HasColumnName("report_id"); builder.Property(x => x.CompetitorChannelId).HasColumnName("competitor_channel_id"); builder.Property(x => x.CompetitorAnalysisId).HasColumnName("competitor_analysis_id"); builder.Property(x => x.CompetitorAnalysisVersion).HasColumnName("competitor_analysis_version");
        builder.HasIndex(x => new { x.ReportId, x.CompetitorAnalysisId }).IsUnique(); builder.HasOne<OpportunityReport>().WithMany().HasForeignKey(x => x.ReportId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<CompetitorChannel>().WithMany().HasForeignKey(x => x.CompetitorChannelId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<CompetitorAnalysis>().WithMany().HasForeignKey(x => x.CompetitorAnalysisId).OnDelete(DeleteBehavior.Restrict);
    }
}
internal sealed class OpportunityCandidateConfiguration : IEntityTypeConfiguration<OpportunityCandidate>
{
    public void Configure(EntityTypeBuilder<OpportunityCandidate> builder)
    {
        builder.ToTable("opportunity_candidates"); builder.HasKey(x => x.Id); builder.Property(x => x.Id).HasColumnName("id"); builder.Property(x => x.ReportId).HasColumnName("report_id");
        foreach (var property in new[] { nameof(OpportunityCandidate.Name), nameof(OpportunityCandidate.Audience), nameof(OpportunityCandidate.Topic), nameof(OpportunityCandidate.ContentFormat), nameof(OpportunityCandidate.Angle) }) builder.Property(property).HasMaxLength(1000).IsRequired();
        builder.Property(x => x.Description).HasColumnName("description").HasColumnType("nvarchar(max)"); builder.Property(x => x.WhyThisOpportunity).HasColumnName("why_this_opportunity").HasColumnType("nvarchar(max)");
        builder.Property(x => x.ObservedDemandSignal).HasColumnName("observed_demand_signal"); builder.Property(x => x.NoveltySignal).HasColumnName("novelty_signal"); builder.Property(x => x.CompetitionRiskSignal).HasColumnName("competition_risk_signal"); builder.Property(x => x.AudienceFitSignal).HasColumnName("audience_fit_signal"); builder.Property(x => x.TransferabilitySignal).HasColumnName("transferability_signal"); builder.Property(x => x.EvidenceStrength).HasColumnName("evidence_strength"); builder.Property(x => x.StoryPotential).HasColumnName("story_potential"); builder.Property(x => x.ProductionComplexity).HasColumnName("production_complexity"); builder.Property(x => x.Confidence).HasColumnName("confidence"); builder.Property(x => x.OverallScore).HasColumnName("overall_score").HasPrecision(5, 2);
        builder.Property(x => x.RisksJson).HasColumnName("risks_json").HasColumnType("nvarchar(max)"); builder.Property(x => x.LimitationsJson).HasColumnName("limitations_json").HasColumnType("nvarchar(max)"); builder.Property(x => x.DecisionStatus).HasColumnName("decision_status").HasConversion<string>().HasMaxLength(30); builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.HasIndex(x => new { x.ReportId, x.OverallScore }); builder.HasOne<OpportunityReport>().WithMany().HasForeignKey(x => x.ReportId).OnDelete(DeleteBehavior.Cascade);
    }
}
internal sealed class OpportunityEvidenceConfiguration : IEntityTypeConfiguration<OpportunityEvidence>
{
    public void Configure(EntityTypeBuilder<OpportunityEvidence> builder)
    {
        builder.ToTable("opportunity_evidence"); builder.HasKey(x => x.Id); builder.Property(x => x.Id).HasColumnName("id"); builder.Property(x => x.CandidateId).HasColumnName("candidate_id"); builder.Property(x => x.CompetitorChannelId).HasColumnName("competitor_channel_id"); builder.Property(x => x.CompetitorAnalysisId).HasColumnName("competitor_analysis_id"); builder.Property(x => x.CompetitorVideoId).HasColumnName("competitor_video_id"); builder.Property(x => x.EvidenceId).HasColumnName("evidence_id").HasMaxLength(200); builder.Property(x => x.Summary).HasColumnName("summary").HasMaxLength(2000);
        builder.HasIndex(x => new { x.CandidateId, x.EvidenceId }).IsUnique(); builder.HasOne<OpportunityCandidate>().WithMany().HasForeignKey(x => x.CandidateId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<CompetitorChannel>().WithMany().HasForeignKey(x => x.CompetitorChannelId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<CompetitorAnalysis>().WithMany().HasForeignKey(x => x.CompetitorAnalysisId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<CompetitorVideo>().WithMany().HasForeignKey(x => x.CompetitorVideoId).OnDelete(DeleteBehavior.Restrict);
    }
}
