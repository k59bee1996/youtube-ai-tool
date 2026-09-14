using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using YoutubeAiFactory.Domain.Localization;

namespace YoutubeAiFactory.Infrastructure.Persistence.Configurations;

internal sealed class ArtifactLocalizationConfiguration : IEntityTypeConfiguration<ArtifactLocalization>
{
    public void Configure(EntityTypeBuilder<ArtifactLocalization> builder)
    {
        builder.ToTable("artifact_localizations", table =>
            table.HasCheckConstraint("ck_artifact_localizations_content_json", "ISJSON([content_json]) = 1"));
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Id).HasColumnName("id");
        builder.Property(item => item.ArtifactType).HasColumnName("artifact_type").HasMaxLength(100).IsRequired();
        builder.Property(item => item.ArtifactId).HasColumnName("artifact_id").IsRequired();
        builder.Property(item => item.ArtifactVersion).HasColumnName("artifact_version").IsRequired();
        builder.Property(item => item.Locale).HasColumnName("locale").HasMaxLength(10).IsRequired();
        builder.Property(item => item.ContentJson).HasColumnName("content_json").HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(item => item.AiRunId).HasColumnName("ai_run_id").IsRequired();
        builder.Property(item => item.PromptKey).HasColumnName("prompt_key").HasMaxLength(100).IsRequired();
        builder.Property(item => item.PromptVersion).HasColumnName("prompt_version").IsRequired();
        builder.Property(item => item.Provider).HasColumnName("provider").HasMaxLength(100).IsRequired();
        builder.Property(item => item.Model).HasColumnName("model").HasMaxLength(100).IsRequired();
        builder.Property(item => item.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.HasIndex(item => new { item.ArtifactType, item.ArtifactId, item.ArtifactVersion, item.Locale }).IsUnique();
    }
}
