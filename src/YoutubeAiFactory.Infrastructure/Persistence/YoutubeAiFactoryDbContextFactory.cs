using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace YoutubeAiFactory.Infrastructure.Persistence;

public sealed class YoutubeAiFactoryDbContextFactory
    : IDesignTimeDbContextFactory<YoutubeAiFactoryDbContext>
{
    public YoutubeAiFactoryDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? throw new InvalidOperationException(
                "ConnectionStrings__DefaultConnection is required to run EF Core design-time commands.");

        var options = new DbContextOptionsBuilder<YoutubeAiFactoryDbContext>()
            .UseSqlServer(connectionString, sqlServer =>
            {
                sqlServer.MigrationsAssembly(typeof(YoutubeAiFactoryDbContext).Assembly.FullName);
                sqlServer.EnableRetryOnFailure();
            })
            .Options;

        return new YoutubeAiFactoryDbContext(options);
    }
}
