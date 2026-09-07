using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace YoutubeAiFactory.Infrastructure.Persistence;

public sealed class YoutubeAiFactoryDbContextFactory
    : IDesignTimeDbContextFactory<YoutubeAiFactoryDbContext>
{
    public YoutubeAiFactoryDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Database")
            ?? "Host=localhost;Port=5432;Database=youtube_ai_factory;Username=yaf;Password=yaf_dev_password";

        var options = new DbContextOptionsBuilder<YoutubeAiFactoryDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new YoutubeAiFactoryDbContext(options);
    }
}
