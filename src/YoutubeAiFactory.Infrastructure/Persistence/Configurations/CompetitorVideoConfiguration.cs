using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using YoutubeAiFactory.Domain.Competitors;

namespace YoutubeAiFactory.Infrastructure.Persistence.Configurations;

internal sealed class CompetitorVideoConfiguration : IEntityTypeConfiguration<CompetitorVideo>
{
    public void Configure(EntityTypeBuilder<CompetitorVideo> builder)
    {
        builder.ToTable("competitor_videos");
        builder.HasKey(video => video.Id);
        builder.Property(video => video.Id).HasColumnName("id");
        builder.Property(video => video.CompetitorChannelId).HasColumnName("competitor_channel_id").IsRequired();
        builder.Property(video => video.YoutubeVideoId).HasColumnName("youtube_video_id").HasMaxLength(100).IsRequired();
        builder.Property(video => video.Title).HasColumnName("title").HasMaxLength(500).IsRequired();
        builder.Property(video => video.Description).HasColumnName("description").HasColumnType("text");
        builder.Property(video => video.Url).HasColumnName("url").HasMaxLength(2_048).IsRequired();
        builder.Property(video => video.ThumbnailUrl).HasColumnName("thumbnail_url").HasMaxLength(2_048);
        builder.Property(video => video.Duration).HasColumnName("duration");
        builder.Property(video => video.ViewCount).HasColumnName("view_count");
        builder.Property(video => video.LikeCount).HasColumnName("like_count");
        builder.Property(video => video.CommentCount).HasColumnName("comment_count");
        builder.Property(video => video.PublishedAt).HasColumnName("published_at");
        builder.Property(video => video.CollectedAt).HasColumnName("collected_at").IsRequired();

        builder.HasIndex(video => new { video.CompetitorChannelId, video.YoutubeVideoId })
            .IsUnique();
    }
}
