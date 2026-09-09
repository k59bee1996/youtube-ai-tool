using YoutubeAiFactory.Application.Common;
using YoutubeAiFactory.Application.Opportunities;

namespace YoutubeAiFactory.Application.Tests;

public sealed class OpportunityAnalysisTests
{
    [Fact]
    public void Scoring_is_deterministic_bounded_and_penalizes_widespread_competition()
    {
        var first = Guid.NewGuid();
        var second = Guid.NewGuid();
        var context = new OpportunityAnalysisContext(Guid.NewGuid(), "Education", "English", "Global", "Creators", 2,
            [new OpportunityEvidenceContext("one", Guid.NewGuid(), first, null, "format", "Explainer", 90, 80),
             new OpportunityEvidenceContext("two", Guid.NewGuid(), second, null, "format", "Explainer", 80, 80)], [], []);
        var concentrated = Candidate(["one"]);
        var widespread = Candidate(["one", "two"]);

        var result = OpportunityScoringEngine.Calculate(concentrated, context);
        Assert.Equal(result, OpportunityScoringEngine.Calculate(concentrated, context));
        Assert.InRange(result.OverallScore, 0, 100);
        Assert.True(result.CompetitionRiskSignal < OpportunityScoringEngine.Calculate(widespread, context).CompetitionRiskSignal);
    }

    [Fact]
    public void Validator_rejects_fabricated_evidence_and_duplicate_structured_opportunities()
    {
        var context = new OpportunityAnalysisContext(Guid.NewGuid(), "Market", "English", "Global", "Audience", 1,
            [new OpportunityEvidenceContext("valid", Guid.NewGuid(), Guid.NewGuid(), null, "topic", "Topic", 50, 70)], [], []);
        Assert.Throws<StructuredOutputException>(() => OpportunityAnalysisValidator.Validate(new OpportunityAnalysisResult([Candidate(["invented"])], []), context, 8));
        Assert.Throws<StructuredOutputException>(() => OpportunityAnalysisValidator.Validate(new OpportunityAnalysisResult([Candidate(["valid"]), Candidate(["valid"])], []), context, 8));
        Assert.Throws<StructuredOutputException>(() => OpportunityAnalysisValidator.Validate(new OpportunityAnalysisResult([Candidate(["valid"]) with { Description = null! }], []), context, 8));
        Assert.Throws<StructuredOutputException>(() => OpportunityAnalysisValidator.Validate(new OpportunityAnalysisResult([Candidate(["valid"]) with { Audience = null! }], []), context, 8));
    }

    private static OpportunityCandidateResult Candidate(IReadOnlyList<string> evidence) => new("History economics", "Description", "Creators", "History", "Explainer", "Ownership", "Evidence-backed", 70, 70, 70, 70, 70, 40, evidence, ["Research burden"], []);
}
