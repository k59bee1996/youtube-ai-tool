using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using YoutubeAiFactory.Domain.Projects;
using YoutubeAiFactory.Domain.Research;
using YoutubeAiFactory.Domain.Videos;

namespace YoutubeAiFactory.Infrastructure.Persistence.Configurations;

internal sealed class ResearchRunConfiguration : IEntityTypeConfiguration<ResearchRun>
{
    public void Configure(EntityTypeBuilder<ResearchRun> builder)
    {
        builder.ToTable("research_runs");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.ProjectId).HasColumnName("project_id");
        builder.Property(x => x.VideoProjectId).HasColumnName("video_project_id");
        builder.Property(x => x.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(x => x.ResearchAlgorithmVersion).HasColumnName("research_algorithm_version").HasMaxLength(100).IsRequired();
        builder.Property(x => x.InputFingerprint).HasColumnName("input_fingerprint").HasMaxLength(128).IsRequired();
        builder.Property(x => x.QueuedAt).HasColumnName("queued_at").IsRequired();
        builder.Property(x => x.StartedAt).HasColumnName("started_at");
        builder.Property(x => x.CompletedAt).HasColumnName("completed_at");
        builder.Property(x => x.SearchQueryCount).HasColumnName("search_query_count").IsRequired();
        builder.Property(x => x.SearchResultCount).HasColumnName("search_result_count").IsRequired();
        builder.Property(x => x.FetchedSourceCount).HasColumnName("fetched_source_count").IsRequired();
        builder.Property(x => x.RelevantSourceCount).HasColumnName("relevant_source_count").IsRequired();
        builder.Property(x => x.EvidenceCount).HasColumnName("evidence_count").IsRequired();
        builder.Property(x => x.ClaimCount).HasColumnName("claim_count").IsRequired();
        builder.Property(x => x.ConflictCount).HasColumnName("conflict_count").IsRequired();
        builder.Property(x => x.SearchFailureCount).HasColumnName("search_failure_count").IsRequired();
        builder.Property(x => x.FetchFailureCount).HasColumnName("fetch_failure_count").IsRequired();
        builder.Property(x => x.ResearchReportId).HasColumnName("research_report_id");
        builder.Property(x => x.FailureReason).HasColumnName("failure_reason").HasMaxLength(2_000);
        builder.HasOne<Project>().WithMany().HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<VideoProject>().WithMany().HasForeignKey(x => x.VideoProjectId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.VideoProjectId, x.QueuedAt });
        builder.HasIndex(x => new { x.ProjectId, x.Status });
    }
}

internal sealed class ResearchReportConfiguration : IEntityTypeConfiguration<ResearchReport>
{
    public void Configure(EntityTypeBuilder<ResearchReport> builder)
    {
        builder.ToTable("research_reports", table =>
            table.HasCheckConstraint("ck_research_reports_result_json", "ISJSON([result_json]) = 1"));
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.ProjectId).HasColumnName("project_id");
        builder.Property(x => x.VideoProjectId).HasColumnName("video_project_id");
        builder.Property(x => x.ResearchRunId).HasColumnName("research_run_id");
        builder.Property(x => x.Version).HasColumnName("version").IsRequired();
        builder.Property(x => x.ResearchAlgorithmVersion).HasColumnName("research_algorithm_version").HasMaxLength(100).IsRequired();
        builder.Property(x => x.InputFingerprint).HasColumnName("input_fingerprint").HasMaxLength(128).IsRequired();
        builder.Property(x => x.ResultJson).HasColumnName("result_json").HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(x => x.SynthesisAiRunId).HasColumnName("synthesis_ai_run_id");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.HasOne<Project>().WithMany().HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<VideoProject>().WithMany().HasForeignKey(x => x.VideoProjectId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ResearchRun>().WithMany().HasForeignKey(x => x.ResearchRunId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.VideoProjectId, x.Version }).IsUnique();
        builder.HasIndex(x => x.ResearchRunId).IsUnique();
    }
}

internal sealed class ResearchSourceConfiguration : IEntityTypeConfiguration<ResearchSource>
{
    public void Configure(EntityTypeBuilder<ResearchSource> builder)
    {
        builder.ToTable("research_sources");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.ResearchRunId).HasColumnName("research_run_id");
        builder.Property(x => x.Url).HasColumnName("url").HasMaxLength(2_048).IsRequired();
        builder.Property(x => x.CanonicalUrl).HasColumnName("canonical_url").HasMaxLength(2_048).IsRequired();
        builder.Property(x => x.Domain).HasColumnName("domain").HasMaxLength(255).IsRequired();
        builder.Property(x => x.Title).HasColumnName("title").HasMaxLength(1_000);
        builder.Property(x => x.Publisher).HasColumnName("publisher").HasMaxLength(500);
        builder.Property(x => x.PublishedAt).HasColumnName("published_at");
        builder.Property(x => x.RetrievedAt).HasColumnName("retrieved_at").IsRequired();
        builder.Property(x => x.Category).HasColumnName("source_category").HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(x => x.FetchStatus).HasColumnName("fetch_status").HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(x => x.ContentHash).HasColumnName("content_hash").HasMaxLength(128);
        builder.Property(x => x.QualityNotes).HasColumnName("quality_notes").HasMaxLength(1_000);
        builder.Property(x => x.FailureReason).HasColumnName("failure_reason").HasMaxLength(1_000);
        builder.HasOne<ResearchRun>().WithMany().HasForeignKey(x => x.ResearchRunId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.ResearchRunId, x.CanonicalUrl }).IsUnique();
        builder.HasIndex(x => new { x.ResearchRunId, x.Domain });
    }
}

