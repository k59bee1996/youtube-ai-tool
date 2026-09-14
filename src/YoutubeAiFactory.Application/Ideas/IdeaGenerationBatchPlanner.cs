namespace YoutubeAiFactory.Application.Ideas;

/// <summary>Splits one idea-generation target into bounded provider requests.</summary>
public static class IdeaGenerationBatchPlanner
{
    public static IReadOnlyList<int> CreateInitialBatchSizes(IdeaGenerationOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (options.TargetIdeaCount is < 1 or > 30)
            throw new ArgumentOutOfRangeException(nameof(options), "TargetIdeaCount must be between 1 and 30.");
        if (options.IdeasPerRequest is < 1 or > 30)
            throw new ArgumentOutOfRangeException(nameof(options), "IdeasPerRequest must be between 1 and 30.");

        var batches = new List<int>();
        for (var remaining = options.TargetIdeaCount; remaining > 0; remaining -= options.IdeasPerRequest)
            batches.Add(Math.Min(options.IdeasPerRequest, remaining));
        return batches;
    }
}
