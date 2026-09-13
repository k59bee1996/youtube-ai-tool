using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace YoutubeAiFactory.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddArtifactLocalizations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "artifact_id",
                schema: "yaf",
                table: "jobs",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "artifact_type",
                schema: "yaf",
                table: "jobs",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "artifact_version",
                schema: "yaf",
                table: "jobs",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "locale",
                schema: "yaf",
                table: "jobs",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "artifact_localizations",
                schema: "yaf",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    artifact_type = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    artifact_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    artifact_version = table.Column<int>(type: "int", nullable: false),
                    locale = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    content_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ai_run_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    prompt_key = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    prompt_version = table.Column<int>(type: "int", nullable: false),
                    provider = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    model = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_artifact_localizations", x => x.id);
                    table.CheckConstraint("ck_artifact_localizations_content_json", "ISJSON([content_json]) = 1");
                });

            migrationBuilder.CreateIndex(
                name: "ux_jobs_active_artifact_localization",
                schema: "yaf",
                table: "jobs",
                columns: new[] { "artifact_type", "artifact_id", "artifact_version", "locale" },
                unique: true,
                filter: "type = 'artifact-localization' AND status IN ('Queued', 'Running', 'Retrying')");

            migrationBuilder.CreateIndex(
                name: "IX_artifact_localizations_artifact_type_artifact_id_artifact_version_locale",
                schema: "yaf",
                table: "artifact_localizations",
                columns: new[] { "artifact_type", "artifact_id", "artifact_version", "locale" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "artifact_localizations",
                schema: "yaf");

            migrationBuilder.DropIndex(
                name: "ux_jobs_active_artifact_localization",
                schema: "yaf",
                table: "jobs");

            migrationBuilder.DropColumn(
                name: "artifact_id",
                schema: "yaf",
                table: "jobs");

            migrationBuilder.DropColumn(
                name: "artifact_type",
                schema: "yaf",
                table: "jobs");

            migrationBuilder.DropColumn(
                name: "artifact_version",
                schema: "yaf",
                table: "jobs");

            migrationBuilder.DropColumn(
                name: "locale",
                schema: "yaf",
                table: "jobs");
        }
    }
}
