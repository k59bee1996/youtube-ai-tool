using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace YoutubeAiFactory.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCompetitorCollectionMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "comment_count",
                schema: "yaf",
                table: "competitor_videos",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "description",
                schema: "yaf",
                table: "competitor_videos",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "duration",
                schema: "yaf",
                table: "competitor_videos",
                type: "interval",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "like_count",
                schema: "yaf",
                table: "competitor_videos",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "thumbnail_url",
                schema: "yaf",
                table: "competitor_videos",
                type: "character varying(2048)",
                maxLength: 2048,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "view_count",
                schema: "yaf",
                table: "competitor_videos",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "handle",
                schema: "yaf",
                table: "competitor_channels",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "subscriber_count",
                schema: "yaf",
                table: "competitor_channels",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "video_count",
                schema: "yaf",
                table: "competitor_channels",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "view_count",
                schema: "yaf",
                table: "competitor_channels",
                type: "bigint",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "comment_count",
                schema: "yaf",
                table: "competitor_videos");

            migrationBuilder.DropColumn(
                name: "description",
                schema: "yaf",
                table: "competitor_videos");

            migrationBuilder.DropColumn(
                name: "duration",
                schema: "yaf",
                table: "competitor_videos");

            migrationBuilder.DropColumn(
                name: "like_count",
                schema: "yaf",
                table: "competitor_videos");

            migrationBuilder.DropColumn(
                name: "thumbnail_url",
                schema: "yaf",
                table: "competitor_videos");

            migrationBuilder.DropColumn(
                name: "view_count",
                schema: "yaf",
                table: "competitor_videos");

            migrationBuilder.DropColumn(
                name: "handle",
                schema: "yaf",
                table: "competitor_channels");

            migrationBuilder.DropColumn(
                name: "subscriber_count",
                schema: "yaf",
                table: "competitor_channels");

            migrationBuilder.DropColumn(
                name: "video_count",
                schema: "yaf",
                table: "competitor_channels");

            migrationBuilder.DropColumn(
                name: "view_count",
                schema: "yaf",
                table: "competitor_channels");
        }
    }
}
