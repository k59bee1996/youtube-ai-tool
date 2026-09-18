using YoutubeAiFactory.Application.Common;
using YoutubeAiFactory.Domain.Research;
using YoutubeAiFactory.Domain.Scripts;

namespace YoutubeAiFactory.Application.Scripts;

public sealed record ScriptValidationMetrics(int TotalWordCount, int EstimatedDurationSeconds,
    IReadOnlyDictionary<(int Section, int Block), int> BlockWordCounts,
    IReadOnlyDictionary<int, int> SectionWordCounts, IReadOnlyList<string> Warnings);

public sealed class ScriptValidator(ScriptOptions options)
{
    public ScriptValidationMetrics ValidateGenerated(VideoScriptResult result, ScriptGenerationContext context)
    {
        ScriptGenerationContextBuilder.ValidateOptions(options);
        if (result.Sections is null) throw Invalid("Script output is missing sections.");
        Required(result.ClosingPayoff, "closing payoff", 4_000);
        if (result.Sections.Count != context.Sections.Count)
            throw Invalid("Script must contain exactly one section for every approved Outline section.");
        if (!result.Sections.Select(item => item.Sequence).Order().SequenceEqual(Enumerable.Range(1, result.Sections.Count)))
            throw Invalid("Script section sequences must be unique and contiguous from 1.");

        var expectedSections = context.Sections.ToDictionary(item => item.Sequence);
        var blockWords = new Dictionary<(int Section, int Block), int>();
        var sectionWords = new Dictionary<int, int>();
        foreach (var section in result.Sections.OrderBy(item => item.Sequence))
        {
            if (!expectedSections.TryGetValue(section.Sequence, out var expected) ||
                expected.OutlineSectionId != section.OutlineSectionId)
                throw Invalid("Script sections must map to the approved Outline in its exact order.");
            if (section.Blocks is null || section.Blocks.Count is < 1 || section.Blocks.Count > options.MaxBlocksPerSection)
                throw Invalid($"Each Script section must contain between 1 and {options.MaxBlocksPerSection} blocks.");
            if (!section.Blocks.Select(item => item.Sequence).Order().SequenceEqual(Enumerable.Range(1, section.Blocks.Count)))
                throw Invalid("Script block sequences must be unique and contiguous from 1.");
            var claims = expected.Claims.ToDictionary(item => item.Id);
            var conflicts = expected.Conflicts.ToDictionary(item => item.Id);
            var sectionTotal = 0;
            foreach (var block in section.Blocks.OrderBy(item => item.Sequence))
            {
                if (!Enum.IsDefined(block.Type)) throw Invalid("Script block type is invalid.");
                Required(block.Text, "block narration", options.MaxNarrationCharactersPerBlock);
                if (block.ClaimIds is null || block.ConflictIds is null)
                    throw Invalid("Script block references are required.");
                if (block.ClaimIds.Distinct().Count() != block.ClaimIds.Count ||
                    block.ConflictIds.Distinct().Count() != block.ConflictIds.Count)
                    throw Invalid("Script block references cannot contain duplicates.");
                if (block.Type is ScriptBlockType.FactualNarration or ScriptBlockType.ConflictExplanation or ScriptBlockType.Quote &&
                    block.ClaimIds.Count == 0)
                    throw Invalid("Factual, conflict, and quote narration must reference a ResearchClaim.");
                foreach (var claimId in block.ClaimIds)
                {
                    if (!claims.TryGetValue(claimId, out var claim))
                        throw Invalid("Script referenced a Claim outside its approved Outline section.");
                    if (claim.SupportStatus == ResearchClaimSupportStatus.Unsupported.ToString())
                        throw Invalid("Unsupported ResearchClaims cannot support factual narration.");
                    if (claim.SupportStatus == ResearchClaimSupportStatus.Conflicted.ToString() &&
                        !block.ConflictIds.Any(id => conflicts.TryGetValue(id, out var conflict) && conflict.ClaimId == claimId))
                        throw Invalid("A conflicted Claim must retain its ResearchConflict reference in the same Script block.");
                }
                foreach (var conflictId in block.ConflictIds)
                {
                    if (!conflicts.TryGetValue(conflictId, out var conflict) || !block.ClaimIds.Contains(conflict.ClaimId))
                        throw Invalid("Script referenced an invalid conflict or separated it from its Claim.");
                }
                var count = ScriptMetrics.CountWords(block.Text);
                if (count == 0) throw Invalid("Script blocks must contain spoken narration.");
                blockWords[(section.Sequence, block.Sequence)] = count;
                sectionTotal += count;
            }
            sectionWords[section.Sequence] = sectionTotal;
        }

        var total = sectionWords.Values.Sum();
        var duration = ScriptMetrics.EstimateDurationSeconds(total, options.PlanningWordsPerMinute);
        var warnings = new List<string>();
        if (total < context.MinimumWordCount)
            warnings.Add($"Script is shorter than the configured target range ({total} words; minimum {context.MinimumWordCount}).");
        if (total > context.MaximumWordCount)
            warnings.Add($"Script is longer than the configured target range ({total} words; maximum {context.MaximumWordCount}).");
        return new(total, duration, blockWords, sectionWords, warnings);
    }

    public static void ValidateAudit(ScriptGroundingAuditResult audit, VideoScriptResult script,
        ScriptGenerationContext context)
    {
        if (audit.Issues is null || audit.Status == ScriptGroundingStatus.Pending)
            throw Invalid("Grounding audit output is incomplete.");
        var blocks = script.Sections.SelectMany(section => section.Blocks.Select(block =>
            (Section: section.Sequence, Block: block.Sequence, Value: block)))
            .ToDictionary(item => (item.Section, item.Block));
        var validClaims = context.Sections.SelectMany(item => item.Claims).Select(item => item.Id).ToHashSet();
        foreach (var issue in audit.Issues)
        {
            if (!Enum.IsDefined(issue.IssueType) || !Enum.IsDefined(issue.Severity))
                throw Invalid("Grounding issue taxonomy is invalid.");
            if (!blocks.TryGetValue((issue.SectionSequence, issue.BlockSequence), out var block))
                throw Invalid("Grounding audit referenced a block outside the Script.");
            Required(issue.ProblematicText, "grounding problematic text", 4_000);
            Required(issue.Explanation, "grounding explanation", 4_000);
            if (issue.RelevantClaimIds is null || issue.RelevantClaimIds.Distinct().Count() != issue.RelevantClaimIds.Count ||
                issue.RelevantClaimIds.Any(id => !validClaims.Contains(id) || !block.Value.ClaimIds.Contains(id)))
                throw Invalid("Grounding audit referenced a Claim outside the audited block.");
        }
        var hasErrors = audit.Issues.Any(item => item.Severity == ScriptGroundingIssueSeverity.Error);
        if (audit.Status == ScriptGroundingStatus.Passed && hasErrors ||
            audit.Status == ScriptGroundingStatus.Failed && !hasErrors)
            throw Invalid("Grounding status does not match the structured issues.");
    }

    private static void Required(string? value, string name, int maximumLength)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > maximumLength)
            throw Invalid($"Script {name} is required and must be at most {maximumLength} characters.");
    }

    private static StructuredOutputException Invalid(string message) => new(message);
}
