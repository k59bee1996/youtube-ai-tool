using YoutubeAiFactory.Domain.Projects;

namespace YoutubeAiFactory.Application.Projects;

public sealed record ProjectDto(
    Guid Id,
    string Name,
    string MarketName,
    string TargetLanguage,
    string TargetGeography,
    string AudienceDescription,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt)
{
    internal static ProjectDto FromDomain(Project project) => new(
        project.Id,
        project.Name,
        project.Market.Name,
        project.Market.TargetLanguage,
        project.Market.TargetGeography,
        project.Audience.Description,
        project.CreatedAt,
        project.UpdatedAt);
}
