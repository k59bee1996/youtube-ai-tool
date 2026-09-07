using YoutubeAiFactory.Domain.AI;

namespace YoutubeAiFactory.Domain.Tests;

public sealed class AiRunTests
{
    [Fact]
    public void Completion_records_usage_and_latency()
    {
        var startedAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var run = new AiRun(
            "competitor-analysis",
            Guid.NewGuid(),
            "provider",
            "model",
            "competitor-analysis",
            1,
            startedAt);

        run.RecordRetry();
        run.Complete(100, 50, 0.0123m, startedAt.AddMilliseconds(750));

        Assert.Equal(AiRunStatus.Succeeded, run.Status);
        Assert.Equal(1, run.RetryCount);
        Assert.Equal(750, run.LatencyMilliseconds);
        Assert.Equal(0.0123m, run.EstimatedCost);
    }
}
