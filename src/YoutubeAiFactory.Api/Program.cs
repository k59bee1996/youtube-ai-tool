using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using YoutubeAiFactory.Api.Endpoints;
using YoutubeAiFactory.Api.Errors;
using YoutubeAiFactory.Api.Health;
using YoutubeAiFactory.Application.Competitors;
using YoutubeAiFactory.Application.Projects;
using YoutubeAiFactory.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddSimpleConsole();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton(
    builder.Configuration.GetSection("CompetitorCollection").Get<CompetitorCollectionOptions>() ?? new());
builder.Services.AddSingleton(
    builder.Configuration.GetSection("CompetitorAnalysis").Get<CompetitorAnalysisOptions>() ?? new());
builder.Services.AddScoped<CreateProjectHandler>();
builder.Services.AddScoped<GetProjectHandler>();
builder.Services.AddScoped<ListProjectsHandler>();
builder.Services.AddScoped<AddCompetitorHandler>();
builder.Services.AddScoped<GetCompetitorHandler>();
builder.Services.AddScoped<ListCompetitorsHandler>();
builder.Services.AddScoped<RunCompetitorAnalysisHandler>();
builder.Services.AddScoped<GetCompetitorAnalysisStatusHandler>();
builder.Services.AddScoped<CompetitorAnalysisContextBuilder>();
builder.Services.AddScoped<CompetitorAnalysisJobProcessor>();
builder.Services
    .AddHealthChecks()
    .AddCheck<PostgresHealthCheck>("postgresql", tags: ["ready"])
    .AddCheck<YouTubeConfigurationHealthCheck>("youtube-configuration", tags: ["ready"]);

var app = builder.Build();

app.UseExceptionHandler();

app.MapGet("/", () => Results.Ok(new
{
    service = "YouTube AI Factory API",
    phase = "phase-3",
}));

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false,
    ResponseWriter = WriteHealthResponseAsync,
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("ready"),
    ResponseWriter = WriteHealthResponseAsync,
});

app.MapProjectEndpoints();
app.MapCompetitorEndpoints();

app.Run();

static Task WriteHealthResponseAsync(
    HttpContext context,
    Microsoft.Extensions.Diagnostics.HealthChecks.HealthReport report)
{
    context.Response.ContentType = "application/json";
    return context.Response.WriteAsync(JsonSerializer.Serialize(new
    {
        status = report.Status.ToString(),
        checks = report.Entries.Select(entry => new
        {
            name = entry.Key,
            status = entry.Value.Status.ToString(),
            description = entry.Value.Description,
        }),
    }));
}

public partial class Program;
