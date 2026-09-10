using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace YoutubeAiFactory.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOpportunityReports : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "project_id",
                schema: "yaf",
                table: "jobs",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "opportunity_reports",
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
                    scoring_algorithm_version = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    source_analysis_count = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_opportunity_reports", x => x.id);
                    table.ForeignKey("FK_opportunity_reports_projects_project_id", x => x.project_id, principalSchema: "yaf", principalTable: "projects", principalColumn: "id", onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "opportunity_candidates",
                schema: "yaf",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    report_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    audience = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    topic = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    content_format = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    angle = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    why_this_opportunity = table.Column<string>(type: "text", nullable: false),
                    observed_demand_signal = table.Column<int>(type: "integer", nullable: false), novelty_signal = table.Column<int>(type: "integer", nullable: false), competition_risk_signal = table.Column<int>(type: "integer", nullable: false), audience_fit_signal = table.Column<int>(type: "integer", nullable: false), transferability_signal = table.Column<int>(type: "integer", nullable: false), evidence_strength = table.Column<int>(type: "integer", nullable: false), story_potential = table.Column<int>(type: "integer", nullable: false), production_complexity = table.Column<int>(type: "integer", nullable: false), confidence = table.Column<int>(type: "integer", nullable: false),
                    overall_score = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    risks_json = table.Column<string>(type: "jsonb", nullable: false), limitations_json = table.Column<string>(type: "jsonb", nullable: false), decision_status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false), created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                },
                constraints: table => { table.PrimaryKey("PK_opportunity_candidates", x => x.id); table.ForeignKey("FK_opportunity_candidates_opportunity_reports_report_id", x => x.report_id, principalSchema: "yaf", principalTable: "opportunity_reports", principalColumn: "id", onDelete: ReferentialAction.Cascade); });

            migrationBuilder.CreateTable(
                name: "opportunity_report_sources",
                schema: "yaf",
                columns: table => new { id = table.Column<Guid>(type: "uuid", nullable: false), report_id = table.Column<Guid>(type: "uuid", nullable: false), competitor_channel_id = table.Column<Guid>(type: "uuid", nullable: false), competitor_analysis_id = table.Column<Guid>(type: "uuid", nullable: false), competitor_analysis_version = table.Column<int>(type: "integer", nullable: false) },
                constraints: table => { table.PrimaryKey("PK_opportunity_report_sources", x => x.id); table.ForeignKey("FK_opportunity_report_sources_opportunity_reports_report_id", x => x.report_id, principalSchema: "yaf", principalTable: "opportunity_reports", principalColumn: "id", onDelete: ReferentialAction.Cascade); });

            migrationBuilder.CreateTable(
                name: "opportunity_evidence",
                schema: "yaf",
                columns: table => new { id = table.Column<Guid>(type: "uuid", nullable: false), candidate_id = table.Column<Guid>(type: "uuid", nullable: false), competitor_channel_id = table.Column<Guid>(type: "uuid", nullable: false), competitor_analysis_id = table.Column<Guid>(type: "uuid", nullable: false), competitor_video_id = table.Column<Guid>(type: "uuid", nullable: true), evidence_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false), summary = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false) },
                constraints: table => { table.PrimaryKey("PK_opportunity_evidence", x => x.id); table.ForeignKey("FK_opportunity_evidence_opportunity_candidates_candidate_id", x => x.candidate_id, principalSchema: "yaf", principalTable: "opportunity_candidates", principalColumn: "id", onDelete: ReferentialAction.Cascade); });

            migrationBuilder.CreateIndex(name: "ux_jobs_active_opportunity_analysis", schema: "yaf", table: "jobs", column: "project_id", unique: true, filter: "type = 'opportunity-analysis' AND status IN ('Queued', 'Running', 'Retrying')");
            migrationBuilder.CreateIndex(name: "IX_opportunity_reports_project_id_created_at", schema: "yaf", table: "opportunity_reports", columns: new[] { "project_id", "created_at" });
            migrationBuilder.CreateIndex(name: "IX_opportunity_reports_project_id_version", schema: "yaf", table: "opportunity_reports", columns: new[] { "project_id", "version" }, unique: true);
            migrationBuilder.CreateIndex(name: "IX_opportunity_candidates_report_id_overall_score", schema: "yaf", table: "opportunity_candidates", columns: new[] { "report_id", "overall_score" });
            migrationBuilder.CreateIndex(name: "IX_opportunity_report_sources_report_id_competitor_analysis_id", schema: "yaf", table: "opportunity_report_sources", columns: new[] { "report_id", "competitor_analysis_id" }, unique: true);
            migrationBuilder.CreateIndex(name: "IX_opportunity_evidence_candidate_id_evidence_id", schema: "yaf", table: "opportunity_evidence", columns: new[] { "candidate_id", "evidence_id" }, unique: true);
            migrationBuilder.CreateIndex(
                name: "IX_opportunity_report_sources_competitor_analysis_id",
                schema: "yaf",
                table: "opportunity_report_sources",
                column: "competitor_analysis_id");

            migrationBuilder.CreateIndex(
                name: "IX_opportunity_report_sources_competitor_channel_id",
                schema: "yaf",
                table: "opportunity_report_sources",
                column: "competitor_channel_id");

            migrationBuilder.CreateIndex(
                name: "IX_opportunity_evidence_competitor_analysis_id",
                schema: "yaf",
                table: "opportunity_evidence",
                column: "competitor_analysis_id");

            migrationBuilder.CreateIndex(
                name: "IX_opportunity_evidence_competitor_channel_id",
                schema: "yaf",
                table: "opportunity_evidence",
                column: "competitor_channel_id");

            migrationBuilder.CreateIndex(
                name: "IX_opportunity_evidence_competitor_video_id",
                schema: "yaf",
                table: "opportunity_evidence",
                column: "competitor_video_id");

            migrationBuilder.AddForeignKey(
                name: "FK_opportunity_evidence_competitor_analyses_competitor_analysi~",
                schema: "yaf",
                table: "opportunity_evidence",
                column: "competitor_analysis_id",
                principalSchema: "yaf",
                principalTable: "competitor_analyses",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_opportunity_evidence_competitor_channels_competitor_channel~",
                schema: "yaf",
                table: "opportunity_evidence",
                column: "competitor_channel_id",
                principalSchema: "yaf",
                principalTable: "competitor_channels",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_opportunity_evidence_competitor_videos_competitor_video_id",
                schema: "yaf",
                table: "opportunity_evidence",
                column: "competitor_video_id",
                principalSchema: "yaf",
                principalTable: "competitor_videos",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_opportunity_report_sources_competitor_analyses_competitor_a~",
                schema: "yaf",
                table: "opportunity_report_sources",
                column: "competitor_analysis_id",
                principalSchema: "yaf",
                principalTable: "competitor_analyses",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_opportunity_report_sources_competitor_channels_competitor_c~",
                schema: "yaf",
                table: "opportunity_report_sources",
                column: "competitor_channel_id",
                principalSchema: "yaf",
                principalTable: "competitor_channels",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_opportunity_evidence_competitor_analyses_competitor_analysi~",
                schema: "yaf",
                table: "opportunity_evidence");

            migrationBuilder.DropForeignKey(
                name: "FK_opportunity_evidence_competitor_channels_competitor_channel~",
                schema: "yaf",
                table: "opportunity_evidence");

            migrationBuilder.DropForeignKey(
                name: "FK_opportunity_evidence_competitor_videos_competitor_video_id",
                schema: "yaf",
                table: "opportunity_evidence");

            migrationBuilder.DropForeignKey(
                name: "FK_opportunity_report_sources_competitor_analyses_competitor_a~",
                schema: "yaf",
                table: "opportunity_report_sources");

            migrationBuilder.DropForeignKey(
                name: "FK_opportunity_report_sources_competitor_channels_competitor_c~",
                schema: "yaf",
                table: "opportunity_report_sources");

            migrationBuilder.DropIndex(
                name: "IX_opportunity_report_sources_competitor_analysis_id",
                schema: "yaf",
                table: "opportunity_report_sources");

            migrationBuilder.DropIndex(
                name: "IX_opportunity_report_sources_competitor_channel_id",
                schema: "yaf",
                table: "opportunity_report_sources");

            migrationBuilder.DropIndex(
                name: "IX_opportunity_evidence_competitor_analysis_id",
                schema: "yaf",
                table: "opportunity_evidence");

            migrationBuilder.DropIndex(
                name: "IX_opportunity_evidence_competitor_channel_id",
                schema: "yaf",
                table: "opportunity_evidence");

            migrationBuilder.DropIndex(
                name: "IX_opportunity_evidence_competitor_video_id",
                schema: "yaf",
                table: "opportunity_evidence");

            migrationBuilder.DropIndex(name: "ux_jobs_active_opportunity_analysis", schema: "yaf", table: "jobs");
            migrationBuilder.DropTable(name: "opportunity_evidence", schema: "yaf");
            migrationBuilder.DropTable(name: "opportunity_report_sources", schema: "yaf");
            migrationBuilder.DropTable(name: "opportunity_candidates", schema: "yaf");
            migrationBuilder.DropTable(name: "opportunity_reports", schema: "yaf");
            migrationBuilder.DropColumn(name: "project_id", schema: "yaf", table: "jobs");
        }
    }
}
