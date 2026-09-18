using YoutubeAiFactory.Application.Outlines;
using YoutubeAiFactory.Application.Scripts;
using YoutubeAiFactory.Domain.Outlines;
using YoutubeAiFactory.Domain.Research;
using YoutubeAiFactory.Domain.Scripts;
using YoutubeAiFactory.Domain.Videos;

namespace YoutubeAiFactory.Application.Tests;

internal sealed class ScriptTestFixture
{
    public ScriptTestFixture()
    {
        Base = new OutlineTestFixture();
        var now = DateTimeOffset.UtcNow;
        var fingerprint = OutlineGenerationContextBuilder.CreateFingerprint(Base.Project, Base.VideoProject,
            Base.Source, Base.Report);
        Outline = new VideoOutline(Base.Project.Id, Base.VideoProject.Id, Base.Report.Id, Base.Report.Version, 1,
            Guid.NewGuid(), "outline-engine:v1", "outline-generation", 1, fingerprint, "fake", "reasoning-model",
            OutlineStructureType.Explainer, "What did castle ownership really require?",
            "Visible prestige depended on continuing obligations.", "Open on prestige versus hidden obligations.",
            Base.VideoProject.ViewerPromise, "Move from the visible symbol to recurring costs and uncertainty.",
            "Ownership was an operating system, not a one-time purchase.", "Build through evidence, then qualify.",
            Base.VideoProject.ExperimentType, Base.VideoProject.VariableBeingTested,
            Base.PilotVideo.ControlStrategy, "Preserve the packaging test without changing the topic or evidence.",
            "[]", "[]", 240, now);

        Sections =
        [
            Section(1, "The contradiction", OutlineSectionPurpose.Hook),
            Section(2, "The operating burden", OutlineSectionPurpose.Context),
            Section(3, "What records cannot prove", OutlineSectionPurpose.Counterpoint),
            Section(4, "The real ownership model", OutlineSectionPurpose.Payoff),
        ];
        SectionClaims =
        [
            new(Sections[0].Id, Base.SupportedClaim.Id, OutlineClaimUsageRole.Core),
            new(Sections[1].Id, Base.CorroboratedClaim.Id, OutlineClaimUsageRole.Supporting),
            new(Sections[2].Id, Base.ConflictedClaim.Id, OutlineClaimUsageRole.Conflict),
            new(Sections[3].Id, Base.SupportedClaim.Id, OutlineClaimUsageRole.Supporting),
        ];
        SectionConflicts = [new(Sections[2].Id, Base.Conflict.Id)];
        SectionGaps = [new(Sections[2].Id, 0)];
        Outline.Approve(now);
        Details = new(Outline, Sections, SectionClaims, SectionConflicts, SectionGaps);

        Base.VideoProject.TransitionTo(VideoProjectStatus.OutlineGenerating, now);
        Base.VideoProject.TransitionTo(VideoProjectStatus.OutlineReady, now);
        Base.VideoProject.TransitionTo(VideoProjectStatus.OutlineApproved, now);
        Context = new ScriptGenerationContextBuilder(Options).Build(Base.Project, Base.VideoProject, Base.Source,
            Details, Base.Research);
    }

    public OutlineTestFixture Base { get; }
    public ScriptOptions Options { get; } = new()
    {
        PlanningWordsPerMinute = 150,
        MinimumLengthRatio = 0.5m,
        MaximumLengthRatio = 1.5m,
    };
    public VideoOutline Outline { get; }
    public VideoOutlineSection[] Sections { get; }
    public VideoOutlineSectionClaim[] SectionClaims { get; }
    public VideoOutlineSectionConflict[] SectionConflicts { get; }
    public VideoOutlineSectionGap[] SectionGaps { get; }
    public VideoOutlineWithDetails Details { get; }
    public ScriptGenerationContext Context { get; }

    public VideoScriptResult ValidResult()
    {
        var words = string.Join(' ', Enumerable.Repeat("grounded", 75));
        return new(Context.Sections.Select(section =>
        {
            var claim = section.Claims.Single();
            var conflicts = section.Conflicts.Select(item => item.Id).ToArray();
            var type = conflicts.Length == 0 ? ScriptBlockType.FactualNarration : ScriptBlockType.ConflictExplanation;
            return new ScriptSectionResult(section.OutlineSectionId, section.Sequence,
            [new ScriptBlockResult(1, type, words, [claim.Id], conflicts)]);
        }).ToArray(), "Ownership depended on continuing obligations, with exact costs varying by context.");
    }

    private VideoOutlineSection Section(int sequence, string heading, OutlineSectionPurpose purpose) =>
        new(Outline.Id, sequence, heading, purpose, "Advance the approved narrative purpose.",
            "Use only the research assigned to this section.", "What does the evidence establish?",
            "Carry the remaining question forward.", 60);
}
