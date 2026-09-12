using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace YoutubeAiFactory.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SqlServerInitialBaseline : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "yaf");

            migrationBuilder.CreateTable(
                name: "jobs",
                schema: "yaf",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    type = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    competitor_channel_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    project_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    opportunity_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    payload = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    retry_count = table.Column<int>(type: "int", nullable: false),
                    max_retries = table.Column<int>(type: "int", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    available_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    started_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    completed_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    failure_reason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_jobs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "projects",
                schema: "yaf",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    market_name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    target_language = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    target_geography = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    audience_description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_projects", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "ai_runs",
                schema: "yaf",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    workflow = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    project_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    competitor_channel_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    provider = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    model = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    prompt_key = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    prompt_version = table.Column<int>(type: "int", nullable: false),
                    input_tokens = table.Column<int>(type: "int", nullable: true),
                    output_tokens = table.Column<int>(type: "int", nullable: true),
                    estimated_cost = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: true),
                    latency_milliseconds = table.Column<long>(type: "bigint", nullable: true),
                    status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    retry_count = table.Column<int>(type: "int", nullable: false),
                    started_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    completed_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    failure_reason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ai_runs", x => x.id);
                    table.ForeignKey(
                        name: "FK_ai_runs_projects_project_id",
                        column: x => x.project_id,
                        principalSchema: "yaf",
                        principalTable: "projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "competitor_channels",
                schema: "yaf",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    project_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    source_url = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false),
                    youtube_channel_id = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, collation: "Latin1_General_100_BIN2"),
                    title = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    handle = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    thumbnail_url = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                    subscriber_count = table.Column<long>(type: "bigint", nullable: true),
                    video_count = table.Column<long>(type: "bigint", nullable: true),
                    view_count = table.Column<long>(type: "bigint", nullable: true),
                    published_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    last_collected_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_competitor_channels", x => x.id);
                    table.ForeignKey(
                        name: "FK_competitor_channels_projects_project_id",
                        column: x => x.project_id,
                        principalSchema: "yaf",
                        principalTable: "projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "opportunity_reports",
                schema: "yaf",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    project_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    version = table.Column<int>(type: "int", nullable: false),
                    ai_run_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    prompt_key = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    prompt_version = table.Column<int>(type: "int", nullable: false),
                    provider = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    model = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    scoring_algorithm_version = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    source_analysis_count = table.Column<int>(type: "int", nullable: false),
                    limitations_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_opportunity_reports", x => x.id);
                    table.ForeignKey(
                        name: "FK_opportunity_reports_projects_project_id",
                        column: x => x.project_id,
                        principalSchema: "yaf",
                        principalTable: "projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "pilots",
                schema: "yaf",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    project_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    version = table.Column<int>(type: "int", nullable: false),
                    ai_run_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    prompt_key = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    prompt_version = table.Column<int>(type: "int", nullable: false),
                    provider = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    model = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    planning_algorithm_version = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    name = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    objective = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    assumptions_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    limitations_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    warnings_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    eligible_idea_count = table.Column<int>(type: "int", nullable: false),
                    revision = table.Column<int>(type: "int", nullable: false),
                    status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    approved_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pilots", x => x.id);
                    table.ForeignKey(
                        name: "FK_pilots_projects_project_id",
                        column: x => x.project_id,
                        principalSchema: "yaf",
                        principalTable: "projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "competitor_analyses",
                schema: "yaf",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    competitor_channel_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    version = table.Column<int>(type: "int", nullable: false),
                    ai_run_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    prompt_key = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    prompt_version = table.Column<int>(type: "int", nullable: false),
                    provider = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    model = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    source_data_as_of = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    analyzed_video_count = table.Column<int>(type: "int", nullable: false),
                    result_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_competitor_analyses", x => x.id);
                    table.ForeignKey(
                        name: "FK_competitor_analyses_competitor_channels_competitor_channel_id",
                        column: x => x.competitor_channel_id,
                        principalSchema: "yaf",
                        principalTable: "competitor_channels",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "competitor_videos",
                schema: "yaf",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    competitor_channel_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    youtube_video_id = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, collation: "Latin1_General_100_BIN2"),
                    title = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    url = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false),
                    thumbnail_url = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                    duration = table.Column<TimeSpan>(type: "time", nullable: true),
                    view_count = table.Column<long>(type: "bigint", nullable: true),
                    like_count = table.Column<long>(type: "bigint", nullable: true),
                    comment_count = table.Column<long>(type: "bigint", nullable: true),
                    published_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    collected_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_competitor_videos", x => x.id);
                    table.ForeignKey(
                        name: "FK_competitor_videos_competitor_channels_competitor_channel_id",
                        column: x => x.competitor_channel_id,
                        principalSchema: "yaf",
                        principalTable: "competitor_channels",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "opportunity_candidates",
                schema: "yaf",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    report_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Audience = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Topic = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    ContentFormat = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Angle = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    why_this_opportunity = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    observed_demand_signal = table.Column<int>(type: "int", nullable: false),
                    novelty_signal = table.Column<int>(type: "int", nullable: false),
                    competition_risk_signal = table.Column<int>(type: "int", nullable: false),
                    audience_fit_signal = table.Column<int>(type: "int", nullable: false),
                    transferability_signal = table.Column<int>(type: "int", nullable: false),
                    evidence_strength = table.Column<int>(type: "int", nullable: false),
                    story_potential = table.Column<int>(type: "int", nullable: false),
                    production_complexity = table.Column<int>(type: "int", nullable: false),
                    confidence = table.Column<int>(type: "int", nullable: false),
                    overall_score = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    risks_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    limitations_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    decision_status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_opportunity_candidates", x => x.id);
                    table.ForeignKey(
                        name: "FK_opportunity_candidates_opportunity_reports_report_id",
                        column: x => x.report_id,
                        principalSchema: "yaf",
                        principalTable: "opportunity_reports",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "opportunity_report_sources",
                schema: "yaf",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    report_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    competitor_channel_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    competitor_analysis_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    competitor_analysis_version = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_opportunity_report_sources", x => x.id);
                    table.ForeignKey(
                        name: "FK_opportunity_report_sources_competitor_analyses_competitor_analysis_id",
                        column: x => x.competitor_analysis_id,
                        principalSchema: "yaf",
                        principalTable: "competitor_analyses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_opportunity_report_sources_competitor_channels_competitor_channel_id",
                        column: x => x.competitor_channel_id,
                        principalSchema: "yaf",
                        principalTable: "competitor_channels",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_opportunity_report_sources_opportunity_reports_report_id",
                        column: x => x.report_id,
                        principalSchema: "yaf",
                        principalTable: "opportunity_reports",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "idea_generations",
                schema: "yaf",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    project_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    opportunity_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    opportunity_report_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    opportunity_report_version = table.Column<int>(type: "int", nullable: false),
                    version = table.Column<int>(type: "int", nullable: false),
                    ai_run_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    prompt_key = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    prompt_version = table.Column<int>(type: "int", nullable: false),
                    provider = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    model = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    scoring_algorithm_version = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_idea_generations", x => x.id);
                    table.ForeignKey(
                        name: "FK_idea_generations_opportunity_candidates_opportunity_id",
                        column: x => x.opportunity_id,
                        principalSchema: "yaf",
                        principalTable: "opportunity_candidates",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_idea_generations_opportunity_reports_opportunity_report_id",
                        column: x => x.opportunity_report_id,
                        principalSchema: "yaf",
                        principalTable: "opportunity_reports",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_idea_generations_projects_project_id",
                        column: x => x.project_id,
                        principalSchema: "yaf",
                        principalTable: "projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "opportunity_evidence",
                schema: "yaf",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    candidate_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    competitor_channel_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    competitor_analysis_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    competitor_video_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    evidence_id = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    summary = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_opportunity_evidence", x => x.id);
                    table.ForeignKey(
                        name: "FK_opportunity_evidence_competitor_analyses_competitor_analysis_id",
                        column: x => x.competitor_analysis_id,
                        principalSchema: "yaf",
                        principalTable: "competitor_analyses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_opportunity_evidence_competitor_channels_competitor_channel_id",
                        column: x => x.competitor_channel_id,
                        principalSchema: "yaf",
                        principalTable: "competitor_channels",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_opportunity_evidence_competitor_videos_competitor_video_id",
                        column: x => x.competitor_video_id,
                        principalSchema: "yaf",
                        principalTable: "competitor_videos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_opportunity_evidence_opportunity_candidates_candidate_id",
                        column: x => x.candidate_id,
                        principalSchema: "yaf",
                        principalTable: "opportunity_candidates",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "video_ideas",
                schema: "yaf",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    project_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    opportunity_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    generation_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    working_title = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Topic = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Angle = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    content_format = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    target_audience = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    viewer_intent = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    hook_concept = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    thumbnail_concept = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    viewer_promise = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    core_question = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    why_viewer_would_care = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Hypothesis = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    opportunity_fit = table.Column<int>(type: "int", nullable: false),
                    observed_demand_alignment = table.Column<int>(type: "int", nullable: false),
                    novelty = table.Column<int>(type: "int", nullable: false),
                    title_potential = table.Column<int>(type: "int", nullable: false),
                    thumbnail_potential = table.Column<int>(type: "int", nullable: false),
                    story_potential = table.Column<int>(type: "int", nullable: false),
                    audience_fit = table.Column<int>(type: "int", nullable: false),
                    evidence_strength = table.Column<int>(type: "int", nullable: false),
                    production_ease = table.Column<int>(type: "int", nullable: false),
                    competition_risk = table.Column<int>(type: "int", nullable: false),
                    research_risk = table.Column<int>(type: "int", nullable: false),
                    confidence = table.Column<int>(type: "int", nullable: false),
                    overall_score = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    duplication_penalty = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    scoring_algorithm_version = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    risks_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    decision_status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_video_ideas", x => x.id);
                    table.ForeignKey(
                        name: "FK_video_ideas_idea_generations_generation_id",
                        column: x => x.generation_id,
                        principalSchema: "yaf",
                        principalTable: "idea_generations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_video_ideas_opportunity_candidates_opportunity_id",
                        column: x => x.opportunity_id,
                        principalSchema: "yaf",
                        principalTable: "opportunity_candidates",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_video_ideas_projects_project_id",
                        column: x => x.project_id,
                        principalSchema: "yaf",
                        principalTable: "projects",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "idea_evidence",
                schema: "yaf",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    idea_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    opportunity_evidence_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    summary = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_idea_evidence", x => x.id);
                    table.ForeignKey(
                        name: "FK_idea_evidence_opportunity_evidence_opportunity_evidence_id",
                        column: x => x.opportunity_evidence_id,
                        principalSchema: "yaf",
                        principalTable: "opportunity_evidence",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_idea_evidence_video_ideas_idea_id",
                        column: x => x.idea_id,
                        principalSchema: "yaf",
                        principalTable: "video_ideas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "pilot_videos",
                schema: "yaf",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    pilot_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    video_idea_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    opportunity_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    sequence = table.Column<int>(type: "int", nullable: false),
                    experiment_type = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    hypothesis = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    variable_being_tested = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    control_strategy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    primary_metric = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    success_signal = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    rationale = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    secondary_metrics_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    notes = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pilot_videos", x => x.id);
                    table.CheckConstraint("ck_pilot_videos_experiment_block", "(sequence BETWEEN 1 AND 4 AND experiment_type = 'Topic') OR (sequence BETWEEN 5 AND 8 AND experiment_type = 'Packaging') OR (sequence BETWEEN 9 AND 12 AND experiment_type = 'Storytelling')");
                    table.CheckConstraint("ck_pilot_videos_sequence", "sequence >= 1 AND sequence <= 12");
                    table.ForeignKey(
                        name: "FK_pilot_videos_opportunity_candidates_opportunity_id",
                        column: x => x.opportunity_id,
                        principalSchema: "yaf",
                        principalTable: "opportunity_candidates",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_pilot_videos_pilots_pilot_id",
                        column: x => x.pilot_id,
                        principalSchema: "yaf",
                        principalTable: "pilots",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_pilot_videos_video_ideas_video_idea_id",
                        column: x => x.video_idea_id,
                        principalSchema: "yaf",
                        principalTable: "video_ideas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ai_runs_competitor_channel_id_started_at",
                schema: "yaf",
                table: "ai_runs",
                columns: new[] { "competitor_channel_id", "started_at" });

            migrationBuilder.CreateIndex(
                name: "IX_ai_runs_project_id_started_at",
                schema: "yaf",
                table: "ai_runs",
                columns: new[] { "project_id", "started_at" });

            migrationBuilder.CreateIndex(
                name: "IX_competitor_analyses_competitor_channel_id_created_at",
                schema: "yaf",
                table: "competitor_analyses",
                columns: new[] { "competitor_channel_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_competitor_analyses_competitor_channel_id_version",
                schema: "yaf",
                table: "competitor_analyses",
                columns: new[] { "competitor_channel_id", "version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_competitor_channels_project_id_youtube_channel_id",
                schema: "yaf",
                table: "competitor_channels",
                columns: new[] { "project_id", "youtube_channel_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_competitor_videos_competitor_channel_id_youtube_video_id",
                schema: "yaf",
                table: "competitor_videos",
                columns: new[] { "competitor_channel_id", "youtube_video_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_idea_evidence_idea_id_opportunity_evidence_id",
                schema: "yaf",
                table: "idea_evidence",
                columns: new[] { "idea_id", "opportunity_evidence_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_idea_evidence_opportunity_evidence_id",
                schema: "yaf",
                table: "idea_evidence",
                column: "opportunity_evidence_id");

            migrationBuilder.CreateIndex(
                name: "IX_idea_generations_opportunity_id_version",
                schema: "yaf",
                table: "idea_generations",
                columns: new[] { "opportunity_id", "version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_idea_generations_opportunity_report_id",
                schema: "yaf",
                table: "idea_generations",
                column: "opportunity_report_id");

            migrationBuilder.CreateIndex(
                name: "IX_idea_generations_project_id_created_at",
                schema: "yaf",
                table: "idea_generations",
                columns: new[] { "project_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_jobs_status_available_at",
                schema: "yaf",
                table: "jobs",
                columns: new[] { "status", "available_at" });

            migrationBuilder.CreateIndex(
                name: "ux_jobs_active_competitor_analysis",
                schema: "yaf",
                table: "jobs",
                column: "competitor_channel_id",
                unique: true,
                filter: "type = 'competitor-analysis' AND status IN ('Queued', 'Running', 'Retrying')");

            migrationBuilder.CreateIndex(
                name: "ux_jobs_active_idea_generation",
                schema: "yaf",
                table: "jobs",
                column: "opportunity_id",
                unique: true,
                filter: "type = 'idea-generation' AND status IN ('Queued', 'Running', 'Retrying')");

            migrationBuilder.CreateIndex(
                name: "ux_jobs_active_project_analysis",
                schema: "yaf",
                table: "jobs",
                columns: new[] { "project_id", "type" },
                unique: true,
                filter: "type IN ('opportunity-analysis', 'pilot-generation') AND status IN ('Queued', 'Running', 'Retrying')");

            migrationBuilder.CreateIndex(
                name: "IX_opportunity_candidates_report_id_overall_score",
                schema: "yaf",
                table: "opportunity_candidates",
                columns: new[] { "report_id", "overall_score" });

            migrationBuilder.CreateIndex(
                name: "IX_opportunity_evidence_candidate_id_evidence_id",
                schema: "yaf",
                table: "opportunity_evidence",
                columns: new[] { "candidate_id", "evidence_id" },
                unique: true);

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
                name: "IX_opportunity_report_sources_report_id_competitor_analysis_id",
                schema: "yaf",
                table: "opportunity_report_sources",
                columns: new[] { "report_id", "competitor_analysis_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_opportunity_reports_project_id_created_at",
                schema: "yaf",
                table: "opportunity_reports",
                columns: new[] { "project_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_opportunity_reports_project_id_version",
                schema: "yaf",
                table: "opportunity_reports",
                columns: new[] { "project_id", "version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_pilot_videos_opportunity_id",
                schema: "yaf",
                table: "pilot_videos",
                column: "opportunity_id");

            migrationBuilder.CreateIndex(
                name: "IX_pilot_videos_pilot_id_sequence",
                schema: "yaf",
                table: "pilot_videos",
                columns: new[] { "pilot_id", "sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_pilot_videos_pilot_id_video_idea_id",
                schema: "yaf",
                table: "pilot_videos",
                columns: new[] { "pilot_id", "video_idea_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_pilot_videos_video_idea_id",
                schema: "yaf",
                table: "pilot_videos",
                column: "video_idea_id");

            migrationBuilder.CreateIndex(
                name: "IX_pilots_project_id_created_at",
                schema: "yaf",
                table: "pilots",
                columns: new[] { "project_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_pilots_project_id_version",
                schema: "yaf",
                table: "pilots",
                columns: new[] { "project_id", "version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_video_ideas_generation_id",
                schema: "yaf",
                table: "video_ideas",
                column: "generation_id");

            migrationBuilder.CreateIndex(
                name: "IX_video_ideas_opportunity_id_overall_score",
                schema: "yaf",
                table: "video_ideas",
                columns: new[] { "opportunity_id", "overall_score" });

            migrationBuilder.CreateIndex(
                name: "IX_video_ideas_project_id_decision_status",
                schema: "yaf",
                table: "video_ideas",
                columns: new[] { "project_id", "decision_status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ai_runs",
                schema: "yaf");

            migrationBuilder.DropTable(
                name: "idea_evidence",
                schema: "yaf");

            migrationBuilder.DropTable(
                name: "jobs",
                schema: "yaf");

            migrationBuilder.DropTable(
                name: "opportunity_report_sources",
                schema: "yaf");

            migrationBuilder.DropTable(
                name: "pilot_videos",
                schema: "yaf");

            migrationBuilder.DropTable(
                name: "opportunity_evidence",
                schema: "yaf");

            migrationBuilder.DropTable(
                name: "pilots",
                schema: "yaf");

            migrationBuilder.DropTable(
                name: "video_ideas",
                schema: "yaf");

            migrationBuilder.DropTable(
                name: "competitor_analyses",
                schema: "yaf");

            migrationBuilder.DropTable(
                name: "competitor_videos",
                schema: "yaf");

            migrationBuilder.DropTable(
                name: "idea_generations",
                schema: "yaf");

            migrationBuilder.DropTable(
                name: "competitor_channels",
                schema: "yaf");

            migrationBuilder.DropTable(
                name: "opportunity_candidates",
                schema: "yaf");

            migrationBuilder.DropTable(
                name: "opportunity_reports",
                schema: "yaf");

            migrationBuilder.DropTable(
                name: "projects",
                schema: "yaf");
        }
    }
}
