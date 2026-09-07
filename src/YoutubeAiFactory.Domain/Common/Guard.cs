namespace YoutubeAiFactory.Domain.Common;

internal static class Guard
{
    public static string Required(string value, string parameterName, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException($"{parameterName} is required.");
        }

        var trimmed = value.Trim();
        if (trimmed.Length > maxLength)
        {
            throw new DomainException($"{parameterName} must be {maxLength} characters or fewer.");
        }

        return trimmed;
    }

    public static Guid NotEmpty(Guid value, string parameterName)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException($"{parameterName} is required.");
        }

        return value;
    }
}
