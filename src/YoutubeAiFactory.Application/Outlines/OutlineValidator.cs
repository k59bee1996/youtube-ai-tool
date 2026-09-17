using YoutubeAiFactory.Application.Common;
using YoutubeAiFactory.Domain.Outlines;
using YoutubeAiFactory.Domain.Pilots;
using YoutubeAiFactory.Domain.Research;

namespace YoutubeAiFactory.Application.Outlines;

public sealed class OutlineValidator(OutlineOptions options)
{
    public IReadOnlyList<string> ValidateGenerated(OutlineGenerationResult result, OutlineGenerationContext context)
    {
        OutlineGenerationContextBuilder.ValidateOptions(options);
        if (result.NarrativeStrategy is null || result.ExperimentAlignment is null || result.Sections is null)
            throw Invalid("Outline output is missing required structured members.");
        if (!Enum.IsDefined(result.NarrativeStrategy.StructureType))
            throw Invalid("Outline structure type is invalid.");
        Required(result.NarrativeStrategy.CoreQuestion, "core question", 1_000);
        Required(result.NarrativeStrategy.CoreTension, "core tension", 2_000);
        Required(result.NarrativeStrategy.OpeningHookConcept, "opening hook concept", 2_000);
        Required(result.NarrativeStrategy.NarrativeProgression, "narrative progression", 4_000);
        Required(result.NarrativeStrategy.Payoff, "payoff", 2_000);
        Required(result.NarrativeStrategy.PacingStrategy, "pacing strategy", 2_000);
        Required(result.ExperimentAlignment.HowOutlineImplementsExperiment, "experiment alignment", 4_000);
        if (result.ExperimentAlignment.RisksToExperimentIntegrity is null ||
            result.ExperimentAlignment.RisksToExperimentIntegrity.Count > 20 ||
            result.ExperimentAlignment.RisksToExperimentIntegrity.Any(item => string.IsNullOrWhiteSpace(item) || item.Length > 2_000))
            throw Invalid("Outline experiment risks are invalid.");
        if (result.Sections.Count < options.MinOutlineSections || result.Sections.Count > options.MaxOutlineSections)
            throw Invalid($"Outline must contain between {options.MinOutlineSections} and {options.MaxOutlineSections} sections.");
        if (!result.Sections.Select(item => item.Sequence).Order().SequenceEqual(Enumerable.Range(1, result.Sections.Count)))
            throw Invalid("Outline section sequences must be unique and contiguous from 1.");

        var claims = context.Claims.ToDictionary(item => item.Id);
        var conflicts = context.Conflicts.ToDictionary(item => item.Id);
        var gaps = context.ResearchGaps.Select(item => item.Index).ToHashSet();
        foreach (var section in result.Sections)
        {
            if (!Enum.IsDefined(section.Purpose)) throw Invalid("Outline section purpose is invalid.");
            Required(section.Heading, "section heading", 500);
            Required(section.Objective, "section objective", 1_000);
            Required(section.Summary, "section summary", 2_000);
            Optional(section.ViewerQuestion, "viewer question", 1_000);
            Optional(section.TransitionIntent, "transition intent", 1_000);
            if (section.EstimatedSeconds is < 15 or > 1_800)
                throw Invalid("Section duration estimates must be between 15 and 1800 seconds.");
            if (section.ClaimReferences is null || section.ConflictIds is null || section.ResearchGapIndexes is null)
                throw Invalid("Outline section references are required.");
            if (section.ClaimReferences.Select(item => (item.ClaimId, item.UsageRole)).Distinct().Count() != section.ClaimReferences.Count ||
                section.ConflictIds.Distinct().Count() != section.ConflictIds.Count ||
                section.ResearchGapIndexes.Distinct().Count() != section.ResearchGapIndexes.Count)
                throw Invalid("Outline section references cannot contain duplicates.");
            if (section.Purpose is not (OutlineSectionPurpose.Hook or OutlineSectionPurpose.Conclusion or
                OutlineSectionPurpose.PatternInterrupt or OutlineSectionPurpose.CTA) && section.ClaimReferences.Count == 0)
                throw Invalid("A factual outline section must reference at least one supplied ResearchClaim.");
            foreach (var reference in section.ClaimReferences)
            {
                if (!Enum.IsDefined(reference.UsageRole)) throw Invalid("Outline claim usage role is invalid.");
                if (!claims.TryGetValue(reference.ClaimId, out var claim))
                    throw Invalid("Outline referenced a claim outside its bounded ResearchReport context.");
                if (claim.SupportStatus == ResearchClaimSupportStatus.Unsupported.ToString())
                    throw Invalid("Unsupported ResearchClaims cannot be used as factual outline support.");
                if (claim.SupportStatus == ResearchClaimSupportStatus.Conflicted.ToString() &&
                    !section.ConflictIds.Any(id => conflicts.TryGetValue(id, out var conflict) && conflict.ClaimId == claim.Id))
                    throw Invalid("A conflicted claim must retain its ResearchConflict reference in the same section.");
            }
            foreach (var conflictId in section.ConflictIds)
            {
                if (!conflicts.TryGetValue(conflictId, out var conflict) ||
                    section.ClaimReferences.All(item => item.ClaimId != conflict.ClaimId))
                    throw Invalid("Outline referenced an invalid conflict or separated it from its claim.");
            }
            if (section.ResearchGapIndexes.Any(index => !gaps.Contains(index)))
                throw Invalid("Outline referenced a research gap outside its source ResearchReport.");
        }

        ValidateNarrativeRelease(result);
        ValidateExperiment(result, context);
        return BuildWarnings(result, context);
    }

