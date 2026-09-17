using YoutubeAiFactory.Application.Common;
using YoutubeAiFactory.Application.Outlines;
using YoutubeAiFactory.Domain.Outlines;
using YoutubeAiFactory.Domain.Pilots;
using YoutubeAiFactory.Domain.Research;

namespace YoutubeAiFactory.Application.Tests;

public sealed class OutlineValidatorTests
{
    private readonly OutlineValidator _validator = new(new OutlineOptions { MinOutlineSections = 4, MaxOutlineSections = 12 });

    [Fact]
    public void Accepts_grounded_contiguous_outline_and_calculates_soft_warnings()
    {
        var fixture = Fixture();

        var warnings = _validator.ValidateGenerated(ValidResult(fixture.SupportedClaimId), fixture.Context);

        Assert.Contains(warnings, warning => warning.Contains("research gap", StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData(1, 2, 4, 5, 6, 7)]
    [InlineData(1, 1, 2, 3, 4, 5)]
    [InlineData(0, 1, 2, 3, 4, 5)]
    public void Rejects_non_contiguous_or_duplicate_sequences(int one, int two, int three, int four, int five, int six)
    {
        var fixture = Fixture();
        var result = ValidResult(fixture.SupportedClaimId);
        result = result with { Sections = result.Sections.Select((section, index) => section with { Sequence = new[] { one, two, three, four, five, six }[index] }).ToArray() };

        var error = Assert.Throws<StructuredOutputException>(() => _validator.ValidateGenerated(result, fixture.Context));

        Assert.Contains("contiguous", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Rejects_fabricated_or_out_of_context_claim_identifier()
    {
        var fixture = Fixture();
        var result = ValidResult(Guid.NewGuid());

        var error = Assert.Throws<StructuredOutputException>(() => _validator.ValidateGenerated(result, fixture.Context));

        Assert.Contains("outside", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Rejects_undefined_controlled_vocabulary_values()
    {
        var fixture = Fixture();
        var result = ValidResult(fixture.SupportedClaimId);
        result = result with
        {
            Sections = result.Sections.Select((section, index) => index == 1
                ? section with { Purpose = (OutlineSectionPurpose)999 }
                : section).ToArray(),
        };

        var error = Assert.Throws<StructuredOutputException>(() => _validator.ValidateGenerated(result, fixture.Context));

        Assert.Contains("purpose", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Rejects_unsupported_claim_used_as_factual_support()
    {
        var fixture = Fixture(includeUnsupportedInContext: true);
        var result = ValidResult(fixture.UnsupportedClaimId);

        var error = Assert.Throws<StructuredOutputException>(() => _validator.ValidateGenerated(result, fixture.Context));

        Assert.Contains("Unsupported", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Conflicted_claim_requires_matching_conflict_in_the_same_section()
    {
        var fixture = Fixture();
        var result = ValidResult(fixture.ConflictedClaimId);

        var error = Assert.Throws<StructuredOutputException>(() => _validator.ValidateGenerated(result, fixture.Context));

        Assert.Contains("conflict", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Storytelling_rise_and_fall_requires_meaningful_structural_implementation()
    {
        var fixture = Fixture(experimentType: PilotExperimentType.Storytelling, variable: "Rise-and-Fall narrative");
        var result = ValidResult(fixture.SupportedClaimId);

        var error = Assert.Throws<StructuredOutputException>(() => _validator.ValidateGenerated(result, fixture.Context));

        Assert.Contains("rise-and-fall", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Accepts_rise_and_fall_only_when_strategy_and_sections_implement_it()
    {
        var fixture = Fixture(experimentType: PilotExperimentType.Storytelling, variable: "Rise-and-Fall narrative");
        var basic = ValidResult(fixture.SupportedClaimId);
        var result = basic with
        {
            NarrativeStrategy = basic.NarrativeStrategy with { StructureType = OutlineStructureType.RiseAndFall },
            Sections =
            [
                basic.Sections[0],
                basic.Sections[1] with { Purpose = OutlineSectionPurpose.Escalation },
                basic.Sections[2],
                basic.Sections[3] with { Purpose = OutlineSectionPurpose.Counterpoint },
                basic.Sections[4] with { Purpose = OutlineSectionPurpose.Payoff },
                basic.Sections[5],
            ],
        };

        _validator.ValidateGenerated(result, fixture.Context);
    }

    [Fact]
    public void Topic_experiment_rejects_a_storytelling_confound_when_structure_must_remain_comparable()
    {
        var fixture = Fixture(experimentType: PilotExperimentType.Topic, variable: "Castle ownership topic");
        var basic = ValidResult(fixture.SupportedClaimId);
        var result = basic with
        {
            NarrativeStrategy = basic.NarrativeStrategy with { StructureType = OutlineStructureType.MysteryReveal },
        };

        var error = Assert.Throws<StructuredOutputException>(() => _validator.ValidateGenerated(result, fixture.Context));

        Assert.Contains("confound", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Requires_a_mid_outline_pattern_interrupt_and_a_final_cta()
    {
        var fixture = Fixture();
        var basic = ValidResult(fixture.SupportedClaimId);
        var withoutPattern = basic with
        {
            Sections = basic.Sections.Where(section => section.Purpose != OutlineSectionPurpose.PatternInterrupt)
                .Select((section, index) => section with { Sequence = index + 1 }).ToArray(),
        };

        var patternError = Assert.Throws<StructuredOutputException>(() => _validator.ValidateGenerated(withoutPattern, fixture.Context));
        Assert.Contains("PatternInterrupt", patternError.Message, StringComparison.OrdinalIgnoreCase);

        var nonFinalCta = basic with
        {
            Sections = basic.Sections.Select(section => section.Purpose == OutlineSectionPurpose.CTA
                ? section with { Sequence = 5 }
                : section.Purpose == OutlineSectionPurpose.Conclusion ? section with { Sequence = 6 }
                : section).ToArray(),
        };
        var ctaError = Assert.Throws<StructuredOutputException>(() => _validator.ValidateGenerated(nonFinalCta, fixture.Context));
        Assert.Contains("final CTA", ctaError.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static OutlineGenerationResult ValidResult(Guid claimId) => new(
        new(OutlineStructureType.Explainer, "What made ownership costly?", "Prestige carried recurring obligations.",
            "Open with the contradiction between visible wealth and hidden cost.",
            "Establish ownership, explain recurring obligations, test the premise, then synthesize.",
            "The viewer understands ownership as an ongoing economic system.", "Build steadily toward the synthesis."),
        new("Preserves the pilot's packaging premise while keeping claims qualified.", []),
        [
            new(1, "The contradiction", OutlineSectionPurpose.Hook, "Frame the central question.",
                "Introduce visible prestige versus hidden obligations without stating new facts.", "What did ownership really require?",
                "Move from the question to necessary context.", [], [], [], 45),
            Section(2, OutlineSectionPurpose.Context, claimId),
            new(3, "Reset the question", OutlineSectionPurpose.PatternInterrupt, "Refresh the open question.",
                "Plan a concise contrast without final narration.", "What remains unclear?", "Return to the mechanism.", [], [], [], 20),
            Section(4, OutlineSectionPurpose.Explanation, claimId),
            Section(5, OutlineSectionPurpose.Conclusion, claimId),
            new(6, "Next step", OutlineSectionPurpose.CTA, "Plan a next-viewer action.",
                "Reserve CTA intent only; do not write spoken copy.", null, null, [], [], [], 20),
        ]);

    private static OutlineSectionResult Section(int sequence, OutlineSectionPurpose purpose, Guid claimId) =>
        new(sequence, $"Section {sequence}", purpose, "Explain the evidence's narrative role.",
            "Use only the referenced research claim and preserve its qualification.", "How does this change the premise?",
            "Carry the remaining question into the next section.", [new(claimId, OutlineClaimUsageRole.Supporting)], [], [], 60);

    private static TestFixture Fixture(bool includeUnsupportedInContext = false,
        PilotExperimentType experimentType = PilotExperimentType.Packaging, string variable = "Hidden-cost framing")
    {
        var supported = Guid.NewGuid();
        var conflicted = Guid.NewGuid();
        var unsupported = Guid.NewGuid();
        var conflict = Guid.NewGuid();
        var claims = new List<OutlineContextClaim>
        {
            new(supported, "Ownership required continuing obligations.", "Fact", ResearchClaimSupportStatus.Corroborated.ToString(), true, 90, []),
            new(conflicted, "Annual estimates vary.", "Estimate", ResearchClaimSupportStatus.Conflicted.ToString(), false, 60, []),
        };
        if (includeUnsupportedInContext)
            claims.Add(new(unsupported, "Every owner was bankrupted.", "Fact", ResearchClaimSupportStatus.Unsupported.ToString(), true, 10, []));
        var context = new OutlineGenerationContext(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1, "research", "outline",
            "The Economics of Owning a Medieval Castle", "Castle economics", "Hidden costs", "Explainer", "History viewers",
            "Understand the real ownership cost", "Reveal hidden obligations", "English", "Global", experimentType,
            "Test the selected pilot variable", variable, "Keep other variables comparable", "CTR", "CTR improves",
            "The report explains recurring ownership obligations.", [], claims, [], [new(conflict, conflicted, "Sources disagree on the estimate.", false)],
            [new(0, "No reliable universal annual figure exists.", [conflicted])], [], []);
        return new(context, supported, conflicted, unsupported);
    }

    private sealed record TestFixture(OutlineGenerationContext Context, Guid SupportedClaimId,
        Guid ConflictedClaimId, Guid UnsupportedClaimId);
}
