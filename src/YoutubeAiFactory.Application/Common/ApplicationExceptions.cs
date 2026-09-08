namespace YoutubeAiFactory.Application.Common;

public abstract class YoutubeAiFactoryException : Exception
{
    protected YoutubeAiFactoryException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}

public sealed class ApplicationValidationException(string message) : YoutubeAiFactoryException(message);

public sealed class ResourceNotFoundException(string message) : YoutubeAiFactoryException(message);

public sealed class ResourceConflictException(string message) : YoutubeAiFactoryException(message);

public sealed class StructuredOutputException(string message, Exception? innerException = null)
    : YoutubeAiFactoryException(message, innerException);

public enum ExternalServiceFailure
{
    Configuration,
    Authentication,
    QuotaExceeded,
    Transient,
    UnexpectedResponse,
}

public sealed class ExternalServiceException(
    string message,
    ExternalServiceFailure failure,
    Exception? innerException = null) : YoutubeAiFactoryException(message, innerException)
{
    public ExternalServiceFailure Failure { get; } = failure;
}
