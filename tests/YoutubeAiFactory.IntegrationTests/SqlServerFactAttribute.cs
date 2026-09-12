namespace YoutubeAiFactory.IntegrationTests;

public sealed class SqlServerFactAttribute : FactAttribute
{
    public SqlServerFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("YAF_TEST_SQLSERVER")))
        {
            Skip = "Set YAF_TEST_SQLSERVER to run SQL Server integration tests.";
        }
    }
}
