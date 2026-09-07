namespace YoutubeAiFactory.IntegrationTests;

public sealed class PostgresFactAttribute : FactAttribute
{
    public PostgresFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("YAF_TEST_POSTGRES")))
        {
            Skip = "Set YAF_TEST_POSTGRES to run PostgreSQL integration tests.";
        }
    }
}
