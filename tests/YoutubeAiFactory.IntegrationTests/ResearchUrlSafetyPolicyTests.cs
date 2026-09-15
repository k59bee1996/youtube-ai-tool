using YoutubeAiFactory.Infrastructure.Research;

namespace YoutubeAiFactory.IntegrationTests;

public sealed class ResearchUrlSafetyPolicyTests
{
    [Theory]
    [InlineData("http://localhost/source")]
    [InlineData("http://127.0.0.1/source")]
    [InlineData("http://10.0.0.1/source")]
    [InlineData("http://100.64.0.1/source")]
    [InlineData("http://172.16.0.1/source")]
    [InlineData("http://192.168.1.1/source")]
    [InlineData("http://192.0.2.1/source")]
    [InlineData("http://198.18.0.1/source")]
    [InlineData("http://198.51.100.1/source")]
    [InlineData("http://203.0.113.1/source")]
    [InlineData("http://224.0.0.1/source")]
    [InlineData("http://169.254.169.254/latest/meta-data")]
    [InlineData("http://[::1]/source")]
    [InlineData("http://[fe80::1]/source")]
    [InlineData("http://[fd00::1]/source")]
    [InlineData("file:///etc/passwd")]
    public async Task Blocks_non_public_or_non_http_source_urls(string value)
    {
        Assert.False(await ResearchUrlSafetyPolicy.IsAllowedAsync(new Uri(value), CancellationToken.None));
    }

    [Fact]
    public async Task Allows_a_globally_routable_literal_address() =>
        Assert.True(await ResearchUrlSafetyPolicy.IsAllowedAsync(new Uri("https://8.8.8.8/source"), CancellationToken.None));
}
