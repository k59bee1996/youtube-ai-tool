using YoutubeAiFactory.Domain.Common;

namespace YoutubeAiFactory.Domain.Outlines;

public sealed class VideoOutlineSection
{
    private VideoOutlineSection() { }

    public VideoOutlineSection(Guid videoOutlineId, int sequence, string heading, OutlineSectionPurpose purpose,
        string objective, string summary, string? viewerQuestion, string? transitionIntent, int? estimatedSeconds)
    {
        Id = Guid.NewGuid();
        VideoOutlineId = Guard.NotEmpty(videoOutlineId, nameof(videoOutlineId));
        if (!Enum.IsDefined(purpose)) throw new DomainException("Outline section purpose is invalid.");
        SetSequence(sequence);
        Purpose = purpose;
        UpdatePlanningContent(heading, objective, summary, viewerQuestion, transitionIntent, estimatedSeconds);
    }

    public Guid Id { get; private set; }
    public Guid VideoOutlineId { get; private set; }
    public int Sequence { get; private set; }
    public string Heading { get; private set; } = string.Empty;
    public OutlineSectionPurpose Purpose { get; private set; }
    public string Objective { get; private set; } = string.Empty;
    public string Summary { get; private set; } = string.Empty;
    public string? ViewerQuestion { get; private set; }
    public string? TransitionIntent { get; private set; }
    public int? EstimatedSeconds { get; private set; }

    public void UpdatePlanningContent(string heading, string objective, string summary, string? viewerQuestion,
        string? transitionIntent, int? estimatedSeconds)
    {
        if (estimatedSeconds is < 15 or > 1_800)
            throw new DomainException("Section timing must be between 15 and 1800 seconds when provided.");
        Heading = Guard.Required(heading, nameof(heading), 500);
        Objective = Guard.Required(objective, nameof(objective), 1_000);
        Summary = Guard.Required(summary, nameof(summary), 2_000);
        ViewerQuestion = Optional(viewerQuestion, 1_000);
        TransitionIntent = Optional(transitionIntent, 1_000);
        EstimatedSeconds = estimatedSeconds;
    }

    public void SetSequence(int sequence)
    {
        if (sequence < 1) throw new DomainException("Outline section sequence must be positive.");
        Sequence = sequence;
    }

    private static string? Optional(string? value, int maxLength) =>
        string.IsNullOrWhiteSpace(value) ? null : Guard.Required(value, nameof(value), maxLength);
}
