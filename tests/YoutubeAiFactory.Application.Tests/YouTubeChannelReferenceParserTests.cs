using YoutubeAiFactory.Application.Common;
using YoutubeAiFactory.Application.Competitors;
using YoutubeAiFactory.Tests.Fixtures;

namespace YoutubeAiFactory.Application.Tests;

public sealed class YouTubeChannelReferenceParserTests
{
    [Theory]
    [InlineData("https://www.youtube.com/@example", YouTubeChannelReferenceKind.Handle, "example")]
    [InlineData("https://youtube.com/@example/", YouTubeChannelReferenceKind.Handle, "example")]
    [InlineData("https://M.YOUTUBE.COM/@Example_Name?view_as=subscriber", YouTubeChannelReferenceKind.Handle, "Example_Name")]
    [InlineData("https://youtube.com/@%E6%BC%A2", YouTubeChannelReferenceKind.Handle, "漢")]
    [InlineData("https://youtube.com/@%E6%9D%B1%E4%BA%AC", YouTubeChannelReferenceKind.Handle, "東京")]
    [InlineData("https://youtube.com/@creator%C2%B7studio", YouTubeChannelReferenceKind.Handle, "creator·studio")]
    [InlineData("https://youtube.com/@Cafe%CC%81", YouTubeChannelReferenceKind.Handle, "Café")]
    [InlineData(
        "https://www.youtube.com/channel/UC1234567890abcdefghij12",
        YouTubeChannelReferenceKind.ChannelId,
        YouTubeTestData.ChannelId)]
    [InlineData(
        "https://www.youtube.com/channel/UC1234567890abcdefghij12/?feature=shared",
        YouTubeChannelReferenceKind.ChannelId,
        YouTubeTestData.ChannelId)]
    public void Parse_returns_supported_reference(
        string input,
        YouTubeChannelReferenceKind expectedKind,
        string expectedValue)
    {
        var result = YouTubeChannelReferenceParser.Parse(input);

        Assert.Equal(expectedKind, result.Kind);
        Assert.Equal(expectedValue, result.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("random text")]
    [InlineData("youtube.com/@example")]
    [InlineData("https://example.com/@example")]
    [InlineData("https://youtube.com.attacker.com/@example")]
    [InlineData("https://notyoutube.com/@example")]
    [InlineData("https://www.youtube.com/watch?v=a1b2c3d4e5F")]
    [InlineData("https://www.youtube.com/c/example")]
    [InlineData("https://www.youtube.com/channel/not-a-channel-id")]
    [InlineData("https://youtube.com/@.example")]
    [InlineData("https://youtube.com/@example-")]
    [InlineData("https://youtube.com/@example%0A")]
    [InlineData("https://youtube.com/@example%0D")]
    [InlineData("https://youtube.com/@abcdefghijklmnopqrstuvwxyzabcde")]
    public void Parse_rejects_invalid_or_unsupported_input(string input)
    {
        Assert.Throws<ApplicationValidationException>(() =>
            YouTubeChannelReferenceParser.Parse(input));
    }

    [Fact]
    public void Parse_rejects_oversized_url_before_resolution()
    {
        var input = $"https://youtube.com/@example?value={new string('a', 2_048)}";

        var exception = Assert.Throws<ApplicationValidationException>(() =>
            YouTubeChannelReferenceParser.Parse(input));

        Assert.Contains("2048", exception.Message, StringComparison.Ordinal);
    }
}
