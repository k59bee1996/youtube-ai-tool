using YoutubeAiFactory.Application.AI;

namespace YoutubeAiFactory.Application.Tests;

public sealed class LlmRequestTests
{
    [Fact]
    public void Constructor_requires_a_versioned_prompt()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new LlmRequest(
            "competitor-analysis",
            0,
            "Return structured JSON.",
            "Analyze these inputs."));
    }

    [Fact]
    public void Constructor_preserves_provider_configuration()
    {
        var configuration = new Dictionary<string, string>
        {
            ["temperature"] = "0.2",
        };

        var request = new LlmRequest(
            "competitor-analysis",
            1,
            "Return structured JSON.",
            "Analyze these inputs.",
            configuration);

        Assert.Equal("0.2", request.ModelConfiguration["temperature"]);
    }
}
