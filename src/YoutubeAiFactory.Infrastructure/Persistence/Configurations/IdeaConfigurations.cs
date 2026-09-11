using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using YoutubeAiFactory.Domain.Ideas;
using YoutubeAiFactory.Domain.Opportunities;
using YoutubeAiFactory.Domain.Projects;

namespace YoutubeAiFactory.Infrastructure.Persistence.Configurations;

internal sealed class IdeaGenerationConfiguration : IEntityTypeConfiguration<IdeaGeneration>
{
    public void Configure(EntityTypeBuilder<IdeaGeneration> builder)
    {
        builder.ToTable("idea_generations"); builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id"); builder.Property(x => x.ProjectId).HasColumnName("project_id"); builder.Property(x => x.OpportunityId).HasColumnName("opportunity_id"); builder.Property(x => x.OpportunityReportId).HasColumnName("opportunity_report_id"); builder.Property(x => x.OpportunityReportVersion).HasColumnName("opportunity_report_version"); builder.Property(x => x.Version).HasColumnName("version"); builder.Property(x => x.AiRunId).HasColumnName("ai_run_id"); builder.Property(x => x.PromptKey).HasColumnName("prompt_key").HasMaxLength(100); builder.Property(x => x.PromptVersion).HasColumnName("prompt_version"); builder.Property(x => x.Provider).HasColumnName("provider").HasMaxLength(100); builder.Property(x => x.Model).HasColumnName("model").HasMaxLength(100); builder.Property(x => x.ScoringAlgorithmVersion).HasColumnName("scoring_algorithm_version").HasMaxLength(100); builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.HasIndex(x => new { x.OpportunityId, x.Version }).IsUnique(); builder.HasIndex(x => new { x.ProjectId, x.CreatedAt });
        builder.HasOne<Project>().WithMany().HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<OpportunityCandidate>().WithMany().HasForeignKey(x => x.OpportunityId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<OpportunityReport>().WithMany().HasForeignKey(x => x.OpportunityReportId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class VideoIdeaConfiguration : IEntityTypeConfiguration<VideoIdea>
{
    public void Configure(EntityTypeBuilder<VideoIdea> builder)
    {
        builder.ToTable("video_ideas"); builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id"); builder.Property(x => x.ProjectId).HasColumnName("project_id"); builder.Property(x => x.OpportunityId).HasColumnName("opportunity_id"); builder.Property(x => x.GenerationId).HasColumnName("generation_id");
        foreach (var property in new[] { nameof(VideoIdea.WorkingTitle), nameof(VideoIdea.Topic), nameof(VideoIdea.Angle), nameof(VideoIdea.ContentFormat), nameof(VideoIdea.TargetAudience), nameof(VideoIdea.ViewerIntent) }) builder.Property(property).HasMaxLength(1000).IsRequired();
        foreach (var property in new[] { nameof(VideoIdea.HookConcept), nameof(VideoIdea.ThumbnailConcept), nameof(VideoIdea.ViewerPromise), nameof(VideoIdea.CoreQuestion), nameof(VideoIdea.WhyViewerWouldCare), nameof(VideoIdea.Hypothesis) }) builder.Property(property).HasColumnType("text").IsRequired();
        builder.Property(x => x.WorkingTitle).HasColumnName("working_title"); builder.Property(x => x.ContentFormat).HasColumnName("content_format"); builder.Property(x => x.TargetAudience).HasColumnName("target_audience"); builder.Property(x => x.ViewerIntent).HasColumnName("viewer_intent"); builder.Property(x => x.HookConcept).HasColumnName("hook_concept"); builder.Property(x => x.ThumbnailConcept).HasColumnName("thumbnail_concept"); builder.Property(x => x.ViewerPromise).HasColumnName("viewer_promise"); builder.Property(x => x.CoreQuestion).HasColumnName("core_question"); builder.Property(x => x.WhyViewerWouldCare).HasColumnName("why_viewer_would_care");
        foreach (var property in new[] { nameof(VideoIdea.OpportunityFit), nameof(VideoIdea.ObservedDemandAlignment), nameof(VideoIdea.Novelty), nameof(VideoIdea.TitlePotential), nameof(VideoIdea.ThumbnailPotential), nameof(VideoIdea.StoryPotential), nameof(VideoIdea.AudienceFit), nameof(VideoIdea.EvidenceStrength), nameof(VideoIdea.ProductionEase), nameof(VideoIdea.CompetitionRisk), nameof(VideoIdea.ResearchRisk), nameof(VideoIdea.Confidence) }) builder.Property(property).HasColumnName(ToSnake(property));
        builder.Property(x => x.OverallScore).HasColumnName("overall_score").HasPrecision(5, 2); builder.Property(x => x.DuplicationPenalty).HasColumnName("duplication_penalty").HasPrecision(5, 2); builder.Property(x => x.ScoringAlgorithmVersion).HasColumnName("scoring_algorithm_version").HasMaxLength(100); builder.Property(x => x.RisksJson).HasColumnName("risks_json").HasColumnType("jsonb"); builder.Property(x => x.DecisionStatus).HasColumnName("decision_status").HasConversion<string>().HasMaxLength(30); builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.HasIndex(x => new { x.OpportunityId, x.OverallScore }); builder.HasIndex(x => new { x.ProjectId, x.DecisionStatus }); builder.HasOne<IdeaGeneration>().WithMany().HasForeignKey(x => x.GenerationId).OnDelete(DeleteBehavior.Cascade); builder.HasOne<Project>().WithMany().HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Cascade); builder.HasOne<OpportunityCandidate>().WithMany().HasForeignKey(x => x.OpportunityId).OnDelete(DeleteBehavior.Restrict);
    }
    private static string ToSnake(string value) => string.Concat(value.Select((c, i) => i > 0 && char.IsUpper(c) ? "_" + char.ToLowerInvariant(c) : char.ToLowerInvariant(c).ToString()));
}

internal sealed class IdeaEvidenceConfiguration : IEntityTypeConfiguration<IdeaEvidence>
{
    public void Configure(EntityTypeBuilder<IdeaEvidence> builder)
    {
        builder.ToTable("idea_evidence"); builder.HasKey(x => x.Id); builder.Property(x => x.Id).HasColumnName("id"); builder.Property(x => x.IdeaId).HasColumnName("idea_id"); builder.Property(x => x.OpportunityEvidenceId).HasColumnName("opportunity_evidence_id"); builder.Property(x => x.Summary).HasColumnName("summary").HasMaxLength(2000);
        builder.HasIndex(x => new { x.IdeaId, x.OpportunityEvidenceId }).IsUnique(); builder.HasOne<VideoIdea>().WithMany().HasForeignKey(x => x.IdeaId).OnDelete(DeleteBehavior.Cascade); builder.HasOne<OpportunityEvidence>().WithMany().HasForeignKey(x => x.OpportunityEvidenceId).OnDelete(DeleteBehavior.Restrict);
    }
}
