using YoutubeAiFactory.Domain.Common;
using YoutubeAiFactory.Domain.Jobs;
using DomainJobStatus = YoutubeAiFactory.Domain.Jobs.JobStatus;

namespace YoutubeAiFactory.Domain.Tests;

public sealed class JobTests
{
    [Fact]
    public void Retryable_failure_schedules_a_bounded_retry()
    {
        var queuedAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var job = new Job("youtube.collect", "{\"channelId\":\"abc\"}", queuedAt, maxRetries: 1);

        job.Start(queuedAt);
        job.Fail("rate limited", retryable: true, queuedAt.AddSeconds(1), queuedAt.AddMinutes(1));

        Assert.Equal(DomainJobStatus.Retrying, job.Status);
        Assert.Equal(1, job.RetryCount);
        Assert.Equal(queuedAt.AddMinutes(1), job.AvailableAt);

        job.Start(queuedAt.AddMinutes(1));
        job.Fail("rate limited again", retryable: true, queuedAt.AddMinutes(1).AddSeconds(1));

        Assert.Equal(DomainJobStatus.Failed, job.Status);
        Assert.NotNull(job.CompletedAt);
    }

    [Fact]
    public void Complete_requires_a_running_job()
    {
        var job = new Job("youtube.collect", "{}", DateTimeOffset.UtcNow);

        Assert.Throws<DomainException>(() => job.Complete(DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Running_job_lease_can_be_renewed_only_by_its_owner()
    {
        var startedAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var renewedAt = startedAt.AddMinutes(5);
        var leaseId = Guid.NewGuid();
        var job = new Job("video-research", "{}", startedAt);

        job.Start(startedAt, leaseId);
        job.RenewLease(leaseId, renewedAt);

        Assert.Equal(renewedAt, job.StartedAt);
        Assert.Equal(startedAt, job.ExecutionStartedAt);
        Assert.Throws<DomainException>(() => job.RenewLease(Guid.NewGuid(), renewedAt.AddMinutes(1)));
    }

    [Fact]
    public void Retry_attempt_can_replace_its_payload_only_while_running()
    {
        var queuedAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var job = new Job("video-research", "{\"researchRunId\":\"first\"}", queuedAt, maxRetries: 1);

        job.Start(queuedAt);
        job.ReplacePayloadForRetry("{\"researchRunId\":\"second\"}");
        job.Fail("temporary failure", retryable: true, queuedAt.AddSeconds(1), queuedAt.AddMinutes(1));

        Assert.Contains("second", job.Payload, StringComparison.Ordinal);
        Assert.Equal(DomainJobStatus.Retrying, job.Status);
        Assert.Throws<DomainException>(() => job.ReplacePayloadForRetry("{}"));
    }
}
