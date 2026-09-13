using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using YoutubeAiFactory.Domain.Ideas;
using YoutubeAiFactory.Domain.Opportunities;
using YoutubeAiFactory.Domain.Pilots;
using YoutubeAiFactory.Domain.Projects;
using YoutubeAiFactory.Domain.Videos;

namespace YoutubeAiFactory.Infrastructure.Persistence.Configurations;

internal sealed class VideoProjectConfiguration : IEntityTypeConfiguration<VideoProject>
{
    public void Configure(EntityTypeBuilder<VideoProject> builder)
    {
        builder.ToTable("video_projects", table =>
            table.HasCheckConstraint("ck_video_projects_source_warnings_json", "ISJSON([source_warnings_json]) = 1"));
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id"); builder.Property(x => x.ProjectId).HasColumnName("project_id");
        builder.Property(x => x.PilotId).HasColumnName("pilot_id"); builder.Property(x => x.PilotVersion).HasColumnName("pilot_version");
        builder.Property(x => x.PilotVideoId).HasColumnName("pilot_video_id"); builder.Property(x => x.VideoIdeaId).HasColumnName("video_idea_id"); builder.Property(x => x.OpportunityId).HasColumnName("opportunity_id");
        builder.Property(x => x.WorkingTitle).HasColumnName("working_title").HasMaxLength(300).IsRequired();
        builder.Property(x => x.Topic).HasColumnName("topic").HasMaxLength(500).IsRequired(); builder.Property(x => x.ContentFormat).HasColumnName("content_format").HasMaxLength(500).IsRequired();
        builder.Property(x => x.Angle).HasColumnName("angle").HasMaxLength(1000).IsRequired(); builder.Property(x => x.TargetAudience).HasColumnName("target_audience").HasMaxLength(1000).IsRequired();
        builder.Property(x => x.HookConcept).HasColumnName("hook_concept").HasColumnType("nvarchar(max)"); builder.Property(x => x.ThumbnailConcept).HasColumnName("thumbnail_concept").HasColumnType("nvarchar(max)"); builder.Property(x => x.ViewerPromise).HasColumnName("viewer_promise").HasColumnType("nvarchar(max)");
        builder.Property(x => x.PilotHypothesis).HasColumnName("pilot_hypothesis").HasColumnType("nvarchar(max)"); builder.Property(x => x.VariableBeingTested).HasColumnName("variable_being_tested").HasColumnType("nvarchar(max)"); builder.Property(x => x.SuccessSignal).HasColumnName("success_signal").HasColumnType("nvarchar(max)"); builder.Property(x => x.ExecutionNotes).HasColumnName("execution_notes").HasColumnType("nvarchar(max)");
        builder.Property(x => x.PrimaryMetric).HasColumnName("primary_metric").HasMaxLength(500).IsRequired(); builder.Property(x => x.ExperimentType).HasColumnName("experiment_type").HasConversion<string>().HasMaxLength(30);
        builder.Property(x => x.SourceWarningsJson).HasColumnName("source_warnings_json").HasColumnType("nvarchar(max)"); builder.Property(x => x.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(30);
        builder.Property(x => x.CreatedAt).HasColumnName("created_at"); builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        builder.HasIndex(x => x.PilotVideoId).IsUnique(); builder.HasIndex(x => new { x.ProjectId, x.CreatedAt });
        builder.HasOne<Project>().WithMany().HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Pilot>().WithMany().HasForeignKey(x => x.PilotId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<PilotVideo>().WithMany().HasForeignKey(x => x.PilotVideoId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<VideoIdea>().WithMany().HasForeignKey(x => x.VideoIdeaId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<OpportunityCandidate>().WithMany().HasForeignKey(x => x.OpportunityId).OnDelete(DeleteBehavior.Restrict);
    }
}
