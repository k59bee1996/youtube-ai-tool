namespace YoutubeAiFactory.Application.Competitors;

public sealed class CompetitorCollectionOptions
{
    public const int MaximumVideoLimit = 50;

    public int VideoLimit { get; init; } = 30;
}
