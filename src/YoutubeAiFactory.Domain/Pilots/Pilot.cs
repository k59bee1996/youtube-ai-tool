using YoutubeAiFactory.Domain.Common;

namespace YoutubeAiFactory.Domain.Pilots;

/// <summary>A versioned, deliberately limited twelve-video learning plan; it is not a production workflow.</summary>
public sealed class Pilot
{
    private Pilot() { }

    public Pilot(Guid projectId, int version, Guid aiRunId, string promptKey, int promptVersion,
        string provider, string model, string planningAlgorithmVersion, string name, string objective,
        string assumptionsJson, string limitationsJson, string warningsJson, int eligibleIdeaCount, DateTimeOffset createdAt)
    {
        if (version < 1 || promptVersion < 1 || eligibleIdeaCount < 12)
            throw new DomainException("Pilot version, prompt version, and eligible idea count are invalid.");
        Id = Guid.NewGuid(); ProjectId = Guard.NotEmpty(projectId, nameof(projectId)); Version = version;
        AiRunId = Guard.NotEmpty(aiRunId, nameof(aiRunId)); PromptKey = Guard.Required(promptKey, nameof(promptKey), 100);
        PromptVersion = promptVersion; Provider = Guard.Required(provider, nameof(provider), 100); Model = Guard.Required(model, nameof(model), 100);
        PlanningAlgorithmVersion = Guard.Required(planningAlgorithmVersion, nameof(planningAlgorithmVersion), 100);
        Name = Guard.Required(name, nameof(name), 300); Objective = Guard.Required(objective, nameof(objective), 4000);
        AssumptionsJson = Guard.Required(assumptionsJson, nameof(assumptionsJson), 20_000);
        LimitationsJson = Guard.Required(limitationsJson, nameof(limitationsJson), 20_000);
        WarningsJson = Guard.Required(warningsJson, nameof(warningsJson), 20_000);
        EligibleIdeaCount = eligibleIdeaCount; Status = PilotStatus.Draft; Revision = 0; CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }
    public Guid ProjectId { get; private set; }
    public int Version { get; private set; }
    public Guid AiRunId { get; private set; }
    public string PromptKey { get; private set; } = string.Empty;
    public int PromptVersion { get; private set; }
    public string Provider { get; private set; } = string.Empty;
    public string Model { get; private set; } = string.Empty;
    public string PlanningAlgorithmVersion { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string Objective { get; private set; } = string.Empty;
    public string AssumptionsJson { get; private set; } = "[]";
    public string LimitationsJson { get; private set; } = "[]";
    public string WarningsJson { get; private set; } = "[]";
    public int EligibleIdeaCount { get; private set; }
    public int Revision { get; private set; }
    public PilotStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? ApprovedAt { get; private set; }

    public void Approve(DateTimeOffset approvedAt)
    {
        if (Status != PilotStatus.Draft) throw new DomainException("Only a draft pilot can be approved.");
        Status = PilotStatus.Approved; ApprovedAt = approvedAt; Revision++;
    }

    public void UpdateWarnings(string warningsJson)
    {
        if (Status != PilotStatus.Draft) throw new DomainException("Only a draft pilot can be changed.");
        WarningsJson = Guard.Required(warningsJson, nameof(warningsJson), 20_000); Revision++;
    }

    public void RecordDraftChange()
    {
        if (Status != PilotStatus.Draft) throw new DomainException("Only a draft pilot can be changed.");
        Revision++;
    }
}
