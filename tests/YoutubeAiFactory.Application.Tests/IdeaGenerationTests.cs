using YoutubeAiFactory.Application.Common;
using YoutubeAiFactory.Application.Ideas;

namespace YoutubeAiFactory.Application.Tests;

public sealed class IdeaGenerationTests
{
    [Fact]
    public void Scoring_is_deterministic_bounded_and_penalizes_risk()
    {
        var context = Context(); var safe = Candidate(context) with { Features = new(80, 80, 80, 80, 80, 20, 10, 10) };
        var risky = safe with { Features = safe.Features with { CompetitionRisk = 90, ResearchRisk = 90 } };
        var first = IdeaScoringEngine.Calculate(safe, context, 0);
        Assert.Equal(first, IdeaScoringEngine.Calculate(safe, context, 0)); Assert.InRange(first.OverallScore, 0, 100); Assert.True(first.OverallScore > IdeaScoringEngine.Calculate(risky, context, 0).OverallScore);
    }

    [Fact]
    public void Validator_rejects_missing_hypothesis_and_invalid_evidence()
    {
        var context = Context(); Assert.Throws<StructuredOutputException>(() => IdeaGenerationValidator.Validate(Candidate(context) with { Hypothesis = "" }, context));
        Assert.Throws<StructuredOutputException>(() => IdeaGenerationValidator.Validate(Candidate(context) with { EvidenceIds = [Guid.NewGuid()] }, context));
    }

    [Fact]
    public void Duplicate_detector_handles_case_punctuation_topic_angle_and_competitor_copies()
    {
        var original = Candidate(Context()); var similar = original with { WorkingTitle = "the economics of owning a medieval castle" };
        Assert.True(IdeaDuplicateDetector.IsNearDuplicate(original, similar, .70m));
        Assert.True(IdeaDuplicateDetector.IsNearDuplicate(original, new ExistingIdeaContext(Guid.NewGuid(), "Different words", original.Topic, original.Angle, original.ContentFormat), .70m));
        Assert.True(IdeaDuplicateDetector.IsNearCompetitorCopy(original, ["The economics of owning a medieval castle"], .70m));
    }

    private static IdeaGenerationContext Context() { var evidence = Guid.NewGuid(); return new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1, "History", "English", "Global", "History fans", "Historic ownership", "Description", "Adults", "Castles", "Explainer", "Economics", "Rationale", 80, 75, 70, [new(evidence, "Ownership-economics pattern")], [], [], []); }
    private static VideoIdeaCandidateResult Candidate(IdeaGenerationContext context) => new("The Economics of Owning a Medieval Castle", "Castles", "Economics", "Explainer", "History fans", "Understand costs", "A surprising cost", "Castle with coins", "See the hidden cost", "What did a castle cost?", "Recognizable asset reveals hidden economics", "Viewers will click because castles make ownership costs tangible.", new(80, 80, 80, 80, 80, 30, 20, 20), [context.Evidence[0].OpportunityEvidenceId], ["Research complexity"], 80);
}
