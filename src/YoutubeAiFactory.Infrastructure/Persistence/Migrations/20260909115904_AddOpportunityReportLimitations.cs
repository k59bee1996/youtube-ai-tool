using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace YoutubeAiFactory.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOpportunityReportLimitations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "limitations_json",
                schema: "yaf",
                table: "opportunity_reports",
                type: "jsonb",
                nullable: false,
                defaultValue: "[]");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "limitations_json",
                schema: "yaf",
                table: "opportunity_reports");
        }
    }
}
