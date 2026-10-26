using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using YoutubeAiFactory.Domain.AI;
using YoutubeAiFactory.Domain.Jobs;
using YoutubeAiFactory.Domain.Projects;
using YoutubeAiFactory.Domain.Research;
using YoutubeAiFactory.Domain.Videos;

namespace YoutubeAiFactory.Infrastructure.Persistence.Configurations;

internal sealed class AiRunConfiguration : IEntityTypeConfiguration<AiRun>
{
    public void Configure(EntityTypeBuilder<AiRun> builder)
    {
        builder.ToTable("ai_runs");
        builder.HasKey(run => run.Id);
        builder.Property(run => run.Id).HasColumnName("id");
        builder.Property(run => run.Workflow).HasColumnName("workflow").HasMaxLength(100).IsRequired();
        builder.Property(run => run.ProjectId).HasColumnName("project_id");
        builder.Property(run => run.CompetitorId).HasColumnName("competitor_channel_id");
        builder.Property(run => run.VideoProjectId).HasColumnName("video_project_id");
        builder.Property(run => run.ResearchRunId).HasColumnName("research_run_id");
        builder.Property(run => run.ResearchReportId).HasColumnName("research_report_id");
        builder.Property(run => run.JobId).HasColumnName("job_id");
        builder.Property(run => run.WorkflowStage).HasColumnName("workflow_stage").HasMaxLength(100);
        builder.Property(run => run.Provider).HasColumnName("provider").HasMaxLength(100).IsRequired();
        builder.Property(run => run.Model).HasColumnName("model").HasMaxLength(100).IsRequired();
        builder.Property(run => run.ModelProfile).HasColumnName("model_profile").HasMaxLength(30).IsRequired();
        builder.Property(run => run.PromptKey).HasColumnName("prompt_key").HasMaxLength(100).IsRequired();
        builder.Property(run => run.PromptVersion).HasColumnName("prompt_version").IsRequired();
        builder.Property(run => run.InputTokens).HasColumnName("input_tokens");
        builder.Property(run => run.OutputTokens).HasColumnName("output_tokens");
        builder.Property(run => run.CachedInputTokens).HasColumnName("cached_input_tokens");
        builder.Property(run => run.ReasoningTokens).HasColumnName("reasoning_tokens");
        builder.Property(run => run.EstimatedCost).HasColumnName("estimated_cost").HasPrecision(18, 6);
        builder.Property(run => run.ProviderReportedCost).HasColumnName("provider_reported_cost").HasPrecision(19, 8);
        builder.Property(run => run.CalculatedEstimatedCost).HasColumnName("calculated_estimated_cost").HasPrecision(19, 8);
        builder.Property(run => run.Currency).HasColumnName("currency").HasMaxLength(3);
        builder.Property(run => run.CostSource).HasColumnName("cost_source").HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(run => run.PricingVersion).HasColumnName("pricing_version").HasMaxLength(100);
        builder.Property(run => run.PricingEffectiveFrom).HasColumnName("pricing_effective_from");
        builder.Property(run => run.ErrorCategory).HasColumnName("error_category").HasMaxLength(100);
        builder.Property(run => run.LatencyMilliseconds).HasColumnName("latency_milliseconds");
        builder.Property(run => run.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(run => run.RetryCount).HasColumnName("retry_count").IsRequired();
        builder.Property(run => run.StartedAt).HasColumnName("started_at").IsRequired();
        builder.Property(run => run.CompletedAt).HasColumnName("completed_at");
        builder.Property(run => run.FailureReason).HasColumnName("failure_reason").HasMaxLength(2_000);

        builder.HasOne<Project>()
            .WithMany()
            .HasForeignKey(run => run.ProjectId)
            .OnDelete(DeleteBehavior.SetNull);
        builder.HasOne<VideoProject>()
            .WithMany()
            .HasForeignKey(run => run.VideoProjectId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ResearchRun>()
            .WithMany()
            .HasForeignKey(run => run.ResearchRunId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ResearchReport>()
            .WithMany()
            .HasForeignKey(run => run.ResearchReportId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Job>()
            .WithMany()
            .HasForeignKey(run => run.JobId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(run => new { run.ProjectId, run.StartedAt });
        builder.HasIndex(run => new { run.CompetitorId, run.StartedAt });
        builder.HasIndex(run => new { run.VideoProjectId, run.StartedAt });
        builder.HasIndex(run => new { run.ResearchRunId, run.StartedAt });
        builder.HasIndex(run => new { run.ResearchReportId, run.StartedAt });
        builder.HasIndex(run => new { run.JobId, run.StartedAt });
        builder.HasIndex(run => new { run.ProjectId, run.Workflow, run.StartedAt });
    }
}
