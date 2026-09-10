using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using YoutubeAiFactory.Domain.Jobs;

namespace YoutubeAiFactory.Infrastructure.Persistence.Configurations;

internal sealed class JobConfiguration : IEntityTypeConfiguration<Job>
{
    public void Configure(EntityTypeBuilder<Job> builder)
    {
        builder.ToTable("jobs");
        builder.HasKey(job => job.Id);
        builder.Property(job => job.Id).HasColumnName("id");
        builder.Property(job => job.Type).HasColumnName("type").HasMaxLength(100).IsRequired();
        builder.Property(job => job.CompetitorChannelId).HasColumnName("competitor_channel_id");
        builder.Property(job => job.ProjectId).HasColumnName("project_id");
        builder.Property(job => job.OpportunityId).HasColumnName("opportunity_id");
        builder.Property(job => job.Payload).HasColumnName("payload").HasColumnType("jsonb").IsRequired();
        builder.Property(job => job.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(job => job.RetryCount).HasColumnName("retry_count").IsRequired();
        builder.Property(job => job.MaxRetries).HasColumnName("max_retries").IsRequired();
        builder.Property(job => job.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(job => job.AvailableAt).HasColumnName("available_at").IsRequired();
        builder.Property(job => job.StartedAt).HasColumnName("started_at");
        builder.Property(job => job.CompletedAt).HasColumnName("completed_at");
        builder.Property(job => job.FailureReason).HasColumnName("failure_reason").HasMaxLength(2_000);

        builder.HasIndex(job => new { job.Status, job.AvailableAt });
        builder.HasIndex(job => job.CompetitorChannelId)
            .HasDatabaseName("ux_jobs_active_competitor_analysis")
            .IsUnique()
            .HasFilter("type = 'competitor-analysis' AND status IN ('Queued', 'Running', 'Retrying')");
        builder.HasIndex(job => job.ProjectId)
            .HasDatabaseName("ux_jobs_active_opportunity_analysis")
            .IsUnique()
            .HasFilter("type = 'opportunity-analysis' AND status IN ('Queued', 'Running', 'Retrying')");
        builder.HasIndex(job => job.OpportunityId)
            .HasDatabaseName("ux_jobs_active_idea_generation")
            .IsUnique()
            .HasFilter("type = 'idea-generation' AND status IN ('Queued', 'Running', 'Retrying')");
    }
}
