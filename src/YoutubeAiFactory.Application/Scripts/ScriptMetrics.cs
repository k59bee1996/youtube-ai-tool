using System.Text.RegularExpressions;

namespace YoutubeAiFactory.Application.Scripts;

public static partial class ScriptMetrics
{
    public static int CountWords(string text) => WordPattern().Count(text ?? string.Empty);

    public static int EstimateDurationSeconds(int wordCount, int wordsPerMinute)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(wordCount);
        ArgumentOutOfRangeException.ThrowIfLessThan(wordsPerMinute, 1);
        return Math.Max(1, (int)Math.Ceiling(wordCount * 60d / wordsPerMinute));
    }

    [GeneratedRegex(@"[\p{L}\p{N}]+(?:['’\-][\p{L}\p{N}]+)*", RegexOptions.CultureInvariant)]
    private static partial Regex WordPattern();
}
