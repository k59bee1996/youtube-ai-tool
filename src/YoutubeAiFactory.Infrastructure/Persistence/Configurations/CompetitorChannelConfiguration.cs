using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using YoutubeAiFactory.Domain.Competitors;
using YoutubeAiFactory.Domain.Projects;

namespace YoutubeAiFactory.Infrastructure.Persistence.Configurations;

internal sealed class CompetitorChannelConfiguration : IEntityTypeConfiguration<CompetitorChannel>
{
    public void Configure(EntityTypeBuilder<CompetitorChannel> builder)
    {
        builder.ToTable("competitor_channels");
        builder.HasKey(channel => channel.Id);
        builder.Property(channel => channel.Id).HasColumnName("id");
        builder.Property(channel => channel.ProjectId).HasColumnName("project_id").IsRequired();
        builder.Property(channel => channel.SourceUrl).HasColumnName("source_url").HasMaxLength(2_048).IsRequired();
        builder.Property(channel => channel.YoutubeChannelId).HasColumnName("youtube_channel_id").HasMaxLength(100).UseCollation("Latin1_General_100_BIN2").IsRequired();
        builder.Property(channel => channel.Title).HasColumnName("title").HasMaxLength(500).IsRequired();
        builder.Property(channel => channel.Description).HasColumnName("description").HasColumnType("nvarchar(max)");
        builder.Property(channel => channel.Handle).HasColumnName("handle").HasMaxLength(100);
        builder.Property(channel => channel.ThumbnailUrl).HasColumnName("thumbnail_url").HasMaxLength(2_048);
        builder.Property(channel => channel.SubscriberCount).HasColumnName("subscriber_count");
        builder.Property(channel => channel.VideoCount).HasColumnName("video_count");
        builder.Property(channel => channel.ViewCount).HasColumnName("view_count");
        builder.Property(channel => channel.PublishedAt).HasColumnName("published_at");
        builder.Property(channel => channel.LastCollectedAt).HasColumnName("last_collected_at").IsRequired();
        builder.Property(channel => channel.CreatedAt).HasColumnName("created_at").IsRequired();

        builder.HasOne<Project>()
            .WithMany()
            .HasForeignKey(channel => channel.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(channel => new { channel.ProjectId, channel.YoutubeChannelId })
            .IsUnique();

        builder.HasMany(channel => channel.Videos)
            .WithOne()
            .HasForeignKey(video => video.CompetitorChannelId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(channel => channel.Videos)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
