using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace YoutubeAiFactory.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddIdeaEngine : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "opportunity_id",
                schema: "yaf",
                table: "jobs",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "idea_generations",
                schema: "yaf",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    project_id = table.Column<Guid>(type: "uuid", nullable: false),
                    opportunity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    opportunity_report_id = table.Column<Guid>(type: "uuid", nullable: false),
                    opportunity_report_version = table.Column<int>(type: "integer", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    ai_run_id = table.Column<Guid>(type: "uuid", nullable: false),
                    prompt_key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    prompt_version = table.Column<int>(type: "integer", nullable: false),
                    provider = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    model = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    scoring_algorithm_version = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_idea_generations", x => x.id);
                    table.ForeignKey(
                        name: "FK_idea_generations_opportunity_candidates_opportunity_id",
                        column: x => x.opportunity_id,
                        principalSchema: "yaf",
                        principalTable: "opportunity_candidates",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_idea_generations_opportunity_reports_opportunity_report_id",
                        column: x => x.opportunity_report_id,
                        principalSchema: "yaf",
                        principalTable: "opportunity_reports",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_idea_generations_projects_project_id",
                        column: x => x.project_id,
                        principalSchema: "yaf",
                        principalTable: "projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "video_ideas",
                schema: "yaf",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    project_id = table.Column<Guid>(type: "uuid", nullable: false),
                    opportunity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    generation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    working_title = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Topic = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Angle = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    content_format = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    target_audience = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    viewer_intent = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    hook_concept = table.Column<string>(type: "text", nullable: false),
                    thumbnail_concept = table.Column<string>(type: "text", nullable: false),
                    viewer_promise = table.Column<string>(type: "text", nullable: false),
                    core_question = table.Column<string>(type: "text", nullable: false),
                    why_viewer_would_care = table.Column<string>(type: "text", nullable: false),
                    Hypothesis = table.Column<string>(type: "text", nullable: false),
                    opportunity_fit = table.Column<int>(type: "integer", nullable: false),
                    observed_demand_alignment = table.Column<int>(type: "integer", nullable: false),
                    novelty = table.Column<int>(type: "integer", nullable: false),
                    title_potential = table.Column<int>(type: "integer", nullable: false),
                    thumbnail_potential = table.Column<int>(type: "integer", nullable: false),
                    story_potential = table.Column<int>(type: "integer", nullable: false),
                    audience_fit = table.Column<int>(type: "integer", nullable: false),
                    evidence_strength = table.Column<int>(type: "integer", nullable: false),
                    production_ease = table.Column<int>(type: "integer", nullable: false),
                    competition_risk = table.Column<int>(type: "integer", nullable: false),
                    research_risk = table.Column<int>(type: "integer", nullable: false),
                    confidence = table.Column<int>(type: "integer", nullable: false),
                    overall_score = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    duplication_penalty = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    scoring_algorithm_version = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    risks_json = table.Column<string>(type: "jsonb", nullable: false),
                    decision_status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_video_ideas", x => x.id);
                    table.ForeignKey(
                        name: "FK_video_ideas_idea_generations_generation_id",
                        column: x => x.generation_id,
                        principalSchema: "yaf",
                        principalTable: "idea_generations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_video_ideas_opportunity_candidates_opportunity_id",
                        column: x => x.opportunity_id,
                        principalSchema: "yaf",
                        principalTable: "opportunity_candidates",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_video_ideas_projects_project_id",
                        column: x => x.project_id,
                        principalSchema: "yaf",
                        principalTable: "projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "idea_evidence",
                schema: "yaf",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    idea_id = table.Column<Guid>(type: "uuid", nullable: false),
                    opportunity_evidence_id = table.Column<Guid>(type: "uuid", nullable: false),
                    summary = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_idea_evidence", x => x.id);
                    table.ForeignKey(
                        name: "FK_idea_evidence_opportunity_evidence_opportunity_evidence_id",
                        column: x => x.opportunity_evidence_id,
                        principalSchema: "yaf",
                        principalTable: "opportunity_evidence",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_idea_evidence_video_ideas_idea_id",
                        column: x => x.idea_id,
                        principalSchema: "yaf",
                        principalTable: "video_ideas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ux_jobs_active_idea_generation",
                schema: "yaf",
                table: "jobs",
                column: "opportunity_id",
                unique: true,
                filter: "type = 'idea-generation' AND status IN ('Queued', 'Running', 'Retrying')");

            migrationBuilder.CreateIndex(
                name: "IX_idea_evidence_idea_id_opportunity_evidence_id",
                schema: "yaf",
                table: "idea_evidence",
                columns: new[] { "idea_id", "opportunity_evidence_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_idea_evidence_opportunity_evidence_id",
                schema: "yaf",
                table: "idea_evidence",
                column: "opportunity_evidence_id");

            migrationBuilder.CreateIndex(
                name: "IX_idea_generations_opportunity_id_version",
                schema: "yaf",
                table: "idea_generations",
                columns: new[] { "opportunity_id", "version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_idea_generations_opportunity_report_id",
                schema: "yaf",
                table: "idea_generations",
                column: "opportunity_report_id");

            migrationBuilder.CreateIndex(
                name: "IX_idea_generations_project_id_created_at",
                schema: "yaf",
                table: "idea_generations",
                columns: new[] { "project_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_video_ideas_generation_id",
                schema: "yaf",
                table: "video_ideas",
                column: "generation_id");

            migrationBuilder.CreateIndex(
                name: "IX_video_ideas_opportunity_id_overall_score",
                schema: "yaf",
                table: "video_ideas",
                columns: new[] { "opportunity_id", "overall_score" });

            migrationBuilder.CreateIndex(
                name: "IX_video_ideas_project_id_decision_status",
                schema: "yaf",
                table: "video_ideas",
                columns: new[] { "project_id", "decision_status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "idea_evidence",
                schema: "yaf");

            migrationBuilder.DropTable(
                name: "video_ideas",
                schema: "yaf");

            migrationBuilder.DropTable(
                name: "idea_generations",
                schema: "yaf");

            migrationBuilder.DropIndex(
                name: "ux_jobs_active_idea_generation",
                schema: "yaf",
                table: "jobs");

            migrationBuilder.DropColumn(
                name: "opportunity_id",
                schema: "yaf",
                table: "jobs");
        }
    }
}
