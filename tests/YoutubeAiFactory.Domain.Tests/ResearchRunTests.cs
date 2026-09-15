using YoutubeAiFactory.Domain.Common;
using YoutubeAiFactory.Domain.Research;

namespace YoutubeAiFactory.Domain.Tests;

public sealed class ResearchRunTests
{
    [Fact]
    public void A_failed_execution_attempt_does_not_become_a_report()
    {
        var now = DateTimeOffset.UtcNow;
        var run = new ResearchRun(Guid.NewGuid(), Guid.NewGuid(), "research-engine:v1", "ABC", now);

        run.Start(now.AddSeconds(1));
        run.Fail("No usable source was retrieved.", new ResearchRunMetrics(2, 0, 0, 0, 0, 0, 0, 0, 2), now.AddSeconds(2));

        Assert.Equal(ResearchRunStatus.Failed, run.Status);
        Assert.Null(run.ResearchReportId);
        Assert.Throws<DomainException>(() => run.Complete(Guid.NewGuid(), new ResearchRunMetrics(1, 1, 1, 1, 1, 1, 0, 0, 0), now.AddSeconds(3)));
    }
}
