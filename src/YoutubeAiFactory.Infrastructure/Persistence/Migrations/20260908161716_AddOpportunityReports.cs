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
        }
    }
}
