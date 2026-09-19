using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using YoutubeAiFactory.Domain.AI;
using YoutubeAiFactory.Domain.Production;
using YoutubeAiFactory.Domain.Projects;
using YoutubeAiFactory.Domain.Research;
using YoutubeAiFactory.Domain.Scripts;
using YoutubeAiFactory.Domain.Videos;

namespace YoutubeAiFactory.Infrastructure.Persistence.Configurations;

internal sealed class ProductionPackageConfiguration : IEntityTypeConfiguration<ProductionPackage>
{
    public void Configure(EntityTypeBuilder<ProductionPackage> b)
    {
        b.ToTable(
            "production_packages",
            t =>
            {
                t.HasCheckConstraint(
                    "ck_production_packages_duration",
                    "[estimated_duration_seconds] > 0"
                );
                t.HasCheckConstraint(
                    "ck_production_packages_warnings_json",
                    "ISJSON([warnings_json]) = 1"
                );
                t.HasCheckConstraint(
                    "ck_production_packages_issues_json",
                    "ISJSON([grounding_issues_json]) = 1"
                );
            }
        );
        b.HasKey(x => x.Id);
        Col(b, x => x.Id, "id");
        Col(b, x => x.ProjectId, "project_id");
        Col(b, x => x.VideoProjectId, "video_project_id");
        Col(b, x => x.VideoScriptId, "video_script_id");
        Col(b, x => x.VideoScriptVersion, "video_script_version");
        Col(b, x => x.VideoOutlineId, "video_outline_id");
        Col(b, x => x.VideoOutlineVersion, "video_outline_version");
        Col(b, x => x.ResearchReportId, "research_report_id");
        Col(b, x => x.ResearchReportVersion, "research_report_version");
        Col(b, x => x.Version, "version");
        b.Property(x => x.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(30);
        b.Property(x => x.GroundingStatus)
            .HasColumnName("grounding_status")
            .HasConversion<string>()
            .HasMaxLength(30);
        Col(b, x => x.GenerationAiRunId, "generation_ai_run_id");
        Col(b, x => x.GroundingAiRunId, "grounding_ai_run_id");
        Text(b, x => x.EngineVersion, "engine_version", 100);
        Text(b, x => x.PromptKey, "prompt_key", 100);
        Col(b, x => x.PromptVersion, "prompt_version");
        Text(b, x => x.InputFingerprint, "input_fingerprint", 128);
        Text(b, x => x.Provider, "provider", 100);
        Text(b, x => x.Model, "model", 100);
        Text(b, x => x.ContentLanguage, "content_language", 50);
        Col(b, x => x.EstimatedDurationSeconds, "estimated_duration_seconds");
        Text(b, x => x.VisualDirection, "visual_direction", 4000);
        Text(b, x => x.PacingDirection, "pacing_direction", 2000);
        Text(b, x => x.ColorDirection, "color_direction", 2000);
        Text(b, x => x.TypographyDirection, "typography_direction", 2000);
        Text(b, x => x.AudioDirection, "audio_direction", 2000);
        Text(b, x => x.ExperimentProductionNotes, "experiment_production_notes", 4000);
        b.Property(x => x.WarningsJson)
            .HasColumnName("warnings_json")
            .HasColumnType("nvarchar(max)");
        b.Property(x => x.GroundingIssuesJson)
            .HasColumnName("grounding_issues_json")
            .HasColumnType("nvarchar(max)");
        Col(b, x => x.CreatedAt, "created_at");
        Col(b, x => x.UpdatedAt, "updated_at");
        Col(b, x => x.ApprovedAt, "approved_at");
        b.HasOne<Project>()
            .WithMany()
            .HasForeignKey(x => x.ProjectId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne<VideoProject>()
            .WithMany()
            .HasForeignKey(x => x.VideoProjectId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne<VideoScript>()
            .WithMany()
            .HasForeignKey(x => x.VideoScriptId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne<ResearchReport>()
            .WithMany()
            .HasForeignKey(x => x.ResearchReportId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne<AiRun>()
            .WithMany()
            .HasForeignKey(x => x.GenerationAiRunId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne<AiRun>()
            .WithMany()
            .HasForeignKey(x => x.GroundingAiRunId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.VideoProjectId, x.Version }).IsUnique();
        b.HasIndex(x => x.VideoProjectId)
            .IsUnique()
            .HasFilter("status = 'Approved'")
            .HasDatabaseName("ux_production_packages_approved");
    }

    private static void Col<T>(
        EntityTypeBuilder<ProductionPackage> b,
        System.Linq.Expressions.Expression<Func<ProductionPackage, T>> p,
        string n
    ) => b.Property(p).HasColumnName(n);

    private static void Text(
        EntityTypeBuilder<ProductionPackage> b,
        System.Linq.Expressions.Expression<Func<ProductionPackage, string>> p,
        string n,
        int len
    ) => b.Property(p).HasColumnName(n).HasMaxLength(len).IsRequired();
}

internal sealed class ProductionSceneConfiguration : IEntityTypeConfiguration<ProductionScene>
{
    public void Configure(EntityTypeBuilder<ProductionScene> b)
    {
        b.ToTable(
            "production_scenes",
            t =>
                t.HasCheckConstraint(
                    "ck_production_scenes_duration",
                    "[estimated_duration_seconds] > 0"
                )
        );
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.ProductionPackageId).HasColumnName("production_package_id");
        b.Property(x => x.Sequence).HasColumnName("sequence");
        b.Property(x => x.Purpose)
            .HasColumnName("purpose")
            .HasConversion<string>()
            .HasMaxLength(30);
        b.Property(x => x.NarrationSummary).HasColumnName("narration_summary").HasMaxLength(2000);
        b.Property(x => x.VisualStrategy).HasColumnName("visual_strategy").HasMaxLength(4000);
        b.Property(x => x.EstimatedDurationSeconds).HasColumnName("estimated_duration_seconds");
        b.Property(x => x.Complexity)
            .HasColumnName("complexity")
            .HasConversion<string>()
            .HasMaxLength(20);
        b.Property(x => x.TransitionIntent).HasColumnName("transition_intent").HasMaxLength(1000);
        b.Property(x => x.MusicBrief).HasColumnName("music_brief").HasMaxLength(1000);
        b.Property(x => x.SoundEffectCue).HasColumnName("sound_effect_cue").HasMaxLength(1000);
        b.Property(x => x.VoiceDirection).HasColumnName("voice_direction").HasMaxLength(1000);
        b.HasOne<ProductionPackage>()
            .WithMany()
            .HasForeignKey(x => x.ProductionPackageId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.ProductionPackageId, x.Sequence }).IsUnique();
    }
}

internal sealed class ProductionSceneScriptBlockConfiguration
    : IEntityTypeConfiguration<ProductionSceneScriptBlock>
{
    public void Configure(EntityTypeBuilder<ProductionSceneScriptBlock> b)
    {
        b.ToTable("production_scene_script_blocks");
        b.HasKey(x => new { x.ProductionSceneId, x.ScriptBlockId });
        b.Property(x => x.ProductionPackageId).HasColumnName("production_package_id");
        b.Property(x => x.ProductionSceneId).HasColumnName("production_scene_id");
        b.Property(x => x.ScriptBlockId).HasColumnName("script_block_id");
        b.Property(x => x.Sequence).HasColumnName("sequence");
        b.HasOne<ProductionPackage>()
            .WithMany()
            .HasForeignKey(x => x.ProductionPackageId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne<ProductionScene>()
            .WithMany()
            .HasForeignKey(x => x.ProductionSceneId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne<VideoScriptBlock>()
            .WithMany()
            .HasForeignKey(x => x.ScriptBlockId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.ProductionPackageId, x.ScriptBlockId }).IsUnique();
        b.HasIndex(x => new { x.ProductionSceneId, x.Sequence }).IsUnique();
    }
}

internal sealed class ProductionAssetConfiguration
    : IEntityTypeConfiguration<ProductionAssetRequirement>
{
    public void Configure(EntityTypeBuilder<ProductionAssetRequirement> b)
    {
        b.ToTable("production_asset_requirements");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.ProductionPackageId).HasColumnName("production_package_id");
        b.Property(x => x.AssetKey).HasColumnName("asset_key").HasMaxLength(100);
        b.Property(x => x.AssetType)
            .HasColumnName("asset_type")
            .HasConversion<string>()
            .HasMaxLength(30);
        b.Property(x => x.AcquisitionMode)
            .HasColumnName("acquisition_mode")
            .HasConversion<string>()
            .HasMaxLength(30);
        b.Property(x => x.CreativeBrief).HasColumnName("creative_brief").HasMaxLength(4000);
        b.Property(x => x.GenerationPrompt)
            .HasColumnName("generation_prompt")
            .HasColumnType("nvarchar(max)");
        b.Property(x => x.SourceSearchBrief)
            .HasColumnName("source_search_brief")
            .HasColumnType("nvarchar(max)");
        b.Property(x => x.RightsVerificationRequired).HasColumnName("rights_verification_required");
        b.Property(x => x.FactualityMode)
            .HasColumnName("factuality_mode")
            .HasConversion<string>()
            .HasMaxLength(40);
        b.Property(x => x.ReuseKey).HasColumnName("reuse_key").HasMaxLength(100);
        b.Property(x => x.Complexity)
            .HasColumnName("complexity")
            .HasConversion<string>()
            .HasMaxLength(20);
        b.HasOne<ProductionPackage>()
            .WithMany()
            .HasForeignKey(x => x.ProductionPackageId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.ProductionPackageId, x.AssetKey }).IsUnique();
        b.HasIndex(x => new { x.ProductionPackageId, x.ReuseKey });
    }
}

internal sealed class ProductionAssetClaimConfiguration
    : IEntityTypeConfiguration<ProductionAssetClaim>
{
    public void Configure(EntityTypeBuilder<ProductionAssetClaim> b)
    {
        b.ToTable("production_asset_claims");
        b.HasKey(x => new { x.ProductionAssetRequirementId, x.ResearchClaimId });
        b.Property(x => x.ProductionAssetRequirementId)
            .HasColumnName("production_asset_requirement_id");
        b.Property(x => x.ResearchClaimId).HasColumnName("research_claim_id");
        b.HasOne<ProductionAssetRequirement>()
            .WithMany()
            .HasForeignKey(x => x.ProductionAssetRequirementId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne<ResearchClaim>()
            .WithMany()
            .HasForeignKey(x => x.ResearchClaimId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class ProductionShotConfiguration : IEntityTypeConfiguration<ProductionShot>
{
    public void Configure(EntityTypeBuilder<ProductionShot> b)
    {
        b.ToTable(
            "production_shots",
            t =>
                t.HasCheckConstraint(
                    "ck_production_shots_duration",
                    "[estimated_duration_seconds] > 0"
                )
        );
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.ProductionSceneId).HasColumnName("production_scene_id");
        b.Property(x => x.Sequence).HasColumnName("sequence");
        b.Property(x => x.ShotType)
            .HasColumnName("shot_type")
            .HasConversion<string>()
            .HasMaxLength(30);
        b.Property(x => x.VisualDescription).HasColumnName("visual_description").HasMaxLength(4000);
        b.Property(x => x.Composition).HasColumnName("composition").HasMaxLength(2000);
        b.Property(x => x.MotionSuggestion).HasColumnName("motion_suggestion").HasMaxLength(2000);
        b.Property(x => x.EstimatedDurationSeconds).HasColumnName("estimated_duration_seconds");
        b.Property(x => x.FactualityMode)
            .HasColumnName("factuality_mode")
            .HasConversion<string>()
            .HasMaxLength(40);
        b.Property(x => x.AssetRequirementId).HasColumnName("asset_requirement_id");
        b.Property(x => x.Notes).HasColumnName("notes").HasMaxLength(4000);
        b.HasOne<ProductionScene>()
            .WithMany()
            .HasForeignKey(x => x.ProductionSceneId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne<ProductionAssetRequirement>()
            .WithMany()
            .HasForeignKey(x => x.AssetRequirementId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.ProductionSceneId, x.Sequence }).IsUnique();
    }
}

internal sealed class ProductionShotClaimConfiguration
    : IEntityTypeConfiguration<ProductionShotClaim>
{
    public void Configure(EntityTypeBuilder<ProductionShotClaim> b)
    {
        b.ToTable("production_shot_claims");
        b.HasKey(x => new { x.ProductionShotId, x.ResearchClaimId });
        b.Property(x => x.ProductionShotId).HasColumnName("production_shot_id");
        b.Property(x => x.ResearchClaimId).HasColumnName("research_claim_id");
        b.HasOne<ProductionShot>()
            .WithMany()
            .HasForeignKey(x => x.ProductionShotId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne<ResearchClaim>()
            .WithMany()
            .HasForeignKey(x => x.ResearchClaimId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class ProductionTextConfiguration : IEntityTypeConfiguration<ProductionOnScreenText>
{
    public void Configure(EntityTypeBuilder<ProductionOnScreenText> b)
    {
        b.ToTable("production_on_screen_text");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.ProductionSceneId).HasColumnName("production_scene_id");
        b.Property(x => x.Sequence).HasColumnName("sequence");
        b.Property(x => x.Text).HasColumnName("text").HasMaxLength(1000);
        b.Property(x => x.Type).HasColumnName("text_type").HasConversion<string>().HasMaxLength(30);
        b.Property(x => x.TimingIntent).HasColumnName("timing_intent").HasMaxLength(500);
        b.HasOne<ProductionScene>()
            .WithMany()
            .HasForeignKey(x => x.ProductionSceneId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.ProductionSceneId, x.Sequence }).IsUnique();
    }
}

internal sealed class ProductionTextClaimConfiguration
    : IEntityTypeConfiguration<ProductionOnScreenTextClaim>
{
    public void Configure(EntityTypeBuilder<ProductionOnScreenTextClaim> b)
    {
        b.ToTable("production_on_screen_text_claims");
        b.HasKey(x => new { x.ProductionOnScreenTextId, x.ResearchClaimId });
        b.Property(x => x.ProductionOnScreenTextId).HasColumnName("production_on_screen_text_id");
        b.Property(x => x.ResearchClaimId).HasColumnName("research_claim_id");
        b.HasOne<ProductionOnScreenText>()
            .WithMany()
            .HasForeignKey(x => x.ProductionOnScreenTextId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne<ResearchClaim>()
            .WithMany()
            .HasForeignKey(x => x.ResearchClaimId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
