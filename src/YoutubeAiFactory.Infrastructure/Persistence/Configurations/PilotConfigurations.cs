using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using YoutubeAiFactory.Domain.Ideas;
using YoutubeAiFactory.Domain.Opportunities;
using YoutubeAiFactory.Domain.Pilots;
using YoutubeAiFactory.Domain.Projects;

namespace YoutubeAiFactory.Infrastructure.Persistence.Configurations;

internal sealed class PilotConfiguration : IEntityTypeConfiguration<Pilot>
{
    public void Configure(EntityTypeBuilder<Pilot> builder)
    {
        builder.ToTable("pilots"); builder.HasKey(x => x.Id); builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.ProjectId).HasColumnName("project_id"); builder.Property(x => x.Version).HasColumnName("version"); builder.Property(x => x.AiRunId).HasColumnName("ai_run_id");
        builder.Property(x => x.PromptKey).HasColumnName("prompt_key").HasMaxLength(100); builder.Property(x => x.PromptVersion).HasColumnName("prompt_version"); builder.Property(x => x.Provider).HasColumnName("provider").HasMaxLength(100); builder.Property(x => x.Model).HasColumnName("model").HasMaxLength(100);
        builder.Property(x => x.PlanningAlgorithmVersion).HasColumnName("planning_algorithm_version").HasMaxLength(100); builder.Property(x => x.Name).HasColumnName("name").HasMaxLength(300); builder.Property(x => x.Objective).HasColumnName("objective").HasColumnType("text");
        builder.Property(x => x.AssumptionsJson).HasColumnName("assumptions_json").HasColumnType("jsonb"); builder.Property(x => x.LimitationsJson).HasColumnName("limitations_json").HasColumnType("jsonb"); builder.Property(x => x.WarningsJson).HasColumnName("warnings_json").HasColumnType("jsonb");
        builder.Property(x => x.EligibleIdeaCount).HasColumnName("eligible_idea_count"); builder.Property(x => x.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(30); builder.Property(x => x.CreatedAt).HasColumnName("created_at"); builder.Property(x => x.ApprovedAt).HasColumnName("approved_at");
        builder.HasIndex(x => new { x.ProjectId, x.Version }).IsUnique(); builder.HasIndex(x => new { x.ProjectId, x.CreatedAt });
        builder.HasOne<Project>().WithMany().HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class PilotVideoConfiguration : IEntityTypeConfiguration<PilotVideo>
{
    public void Configure(EntityTypeBuilder<PilotVideo> builder)
    {
        builder.ToTable("pilot_videos", table => table.HasCheckConstraint("ck_pilot_videos_sequence", "sequence >= 1 AND sequence <= 12"));
        builder.HasKey(x => x.Id); builder.Property(x => x.Id).HasColumnName("id"); builder.Property(x => x.PilotId).HasColumnName("pilot_id"); builder.Property(x => x.VideoIdeaId).HasColumnName("video_idea_id"); builder.Property(x => x.OpportunityId).HasColumnName("opportunity_id");
        builder.Property(x => x.Sequence).HasColumnName("sequence"); builder.Property(x => x.ExperimentType).HasColumnName("experiment_type").HasConversion<string>().HasMaxLength(30);
        builder.Property(x => x.Hypothesis).HasColumnName("hypothesis").HasColumnType("text").IsRequired(); builder.Property(x => x.Rationale).HasColumnName("rationale").HasColumnType("text").IsRequired();
        builder.Property(x => x.VariableBeingTested).HasColumnName("variable_being_tested").HasColumnType("text").IsRequired(); builder.Property(x => x.ControlStrategy).HasColumnName("control_strategy").HasColumnType("text").IsRequired(); builder.Property(x => x.SuccessSignal).HasColumnName("success_signal").HasColumnType("text").IsRequired();
        builder.Property(x => x.PrimaryMetric).HasColumnName("primary_metric").HasMaxLength(500); builder.Property(x => x.SecondaryMetricsJson).HasColumnName("secondary_metrics_json").HasColumnType("jsonb"); builder.Property(x => x.Notes).HasColumnName("notes").HasColumnType("text");
        builder.HasIndex(x => new { x.PilotId, x.Sequence }).IsUnique(); builder.HasIndex(x => new { x.PilotId, x.VideoIdeaId }).IsUnique();
        builder.HasOne<Pilot>().WithMany().HasForeignKey(x => x.PilotId).OnDelete(DeleteBehavior.Cascade); builder.HasOne<VideoIdea>().WithMany().HasForeignKey(x => x.VideoIdeaId).OnDelete(DeleteBehavior.Restrict); builder.HasOne<OpportunityCandidate>().WithMany().HasForeignKey(x => x.OpportunityId).OnDelete(DeleteBehavior.Restrict);
    }
}
