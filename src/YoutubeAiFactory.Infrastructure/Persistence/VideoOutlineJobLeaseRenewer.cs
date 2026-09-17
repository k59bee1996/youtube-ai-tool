using Microsoft.EntityFrameworkCore;
using YoutubeAiFactory.Application.Persistence;

namespace YoutubeAiFactory.Infrastructure.Persistence;

internal sealed class VideoOutlineJobLeaseRenewer(IDbContextFactory<YoutubeAiFactoryDbContext> dbContextFactory)
    : IVideoOutlineJobLeaseRenewer
{
    public async Task<bool> RenewAsync(Guid jobId, Guid leaseId, DateTimeOffset renewedAt,
        CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var affected = await dbContext.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE [yaf].[jobs]
            SET [started_at] = {renewedAt}
            WHERE [id] = {jobId}
              AND [type] = 'outline-generation'
              AND [status] = 'Running'
              AND [lease_id] = {leaseId}
            """, cancellationToken);
        return affected == 1;
    }
}
