using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace YoutubeAiFactory.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEvidenceGroundedScripts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_jobs_active_video_project_workflow",
                schema: "yaf",
                table: "jobs");

            migrationBuilder.CreateTable(
                name: "video_scripts",
                schema: "yaf",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    project_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    video_project_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    video_outline_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    video_outline_version = table.Column<int>(type: "int", nullable: false),
                    research_report_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    research_report_version = table.Column<int>(type: "int", nullable: false),
                    version = table.Column<int>(type: "int", nullable: false),
                    status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    grounding_status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    generation_ai_run_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    grounding_ai_run_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    script_engine_version = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    prompt_key = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    prompt_version = table.Column<int>(type: "int", nullable: false),
                    input_fingerprint = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    provider = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    model = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    content_language = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    total_word_count = table.Column<int>(type: "int", nullable: false),
                    estimated_duration_seconds = table.Column<int>(type: "int", nullable: false),
                    warnings_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    grounding_issues_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    approved_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_video_scripts", x => x.id);
                    table.CheckConstraint("ck_video_scripts_duration", "[estimated_duration_seconds] > 0");
                    table.CheckConstraint("ck_video_scripts_grounding_issues_json", "ISJSON([grounding_issues_json]) = 1");
                    table.CheckConstraint("ck_video_scripts_warnings_json", "ISJSON([warnings_json]) = 1");
                    table.CheckConstraint("ck_video_scripts_word_count", "[total_word_count] > 0");
                    table.ForeignKey(
                        name: "FK_video_scripts_ai_runs_generation_ai_run_id",
                        column: x => x.generation_ai_run_id,
                        principalSchema: "yaf",
                        principalTable: "ai_runs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_video_scripts_ai_runs_grounding_ai_run_id",
                        column: x => x.grounding_ai_run_id,
                        principalSchema: "yaf",
                        principalTable: "ai_runs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_video_scripts_projects_project_id",
                        column: x => x.project_id,
                        principalSchema: "yaf",
                        principalTable: "projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_video_scripts_research_reports_research_report_id",
                        column: x => x.research_report_id,
                        principalSchema: "yaf",
                        principalTable: "research_reports",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_video_scripts_video_outlines_video_outline_id",
                        column: x => x.video_outline_id,
                        principalSchema: "yaf",
                        principalTable: "video_outlines",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_video_scripts_video_projects_video_project_id",
                        column: x => x.video_project_id,
                        principalSchema: "yaf",
                        principalTable: "video_projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "video_script_sections",
                schema: "yaf",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    video_script_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    video_outline_section_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    sequence = table.Column<int>(type: "int", nullable: false),
                    heading = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    word_count = table.Column<int>(type: "int", nullable: false),
                    estimated_duration_seconds = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_video_script_sections", x => x.id);
                    table.CheckConstraint("ck_video_script_sections_duration", "[estimated_duration_seconds] > 0");
                    table.CheckConstraint("ck_video_script_sections_word_count", "[word_count] > 0");
                    table.ForeignKey(
                        name: "FK_video_script_sections_video_outline_sections_video_outline_section_id",
                        column: x => x.video_outline_section_id,
                        principalSchema: "yaf",
                        principalTable: "video_outline_sections",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_video_script_sections_video_scripts_video_script_id",
                        column: x => x.video_script_id,
                        principalSchema: "yaf",
                        principalTable: "video_scripts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "video_script_blocks",
                schema: "yaf",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    video_script_section_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    sequence = table.Column<int>(type: "int", nullable: false),
                    block_type = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    narration_text = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    word_count = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_video_script_blocks", x => x.id);
                    table.CheckConstraint("ck_video_script_blocks_word_count", "[word_count] > 0");
                    table.ForeignKey(
                        name: "FK_video_script_blocks_video_script_sections_video_script_section_id",
                        column: x => x.video_script_section_id,
                        principalSchema: "yaf",
                        principalTable: "video_script_sections",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "video_script_block_claims",
                schema: "yaf",
                columns: table => new
                {
                    script_block_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    research_claim_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_video_script_block_claims", x => new { x.script_block_id, x.research_claim_id });
                    table.ForeignKey(
                        name: "FK_video_script_block_claims_research_claims_research_claim_id",
                        column: x => x.research_claim_id,
                        principalSchema: "yaf",
                        principalTable: "research_claims",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_video_script_block_claims_video_script_blocks_script_block_id",
                        column: x => x.script_block_id,
                        principalSchema: "yaf",
                        principalTable: "video_script_blocks",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "video_script_block_conflicts",
                schema: "yaf",
                columns: table => new
                {
                    script_block_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    research_conflict_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_video_script_block_conflicts", x => new { x.script_block_id, x.research_conflict_id });
                    table.ForeignKey(
                        name: "FK_video_script_block_conflicts_research_conflicts_research_conflict_id",
                        column: x => x.research_conflict_id,
                        principalSchema: "yaf",
                        principalTable: "research_conflicts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_video_script_block_conflicts_video_script_blocks_script_block_id",
                        column: x => x.script_block_id,
                        principalSchema: "yaf",
                        principalTable: "video_script_blocks",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ux_jobs_active_video_project_workflow",
                schema: "yaf",
                table: "jobs",
                columns: new[] { "video_project_id", "type" },
                unique: true,
                filter: "type IN ('video-research', 'outline-generation', 'script-workflow') AND status IN ('Queued', 'Running', 'Retrying')");

            migrationBuilder.CreateIndex(
                name: "IX_video_script_block_claims_research_claim_id",
                schema: "yaf",
                table: "video_script_block_claims",
                column: "research_claim_id");

            migrationBuilder.CreateIndex(
                name: "IX_video_script_block_conflicts_research_conflict_id",
                schema: "yaf",
                table: "video_script_block_conflicts",
                column: "research_conflict_id");

            migrationBuilder.CreateIndex(
                name: "IX_video_script_blocks_video_script_section_id_sequence",
                schema: "yaf",
                table: "video_script_blocks",
                columns: new[] { "video_script_section_id", "sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_video_script_sections_video_outline_section_id",
                schema: "yaf",
                table: "video_script_sections",
                column: "video_outline_section_id");

            migrationBuilder.CreateIndex(
                name: "IX_video_script_sections_video_script_id_sequence",
                schema: "yaf",
                table: "video_script_sections",
                columns: new[] { "video_script_id", "sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_video_script_sections_video_script_id_video_outline_section_id",
                schema: "yaf",
                table: "video_script_sections",
                columns: new[] { "video_script_id", "video_outline_section_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_video_scripts_generation_ai_run_id",
                schema: "yaf",
                table: "video_scripts",
                column: "generation_ai_run_id");

            migrationBuilder.CreateIndex(
                name: "IX_video_scripts_grounding_ai_run_id",
                schema: "yaf",
                table: "video_scripts",
                column: "grounding_ai_run_id");

            migrationBuilder.CreateIndex(
                name: "IX_video_scripts_project_id_video_project_id_created_at",
                schema: "yaf",
                table: "video_scripts",
                columns: new[] { "project_id", "video_project_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_video_scripts_research_report_id",
                schema: "yaf",
                table: "video_scripts",
                column: "research_report_id");

            migrationBuilder.CreateIndex(
                name: "IX_video_scripts_video_outline_id",
                schema: "yaf",
                table: "video_scripts",
                column: "video_outline_id");

            migrationBuilder.CreateIndex(
                name: "IX_video_scripts_video_project_id_version",
                schema: "yaf",
                table: "video_scripts",
                columns: new[] { "video_project_id", "version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_video_scripts_approved",
                schema: "yaf",
                table: "video_scripts",
                column: "video_project_id",
                unique: true,
                filter: "status = 'Approved'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "video_script_block_claims",
                schema: "yaf");

            migrationBuilder.DropTable(
                name: "video_script_block_conflicts",
                schema: "yaf");

            migrationBuilder.DropTable(
                name: "video_script_blocks",
                schema: "yaf");

            migrationBuilder.DropTable(
                name: "video_script_sections",
                schema: "yaf");

            migrationBuilder.DropTable(
                name: "video_scripts",
                schema: "yaf");

            migrationBuilder.DropIndex(
                name: "ux_jobs_active_video_project_workflow",
                schema: "yaf",
                table: "jobs");

            migrationBuilder.CreateIndex(
                name: "ux_jobs_active_video_project_workflow",
                schema: "yaf",
                table: "jobs",
                columns: new[] { "video_project_id", "type" },
                unique: true,
                filter: "type IN ('video-research', 'outline-generation') AND status IN ('Queued', 'Running', 'Retrying')");
        }
    }
}
