using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using YoutubeAiFactory.Domain.Competitors;

namespace YoutubeAiFactory.Infrastructure.Persistence.Configurations;

internal sealed class CompetitorAnalysisConfiguration : IEntityTypeConfiguration<CompetitorAnalysis>
{
    public void Configure(EntityTypeBuilder<CompetitorAnalysis> builder)
    {
        builder.ToTable("competitor_analyses");
        builder.HasKey(analysis => analysis.Id);
        builder.Property(analysis => analysis.Id).HasColumnName("id");
        builder.Property(analysis => analysis.CompetitorChannelId).HasColumnName("competitor_channel_id").IsRequired();
        builder.Property(analysis => analysis.Version).HasColumnName("version").IsRequired();
        builder.Property(analysis => analysis.AiRunId).HasColumnName("ai_run_id").IsRequired();
        builder.Property(analysis => analysis.PromptKey).HasColumnName("prompt_key").HasMaxLength(100).IsRequired();
        builder.Property(analysis => analysis.PromptVersion).HasColumnName("prompt_version").IsRequired();
        builder.Property(analysis => analysis.Provider).HasColumnName("provider").HasMaxLength(100).IsRequired();
        builder.Property(analysis => analysis.Model).HasColumnName("model").HasMaxLength(100).IsRequired();
        builder.Property(analysis => analysis.SourceDataAsOf).HasColumnName("source_data_as_of").IsRequired();
        builder.Property(analysis => analysis.AnalyzedVideoCount).HasColumnName("analyzed_video_count").IsRequired();
        builder.Property(analysis => analysis.ResultJson).HasColumnName("result_json").HasColumnType("jsonb").IsRequired();
        builder.Property(analysis => analysis.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.HasIndex(analysis => new { analysis.CompetitorChannelId, analysis.Version }).IsUnique();
        builder.HasIndex(analysis => new { analysis.CompetitorChannelId, analysis.CreatedAt });
        builder.HasOne<CompetitorChannel>().WithMany().HasForeignKey(analysis => analysis.CompetitorChannelId).OnDelete(DeleteBehavior.Cascade);
    }
}
