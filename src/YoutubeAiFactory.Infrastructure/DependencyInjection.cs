using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using YoutubeAiFactory.Application.AI;
using YoutubeAiFactory.Application.Competitors;
using YoutubeAiFactory.Application.Persistence;
using YoutubeAiFactory.Application.Observability;
using YoutubeAiFactory.Application.Research;
using YoutubeAiFactory.Infrastructure.AI;
using YoutubeAiFactory.Infrastructure.Persistence;
using YoutubeAiFactory.Infrastructure.Research;
using YoutubeAiFactory.Infrastructure.YouTube;
using YoutubeAiFactory.Infrastructure.Observability;

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
        services.AddScoped<IObservabilityQueries, SqlServerObservabilityQueries>();
        services.AddSingleton<IVideoResearchJobLeaseRenewer, VideoResearchJobLeaseRenewer>();
        services.AddSingleton<IVideoOutlineJobLeaseRenewer, VideoOutlineJobLeaseRenewer>();
        services.AddSingleton<IVideoScriptJobLeaseRenewer, VideoScriptJobLeaseRenewer>();
        services.AddSingleton<IProductionPackageJobLeaseRenewer, ProductionPackageJobLeaseRenewer>();
        services.Configure<YouTubeOptions>(configuration.GetSection(YouTubeOptions.SectionName));
        services.Configure<AiOptions>(configuration.GetSection(AiOptions.SectionName));
        services.Configure<ResearchSearchOptions>(configuration.GetSection(ResearchSearchOptions.SectionName));
        services.Configure<ResearchFetchOptions>(configuration.GetSection(ResearchFetchOptions.SectionName));
        services.AddSingleton<IAiModelResolver, ConfigurationAiModelResolver>();
        services.AddHttpClient<ILlmProvider, OpenAiLlmProvider>(client =>
        {
            client.BaseAddress = new Uri("https://api.openai.com/v1/");
            // OpenAiLlmProvider enforces the resolved model's timeout per request.
            // The HttpClient default (100 seconds) would otherwise cancel longer workflows first.
            client.Timeout = System.Threading.Timeout.InfiniteTimeSpan;
        });
        services.AddHttpClient<IYouTubeClient, YouTubeClient>(client =>
        {
            client.BaseAddress = new Uri("https://www.googleapis.com/youtube/v3/");
            client.Timeout = TimeSpan.FromSeconds(30);
        });
        services.AddHttpClient<IResearchSearchClient, TavilyResearchSearchClient>(client => client.Timeout = Timeout.InfiniteTimeSpan);
        services.AddHttpClient<IResearchContentFetcher, HttpResearchContentFetcher>(client => client.Timeout = Timeout.InfiniteTimeSpan)
            .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
            {
                AllowAutoRedirect = false,
                UseProxy = false,
                ConnectCallback = ResearchUrlSafetyPolicy.ConnectAsync,
            });

        return services;
    }
}
