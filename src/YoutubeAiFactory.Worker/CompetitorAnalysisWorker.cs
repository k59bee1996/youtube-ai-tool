using YoutubeAiFactory.Application.Competitors;

namespace YoutubeAiFactory.Worker;

public sealed class CompetitorAnalysisWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<CompetitorAnalysisWorker> logger) : BackgroundService
{
    private static readonly Action<ILogger, Exception?> LogStarted =
        LoggerMessage.Define(LogLevel.Information, new EventId(1, nameof(LogStarted)), "Competitor analysis worker started.");
    private static readonly Action<ILogger, Exception?> LogLoopFailed =
        LoggerMessage.Define(LogLevel.Error, new EventId(2, nameof(LogLoopFailed)), "Competitor analysis worker loop failed.");

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        LogStarted(logger, null);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var processor = scope.ServiceProvider.GetRequiredService<CompetitorAnalysisJobProcessor>();
                var processed = await processor.ProcessNextAsync(stoppingToken);
                if (!processed) await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
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
