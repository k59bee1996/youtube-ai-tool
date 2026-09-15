using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace YoutubeAiFactory.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEvidenceBackedResearch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "video_project_id",
                schema: "yaf",
                table: "jobs",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "research_run_id",
                schema: "yaf",
                table: "ai_runs",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "video_project_id",
                schema: "yaf",
                table: "ai_runs",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "research_runs",
                schema: "yaf",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    project_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    video_project_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    research_algorithm_version = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    input_fingerprint = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    queued_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    started_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    completed_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    search_query_count = table.Column<int>(type: "int", nullable: false),
                    search_result_count = table.Column<int>(type: "int", nullable: false),
                    fetched_source_count = table.Column<int>(type: "int", nullable: false),
                    relevant_source_count = table.Column<int>(type: "int", nullable: false),
                    evidence_count = table.Column<int>(type: "int", nullable: false),
                    claim_count = table.Column<int>(type: "int", nullable: false),
                    conflict_count = table.Column<int>(type: "int", nullable: false),
                    search_failure_count = table.Column<int>(type: "int", nullable: false),
                    fetch_failure_count = table.Column<int>(type: "int", nullable: false),
                    research_report_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    failure_reason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_research_runs", x => x.id);
                    table.ForeignKey(
                        name: "FK_research_runs_projects_project_id",
                        column: x => x.project_id,
                        principalSchema: "yaf",
                        principalTable: "projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_research_runs_video_projects_video_project_id",
                        column: x => x.video_project_id,
                        principalSchema: "yaf",
                        principalTable: "video_projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "research_reports",
                schema: "yaf",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    project_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    video_project_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    research_run_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    version = table.Column<int>(type: "int", nullable: false),
                    research_algorithm_version = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    input_fingerprint = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    result_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    synthesis_ai_run_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_research_reports", x => x.id);
                    table.CheckConstraint("ck_research_reports_result_json", "ISJSON([result_json]) = 1");
                    table.ForeignKey(
                        name: "FK_research_reports_projects_project_id",
                        column: x => x.project_id,
                        principalSchema: "yaf",
                        principalTable: "projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_research_reports_research_runs_research_run_id",
                        column: x => x.research_run_id,
                        principalSchema: "yaf",
                        principalTable: "research_runs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_research_reports_video_projects_video_project_id",
                        column: x => x.video_project_id,
                        principalSchema: "yaf",
                        principalTable: "video_projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "research_sources",
                schema: "yaf",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    research_run_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    url = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false),
                    canonical_url = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false),
                    domain = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    title = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    publisher = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    published_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    retrieved_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    source_category = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    fetch_status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    content_hash = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    quality_notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    failure_reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_research_sources", x => x.id);
                    table.ForeignKey(
                        name: "FK_research_sources_research_runs_research_run_id",
                        column: x => x.research_run_id,
                        principalSchema: "yaf",
                        principalTable: "research_runs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "research_claims",
                schema: "yaf",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    research_report_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    statement = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    claim_type = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    support_status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    confidence = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    is_critical = table.Column<bool>(type: "bit", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_research_claims", x => x.id);
                    table.ForeignKey(
                        name: "FK_research_claims_research_reports_research_report_id",
                        column: x => x.research_report_id,
                        principalSchema: "yaf",
                        principalTable: "research_reports",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "research_evidence",
                schema: "yaf",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    research_run_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    research_source_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    evidence_type = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    fact = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    supporting_excerpt = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    source_locator = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    confidence = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_research_evidence", x => x.id);
                    table.ForeignKey(
                        name: "FK_research_evidence_research_runs_research_run_id",
                        column: x => x.research_run_id,
                        principalSchema: "yaf",
                        principalTable: "research_runs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_research_evidence_research_sources_research_source_id",
                        column: x => x.research_source_id,
                        principalSchema: "yaf",
                        principalTable: "research_sources",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "research_claim_evidence",
                schema: "yaf",
                columns: table => new
                {
                    research_claim_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    research_evidence_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    stance = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_research_claim_evidence", x => new { x.research_claim_id, x.research_evidence_id, x.stance });
                    table.ForeignKey(
                        name: "FK_research_claim_evidence_research_claims_research_claim_id",
                        column: x => x.research_claim_id,
                        principalSchema: "yaf",
                        principalTable: "research_claims",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_research_claim_evidence_research_evidence_research_evidence_id",
                        column: x => x.research_evidence_id,
                        principalSchema: "yaf",
                        principalTable: "research_evidence",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "research_conflicts",
                schema: "yaf",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    research_report_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    research_claim_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    supporting_evidence_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    contradicting_evidence_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    explanation = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    is_resolved = table.Column<bool>(type: "bit", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_research_conflicts", x => x.id);
                    table.ForeignKey(
                        name: "FK_research_conflicts_research_claims_research_claim_id",
                        column: x => x.research_claim_id,
                        principalSchema: "yaf",
                        principalTable: "research_claims",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_research_conflicts_research_evidence_contradicting_evidence_id",
                        column: x => x.contradicting_evidence_id,
                        principalSchema: "yaf",
                        principalTable: "research_evidence",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_research_conflicts_research_evidence_supporting_evidence_id",
                        column: x => x.supporting_evidence_id,
                        principalSchema: "yaf",
                        principalTable: "research_evidence",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_research_conflicts_research_reports_research_report_id",
                        column: x => x.research_report_id,
                        principalSchema: "yaf",
                        principalTable: "research_reports",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ux_jobs_active_video_project_research",
                schema: "yaf",
                table: "jobs",
                column: "video_project_id",
                unique: true,
                filter: "type = 'video-research' AND status IN ('Queued', 'Running', 'Retrying')");

            migrationBuilder.CreateIndex(
                name: "IX_ai_runs_research_run_id_started_at",
                schema: "yaf",
                table: "ai_runs",
                columns: new[] { "research_run_id", "started_at" });

            migrationBuilder.CreateIndex(
                name: "IX_ai_runs_video_project_id_started_at",
                schema: "yaf",
                table: "ai_runs",
                columns: new[] { "video_project_id", "started_at" });

            migrationBuilder.CreateIndex(
                name: "IX_research_claim_evidence_research_evidence_id",
                schema: "yaf",
                table: "research_claim_evidence",
                column: "research_evidence_id");

            migrationBuilder.CreateIndex(
                name: "IX_research_claims_research_report_id_support_status",
                schema: "yaf",
                table: "research_claims",
                columns: new[] { "research_report_id", "support_status" });

            migrationBuilder.CreateIndex(
                name: "IX_research_conflicts_contradicting_evidence_id",
                schema: "yaf",
                table: "research_conflicts",
                column: "contradicting_evidence_id");

            migrationBuilder.CreateIndex(
                name: "IX_research_conflicts_research_claim_id_supporting_evidence_id_contradicting_evidence_id",
                schema: "yaf",
                table: "research_conflicts",
                columns: new[] { "research_claim_id", "supporting_evidence_id", "contradicting_evidence_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_research_conflicts_research_report_id_research_claim_id",
                schema: "yaf",
                table: "research_conflicts",
                columns: new[] { "research_report_id", "research_claim_id" });

            migrationBuilder.CreateIndex(
                name: "IX_research_conflicts_supporting_evidence_id",
                schema: "yaf",
                table: "research_conflicts",
                column: "supporting_evidence_id");

            migrationBuilder.CreateIndex(
                name: "IX_research_evidence_research_run_id_research_source_id",
                schema: "yaf",
                table: "research_evidence",
                columns: new[] { "research_run_id", "research_source_id" });

            migrationBuilder.CreateIndex(
                name: "IX_research_evidence_research_source_id",
                schema: "yaf",
                table: "research_evidence",
                column: "research_source_id");

            migrationBuilder.CreateIndex(
                name: "IX_research_reports_project_id",
                schema: "yaf",
                table: "research_reports",
                column: "project_id");

            migrationBuilder.CreateIndex(
                name: "IX_research_reports_research_run_id",
                schema: "yaf",
                table: "research_reports",
                column: "research_run_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_research_reports_video_project_id_version",
                schema: "yaf",
                table: "research_reports",
                columns: new[] { "video_project_id", "version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_research_runs_project_id_status",
                schema: "yaf",
                table: "research_runs",
                columns: new[] { "project_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_research_runs_video_project_id_queued_at",
                schema: "yaf",
                table: "research_runs",
                columns: new[] { "video_project_id", "queued_at" });

            migrationBuilder.CreateIndex(
                name: "IX_research_sources_research_run_id_canonical_url",
                schema: "yaf",
                table: "research_sources",
                columns: new[] { "research_run_id", "canonical_url" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_research_sources_research_run_id_domain",
                schema: "yaf",
                table: "research_sources",
                columns: new[] { "research_run_id", "domain" });

            migrationBuilder.AddForeignKey(
                name: "FK_ai_runs_research_runs_research_run_id",
                schema: "yaf",
                table: "ai_runs",
                column: "research_run_id",
                principalSchema: "yaf",
                principalTable: "research_runs",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ai_runs_video_projects_video_project_id",
                schema: "yaf",
                table: "ai_runs",
                column: "video_project_id",
                principalSchema: "yaf",
                principalTable: "video_projects",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ai_runs_research_runs_research_run_id",
                schema: "yaf",
                table: "ai_runs");

            migrationBuilder.DropForeignKey(
                name: "FK_ai_runs_video_projects_video_project_id",
                schema: "yaf",
                table: "ai_runs");

            migrationBuilder.DropTable(
                name: "research_claim_evidence",
                schema: "yaf");

            migrationBuilder.DropTable(
                name: "research_conflicts",
                schema: "yaf");

            migrationBuilder.DropTable(
                name: "research_claims",
                schema: "yaf");

            migrationBuilder.DropTable(
                name: "research_evidence",
                schema: "yaf");

            migrationBuilder.DropTable(
                name: "research_reports",
                schema: "yaf");

            migrationBuilder.DropTable(
                name: "research_sources",
                schema: "yaf");

            migrationBuilder.DropTable(
                name: "research_runs",
                schema: "yaf");

            migrationBuilder.DropIndex(
                name: "ux_jobs_active_video_project_research",
                schema: "yaf",
                table: "jobs");

            migrationBuilder.DropIndex(
                name: "IX_ai_runs_research_run_id_started_at",
                schema: "yaf",
                table: "ai_runs");

            migrationBuilder.DropIndex(
                name: "IX_ai_runs_video_project_id_started_at",
                schema: "yaf",
                table: "ai_runs");

            migrationBuilder.DropColumn(
                name: "video_project_id",
                schema: "yaf",
                table: "jobs");

            migrationBuilder.DropColumn(
                name: "research_run_id",
                schema: "yaf",
                table: "ai_runs");

            migrationBuilder.DropColumn(
                name: "video_project_id",
                schema: "yaf",
                table: "ai_runs");
        }
    }
}
