using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace YoutubeAiFactory.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class HardenCompetitorAnalysisJobs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "competitor_channel_id",
                schema: "yaf",
                table: "jobs",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ux_jobs_active_competitor_analysis",
                schema: "yaf",
                table: "jobs",
                column: "competitor_channel_id",
                unique: true,
                filter: "type = 'competitor-analysis' AND status IN ('Queued', 'Running', 'Retrying')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_jobs_active_competitor_analysis",
                schema: "yaf",
                table: "jobs");

            migrationBuilder.DropColumn(
                name: "competitor_channel_id",
                schema: "yaf",
                table: "jobs");
        }
    }
}
