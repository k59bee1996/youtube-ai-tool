using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using YoutubeAiFactory.Domain.Projects;

namespace YoutubeAiFactory.Infrastructure.Persistence.Configurations;

internal sealed class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> builder)
    {
        builder.ToTable("projects");
        builder.HasKey(project => project.Id);
        builder.Property(project => project.Id).HasColumnName("id");
        builder.Property(project => project.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        builder.Property(project => project.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(project => project.UpdatedAt).HasColumnName("updated_at");

        builder.OwnsOne(project => project.Market, market =>
        {
            market.Property(value => value.Name)
                .HasColumnName("market_name")
                .HasMaxLength(200)
                .IsRequired();
            market.Property(value => value.TargetLanguage)
                .HasColumnName("target_language")
                .HasMaxLength(50)
                .IsRequired();
            market.Property(value => value.TargetGeography)
                .HasColumnName("target_geography")
                .HasMaxLength(100)
                .IsRequired();
        });

        builder.OwnsOne(project => project.Audience, audience =>
        {
            audience.Property(value => value.Description)
                .HasColumnName("audience_description")
                .HasMaxLength(2_000)
                .IsRequired();
        });
    }
}
