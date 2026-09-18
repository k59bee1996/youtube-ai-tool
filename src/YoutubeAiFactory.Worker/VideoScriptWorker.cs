using YoutubeAiFactory.Application.Scripts;

namespace YoutubeAiFactory.Worker;

public sealed class VideoScriptWorker(IServiceScopeFactory scopeFactory, ILogger<VideoScriptWorker> logger)
    : BackgroundService
{
    private static readonly Action<ILogger, Exception?> LogIterationFailed = LoggerMessage.Define(
        LogLevel.Error, new EventId(1, nameof(LogIterationFailed)), "Video Script worker iteration failed.");

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var processor = scope.ServiceProvider.GetRequiredService<VideoScriptJobProcessor>();
                if (!await processor.ProcessNextAsync(stoppingToken))
                    await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
            catch (Exception exception)
            {
                LogIterationFailed(logger, exception);
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }
}
