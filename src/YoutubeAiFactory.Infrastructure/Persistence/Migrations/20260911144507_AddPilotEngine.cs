using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace YoutubeAiFactory.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPilotEngine : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_jobs_active_opportunity_analysis",
                schema: "yaf",
                table: "jobs");

            migrationBuilder.CreateTable(
                name: "pilots",
                schema: "yaf",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    project_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    ai_run_id = table.Column<Guid>(type: "uuid", nullable: false),
                    prompt_key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    prompt_version = table.Column<int>(type: "integer", nullable: false),
                    provider = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    model = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    planning_algorithm_version = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    objective = table.Column<string>(type: "text", nullable: false),
                    assumptions_json = table.Column<string>(type: "jsonb", nullable: false),
                    limitations_json = table.Column<string>(type: "jsonb", nullable: false),
                    warnings_json = table.Column<string>(type: "jsonb", nullable: false),
                    eligible_idea_count = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    approved_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pilots", x => x.id);
                    table.ForeignKey(
                        name: "FK_pilots_projects_project_id",
                        column: x => x.project_id,
                        principalSchema: "yaf",
                        principalTable: "projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "pilot_videos",
                schema: "yaf",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    pilot_id = table.Column<Guid>(type: "uuid", nullable: false),
                    video_idea_id = table.Column<Guid>(type: "uuid", nullable: false),
                    opportunity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sequence = table.Column<int>(type: "integer", nullable: false),
                    experiment_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    hypothesis = table.Column<string>(type: "text", nullable: false),
                    variable_being_tested = table.Column<string>(type: "text", nullable: false),
                    control_strategy = table.Column<string>(type: "text", nullable: false),
                    primary_metric = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    success_signal = table.Column<string>(type: "text", nullable: false),
                    rationale = table.Column<string>(type: "text", nullable: false),
                    secondary_metrics_json = table.Column<string>(type: "jsonb", nullable: false),
                    notes = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pilot_videos", x => x.id);
                    table.CheckConstraint("ck_pilot_videos_sequence", "sequence >= 1 AND sequence <= 12");
                    table.ForeignKey(
                        name: "FK_pilot_videos_opportunity_candidates_opportunity_id",
                        column: x => x.opportunity_id,
                        principalSchema: "yaf",
                        principalTable: "opportunity_candidates",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_pilot_videos_pilots_pilot_id",
                        column: x => x.pilot_id,
                        principalSchema: "yaf",
                        principalTable: "pilots",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_pilot_videos_video_ideas_video_idea_id",
                        column: x => x.video_idea_id,
                        principalSchema: "yaf",
                        principalTable: "video_ideas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ux_jobs_active_project_analysis",
                schema: "yaf",
                table: "jobs",
                columns: new[] { "project_id", "type" },
                unique: true,
                filter: "type IN ('opportunity-analysis', 'pilot-generation') AND status IN ('Queued', 'Running', 'Retrying')");

            migrationBuilder.CreateIndex(
                name: "IX_pilot_videos_opportunity_id",
                schema: "yaf",
                table: "pilot_videos",
                column: "opportunity_id");

            migrationBuilder.CreateIndex(
                name: "IX_pilot_videos_pilot_id_sequence",
                schema: "yaf",
                table: "pilot_videos",
                columns: new[] { "pilot_id", "sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_pilot_videos_pilot_id_video_idea_id",
                schema: "yaf",
                table: "pilot_videos",
                columns: new[] { "pilot_id", "video_idea_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_pilot_videos_video_idea_id",
                schema: "yaf",
                table: "pilot_videos",
                column: "video_idea_id");

            migrationBuilder.CreateIndex(
                name: "IX_pilots_project_id_created_at",
                schema: "yaf",
                table: "pilots",
                columns: new[] { "project_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_pilots_project_id_version",
                schema: "yaf",
                table: "pilots",
                columns: new[] { "project_id", "version" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "pilot_videos",
                schema: "yaf");

            migrationBuilder.DropTable(
                name: "pilots",
                schema: "yaf");

            migrationBuilder.DropIndex(
                name: "ux_jobs_active_project_analysis",
                schema: "yaf",
                table: "jobs");

            migrationBuilder.CreateIndex(
                name: "ux_jobs_active_opportunity_analysis",
                schema: "yaf",
                table: "jobs",
                column: "project_id",
                unique: true,
                filter: "type = 'opportunity-analysis' AND status IN ('Queued', 'Running', 'Retrying')");
        }
    }
}
