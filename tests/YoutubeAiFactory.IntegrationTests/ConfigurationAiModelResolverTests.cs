using Microsoft.Extensions.Options;
using YoutubeAiFactory.Application.AI;
using YoutubeAiFactory.Application.Common;
using YoutubeAiFactory.Infrastructure.AI;

namespace YoutubeAiFactory.IntegrationTests;

public sealed class ConfigurationAiModelResolverTests
{
    [Fact]
    public void Resolver_independently_maps_all_profiles_and_allows_a_shared_model()
    {
        var options = new AiOptions
        {
            Models = new Dictionary<string, AiModelProfileOptions>
            {
                ["Fast"] = new() { Model = "shared" },
                ["Reasoning"] = new() { Model = "shared" },
                ["Premium"] = new() { Model = "premium" },
            },
        };
        var resolver = new ConfigurationAiModelResolver(Options.Create(options));

        Assert.Equal("shared", resolver.Resolve(AiModelProfile.Fast).Model);
        Assert.Equal("shared", resolver.Resolve(AiModelProfile.Reasoning).Model);
        Assert.Equal("premium", resolver.Resolve(AiModelProfile.Premium).Model);
    }

    [Theory]
    [InlineData("", "OpenAI", "must specify a model")]
    [InlineData("model", "Unsupported", "unsupported provider")]
    public void Resolver_rejects_invalid_profile_configuration(string model, string provider, string expected)
    {
        var resolver = new ConfigurationAiModelResolver(Options.Create(new AiOptions
        {
            Models = new Dictionary<string, AiModelProfileOptions> { ["Premium"] = new() { Model = model, Provider = provider } },
        }));

        var exception = Assert.Throws<ApplicationValidationException>(() => resolver.Resolve(AiModelProfile.Premium));
        Assert.Contains(expected, exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Resolver_fails_clearly_when_a_required_profile_is_missing()
    {
        var resolver = new ConfigurationAiModelResolver(Options.Create(new AiOptions()));

        var exception = Assert.Throws<ApplicationValidationException>(() => resolver.Resolve(AiModelProfile.Premium));
        Assert.Equal("AI model profile 'Premium' is not configured.", exception.Message);
    }
}
