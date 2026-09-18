using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using YoutubeAiFactory.Domain.AI;
using YoutubeAiFactory.Domain.Outlines;
using YoutubeAiFactory.Domain.Projects;
using YoutubeAiFactory.Domain.Research;
using YoutubeAiFactory.Domain.Scripts;
using YoutubeAiFactory.Domain.Videos;

namespace YoutubeAiFactory.Infrastructure.Persistence.Configurations;

internal sealed class VideoScriptConfiguration : IEntityTypeConfiguration<VideoScript>
{
    public void Configure(EntityTypeBuilder<VideoScript> builder)
    {
        builder.ToTable("video_scripts", table =>
        {
            table.HasCheckConstraint("ck_video_scripts_warnings_json", "ISJSON([warnings_json]) = 1");
            table.HasCheckConstraint("ck_video_scripts_grounding_issues_json", "ISJSON([grounding_issues_json]) = 1");
            table.HasCheckConstraint("ck_video_scripts_word_count", "[total_word_count] > 0");
            table.HasCheckConstraint("ck_video_scripts_duration", "[estimated_duration_seconds] > 0");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.ProjectId).HasColumnName("project_id");
        builder.Property(x => x.VideoProjectId).HasColumnName("video_project_id");
        builder.Property(x => x.VideoOutlineId).HasColumnName("video_outline_id");
        builder.Property(x => x.VideoOutlineVersion).HasColumnName("video_outline_version");
        builder.Property(x => x.ResearchReportId).HasColumnName("research_report_id");
        builder.Property(x => x.ResearchReportVersion).HasColumnName("research_report_version");
        builder.Property(x => x.Version).HasColumnName("version");
        builder.Property(x => x.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(x => x.GroundingStatus).HasColumnName("grounding_status").HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(x => x.GenerationAiRunId).HasColumnName("generation_ai_run_id");
        builder.Property(x => x.GroundingAiRunId).HasColumnName("grounding_ai_run_id");
        builder.Property(x => x.ScriptEngineVersion).HasColumnName("script_engine_version").HasMaxLength(100).IsRequired();
        builder.Property(x => x.PromptKey).HasColumnName("prompt_key").HasMaxLength(100).IsRequired();
        builder.Property(x => x.PromptVersion).HasColumnName("prompt_version").IsRequired();
        builder.Property(x => x.InputFingerprint).HasColumnName("input_fingerprint").HasMaxLength(128).IsRequired();
        builder.Property(x => x.Provider).HasColumnName("provider").HasMaxLength(100).IsRequired();
        builder.Property(x => x.Model).HasColumnName("model").HasMaxLength(100).IsRequired();
        builder.Property(x => x.ContentLanguage).HasColumnName("content_language").HasMaxLength(50).IsRequired();
        builder.Property(x => x.TotalWordCount).HasColumnName("total_word_count").IsRequired();
        builder.Property(x => x.EstimatedDurationSeconds).HasColumnName("estimated_duration_seconds").IsRequired();
        builder.Property(x => x.WarningsJson).HasColumnName("warnings_json").HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(x => x.GroundingIssuesJson).HasColumnName("grounding_issues_json").HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").IsRequired();
        builder.Property(x => x.ApprovedAt).HasColumnName("approved_at");
        builder.HasOne<Project>().WithMany().HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<VideoProject>().WithMany().HasForeignKey(x => x.VideoProjectId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<VideoOutline>().WithMany().HasForeignKey(x => x.VideoOutlineId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ResearchReport>().WithMany().HasForeignKey(x => x.ResearchReportId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<AiRun>().WithMany().HasForeignKey(x => x.GenerationAiRunId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<AiRun>().WithMany().HasForeignKey(x => x.GroundingAiRunId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.VideoProjectId, x.Version }).IsUnique();
        builder.HasIndex(x => new { x.ProjectId, x.VideoProjectId, x.CreatedAt });
        builder.HasIndex(x => x.VideoProjectId).HasDatabaseName("ux_video_scripts_approved")
            .IsUnique().HasFilter("status = 'Approved'");
    }
}

internal sealed class VideoScriptSectionConfiguration : IEntityTypeConfiguration<VideoScriptSection>
{
    public void Configure(EntityTypeBuilder<VideoScriptSection> builder)
    {
        builder.ToTable("video_script_sections", table =>
        {
            table.HasCheckConstraint("ck_video_script_sections_word_count", "[word_count] > 0");
            table.HasCheckConstraint("ck_video_script_sections_duration", "[estimated_duration_seconds] > 0");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.VideoScriptId).HasColumnName("video_script_id");
        builder.Property(x => x.VideoOutlineSectionId).HasColumnName("video_outline_section_id");
        builder.Property(x => x.Sequence).HasColumnName("sequence");
        builder.Property(x => x.Heading).HasColumnName("heading").HasMaxLength(500).IsRequired();
        builder.Property(x => x.WordCount).HasColumnName("word_count").IsRequired();
        builder.Property(x => x.EstimatedDurationSeconds).HasColumnName("estimated_duration_seconds").IsRequired();
        builder.HasOne<VideoScript>().WithMany().HasForeignKey(x => x.VideoScriptId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<VideoOutlineSection>().WithMany().HasForeignKey(x => x.VideoOutlineSectionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.VideoScriptId, x.Sequence }).IsUnique();
        builder.HasIndex(x => new { x.VideoScriptId, x.VideoOutlineSectionId }).IsUnique();
    }
}

internal sealed class VideoScriptBlockConfiguration : IEntityTypeConfiguration<VideoScriptBlock>
{
    public void Configure(EntityTypeBuilder<VideoScriptBlock> builder)
    {
        builder.ToTable("video_script_blocks", table =>
            table.HasCheckConstraint("ck_video_script_blocks_word_count", "[word_count] > 0"));
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.VideoScriptSectionId).HasColumnName("video_script_section_id");
        builder.Property(x => x.Sequence).HasColumnName("sequence");
        builder.Property(x => x.Type).HasColumnName("block_type").HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(x => x.Text).HasColumnName("narration_text").HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(x => x.WordCount).HasColumnName("word_count").IsRequired();
        builder.HasOne<VideoScriptSection>().WithMany().HasForeignKey(x => x.VideoScriptSectionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.VideoScriptSectionId, x.Sequence }).IsUnique();
    }
}

internal sealed class VideoScriptBlockClaimConfiguration : IEntityTypeConfiguration<VideoScriptBlockClaim>
{
    public void Configure(EntityTypeBuilder<VideoScriptBlockClaim> builder)
    {
        builder.ToTable("video_script_block_claims");
        builder.HasKey(x => new { x.ScriptBlockId, x.ResearchClaimId });
        builder.Property(x => x.ScriptBlockId).HasColumnName("script_block_id");
        builder.Property(x => x.ResearchClaimId).HasColumnName("research_claim_id");
        builder.HasOne<VideoScriptBlock>().WithMany().HasForeignKey(x => x.ScriptBlockId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ResearchClaim>().WithMany().HasForeignKey(x => x.ResearchClaimId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => x.ResearchClaimId);
    }
}

internal sealed class VideoScriptBlockConflictConfiguration : IEntityTypeConfiguration<VideoScriptBlockConflict>
{
    public void Configure(EntityTypeBuilder<VideoScriptBlockConflict> builder)
    {
        builder.ToTable("video_script_block_conflicts");
        builder.HasKey(x => new { x.ScriptBlockId, x.ResearchConflictId });
        builder.Property(x => x.ScriptBlockId).HasColumnName("script_block_id");
        builder.Property(x => x.ResearchConflictId).HasColumnName("research_conflict_id");
        builder.HasOne<VideoScriptBlock>().WithMany().HasForeignKey(x => x.ScriptBlockId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ResearchConflict>().WithMany().HasForeignKey(x => x.ResearchConflictId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => x.ResearchConflictId);
    }
}
