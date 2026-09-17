using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace YoutubeAiFactory.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEvidenceGroundedOutlines : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_jobs_active_video_project_research",
                schema: "yaf",
                table: "jobs");

            migrationBuilder.AddColumn<Guid>(
                name: "research_report_id",
                schema: "yaf",
                table: "ai_runs",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "video_outlines",
                schema: "yaf",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    project_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    video_project_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    research_report_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    research_report_version = table.Column<int>(type: "int", nullable: false),
                    version = table.Column<int>(type: "int", nullable: false),
                    status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    generation_ai_run_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    outline_algorithm_version = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    prompt_key = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    prompt_version = table.Column<int>(type: "int", nullable: false),
                    input_fingerprint = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    provider = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    model = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    structure_type = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    core_question = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    core_tension = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    opening_hook_concept = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    viewer_promise = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    narrative_progression = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    payoff = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    pacing_strategy = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    experiment_type = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    variable_being_tested = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    control_strategy = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    experiment_alignment = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    experiment_risks_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    warnings_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    total_estimated_seconds = table.Column<int>(type: "int", nullable: true),
                    transitions_require_review = table.Column<bool>(type: "bit", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    approved_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_video_outlines", x => x.id);
                    table.CheckConstraint("ck_video_outlines_experiment_risks_json", "ISJSON([experiment_risks_json]) = 1");
                    table.CheckConstraint("ck_video_outlines_warnings_json", "ISJSON([warnings_json]) = 1");
                    table.ForeignKey(
                        name: "FK_video_outlines_ai_runs_generation_ai_run_id",
                        column: x => x.generation_ai_run_id,
                        principalSchema: "yaf",
                        principalTable: "ai_runs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_video_outlines_projects_project_id",
                        column: x => x.project_id,
                        principalSchema: "yaf",
                        principalTable: "projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_video_outlines_research_reports_research_report_id",
                        column: x => x.research_report_id,
                        principalSchema: "yaf",
                        principalTable: "research_reports",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_video_outlines_video_projects_video_project_id",
                        column: x => x.video_project_id,
                        principalSchema: "yaf",
                        principalTable: "video_projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "video_outline_sections",
                schema: "yaf",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    video_outline_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    sequence = table.Column<int>(type: "int", nullable: false),
                    heading = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    purpose = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    objective = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    summary = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    viewer_question = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    transition_intent = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    estimated_seconds = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_video_outline_sections", x => x.id);
                    table.ForeignKey(
                        name: "FK_video_outline_sections_video_outlines_video_outline_id",
                        column: x => x.video_outline_id,
                        principalSchema: "yaf",
                        principalTable: "video_outlines",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "video_outline_section_claims",
                schema: "yaf",
                columns: table => new
                {
                    outline_section_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    research_claim_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    usage_role = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_video_outline_section_claims", x => new { x.outline_section_id, x.research_claim_id, x.usage_role });
                    table.ForeignKey(
                        name: "FK_video_outline_section_claims_research_claims_research_claim_id",
                        column: x => x.research_claim_id,
                        principalSchema: "yaf",
                        principalTable: "research_claims",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_video_outline_section_claims_video_outline_sections_outline_section_id",
                        column: x => x.outline_section_id,
                        principalSchema: "yaf",
                        principalTable: "video_outline_sections",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "video_outline_section_conflicts",
                schema: "yaf",
                columns: table => new
                {
                    outline_section_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    research_conflict_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_video_outline_section_conflicts", x => new { x.outline_section_id, x.research_conflict_id });
                    table.ForeignKey(
                        name: "FK_video_outline_section_conflicts_research_conflicts_research_conflict_id",
                        column: x => x.research_conflict_id,
                        principalSchema: "yaf",
                        principalTable: "research_conflicts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_video_outline_section_conflicts_video_outline_sections_outline_section_id",
                        column: x => x.outline_section_id,
                        principalSchema: "yaf",
                        principalTable: "video_outline_sections",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "video_outline_section_gaps",
                schema: "yaf",
                columns: table => new
                {
                    outline_section_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    research_gap_index = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_video_outline_section_gaps", x => new { x.outline_section_id, x.research_gap_index });
                    table.ForeignKey(
                        name: "FK_video_outline_section_gaps_video_outline_sections_outline_section_id",
                        column: x => x.outline_section_id,
                        principalSchema: "yaf",
                        principalTable: "video_outline_sections",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ux_jobs_active_video_project_workflow",
                schema: "yaf",
                table: "jobs",
                columns: new[] { "video_project_id", "type" },
                unique: true,
                filter: "type IN ('video-research', 'outline-generation') AND status IN ('Queued', 'Running', 'Retrying')");

            migrationBuilder.CreateIndex(
                name: "IX_ai_runs_research_report_id_started_at",
                schema: "yaf",
                table: "ai_runs",
                columns: new[] { "research_report_id", "started_at" });

            migrationBuilder.CreateIndex(
                name: "IX_video_outline_section_claims_research_claim_id",
                schema: "yaf",
                table: "video_outline_section_claims",
                column: "research_claim_id");

            migrationBuilder.CreateIndex(
                name: "IX_video_outline_section_conflicts_research_conflict_id",
                schema: "yaf",
                table: "video_outline_section_conflicts",
                column: "research_conflict_id");

            migrationBuilder.CreateIndex(
                name: "IX_video_outline_sections_video_outline_id_sequence",
                schema: "yaf",
                table: "video_outline_sections",
                columns: new[] { "video_outline_id", "sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_video_outlines_generation_ai_run_id",
                schema: "yaf",
                table: "video_outlines",
                column: "generation_ai_run_id");

            migrationBuilder.CreateIndex(
                name: "IX_video_outlines_project_id_video_project_id_created_at",
                schema: "yaf",
                table: "video_outlines",
                columns: new[] { "project_id", "video_project_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_video_outlines_research_report_id",
                schema: "yaf",
                table: "video_outlines",
                column: "research_report_id");

            migrationBuilder.CreateIndex(
                name: "IX_video_outlines_video_project_id_version",
                schema: "yaf",
                table: "video_outlines",
                columns: new[] { "video_project_id", "version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_video_outlines_approved",
                schema: "yaf",
                table: "video_outlines",
                column: "video_project_id",
                unique: true,
                filter: "status = 'Approved'");

            migrationBuilder.AddForeignKey(
                name: "FK_ai_runs_research_reports_research_report_id",
                schema: "yaf",
                table: "ai_runs",
                column: "research_report_id",
                principalSchema: "yaf",
                principalTable: "research_reports",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ai_runs_research_reports_research_report_id",
                schema: "yaf",
                table: "ai_runs");

            migrationBuilder.DropTable(
                name: "video_outline_section_claims",
                schema: "yaf");

            migrationBuilder.DropTable(
                name: "video_outline_section_conflicts",
                schema: "yaf");

            migrationBuilder.DropTable(
                name: "video_outline_section_gaps",
                schema: "yaf");

            migrationBuilder.DropTable(
                name: "video_outline_sections",
                schema: "yaf");

            migrationBuilder.DropTable(
                name: "video_outlines",
                schema: "yaf");

            migrationBuilder.DropIndex(
                name: "ux_jobs_active_video_project_workflow",
                schema: "yaf",
                table: "jobs");

            migrationBuilder.DropIndex(
                name: "IX_ai_runs_research_report_id_started_at",
                schema: "yaf",
                table: "ai_runs");

            migrationBuilder.DropColumn(
                name: "research_report_id",
                schema: "yaf",
                table: "ai_runs");

            migrationBuilder.CreateIndex(
                name: "ux_jobs_active_video_project_research",
                schema: "yaf",
                table: "jobs",
                column: "video_project_id",
                unique: true,
                filter: "type = 'video-research' AND status IN ('Queued', 'Running', 'Retrying')");
        }
    }
}
