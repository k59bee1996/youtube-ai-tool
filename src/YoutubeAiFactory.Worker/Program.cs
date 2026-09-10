using YoutubeAiFactory.Application.Competitors;
using YoutubeAiFactory.Application.Ideas;
using YoutubeAiFactory.Application.Opportunities;
using YoutubeAiFactory.Infrastructure;
using YoutubeAiFactory.Worker;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton(builder.Configuration.GetSection("CompetitorAnalysis").Get<CompetitorAnalysisOptions>() ?? new());
builder.Services.AddSingleton(builder.Configuration.GetSection("OpportunityAnalysis").Get<OpportunityAnalysisOptions>() ?? new());
builder.Services.AddSingleton(builder.Configuration.GetSection("IdeaGeneration").Get<IdeaGenerationOptions>() ?? new());
builder.Services.AddScoped<CompetitorAnalysisContextBuilder>();
builder.Services.AddScoped<CompetitorAnalysisJobProcessor>();
builder.Services.AddScoped<OpportunityAnalysisContextBuilder>();
builder.Services.AddScoped<OpportunityScoringEngine>();
builder.Services.AddScoped<OpportunityAnalysisJobProcessor>();
builder.Services.AddScoped<IdeaGenerationContextBuilder>();
builder.Services.AddScoped<IdeaGenerationJobProcessor>();
builder.Services.AddHostedService<DatabaseHeartbeatWorker>();
builder.Services.AddHostedService<CompetitorAnalysisWorker>();
builder.Services.AddHostedService<OpportunityAnalysisWorker>();
builder.Services.AddHostedService<IdeaGenerationWorker>();

var host = builder.Build();
host.Run();
