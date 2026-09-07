using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using YoutubeAiFactory.Application.Common;
using YoutubeAiFactory.Domain.Common;

namespace YoutubeAiFactory.Api.Errors;

internal sealed class ApiExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    private static readonly Action<ILogger, Exception?> LogUnexpectedError =
        LoggerMessage.Define(
            LogLevel.Error,
            new EventId(1, nameof(LogUnexpectedError)),
            "An unhandled API error occurred.");

    private static readonly Action<ILogger, ExternalServiceFailure, Exception?> LogExternalServiceError =
        LoggerMessage.Define<ExternalServiceFailure>(
            LogLevel.Warning,
            new EventId(2, nameof(LogExternalServiceError)),
            "YouTube request failed with classification {Failure}.");

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (status, title, detail) = Map(exception);
        if (status == StatusCodes.Status500InternalServerError)
        {
            LogUnexpectedError(logger, exception);
        }
        else if (exception is ExternalServiceException externalServiceException)
        {
            LogExternalServiceError(logger, externalServiceException.Failure, externalServiceException);
        }

        httpContext.Response.StatusCode = status;
        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = new ProblemDetails
            {
                Status = status,
                Title = title,
                Detail = detail,
                Type = $"https://httpstatuses.com/{status}",
            },
            Exception = exception,
        });
    }

    private static (int Status, string Title, string Detail) Map(Exception exception) => exception switch
    {
        BadHttpRequestException badHttpRequestException =>
            (badHttpRequestException.StatusCode, "Invalid request", badHttpRequestException.Message),
        ApplicationValidationException or DomainException =>
            (StatusCodes.Status400BadRequest, "Validation failed", exception.Message),
        ResourceNotFoundException =>
            (StatusCodes.Status404NotFound, "Resource not found", exception.Message),
        ResourceConflictException =>
            (StatusCodes.Status409Conflict, "Resource conflict", exception.Message),
        ExternalServiceException { Failure: ExternalServiceFailure.QuotaExceeded } =>
            (StatusCodes.Status429TooManyRequests, "YouTube quota unavailable", exception.Message),
        ExternalServiceException
        {
            Failure: ExternalServiceFailure.Configuration or ExternalServiceFailure.Authentication,
        } => (StatusCodes.Status503ServiceUnavailable, "YouTube integration unavailable", exception.Message),
        ExternalServiceException { Failure: ExternalServiceFailure.Transient } =>
            (StatusCodes.Status503ServiceUnavailable, "YouTube temporarily unavailable", exception.Message),
        ExternalServiceException =>
            (StatusCodes.Status502BadGateway, "YouTube request failed", exception.Message),
        _ =>
            (StatusCodes.Status500InternalServerError, "Unexpected server error", "An unexpected error occurred."),
    };
}
