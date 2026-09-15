using YoutubeAiFactory.Application.Research;
using YoutubeAiFactory.Application.Common;

namespace YoutubeAiFactory.Application.Tests;

public sealed class ResearchUrlCanonicalizerTests
{
    [Fact]
    public void Removes_tracking_and_fragment_without_removing_meaningful_query_parameters()
    {
        var result = ResearchUrlCanonicalizer.Canonicalize("https://Example.org/report?year=1300&utm_source=newsletter#section");

        Assert.Equal("https://example.org/report?year=1300", result);
    }

    [Theory]
    [InlineData("file:///etc/passwd")]
    [InlineData("ftp://example.org/item")]
    [InlineData("not a url")]
    public void Rejects_non_public_web_schemes(string value) => Assert.Throws<ArgumentException>(() => ResearchUrlCanonicalizer.Canonicalize(value));

    [Fact]
    public void Preserves_a_retryable_search_failure_when_no_source_urls_are_available()
    {
        var result = ResearchSearchFailurePolicy.GetRetryableNoResultsFailure([
            new ExternalServiceException("Rate limited", ExternalServiceFailure.QuotaExceeded),
            new ExternalServiceException("Provider unavailable", ExternalServiceFailure.Transient),
        ]);

        Assert.NotNull(result);
        Assert.Equal(ExternalServiceFailure.QuotaExceeded, result.Failure);
    }

    [Fact]
    public void Does_not_retry_a_no_results_response_with_only_permanent_failures()
    {
        var result = ResearchSearchFailurePolicy.GetRetryableNoResultsFailure([
            new ExternalServiceException("Bad key", ExternalServiceFailure.Authentication),
        ]);

        Assert.Null(result);
    }
}
