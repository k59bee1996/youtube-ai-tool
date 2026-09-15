using YoutubeAiFactory.Infrastructure.Research;

namespace YoutubeAiFactory.IntegrationTests;

public sealed class ResearchUrlSafetyPolicyTests
{
    [Theory]
    [InlineData("http://localhost/source")]
    [InlineData("http://127.0.0.1/source")]
    [InlineData("http://10.0.0.1/source")]
    [InlineData("http://172.16.0.1/source")]
    [InlineData("http://192.168.1.1/source")]
    [InlineData("http://169.254.169.254/latest/meta-data")]
    [InlineData("http://[::1]/source")]
    [InlineData("http://[fe80::1]/source")]
    [InlineData("http://[fd00::1]/source")]
    [InlineData("file:///etc/passwd")]
    public async Task Blocks_non_public_or_non_http_source_urls(string value)
    {
        Assert.False(await ResearchUrlSafetyPolicy.IsAllowedAsync(new Uri(value), CancellationToken.None));
    }
}
