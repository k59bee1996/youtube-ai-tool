using Microsoft.EntityFrameworkCore;
using YoutubeAiFactory.Application.Persistence;

namespace YoutubeAiFactory.Infrastructure.Persistence;

internal sealed class ProductionPackageJobLeaseRenewer(
    IDbContextFactory<YoutubeAiFactoryDbContext> factory
) : IProductionPackageJobLeaseRenewer
{
    public async Task<bool> RenewAsync(
        Guid jobId,
        Guid leaseId,
        DateTimeOffset renewedAt,
        CancellationToken cancellationToken
    )
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        return await db.Database.ExecuteSqlInterpolatedAsync(
                $"""UPDATE [yaf].[jobs] SET [started_at] = {renewedAt} WHERE [id] = {jobId} AND [type] = 'production-package' AND [status] = 'Running' AND [lease_id] = {leaseId}""",
                cancellationToken
            ) == 1;
    }
}
