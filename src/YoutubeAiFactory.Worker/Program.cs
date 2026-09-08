using YoutubeAiFactory.Application.Competitors;
using YoutubeAiFactory.Infrastructure;
using YoutubeAiFactory.Worker;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton(builder.Configuration.GetSection("CompetitorAnalysis").Get<CompetitorAnalysisOptions>() ?? new());
builder.Services.AddScoped<CompetitorAnalysisContextBuilder>();
builder.Services.AddScoped<CompetitorAnalysisJobProcessor>();
builder.Services.AddHostedService<DatabaseHeartbeatWorker>();
builder.Services.AddHostedService<CompetitorAnalysisWorker>();

var host = builder.Build();
host.Run();
