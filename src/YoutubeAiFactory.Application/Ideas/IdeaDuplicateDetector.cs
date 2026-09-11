namespace YoutubeAiFactory.Application.Ideas;

public static class IdeaDuplicateDetector
{
    public static decimal Similarity(string first, string second)
    {
        var a = Tokens(first); var b = Tokens(second);
        if (a.Count == 0 || b.Count == 0) return 0;
        return decimal.Divide(a.Intersect(b).Count(), a.Union(b).Count());
    }
    public static bool IsNearDuplicate(VideoIdeaCandidateResult candidate, VideoIdeaCandidateResult other, decimal threshold) =>
        Similarity(candidate.WorkingTitle, other.WorkingTitle) >= threshold ||
        Normalize(candidate.Topic) == Normalize(other.Topic) && Normalize(candidate.Angle) == Normalize(other.Angle) && Normalize(candidate.ContentFormat) == Normalize(other.ContentFormat);
    public static bool IsNearDuplicate(VideoIdeaCandidateResult candidate, ExistingIdeaContext other, decimal threshold) =>
        Similarity(candidate.WorkingTitle, other.WorkingTitle) >= threshold ||
        Normalize(candidate.Topic) == Normalize(other.Topic) && Normalize(candidate.Angle) == Normalize(other.Angle) && Normalize(candidate.ContentFormat) == Normalize(other.ContentFormat);
    public static bool IsNearCompetitorCopy(VideoIdeaCandidateResult candidate, IEnumerable<string> competitorTitles, decimal threshold) => competitorTitles.Any(title => Similarity(candidate.WorkingTitle, title) >= threshold);
    private static HashSet<string> Tokens(string value) => value.ToLowerInvariant().Split([' ', '-', '_', ':', ',', '.', '!', '?', '(', ')'], StringSplitOptions.RemoveEmptyEntries).Where(token => token.Length > 1).ToHashSet();
    private static string Normalize(string value) => string.Concat(value.ToLowerInvariant().Where(char.IsLetterOrDigit));
}
