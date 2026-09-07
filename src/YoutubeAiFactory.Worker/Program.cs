using YoutubeAiFactory.Infrastructure;
using YoutubeAiFactory.Worker;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHostedService<DatabaseHeartbeatWorker>();

var host = builder.Build();
host.Run();
