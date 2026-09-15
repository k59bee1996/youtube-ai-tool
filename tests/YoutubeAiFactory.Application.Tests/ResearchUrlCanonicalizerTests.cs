using YoutubeAiFactory.Application.Research;

namespace YoutubeAiFactory.Application.Tests;

public sealed class ResearchUrlCanonicalizerTests
{
    [Fact]
    public void Removes_tracking_and_fragment_without_removing_meaningful_query_parameters()
    {
        var result = ResearchUrlCanonicalizer.Canonicalize("https://Example.org/report?year=1300&utm_source=newsletter#section");

        Assert.Equal("https://example.org/report?year=1300", result);
    }

    [Theory]
    [InlineData("file:///etc/passwd")]
    [InlineData("ftp://example.org/item")]
    [InlineData("not a url")]
    public void Rejects_non_public_web_schemes(string value) => Assert.Throws<ArgumentException>(() => ResearchUrlCanonicalizer.Canonicalize(value));
}
