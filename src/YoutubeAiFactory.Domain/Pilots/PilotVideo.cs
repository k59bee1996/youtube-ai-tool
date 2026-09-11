using YoutubeAiFactory.Domain.Common;

namespace YoutubeAiFactory.Domain.Pilots;

/// <summary>One explicit experiment slot in a pilot. Its idea remains the source of content metadata.</summary>
public sealed class PilotVideo
{
    private PilotVideo() { }

    public PilotVideo(Guid pilotId, Guid videoIdeaId, Guid opportunityId, int sequence, PilotExperimentType experimentType,
        string hypothesis, string variableBeingTested, string controlStrategy, string primaryMetric,
        string successSignal, string rationale, string? secondaryMetricsJson = null, string? notes = null)
    {
        if (sequence is < 1 or > 12) throw new DomainException("Pilot sequence must be between 1 and 12.");
        if (experimentType != ExpectedExperimentType(sequence))
            throw new DomainException("Pilot experiment blocks must be Topic 1-4, Packaging 5-8, and Storytelling 9-12.");
        Id = Guid.NewGuid(); PilotId = Guard.NotEmpty(pilotId, nameof(pilotId)); VideoIdeaId = Guard.NotEmpty(videoIdeaId, nameof(videoIdeaId));
        OpportunityId = Guard.NotEmpty(opportunityId, nameof(opportunityId)); Sequence = sequence; ExperimentType = experimentType;
        Hypothesis = Guard.Required(hypothesis, nameof(hypothesis), 4000); VariableBeingTested = Guard.Required(variableBeingTested, nameof(variableBeingTested), 2000);
        ControlStrategy = Guard.Required(controlStrategy, nameof(controlStrategy), 2000); PrimaryMetric = Guard.Required(primaryMetric, nameof(primaryMetric), 500);
        SuccessSignal = Guard.Required(successSignal, nameof(successSignal), 2000); Rationale = Guard.Required(rationale, nameof(rationale), 4000);
        SecondaryMetricsJson = secondaryMetricsJson ?? "[]"; Notes = notes;
    }

    public Guid Id { get; private set; }
    public Guid PilotId { get; private set; }
    public Guid VideoIdeaId { get; private set; }
    public Guid OpportunityId { get; private set; }
    public int Sequence { get; private set; }
    public PilotExperimentType ExperimentType { get; private set; }
    public string Hypothesis { get; private set; } = string.Empty;
    public string VariableBeingTested { get; private set; } = string.Empty;
    public string ControlStrategy { get; private set; } = string.Empty;
    public string PrimaryMetric { get; private set; } = string.Empty;
    public string SuccessSignal { get; private set; } = string.Empty;
    public string Rationale { get; private set; } = string.Empty;
    public string SecondaryMetricsJson { get; private set; } = "[]";
    public string? Notes { get; private set; }

    public void Replace(Guid videoIdeaId, Guid opportunityId, string hypothesis, string variableBeingTested,
        string controlStrategy, string primaryMetric, string successSignal, string rationale)
    {
        VideoIdeaId = Guard.NotEmpty(videoIdeaId, nameof(videoIdeaId)); OpportunityId = Guard.NotEmpty(opportunityId, nameof(opportunityId));
        Hypothesis = Guard.Required(hypothesis, nameof(hypothesis), 4000); VariableBeingTested = Guard.Required(variableBeingTested, nameof(variableBeingTested), 2000);
        ControlStrategy = Guard.Required(controlStrategy, nameof(controlStrategy), 2000); PrimaryMetric = Guard.Required(primaryMetric, nameof(primaryMetric), 500);
        SuccessSignal = Guard.Required(successSignal, nameof(successSignal), 2000); Rationale = Guard.Required(rationale, nameof(rationale), 4000);
        SecondaryMetricsJson = "[]";
        Notes = null;
    }

    public void SwapContentsWith(PilotVideo other)
    {
        if (other is null || PilotId != other.PilotId || ExperimentType != other.ExperimentType)
            throw new DomainException("Pilot slots can only be reordered within the same experiment block.");
        (VideoIdeaId, other.VideoIdeaId) = (other.VideoIdeaId, VideoIdeaId);
        (OpportunityId, other.OpportunityId) = (other.OpportunityId, OpportunityId);
        (Hypothesis, other.Hypothesis) = (other.Hypothesis, Hypothesis);
        (VariableBeingTested, other.VariableBeingTested) = (other.VariableBeingTested, VariableBeingTested);
        (ControlStrategy, other.ControlStrategy) = (other.ControlStrategy, ControlStrategy);
        (PrimaryMetric, other.PrimaryMetric) = (other.PrimaryMetric, PrimaryMetric);
        (SuccessSignal, other.SuccessSignal) = (other.SuccessSignal, SuccessSignal);
        (Rationale, other.Rationale) = (other.Rationale, Rationale);
        (SecondaryMetricsJson, other.SecondaryMetricsJson) = (other.SecondaryMetricsJson, SecondaryMetricsJson);
        (Notes, other.Notes) = (other.Notes, Notes);
    }

    private static PilotExperimentType ExpectedExperimentType(int sequence) =>
        sequence <= 4 ? PilotExperimentType.Topic :
        sequence <= 8 ? PilotExperimentType.Packaging : PilotExperimentType.Storytelling;
}
