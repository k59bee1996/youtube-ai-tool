using YoutubeAiFactory.Domain.Common;

namespace YoutubeAiFactory.Domain.Research;

/// <summary>Immutable successful evidence-backed research artifact for one VideoProject.</summary>
public sealed class ResearchReport
{
    private ResearchReport() { }

    public ResearchReport(Guid projectId, Guid videoProjectId, Guid researchRunId, int version,
        string algorithmVersion, string inputFingerprint, string resultJson, Guid? synthesisAiRunId, DateTimeOffset createdAt,
        Guid? id = null)
    {
        if (version < 1) throw new DomainException("Research report version must be positive.");
        Id = id is { } suppliedId && suppliedId != Guid.Empty ? suppliedId : Guid.NewGuid();
        ProjectId = Guard.NotEmpty(projectId, nameof(projectId));
        VideoProjectId = Guard.NotEmpty(videoProjectId, nameof(videoProjectId));
        ResearchRunId = Guard.NotEmpty(researchRunId, nameof(researchRunId));
        Version = version;
        ResearchAlgorithmVersion = Guard.Required(algorithmVersion, nameof(algorithmVersion), 100);
        InputFingerprint = Guard.Required(inputFingerprint, nameof(inputFingerprint), 128);
        ResultJson = Guard.Required(resultJson, nameof(resultJson), 200_000);
        SynthesisAiRunId = synthesisAiRunId;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }
    public Guid ProjectId { get; private set; }
    public Guid VideoProjectId { get; private set; }
    public Guid ResearchRunId { get; private set; }
    public int Version { get; private set; }
    public string ResearchAlgorithmVersion { get; private set; } = string.Empty;
    public string InputFingerprint { get; private set; } = string.Empty;
    public string ResultJson { get; private set; } = string.Empty;
    public Guid? SynthesisAiRunId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
}
