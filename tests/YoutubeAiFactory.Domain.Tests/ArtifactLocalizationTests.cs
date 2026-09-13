using YoutubeAiFactory.Domain.Localization;

namespace YoutubeAiFactory.Domain.Tests;

public sealed class ArtifactLocalizationTests
{
    [Fact]
    public void Localization_is_bound_to_a_specific_artifact_version_and_locale()
    {
        var localization = new ArtifactLocalization(
            LocalizableArtifactTypes.CompetitorAnalysis,
            Guid.NewGuid(),
            3,
            "VI",
            "{}",
            Guid.NewGuid(),
            "competitor-analysis-localization",
            1,
            "Fake",
            "fake-model",
            DateTimeOffset.UtcNow);

        Assert.Equal(3, localization.ArtifactVersion);
        Assert.Equal("vi", localization.Locale);
        Assert.Equal("{}", localization.ContentJson);
    }
}
