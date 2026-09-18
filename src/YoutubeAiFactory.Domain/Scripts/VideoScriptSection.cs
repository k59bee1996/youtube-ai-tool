using YoutubeAiFactory.Domain.Common;

namespace YoutubeAiFactory.Domain.Scripts;

public sealed class VideoScriptSection
{
    private VideoScriptSection() { }

    public VideoScriptSection(Guid videoScriptId, Guid videoOutlineSectionId, int sequence, string heading,
        int wordCount, int estimatedDurationSeconds)
    {
        Id = Guid.NewGuid();
        VideoScriptId = Guard.NotEmpty(videoScriptId, nameof(videoScriptId));
        VideoOutlineSectionId = Guard.NotEmpty(videoOutlineSectionId, nameof(videoOutlineSectionId));
        if (sequence < 1) throw new DomainException("Script section sequence must be positive.");
        Sequence = sequence;
        Heading = Guard.Required(heading, nameof(heading), 500);
        SetMetrics(wordCount, estimatedDurationSeconds);
    }

    public Guid Id { get; private set; }
    public Guid VideoScriptId { get; private set; }
    public Guid VideoOutlineSectionId { get; private set; }
    public int Sequence { get; private set; }
    public string Heading { get; private set; } = string.Empty;
    public int WordCount { get; private set; }
    public int EstimatedDurationSeconds { get; private set; }

    public void SetMetrics(int wordCount, int estimatedDurationSeconds)
    {
        if (wordCount < 1 || estimatedDurationSeconds < 1)
            throw new DomainException("Script section metrics must be positive.");
        WordCount = wordCount;
        EstimatedDurationSeconds = estimatedDurationSeconds;
    }
}
