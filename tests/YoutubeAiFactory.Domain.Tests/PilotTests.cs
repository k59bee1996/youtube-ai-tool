using YoutubeAiFactory.Domain.Common;
using YoutubeAiFactory.Domain.Pilots;

namespace YoutubeAiFactory.Domain.Tests;

public sealed class PilotTests
{
    [Fact]
    public void Draft_pilot_can_be_approved_once()
    {
        var pilot = CreatePilot(); var now = DateTimeOffset.UtcNow;
        pilot.Approve(now);
        Assert.Equal(PilotStatus.Approved, pilot.Status); Assert.Equal(now, pilot.ApprovedAt);
        Assert.Throws<DomainException>(() => pilot.Approve(now));
    }

    [Fact]
    public void Pilot_video_rejects_sequences_outside_the_twelve_slot_invariant()
    {
        Assert.Throws<DomainException>(() => new PilotVideo(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 13, PilotExperimentType.Topic, "Hypothesis", "Variable", "Control", "Metric", "Signal", "Rationale"));
    }

    [Fact]
    public void Slots_can_swap_contents_only_inside_the_same_experiment_block()
    {
        var pilotId = Guid.NewGuid(); var first = CreateVideo(pilotId, 1, PilotExperimentType.Topic); var second = CreateVideo(pilotId, 2, PilotExperimentType.Topic); var original = first.VideoIdeaId;
        first.SwapContentsWith(second);
        Assert.NotEqual(original, first.VideoIdeaId);
        Assert.Throws<DomainException>(() => first.SwapContentsWith(CreateVideo(pilotId, 5, PilotExperimentType.Packaging)));
    }

    private static Pilot CreatePilot() => new(Guid.NewGuid(), 1, Guid.NewGuid(), "pilot-generation", 1, "fake", "fake", "pilot-planning:v1", "Pilot", "Learn", "[]", "[]", "[]", 12, DateTimeOffset.UtcNow);
    private static PilotVideo CreateVideo(Guid pilotId, int sequence, PilotExperimentType type) => new(pilotId, Guid.NewGuid(), Guid.NewGuid(), sequence, type, "Hypothesis", "Variable", "Control", "Metric", "Signal", "Rationale");
}
