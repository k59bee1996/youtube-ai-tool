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
        Assert.Equal("Reasoning", run.ModelProfile);
    }

    [Fact]
    public void Completion_preserves_token_subcategories_without_double_counting()
    {
        var startedAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var run = new AiRun("script", Guid.NewGuid(), "provider", "model", "script", 1, startedAt);

        run.Complete(100, 80, 0.25m, startedAt.AddSeconds(1), cachedInputTokens: 40, reasoningTokens: 20,
            currency: "usd", pricingVersion: "fixture-v1", pricingEffectiveFrom: startedAt);

        Assert.Equal(100, run.InputTokens);
        Assert.Equal(80, run.OutputTokens);
        Assert.Equal(40, run.CachedInputTokens);
        Assert.Equal(20, run.ReasoningTokens);
        Assert.Equal(AiCostSource.PriceCalculated, run.CostSource);
        Assert.Equal("USD", run.Currency);
        Assert.Equal("fixture-v1", run.PricingVersion);
    }

    [Fact]
    public void Completion_without_cost_keeps_cost_unavailable()
    {
        var startedAt = DateTimeOffset.UtcNow;
        var run = new AiRun("research", Guid.NewGuid(), "provider", "model", "research", 1, startedAt);

        run.Complete(10, 5, null, startedAt.AddSeconds(1));

        Assert.Equal(AiCostSource.Unavailable, run.CostSource);
        Assert.Null(run.Currency);
        Assert.Null(run.ProviderReportedCost);
        Assert.Null(run.CalculatedEstimatedCost);
    }
}
