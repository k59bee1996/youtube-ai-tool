using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace YoutubeAiFactory.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddVideoProjects : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "video_projects",
                schema: "yaf",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    project_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    pilot_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    pilot_version = table.Column<int>(type: "int", nullable: false),
                    pilot_video_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    video_idea_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    opportunity_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    working_title = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    topic = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    angle = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    content_format = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    target_audience = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    hook_concept = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    thumbnail_concept = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    viewer_promise = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    experiment_type = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    pilot_hypothesis = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    variable_being_tested = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    primary_metric = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    success_signal = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    source_warnings_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    execution_notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_video_projects", x => x.id);
                    table.CheckConstraint("ck_video_projects_source_warnings_json", "ISJSON([source_warnings_json]) = 1");
                    table.ForeignKey(
                        name: "FK_video_projects_opportunity_candidates_opportunity_id",
                        column: x => x.opportunity_id,
                        principalSchema: "yaf",
                        principalTable: "opportunity_candidates",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_video_projects_pilot_videos_pilot_video_id",
                        column: x => x.pilot_video_id,
                        principalSchema: "yaf",
                        principalTable: "pilot_videos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_video_projects_pilots_pilot_id",
                        column: x => x.pilot_id,
                        principalSchema: "yaf",
                        principalTable: "pilots",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_video_projects_projects_project_id",
                        column: x => x.project_id,
                        principalSchema: "yaf",
                        principalTable: "projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_video_projects_video_ideas_video_idea_id",
                        column: x => x.video_idea_id,
                        principalSchema: "yaf",
                        principalTable: "video_ideas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_video_projects_opportunity_id",
                schema: "yaf",
                table: "video_projects",
                column: "opportunity_id");

            migrationBuilder.CreateIndex(
                name: "IX_video_projects_pilot_id",
                schema: "yaf",
                table: "video_projects",
                column: "pilot_id");

            migrationBuilder.CreateIndex(
                name: "IX_video_projects_pilot_video_id",
                schema: "yaf",
                table: "video_projects",
                column: "pilot_video_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_video_projects_project_id_created_at",
                schema: "yaf",
                table: "video_projects",
                columns: new[] { "project_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_video_projects_video_idea_id",
                schema: "yaf",
                table: "video_projects",
                column: "video_idea_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "video_projects",
                schema: "yaf");
        }
    }
}
