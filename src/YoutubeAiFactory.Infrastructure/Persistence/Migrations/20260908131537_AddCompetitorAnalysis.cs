using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace YoutubeAiFactory.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCompetitorAnalysis : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "competitor_channel_id",
                schema: "yaf",
                table: "ai_runs",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "competitor_analyses",
                schema: "yaf",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    competitor_channel_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    ai_run_id = table.Column<Guid>(type: "uuid", nullable: false),
                    prompt_key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    prompt_version = table.Column<int>(type: "integer", nullable: false),
                    provider = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    model = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    source_data_as_of = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    analyzed_video_count = table.Column<int>(type: "integer", nullable: false),
                    result_json = table.Column<string>(type: "jsonb", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_competitor_analyses", x => x.id);
                    table.ForeignKey(
                        name: "FK_competitor_analyses_competitor_channels_competitor_channel_~",
                        column: x => x.competitor_channel_id,
                        principalSchema: "yaf",
                        principalTable: "competitor_channels",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ai_runs_competitor_channel_id_started_at",
                schema: "yaf",
                table: "ai_runs",
                columns: new[] { "competitor_channel_id", "started_at" });

            migrationBuilder.CreateIndex(
                name: "IX_competitor_analyses_competitor_channel_id_created_at",
                schema: "yaf",
                table: "competitor_analyses",
                columns: new[] { "competitor_channel_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_competitor_analyses_competitor_channel_id_version",
                schema: "yaf",
                table: "competitor_analyses",
                columns: new[] { "competitor_channel_id", "version" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "competitor_analyses",
                schema: "yaf");

            migrationBuilder.DropIndex(
                name: "IX_ai_runs_competitor_channel_id_started_at",
                schema: "yaf",
                table: "ai_runs");

            migrationBuilder.DropColumn(
                name: "competitor_channel_id",
                schema: "yaf",
                table: "ai_runs");
        }
    }
}
