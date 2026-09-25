using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace YoutubeAiFactory.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddObservabilityCostAccounting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "cached_input_tokens",
                schema: "yaf",
                table: "ai_runs",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "calculated_estimated_cost",
                schema: "yaf",
                table: "ai_runs",
                type: "decimal(19,8)",
                precision: 19,
                scale: 8,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "cost_source",
                schema: "yaf",
                table: "ai_runs",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "Unavailable");

            migrationBuilder.AddColumn<string>(
                name: "currency",
                schema: "yaf",
                table: "ai_runs",
                type: "nvarchar(3)",
                maxLength: 3,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "error_category",
                schema: "yaf",
                table: "ai_runs",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "job_id",
                schema: "yaf",
                table: "ai_runs",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "pricing_effective_from",
                schema: "yaf",
                table: "ai_runs",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "pricing_version",
                schema: "yaf",
                table: "ai_runs",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "provider_reported_cost",
                schema: "yaf",
                table: "ai_runs",
                type: "decimal(19,8)",
                precision: 19,
                scale: 8,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "reasoning_tokens",
                schema: "yaf",
                table: "ai_runs",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "workflow_stage",
                schema: "yaf",
                table: "ai_runs",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_jobs_project_id_created_at",
                schema: "yaf",
                table: "jobs",
                columns: new[] { "project_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_jobs_video_project_id_created_at",
                schema: "yaf",
                table: "jobs",
                columns: new[] { "video_project_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_ai_runs_job_id_started_at",
                schema: "yaf",
                table: "ai_runs",
                columns: new[] { "job_id", "started_at" });

            migrationBuilder.CreateIndex(
                name: "IX_ai_runs_project_id_workflow_started_at",
                schema: "yaf",
                table: "ai_runs",
                columns: new[] { "project_id", "workflow", "started_at" });

            migrationBuilder.AddForeignKey(
                name: "FK_ai_runs_jobs_job_id",
                schema: "yaf",
                table: "ai_runs",
                column: "job_id",
                principalSchema: "yaf",
                principalTable: "jobs",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ai_runs_jobs_job_id",
                schema: "yaf",
                table: "ai_runs");

            migrationBuilder.DropIndex(
                name: "IX_jobs_project_id_created_at",
                schema: "yaf",
                table: "jobs");

            migrationBuilder.DropIndex(
                name: "IX_jobs_video_project_id_created_at",
                schema: "yaf",
                table: "jobs");

            migrationBuilder.DropIndex(
                name: "IX_ai_runs_job_id_started_at",
                schema: "yaf",
                table: "ai_runs");

            migrationBuilder.DropIndex(
                name: "IX_ai_runs_project_id_workflow_started_at",
                schema: "yaf",
                table: "ai_runs");

            migrationBuilder.DropColumn(
                name: "cached_input_tokens",
                schema: "yaf",
                table: "ai_runs");

            migrationBuilder.DropColumn(
                name: "calculated_estimated_cost",
                schema: "yaf",
                table: "ai_runs");

            migrationBuilder.DropColumn(
                name: "cost_source",
                schema: "yaf",
                table: "ai_runs");

            migrationBuilder.DropColumn(
                name: "currency",
                schema: "yaf",
                table: "ai_runs");

            migrationBuilder.DropColumn(
                name: "error_category",
                schema: "yaf",
                table: "ai_runs");

            migrationBuilder.DropColumn(
                name: "job_id",
                schema: "yaf",
                table: "ai_runs");

            migrationBuilder.DropColumn(
                name: "pricing_effective_from",
                schema: "yaf",
                table: "ai_runs");

            migrationBuilder.DropColumn(
                name: "pricing_version",
                schema: "yaf",
                table: "ai_runs");

            migrationBuilder.DropColumn(
                name: "provider_reported_cost",
                schema: "yaf",
                table: "ai_runs");

            migrationBuilder.DropColumn(
                name: "reasoning_tokens",
                schema: "yaf",
                table: "ai_runs");

            migrationBuilder.DropColumn(
                name: "workflow_stage",
                schema: "yaf",
                table: "ai_runs");
        }
    }
}
