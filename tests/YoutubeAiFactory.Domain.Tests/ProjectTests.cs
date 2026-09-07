using YoutubeAiFactory.Domain.Common;
using YoutubeAiFactory.Domain.Projects;

namespace YoutubeAiFactory.Domain.Tests;

public sealed class ProjectTests
{
    [Fact]
    public void Constructor_creates_project_with_normalized_settings()
    {
        var createdAt = new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero);

        var project = new Project(
            "  Creator Science  ",
            new Market("Productivity", "English", "United States"),
            new AudienceProfile("Busy knowledge workers"),
            createdAt);

        Assert.NotEqual(Guid.Empty, project.Id);
        Assert.Equal("Creator Science", project.Name);
        Assert.Equal("Productivity", project.Market.Name);
        Assert.Equal(createdAt, project.CreatedAt);
    }

    [Fact]
    public void Constructor_rejects_missing_audience_description()
    {
        var exception = Assert.Throws<DomainException>(() => new AudienceProfile(" "));

        Assert.Equal("description is required.", exception.Message);
    }
}
