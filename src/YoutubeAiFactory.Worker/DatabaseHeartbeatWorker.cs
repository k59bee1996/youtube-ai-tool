using Microsoft.EntityFrameworkCore;
using YoutubeAiFactory.Infrastructure.Persistence;

namespace YoutubeAiFactory.Worker;

public sealed class DatabaseHeartbeatWorker(
    IDbContextFactory<YoutubeAiFactoryDbContext> contextFactory,
    ILogger<DatabaseHeartbeatWorker> logger) : BackgroundService
{
    private static readonly Action<ILogger, Exception?> LogWorkerStarted =
        LoggerMessage.Define(
            LogLevel.Information,
            new EventId(1, nameof(LogWorkerStarted)),
            "YouTube AI Factory worker started.");

    private static readonly Action<ILogger, Exception?> LogDatabaseHealthy =
        LoggerMessage.Define(
            LogLevel.Debug,
            new EventId(2, nameof(LogDatabaseHealthy)),
            "PostgreSQL connectivity check: healthy");

    private static readonly Action<ILogger, Exception?> LogDatabaseUnavailable =
        LoggerMessage.Define(
            LogLevel.Warning,
            new EventId(3, nameof(LogDatabaseUnavailable)),
            "PostgreSQL connectivity check: unavailable");

    private static readonly Action<ILogger, Exception?> LogDatabaseCheckFailed =
        LoggerMessage.Define(
            LogLevel.Warning,
            new EventId(4, nameof(LogDatabaseCheckFailed)),
            "PostgreSQL connectivity check failed.");

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        LogWorkerStarted(logger, null);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var context = await contextFactory.CreateDbContextAsync(stoppingToken);
                var canConnect = await context.Database.CanConnectAsync(stoppingToken);
                if (canConnect)
                {
                    LogDatabaseHealthy(logger, null);
                }
                else
                {
                    LogDatabaseUnavailable(logger, null);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                LogDatabaseCheckFailed(logger, exception);
            }

            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
    }
}
