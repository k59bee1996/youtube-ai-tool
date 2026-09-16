using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using YoutubeAiFactory.Domain.AI;
using YoutubeAiFactory.Domain.Outlines;
using YoutubeAiFactory.Domain.Projects;
using YoutubeAiFactory.Domain.Research;
using YoutubeAiFactory.Domain.Videos;

namespace YoutubeAiFactory.Infrastructure.Persistence.Configurations;

internal sealed class VideoOutlineConfiguration : IEntityTypeConfiguration<VideoOutline>
{
    public void Configure(EntityTypeBuilder<VideoOutline> builder)
    {
        builder.ToTable("video_outlines", table =>
        {
            table.HasCheckConstraint("ck_video_outlines_experiment_risks_json", "ISJSON([experiment_risks_json]) = 1");
            table.HasCheckConstraint("ck_video_outlines_warnings_json", "ISJSON([warnings_json]) = 1");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.ProjectId).HasColumnName("project_id");
        builder.Property(x => x.VideoProjectId).HasColumnName("video_project_id");
        builder.Property(x => x.ResearchReportId).HasColumnName("research_report_id");
        builder.Property(x => x.ResearchReportVersion).HasColumnName("research_report_version");
        builder.Property(x => x.Version).HasColumnName("version");
        builder.Property(x => x.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(x => x.GenerationAiRunId).HasColumnName("generation_ai_run_id");
        builder.Property(x => x.OutlineAlgorithmVersion).HasColumnName("outline_algorithm_version").HasMaxLength(100).IsRequired();
        builder.Property(x => x.PromptKey).HasColumnName("prompt_key").HasMaxLength(100).IsRequired();
        builder.Property(x => x.PromptVersion).HasColumnName("prompt_version").IsRequired();
        builder.Property(x => x.InputFingerprint).HasColumnName("input_fingerprint").HasMaxLength(128).IsRequired();
        builder.Property(x => x.Provider).HasColumnName("provider").HasMaxLength(100).IsRequired();
        builder.Property(x => x.Model).HasColumnName("model").HasMaxLength(100).IsRequired();
        builder.Property(x => x.StructureType).HasColumnName("structure_type").HasConversion<string>().HasMaxLength(40).IsRequired();
        builder.Property(x => x.CoreQuestion).HasColumnName("core_question").HasMaxLength(1_000).IsRequired();
        builder.Property(x => x.CoreTension).HasColumnName("core_tension").HasMaxLength(2_000).IsRequired();
        builder.Property(x => x.OpeningHookConcept).HasColumnName("opening_hook_concept").HasMaxLength(2_000).IsRequired();
        builder.Property(x => x.ViewerPromise).HasColumnName("viewer_promise").HasMaxLength(2_000).IsRequired();
        builder.Property(x => x.NarrativeProgression).HasColumnName("narrative_progression").HasMaxLength(4_000).IsRequired();
        builder.Property(x => x.Payoff).HasColumnName("payoff").HasMaxLength(2_000).IsRequired();
        builder.Property(x => x.PacingStrategy).HasColumnName("pacing_strategy").HasMaxLength(2_000).IsRequired();
        builder.Property(x => x.ExperimentType).HasColumnName("experiment_type").HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(x => x.VariableBeingTested).HasColumnName("variable_being_tested").HasMaxLength(2_000).IsRequired();
        builder.Property(x => x.ControlStrategy).HasColumnName("control_strategy").HasMaxLength(2_000).IsRequired();
        builder.Property(x => x.HowOutlineImplementsExperiment).HasColumnName("experiment_alignment").HasMaxLength(4_000).IsRequired();
        builder.Property(x => x.ExperimentRisksJson).HasColumnName("experiment_risks_json").HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(x => x.WarningsJson).HasColumnName("warnings_json").HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(x => x.TotalEstimatedSeconds).HasColumnName("total_estimated_seconds");
        builder.Property(x => x.TransitionsRequireReview).HasColumnName("transitions_require_review").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").IsRequired();
        builder.Property(x => x.ApprovedAt).HasColumnName("approved_at");
        builder.HasOne<Project>().WithMany().HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<VideoProject>().WithMany().HasForeignKey(x => x.VideoProjectId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ResearchReport>().WithMany().HasForeignKey(x => x.ResearchReportId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<AiRun>().WithMany().HasForeignKey(x => x.GenerationAiRunId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.VideoProjectId, x.Version }).IsUnique();
        builder.HasIndex(x => new { x.ProjectId, x.VideoProjectId, x.CreatedAt });
        builder.HasIndex(x => x.VideoProjectId).HasDatabaseName("ux_video_outlines_approved")
            .IsUnique().HasFilter("status = 'Approved'");
    }
}

internal sealed class VideoOutlineSectionConfiguration : IEntityTypeConfiguration<VideoOutlineSection>
{
    public void Configure(EntityTypeBuilder<VideoOutlineSection> builder)
    {
        builder.ToTable("video_outline_sections");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.VideoOutlineId).HasColumnName("video_outline_id");
        builder.Property(x => x.Sequence).HasColumnName("sequence");
        builder.Property(x => x.Heading).HasColumnName("heading").HasMaxLength(500).IsRequired();
        builder.Property(x => x.Purpose).HasColumnName("purpose").HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(x => x.Objective).HasColumnName("objective").HasMaxLength(1_000).IsRequired();
        builder.Property(x => x.Summary).HasColumnName("summary").HasMaxLength(2_000).IsRequired();
        builder.Property(x => x.ViewerQuestion).HasColumnName("viewer_question").HasMaxLength(1_000);
        builder.Property(x => x.TransitionIntent).HasColumnName("transition_intent").HasMaxLength(1_000);
        builder.Property(x => x.EstimatedSeconds).HasColumnName("estimated_seconds");
        builder.HasOne<VideoOutline>().WithMany().HasForeignKey(x => x.VideoOutlineId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.VideoOutlineId, x.Sequence }).IsUnique();
    }
}

internal sealed class VideoOutlineSectionClaimConfiguration : IEntityTypeConfiguration<VideoOutlineSectionClaim>
{
    public void Configure(EntityTypeBuilder<VideoOutlineSectionClaim> builder)
    {
        builder.ToTable("video_outline_section_claims");
        builder.HasKey(x => new { x.OutlineSectionId, x.ResearchClaimId, x.UsageRole });
        builder.Property(x => x.OutlineSectionId).HasColumnName("outline_section_id");
        builder.Property(x => x.ResearchClaimId).HasColumnName("research_claim_id");
        builder.Property(x => x.UsageRole).HasColumnName("usage_role").HasConversion<string>().HasMaxLength(30);
        builder.HasOne<VideoOutlineSection>().WithMany().HasForeignKey(x => x.OutlineSectionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ResearchClaim>().WithMany().HasForeignKey(x => x.ResearchClaimId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => x.ResearchClaimId);
    }
}

internal sealed class VideoOutlineSectionConflictConfiguration : IEntityTypeConfiguration<VideoOutlineSectionConflict>
{
    public void Configure(EntityTypeBuilder<VideoOutlineSectionConflict> builder)
    {
        builder.ToTable("video_outline_section_conflicts");
        builder.HasKey(x => new { x.OutlineSectionId, x.ResearchConflictId });
        builder.Property(x => x.OutlineSectionId).HasColumnName("outline_section_id");
        builder.Property(x => x.ResearchConflictId).HasColumnName("research_conflict_id");
        builder.HasOne<VideoOutlineSection>().WithMany().HasForeignKey(x => x.OutlineSectionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ResearchConflict>().WithMany().HasForeignKey(x => x.ResearchConflictId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => x.ResearchConflictId);
    }
}

internal sealed class VideoOutlineSectionGapConfiguration : IEntityTypeConfiguration<VideoOutlineSectionGap>
{
    public void Configure(EntityTypeBuilder<VideoOutlineSectionGap> builder)
    {
        builder.ToTable("video_outline_section_gaps");
        builder.HasKey(x => new { x.OutlineSectionId, x.ResearchGapIndex });
        builder.Property(x => x.OutlineSectionId).HasColumnName("outline_section_id");
        builder.Property(x => x.ResearchGapIndex).HasColumnName("research_gap_index");
        builder.HasOne<VideoOutlineSection>().WithMany().HasForeignKey(x => x.OutlineSectionId).OnDelete(DeleteBehavior.Restrict);
    }
}
