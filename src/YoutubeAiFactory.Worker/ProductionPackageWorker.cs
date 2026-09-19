using YoutubeAiFactory.Application.Production;

namespace YoutubeAiFactory.Worker;

public sealed class ProductionPackageWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<ProductionPackageWorker> logger
) : BackgroundService
{
    private static readonly Action<ILogger, Exception?> LogFailure = LoggerMessage.Define(
        LogLevel.Error,
        new EventId(1, "ProductionPackageWorkerFailed"),
        "Production package worker iteration failed."
    );

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var processor =
                    scope.ServiceProvider.GetRequiredService<ProductionPackageJobProcessor>();
                if (!await processor.ProcessNextAsync(stoppingToken))
                    await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
            catch (Exception ex)
            {
                LogFailure(logger, ex);
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }
}
