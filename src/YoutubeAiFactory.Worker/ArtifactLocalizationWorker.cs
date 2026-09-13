using YoutubeAiFactory.Application.Localization;

namespace YoutubeAiFactory.Worker;

public sealed class ArtifactLocalizationWorker(IServiceScopeFactory scopeFactory, ILogger<ArtifactLocalizationWorker> logger) : BackgroundService
{
    private static readonly Action<ILogger, Exception?> LogLoopFailed = LoggerMessage.Define(LogLevel.Error, new EventId(1, nameof(LogLoopFailed)), "Artifact localization worker loop failed.");
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var processor = scope.ServiceProvider.GetRequiredService<ArtifactLocalizationJobProcessor>();
                if (!await processor.ProcessNextAsync(stoppingToken)) await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception exception)
            {
                LogLoopFailed(logger, exception);
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }
}