    private static void ValidateNarrativeRelease(OutlineGenerationResult result)
    {
        var hookCount = result.Sections.Count(item => item.Purpose == OutlineSectionPurpose.Hook);
        if (hookCount == 0)
            throw Invalid("Outline must include a Hook section.");
        var ctas = result.Sections.Where(item => item.Purpose == OutlineSectionPurpose.CTA).ToArray();
        if (ctas.Length != 1 || ctas[0].Sequence != result.Sections.Count)
            throw Invalid("Outline must include exactly one final CTA planning section.");
        if (!result.Sections.Any(item => item.Purpose == OutlineSectionPurpose.PatternInterrupt &&
            item.Sequence > 1 && item.Sequence < result.Sections.Count))
            throw Invalid("Outline must include a mid-outline PatternInterrupt section.");
    }

    private static void ValidateExperiment(OutlineGenerationResult result, OutlineGenerationContext context)
    {
        var variable = Normalize(context.VariableBeingTested);
        var control = Normalize(context.ControlStrategy);
        if (context.ExperimentType == PilotExperimentType.Storytelling && variable.Contains("rise", StringComparison.Ordinal) &&
            variable.Contains("fall", StringComparison.Ordinal))
        {
            var purposes = result.Sections.Select(item => item.Purpose).ToHashSet();
            if (result.NarrativeStrategy.StructureType != OutlineStructureType.RiseAndFall ||
                !purposes.Contains(OutlineSectionPurpose.Escalation) ||
                !purposes.Overlaps([OutlineSectionPurpose.Counterpoint, OutlineSectionPurpose.Conflict]) ||
                !purposes.Overlaps([OutlineSectionPurpose.Payoff, OutlineSectionPurpose.Conclusion]))
                throw Invalid("The outline does not structurally implement the Pilot's rise-and-fall storytelling variable.");
        }
        if (context.ExperimentType == PilotExperimentType.Topic &&
            ContainsAny(control, "comparable", "consistent", "same structure", "standard") &&
            result.NarrativeStrategy.StructureType is OutlineStructureType.MysteryReveal or OutlineStructureType.Transformation)
            throw Invalid("The outline introduces a storytelling confound that conflicts with the Topic experiment control strategy.");
    }

    private static string[] BuildWarnings(OutlineGenerationResult result, OutlineGenerationContext context)
    {
        var warnings = new List<string>();
        warnings.AddRange(context.ResearchWarnings.Select(item => $"Research warning: {item}"));
        var timedSectionCount = result.Sections.Count(item => item.EstimatedSeconds.HasValue);
        if (timedSectionCount > 0 && timedSectionCount < result.Sections.Count)
            warnings.Add("Some Sections have no timing estimate; total estimated duration covers only timed Sections.");
        if (context.ResearchGaps.Count > 0)
            warnings.Add($"The source ResearchReport contains {context.ResearchGaps.Count} unresolved research gap(s); review referenced gaps before approval.");
        var critical = context.Claims.Where(item => item.IsCritical).Select(item => item.Id).ToHashSet();
        var referenced = result.Sections.SelectMany(item => item.ClaimReferences).Select(item => item.ClaimId).ToHashSet();
        if (critical.Count > 0 && !critical.Overlaps(referenced))
            warnings.Add("No critical ResearchClaim is referenced; verify that the narrative can fulfill its core question and viewer promise.");
        var sectionCoverage = result.Sections.Select(section => section.ClaimReferences.Count(item => critical.Contains(item.ClaimId))).ToArray();
        if (critical.Count >= 3 && sectionCoverage.Count(count => count > 0) == 1)
            warnings.Add("All referenced critical claims are concentrated in one section; review narrative balance.");
        var conclusion = result.Sections.FirstOrDefault(item => item.Purpose == OutlineSectionPurpose.Conclusion);
        if (conclusion is not null)
        {
            var earlier = result.Sections.Where(item => item.Sequence < conclusion.Sequence)
                .SelectMany(item => item.ClaimReferences).Select(item => item.ClaimId).ToHashSet();
            if (conclusion.ClaimReferences.Any(item => !earlier.Contains(item.ClaimId)))
                warnings.Add("The conclusion references research not introduced earlier; verify that it synthesizes rather than adds a late major fact.");
        }
        return warnings.Distinct(StringComparer.Ordinal).ToArray();
    }

    private static void Required(string? value, string name, int maximumLength)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > maximumLength)
            throw Invalid($"Outline {name} is required and must be at most {maximumLength} characters.");
    }

    private static void Optional(string? value, string name, int maximumLength)
    {
        if (value?.Length > maximumLength) throw Invalid($"Outline {name} must be at most {maximumLength} characters.");
    }

    private static string Normalize(string value) => value.Trim().ToLowerInvariant();
    private static bool ContainsAny(string value, params string[] candidates) => candidates.Any(value.Contains);
    private static StructuredOutputException Invalid(string message) => new(message);
}
