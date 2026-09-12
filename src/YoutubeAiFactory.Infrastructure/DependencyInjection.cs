using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using YoutubeAiFactory.Application.AI;
using YoutubeAiFactory.Application.Competitors;
using YoutubeAiFactory.Application.Persistence;
using YoutubeAiFactory.Infrastructure.AI;
using YoutubeAiFactory.Infrastructure.Persistence;
using YoutubeAiFactory.Infrastructure.YouTube;

namespace YoutubeAiFactory.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is required.");

        services.AddDbContextFactory<YoutubeAiFactoryDbContext>(options =>
            options.UseSqlServer(connectionString, sqlServer =>
            {
                sqlServer.MigrationsAssembly(typeof(YoutubeAiFactoryDbContext).Assembly.FullName);
                sqlServer.EnableRetryOnFailure();
            }));

        services.AddScoped<IYoutubeAiFactoryStore, YoutubeAiFactoryStore>();
        services.Configure<YouTubeOptions>(configuration.GetSection(YouTubeOptions.SectionName));
        services.Configure<AiOptions>(configuration.GetSection(AiOptions.SectionName));
        services.AddHttpClient<ILlmProvider, OpenAiLlmProvider>(client => client.BaseAddress = new Uri("https://api.openai.com/v1/"));
        services.AddHttpClient<IYouTubeClient, YouTubeClient>(client =>
        {
            client.BaseAddress = new Uri("https://www.googleapis.com/youtube/v3/");
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        return services;
    }
}
