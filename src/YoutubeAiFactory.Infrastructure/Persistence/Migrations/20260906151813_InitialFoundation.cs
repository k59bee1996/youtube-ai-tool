using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace YoutubeAiFactory.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "yaf");

            migrationBuilder.CreateTable(
                name: "jobs",
                schema: "yaf",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    payload = table.Column<string>(type: "jsonb", nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    retry_count = table.Column<int>(type: "integer", nullable: false),
                    max_retries = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    available_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    failure_reason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_jobs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "projects",
                schema: "yaf",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    market_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    target_language = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    target_geography = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    audience_description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_projects", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "ai_runs",
                schema: "yaf",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    workflow = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    project_id = table.Column<Guid>(type: "uuid", nullable: true),
                    provider = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    model = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    prompt_key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    prompt_version = table.Column<int>(type: "integer", nullable: false),
                    input_tokens = table.Column<int>(type: "integer", nullable: true),
                    output_tokens = table.Column<int>(type: "integer", nullable: true),
                    estimated_cost = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: true),
                    latency_milliseconds = table.Column<long>(type: "bigint", nullable: true),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    retry_count = table.Column<int>(type: "integer", nullable: false),
                    started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    failure_reason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ai_runs", x => x.id);
                    table.ForeignKey(
                        name: "FK_ai_runs_projects_project_id",
                        column: x => x.project_id,
                        principalSchema: "yaf",
                        principalTable: "projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "competitor_channels",
                schema: "yaf",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    project_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    youtube_channel_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    thumbnail_url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    published_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    last_collected_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_competitor_channels", x => x.id);
                    table.ForeignKey(
                        name: "FK_competitor_channels_projects_project_id",
                        column: x => x.project_id,
                        principalSchema: "yaf",
                        principalTable: "projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "competitor_videos",
                schema: "yaf",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    competitor_channel_id = table.Column<Guid>(type: "uuid", nullable: false),
                    youtube_video_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    published_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    collected_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_competitor_videos", x => x.id);
                    table.ForeignKey(
                        name: "FK_competitor_videos_competitor_channels_competitor_channel_id",
                        column: x => x.competitor_channel_id,
                        principalSchema: "yaf",
                        principalTable: "competitor_channels",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ai_runs_project_id_started_at",
                schema: "yaf",
                table: "ai_runs",
                columns: new[] { "project_id", "started_at" });

            migrationBuilder.CreateIndex(
                name: "IX_competitor_channels_project_id_youtube_channel_id",
                schema: "yaf",
                table: "competitor_channels",
                columns: new[] { "project_id", "youtube_channel_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_competitor_videos_competitor_channel_id_youtube_video_id",
                schema: "yaf",
                table: "competitor_videos",
                columns: new[] { "competitor_channel_id", "youtube_video_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_jobs_status_available_at",
                schema: "yaf",
                table: "jobs",
                columns: new[] { "status", "available_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ai_runs",
                schema: "yaf");

            migrationBuilder.DropTable(
                name: "competitor_videos",
                schema: "yaf");

            migrationBuilder.DropTable(
                name: "jobs",
                schema: "yaf");

            migrationBuilder.DropTable(
                name: "competitor_channels",
                schema: "yaf");

            migrationBuilder.DropTable(
                name: "projects",
                schema: "yaf");
        }
    }
}
