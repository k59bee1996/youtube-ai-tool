using YoutubeAiFactory.Application.Common;

namespace YoutubeAiFactory.Application.Research;

public static class ResearchSearchFailurePolicy
{
    public static ExternalServiceException? GetRetryableNoResultsFailure(IEnumerable<ExternalServiceException> failures)
    {
        var transient = failures.FirstOrDefault(item => item.Failure == ExternalServiceFailure.Transient);
        var quota = failures.FirstOrDefault(item => item.Failure == ExternalServiceFailure.QuotaExceeded);
        var failure = quota ?? transient;
        return failure is null ? null : new ExternalServiceException("Research search is temporarily unavailable and returned no usable source URLs.", failure.Failure, failure);
    }
}
