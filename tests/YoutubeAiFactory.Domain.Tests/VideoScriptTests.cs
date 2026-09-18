using YoutubeAiFactory.Domain.Common;
using YoutubeAiFactory.Domain.Scripts;

namespace YoutubeAiFactory.Domain.Tests;

public sealed class VideoScriptTests
{
    [Fact]
    public void Narration_edit_invalidates_grounding_and_requires_revalidation_before_approval()
    {
        var script = Create();
        var editedAt = DateTimeOffset.UtcNow.AddMinutes(1);

        script.RecordNarrationEdit(625, 250, "[]", editedAt);

        Assert.Equal(VideoScriptStatus.Ready, script.Status);
        Assert.Equal(ScriptGroundingStatus.Pending, script.GroundingStatus);
        Assert.Null(script.GroundingAiRunId);
        Assert.Throws<DomainException>(() => script.Approve(editedAt.AddMinutes(1)));

        var auditRunId = Guid.NewGuid();
        script.RecordGroundingResult(ScriptGroundingStatus.Passed, auditRunId, "[]", editedAt.AddMinutes(2));
        script.Approve(editedAt.AddMinutes(3));

        Assert.Equal(VideoScriptStatus.Approved, script.Status);
        Assert.Equal(auditRunId, script.GroundingAiRunId);
    }

    [Fact]
    public void Approved_script_is_immutable()
    {
        var script = Create();
        script.Approve(DateTimeOffset.UtcNow);

        Assert.Throws<DomainException>(() => script.RecordNarrationEdit(600, 240, "[]", DateTimeOffset.UtcNow));
        Assert.Throws<DomainException>(() => script.RecordGroundingResult(ScriptGroundingStatus.Passed,
            Guid.NewGuid(), "[]", DateTimeOffset.UtcNow));
        Assert.Throws<DomainException>(() => script.Approve(DateTimeOffset.UtcNow));
    }

    [Theory]
    [InlineData(0, 60)]
    [InlineData(100, 0)]
    public void Rejects_non_positive_backend_metrics(int words, int seconds)
    {
        Assert.Throws<DomainException>(() => Create(words, seconds));
    }

    private static VideoScript Create(int words = 600, int seconds = 240) => new(Guid.NewGuid(), Guid.NewGuid(),
        Guid.NewGuid(), 1, Guid.NewGuid(), 1, 1, Guid.NewGuid(), Guid.NewGuid(), "script-engine:v1",
        "script-generation", 1, new string('a', 64), "fake", "premium-model", "English", words, seconds,
        "[]", "[]", DateTimeOffset.UtcNow);
}
