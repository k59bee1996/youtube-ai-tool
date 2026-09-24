using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace YoutubeAiFactory.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProductionPackages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_jobs_active_video_project_workflow",
                schema: "yaf",
                table: "jobs");

            migrationBuilder.CreateTable(
                name: "production_packages",
                schema: "yaf",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    project_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    video_project_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    video_script_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    video_script_version = table.Column<int>(type: "int", nullable: false),
                    video_outline_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    video_outline_version = table.Column<int>(type: "int", nullable: false),
                    research_report_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    research_report_version = table.Column<int>(type: "int", nullable: false),
                    version = table.Column<int>(type: "int", nullable: false),
                    status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    grounding_status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    generation_ai_run_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    grounding_ai_run_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    engine_version = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    prompt_key = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    prompt_version = table.Column<int>(type: "int", nullable: false),
                    input_fingerprint = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    provider = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    model = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    content_language = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    estimated_duration_seconds = table.Column<int>(type: "int", nullable: false),
                    visual_direction = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    pacing_direction = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    color_direction = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    typography_direction = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    audio_direction = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    experiment_production_notes = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    warnings_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    grounding_issues_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    approved_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_production_packages", x => x.id);
                    table.CheckConstraint("ck_production_packages_duration", "[estimated_duration_seconds] > 0");
                    table.CheckConstraint("ck_production_packages_issues_json", "ISJSON([grounding_issues_json]) = 1");
                    table.CheckConstraint("ck_production_packages_warnings_json", "ISJSON([warnings_json]) = 1");
                    table.ForeignKey(
                        name: "FK_production_packages_ai_runs_generation_ai_run_id",
                        column: x => x.generation_ai_run_id,
                        principalSchema: "yaf",
                        principalTable: "ai_runs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_production_packages_ai_runs_grounding_ai_run_id",
                        column: x => x.grounding_ai_run_id,
                        principalSchema: "yaf",
                        principalTable: "ai_runs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_production_packages_projects_project_id",
                        column: x => x.project_id,
                        principalSchema: "yaf",
                        principalTable: "projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_production_packages_research_reports_research_report_id",
                        column: x => x.research_report_id,
                        principalSchema: "yaf",
                        principalTable: "research_reports",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_production_packages_video_projects_video_project_id",
                        column: x => x.video_project_id,
                        principalSchema: "yaf",
                        principalTable: "video_projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_production_packages_video_scripts_video_script_id",
                        column: x => x.video_script_id,
                        principalSchema: "yaf",
                        principalTable: "video_scripts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "production_asset_requirements",
                schema: "yaf",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    production_package_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    asset_key = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    asset_type = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    acquisition_mode = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    creative_brief = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    generation_prompt = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    source_search_brief = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    rights_verification_required = table.Column<bool>(type: "bit", nullable: false),
                    factuality_mode = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    reuse_key = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    complexity = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_production_asset_requirements", x => x.id);
                    table.ForeignKey(
                        name: "FK_production_asset_requirements_production_packages_production_package_id",
                        column: x => x.production_package_id,
                        principalSchema: "yaf",
                        principalTable: "production_packages",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "production_scenes",
                schema: "yaf",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    production_package_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    sequence = table.Column<int>(type: "int", nullable: false),
                    purpose = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    narration_summary = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    visual_strategy = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    estimated_duration_seconds = table.Column<int>(type: "int", nullable: false),
                    complexity = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    transition_intent = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    music_brief = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    sound_effect_cue = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    voice_direction = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_production_scenes", x => x.id);
                    table.CheckConstraint("ck_production_scenes_duration", "[estimated_duration_seconds] > 0");
                    table.ForeignKey(
                        name: "FK_production_scenes_production_packages_production_package_id",
                        column: x => x.production_package_id,
                        principalSchema: "yaf",
                        principalTable: "production_packages",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "production_asset_claims",
                schema: "yaf",
                columns: table => new
                {
                    production_asset_requirement_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    research_claim_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_production_asset_claims", x => new { x.production_asset_requirement_id, x.research_claim_id });
                    table.ForeignKey(
                        name: "FK_production_asset_claims_production_asset_requirements_production_asset_requirement_id",
                        column: x => x.production_asset_requirement_id,
                        principalSchema: "yaf",
                        principalTable: "production_asset_requirements",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_production_asset_claims_research_claims_research_claim_id",
                        column: x => x.research_claim_id,
                        principalSchema: "yaf",
                        principalTable: "research_claims",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "production_on_screen_text",
                schema: "yaf",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    production_scene_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    sequence = table.Column<int>(type: "int", nullable: false),
                    text = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    text_type = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    timing_intent = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_production_on_screen_text", x => x.id);
                    table.ForeignKey(
                        name: "FK_production_on_screen_text_production_scenes_production_scene_id",
                        column: x => x.production_scene_id,
                        principalSchema: "yaf",
                        principalTable: "production_scenes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "production_scene_script_blocks",
                schema: "yaf",
                columns: table => new
                {
                    production_scene_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    script_block_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    production_package_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    sequence = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_production_scene_script_blocks", x => new { x.production_scene_id, x.script_block_id });
                    table.ForeignKey(
                        name: "FK_production_scene_script_blocks_production_packages_production_package_id",
                        column: x => x.production_package_id,
                        principalSchema: "yaf",
                        principalTable: "production_packages",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_production_scene_script_blocks_production_scenes_production_scene_id",
                        column: x => x.production_scene_id,
                        principalSchema: "yaf",
                        principalTable: "production_scenes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_production_scene_script_blocks_video_script_blocks_script_block_id",
                        column: x => x.script_block_id,
                        principalSchema: "yaf",
                        principalTable: "video_script_blocks",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "production_shots",
                schema: "yaf",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    production_scene_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    sequence = table.Column<int>(type: "int", nullable: false),
                    shot_type = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    visual_description = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    composition = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    motion_suggestion = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    estimated_duration_seconds = table.Column<int>(type: "int", nullable: false),
                    factuality_mode = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    asset_requirement_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    notes = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_production_shots", x => x.id);
                    table.CheckConstraint("ck_production_shots_duration", "[estimated_duration_seconds] > 0");
                    table.ForeignKey(
                        name: "FK_production_shots_production_asset_requirements_asset_requirement_id",
                        column: x => x.asset_requirement_id,
                        principalSchema: "yaf",
                        principalTable: "production_asset_requirements",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_production_shots_production_scenes_production_scene_id",
                        column: x => x.production_scene_id,
                        principalSchema: "yaf",
                        principalTable: "production_scenes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "production_on_screen_text_claims",
                schema: "yaf",
                columns: table => new
                {
                    production_on_screen_text_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    research_claim_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_production_on_screen_text_claims", x => new { x.production_on_screen_text_id, x.research_claim_id });
                    table.ForeignKey(
                        name: "FK_production_on_screen_text_claims_production_on_screen_text_production_on_screen_text_id",
                        column: x => x.production_on_screen_text_id,
                        principalSchema: "yaf",
                        principalTable: "production_on_screen_text",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_production_on_screen_text_claims_research_claims_research_claim_id",
                        column: x => x.research_claim_id,
                        principalSchema: "yaf",
                        principalTable: "research_claims",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "production_shot_claims",
                schema: "yaf",
                columns: table => new
                {
                    production_shot_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    research_claim_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_production_shot_claims", x => new { x.production_shot_id, x.research_claim_id });
                    table.ForeignKey(
                        name: "FK_production_shot_claims_production_shots_production_shot_id",
                        column: x => x.production_shot_id,
                        principalSchema: "yaf",
                        principalTable: "production_shots",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_production_shot_claims_research_claims_research_claim_id",
                        column: x => x.research_claim_id,
                        principalSchema: "yaf",
                        principalTable: "research_claims",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ux_jobs_active_video_project_workflow",
                schema: "yaf",
                table: "jobs",
                columns: new[] { "video_project_id", "type" },
                unique: true,
                filter: "type IN ('video-research', 'outline-generation', 'script-workflow', 'production-package') AND status IN ('Queued', 'Running', 'Retrying')");

            migrationBuilder.CreateIndex(
                name: "IX_production_asset_claims_research_claim_id",
                schema: "yaf",
                table: "production_asset_claims",
                column: "research_claim_id");

            migrationBuilder.CreateIndex(
                name: "IX_production_asset_requirements_production_package_id_asset_key",
                schema: "yaf",
                table: "production_asset_requirements",
                columns: new[] { "production_package_id", "asset_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_production_asset_requirements_production_package_id_reuse_key",
                schema: "yaf",
                table: "production_asset_requirements",
                columns: new[] { "production_package_id", "reuse_key" });

            migrationBuilder.CreateIndex(
                name: "IX_production_on_screen_text_production_scene_id_sequence",
                schema: "yaf",
                table: "production_on_screen_text",
                columns: new[] { "production_scene_id", "sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_production_on_screen_text_claims_research_claim_id",
                schema: "yaf",
                table: "production_on_screen_text_claims",
                column: "research_claim_id");

            migrationBuilder.CreateIndex(
                name: "IX_production_packages_generation_ai_run_id",
                schema: "yaf",
                table: "production_packages",
                column: "generation_ai_run_id");

            migrationBuilder.CreateIndex(
                name: "IX_production_packages_grounding_ai_run_id",
                schema: "yaf",
                table: "production_packages",
                column: "grounding_ai_run_id");

            migrationBuilder.CreateIndex(
                name: "IX_production_packages_project_id",
                schema: "yaf",
                table: "production_packages",
                column: "project_id");

            migrationBuilder.CreateIndex(
                name: "IX_production_packages_research_report_id",
                schema: "yaf",
                table: "production_packages",
                column: "research_report_id");

            migrationBuilder.CreateIndex(
                name: "IX_production_packages_video_project_id_version",
                schema: "yaf",
                table: "production_packages",
                columns: new[] { "video_project_id", "version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_production_packages_video_script_id",
                schema: "yaf",
                table: "production_packages",
                column: "video_script_id");

            migrationBuilder.CreateIndex(
                name: "ux_production_packages_approved",
                schema: "yaf",
                table: "production_packages",
                column: "video_project_id",
                unique: true,
                filter: "status = 'Approved'");

            migrationBuilder.CreateIndex(
                name: "IX_production_scene_script_blocks_production_package_id_script_block_id",
                schema: "yaf",
                table: "production_scene_script_blocks",
                columns: new[] { "production_package_id", "script_block_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_production_scene_script_blocks_production_scene_id_sequence",
                schema: "yaf",
                table: "production_scene_script_blocks",
                columns: new[] { "production_scene_id", "sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_production_scene_script_blocks_script_block_id",
                schema: "yaf",
                table: "production_scene_script_blocks",
                column: "script_block_id");

            migrationBuilder.CreateIndex(
                name: "IX_production_scenes_production_package_id_sequence",
                schema: "yaf",
                table: "production_scenes",
                columns: new[] { "production_package_id", "sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_production_shot_claims_research_claim_id",
                schema: "yaf",
                table: "production_shot_claims",
                column: "research_claim_id");

            migrationBuilder.CreateIndex(
                name: "IX_production_shots_asset_requirement_id",
                schema: "yaf",
                table: "production_shots",
                column: "asset_requirement_id");

            migrationBuilder.CreateIndex(
                name: "IX_production_shots_production_scene_id_sequence",
                schema: "yaf",
                table: "production_shots",
                columns: new[] { "production_scene_id", "sequence" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "production_asset_claims",
                schema: "yaf");

            migrationBuilder.DropTable(
                name: "production_on_screen_text_claims",
                schema: "yaf");

            migrationBuilder.DropTable(
                name: "production_scene_script_blocks",
                schema: "yaf");

            migrationBuilder.DropTable(
                name: "production_shot_claims",
                schema: "yaf");

            migrationBuilder.DropTable(
                name: "production_on_screen_text",
                schema: "yaf");

            migrationBuilder.DropTable(
                name: "production_shots",
                schema: "yaf");

            migrationBuilder.DropTable(
                name: "production_asset_requirements",
                schema: "yaf");

            migrationBuilder.DropTable(
                name: "production_scenes",
                schema: "yaf");

            migrationBuilder.DropTable(
                name: "production_packages",
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
                filter: "type IN ('video-research', 'outline-generation', 'script-workflow') AND status IN ('Queued', 'Running', 'Retrying')");
        }
    }
}