internal sealed class ResearchEvidenceConfiguration : IEntityTypeConfiguration<ResearchEvidence>
{
    public void Configure(EntityTypeBuilder<ResearchEvidence> builder)
    {
        builder.ToTable("research_evidence");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.ResearchRunId).HasColumnName("research_run_id");
        builder.Property(x => x.ResearchSourceId).HasColumnName("research_source_id");
        builder.Property(x => x.Type).HasColumnName("evidence_type").HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(x => x.Fact).HasColumnName("fact").HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(x => x.SupportingExcerpt).HasColumnName("supporting_excerpt").HasMaxLength(2_000).IsRequired();
        builder.Property(x => x.SourceLocator).HasColumnName("source_locator").HasMaxLength(500).IsRequired();
        builder.Property(x => x.Confidence).HasColumnName("confidence").HasPrecision(5, 2).IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.HasOne<ResearchRun>().WithMany().HasForeignKey(x => x.ResearchRunId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ResearchSource>().WithMany().HasForeignKey(x => x.ResearchSourceId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.ResearchRunId, x.ResearchSourceId });
    }
}

internal sealed class ResearchClaimConfiguration : IEntityTypeConfiguration<ResearchClaim>
{
    public void Configure(EntityTypeBuilder<ResearchClaim> builder)
    {
        builder.ToTable("research_claims");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.ResearchReportId).HasColumnName("research_report_id");
        builder.Property(x => x.Statement).HasColumnName("statement").HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(x => x.Type).HasColumnName("claim_type").HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(x => x.SupportStatus).HasColumnName("support_status").HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(x => x.Confidence).HasColumnName("confidence").HasPrecision(5, 2).IsRequired();
        builder.Property(x => x.IsCritical).HasColumnName("is_critical").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.HasOne<ResearchReport>().WithMany().HasForeignKey(x => x.ResearchReportId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.ResearchReportId, x.SupportStatus });
    }
}

internal sealed class ResearchClaimEvidenceConfiguration : IEntityTypeConfiguration<ResearchClaimEvidence>
{
    public void Configure(EntityTypeBuilder<ResearchClaimEvidence> builder)
    {
        builder.ToTable("research_claim_evidence");
        builder.HasKey(x => new { x.ResearchClaimId, x.ResearchEvidenceId, x.Stance });
        builder.Property(x => x.ResearchClaimId).HasColumnName("research_claim_id");
        builder.Property(x => x.ResearchEvidenceId).HasColumnName("research_evidence_id");
        builder.Property(x => x.Stance).HasColumnName("stance").HasConversion<string>().HasMaxLength(20);
        builder.HasOne<ResearchClaim>().WithMany().HasForeignKey(x => x.ResearchClaimId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ResearchEvidence>().WithMany().HasForeignKey(x => x.ResearchEvidenceId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => x.ResearchEvidenceId);
    }
}

internal sealed class ResearchConflictConfiguration : IEntityTypeConfiguration<ResearchConflict>
{
    public void Configure(EntityTypeBuilder<ResearchConflict> builder)
    {
        builder.ToTable("research_conflicts");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.ResearchReportId).HasColumnName("research_report_id");
        builder.Property(x => x.ResearchClaimId).HasColumnName("research_claim_id");
        builder.Property(x => x.SupportingEvidenceId).HasColumnName("supporting_evidence_id");
        builder.Property(x => x.ContradictingEvidenceId).HasColumnName("contradicting_evidence_id");
        builder.Property(x => x.Explanation).HasColumnName("explanation").HasMaxLength(2_000).IsRequired();
        builder.Property(x => x.IsResolved).HasColumnName("is_resolved").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.HasOne<ResearchReport>().WithMany().HasForeignKey(x => x.ResearchReportId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ResearchClaim>().WithMany().HasForeignKey(x => x.ResearchClaimId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ResearchEvidence>().WithMany().HasForeignKey(x => x.SupportingEvidenceId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ResearchEvidence>().WithMany().HasForeignKey(x => x.ContradictingEvidenceId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.ResearchReportId, x.ResearchClaimId });
        builder.HasIndex(x => new { x.ResearchClaimId, x.SupportingEvidenceId, x.ContradictingEvidenceId }).IsUnique();
    }
}
