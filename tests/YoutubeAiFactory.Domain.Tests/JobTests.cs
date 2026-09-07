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
}
