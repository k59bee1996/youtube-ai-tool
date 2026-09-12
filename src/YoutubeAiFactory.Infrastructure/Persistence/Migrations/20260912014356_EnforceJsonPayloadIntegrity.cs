using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace YoutubeAiFactory.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class EnforceJsonPayloadIntegrity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddCheckConstraint(
                name: "ck_video_ideas_risks_json",
                schema: "yaf",
                table: "video_ideas",
                sql: "ISJSON([risks_json]) = 1");

            migrationBuilder.AddCheckConstraint(
                name: "ck_pilots_assumptions_json",
                schema: "yaf",
                table: "pilots",
                sql: "ISJSON([assumptions_json]) = 1");

            migrationBuilder.AddCheckConstraint(
                name: "ck_pilots_limitations_json",
                schema: "yaf",
                table: "pilots",
                sql: "ISJSON([limitations_json]) = 1");

            migrationBuilder.AddCheckConstraint(
                name: "ck_pilots_warnings_json",
                schema: "yaf",
                table: "pilots",
                sql: "ISJSON([warnings_json]) = 1");

            migrationBuilder.AddCheckConstraint(
                name: "ck_pilot_videos_secondary_metrics_json",
                schema: "yaf",
                table: "pilot_videos",
                sql: "ISJSON([secondary_metrics_json]) = 1");

            migrationBuilder.AddCheckConstraint(
                name: "ck_opportunity_reports_limitations_json",
                schema: "yaf",
                table: "opportunity_reports",
                sql: "ISJSON([limitations_json]) = 1");

            migrationBuilder.AddCheckConstraint(
                name: "ck_opportunity_candidates_limitations_json",
                schema: "yaf",
                table: "opportunity_candidates",
                sql: "ISJSON([limitations_json]) = 1");

            migrationBuilder.AddCheckConstraint(
                name: "ck_opportunity_candidates_risks_json",
                schema: "yaf",
                table: "opportunity_candidates",
                sql: "ISJSON([risks_json]) = 1");

            migrationBuilder.AddCheckConstraint(
                name: "ck_jobs_payload_json",
                schema: "yaf",
                table: "jobs",
                sql: "ISJSON([payload]) = 1");

            migrationBuilder.AddCheckConstraint(
                name: "ck_competitor_analyses_result_json",
                schema: "yaf",
                table: "competitor_analyses",
                sql: "ISJSON([result_json]) = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_video_ideas_risks_json",
                schema: "yaf",
                table: "video_ideas");

            migrationBuilder.DropCheckConstraint(
                name: "ck_pilots_assumptions_json",
                schema: "yaf",
                table: "pilots");

            migrationBuilder.DropCheckConstraint(
                name: "ck_pilots_limitations_json",
                schema: "yaf",
                table: "pilots");

            migrationBuilder.DropCheckConstraint(
                name: "ck_pilots_warnings_json",
                schema: "yaf",
                table: "pilots");

            migrationBuilder.DropCheckConstraint(
                name: "ck_pilot_videos_secondary_metrics_json",
                schema: "yaf",
                table: "pilot_videos");

            migrationBuilder.DropCheckConstraint(
                name: "ck_opportunity_reports_limitations_json",
                schema: "yaf",
                table: "opportunity_reports");

            migrationBuilder.DropCheckConstraint(
                name: "ck_opportunity_candidates_limitations_json",
                schema: "yaf",
                table: "opportunity_candidates");

            migrationBuilder.DropCheckConstraint(
                name: "ck_opportunity_candidates_risks_json",
                schema: "yaf",
                table: "opportunity_candidates");

            migrationBuilder.DropCheckConstraint(
                name: "ck_jobs_payload_json",
                schema: "yaf",
                table: "jobs");

            migrationBuilder.DropCheckConstraint(
                name: "ck_competitor_analyses_result_json",
                schema: "yaf",
                table: "competitor_analyses");
        }
    }
}
