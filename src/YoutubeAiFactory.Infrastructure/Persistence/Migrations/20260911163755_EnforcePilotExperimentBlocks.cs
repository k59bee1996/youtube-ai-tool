using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace YoutubeAiFactory.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class EnforcePilotExperimentBlocks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddCheckConstraint(
                name: "ck_pilot_videos_experiment_block",
                schema: "yaf",
                table: "pilot_videos",
                sql: "(sequence BETWEEN 1 AND 4 AND experiment_type = 'Topic') OR (sequence BETWEEN 5 AND 8 AND experiment_type = 'Packaging') OR (sequence BETWEEN 9 AND 12 AND experiment_type = 'Storytelling')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_pilot_videos_experiment_block",
                schema: "yaf",
                table: "pilot_videos");
        }
    }
}
