using YoutubeAiFactory.Application.Research;

namespace YoutubeAiFactory.Worker;

public sealed class VideoResearchWorker(IServiceScopeFactory scopeFactory, ILogger<VideoResearchWorker> logger) : BackgroundService
{
    private static readonly Action<ILogger, Exception?> LogIterationFailed = LoggerMessage.Define(
        LogLevel.Error, new EventId(1, nameof(LogIterationFailed)), "Video research worker iteration failed.");

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var processor = scope.ServiceProvider.GetRequiredService<VideoResearchJobProcessor>();
                if (!await processor.ProcessNextAsync(stoppingToken)) await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
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
