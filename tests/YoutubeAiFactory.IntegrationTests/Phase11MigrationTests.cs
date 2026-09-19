using Microsoft.EntityFrameworkCore;
using YoutubeAiFactory.Infrastructure.Persistence;

namespace YoutubeAiFactory.IntegrationTests;

public sealed class Phase11MigrationTests
{
    [SqlServerFact]
    public async Task Migration_creates_relational_production_contract_and_active_job_fence()
    {
        await using var context = new YoutubeAiFactoryDbContext(
            SqlServerPersistenceTests.CreateOptions()
        );
        await context.Database.EnsureDeletedAsync();
        try
        {
            await context.Database.MigrateAsync();
            var tables = await context
                .Database.SqlQueryRaw<int>(
                    """
                    SELECT COUNT(*) AS [Value] FROM sys.tables
                    WHERE schema_id = SCHEMA_ID('yaf') AND name LIKE 'production_%'
                    """
                )
                .SingleAsync();
            var mappingIndex = await context
                .Database.SqlQueryRaw<int>(
                    """
                    SELECT COUNT(*) AS [Value] FROM sys.indexes
                    WHERE name = 'IX_production_scene_script_blocks_production_package_id_script_block_id'
                      AND is_unique = 1
                    """
                )
                .SingleAsync();
            var jobFence = await context
                .Database.SqlQueryRaw<int>(
                    """
                    SELECT COUNT(*) AS [Value] FROM sys.indexes
                    WHERE name = 'ux_jobs_active_video_project_workflow'
                      AND filter_definition LIKE '%production-package%'
                    """
                )
                .SingleAsync();

            Assert.Equal(9, tables);
            Assert.Equal(1, mappingIndex);
            Assert.Equal(1, jobFence);
        }
        finally
        {
            await context.Database.EnsureDeletedAsync();
        }
    }
}
