using YoutubeAiFactory.Application.Research;
using YoutubeAiFactory.Domain.Research;

namespace YoutubeAiFactory.Application.Tests;

public sealed class ResearchSupportAnalyzerTests
{
    [Fact]
    public void Calculates_supported_corroborated_and_conflicted_from_evidence_relationships()
    {
        var runId = Guid.NewGuid();
        var sourceOne = Source(runId, "https://example.org/a", "example.org", "hash-a");
        var sourceTwo = Source(runId, "https://archive.example.net/b", "archive.example.net", "hash-b");
        var first = Evidence(runId, sourceOne.Id, "A documented fact.");
        var second = Evidence(runId, sourceTwo.Id, "A documented fact.");
        var analyzer = new ResearchSupportAnalyzer();
        var evidence = new Dictionary<Guid, ResearchEvidence> { [first.Id] = first, [second.Id] = second };
        var sources = new Dictionary<Guid, ResearchSource> { [sourceOne.Id] = sourceOne, [sourceTwo.Id] = sourceTwo };
        var claim = Guid.NewGuid();

        Assert.Equal(ResearchClaimSupportStatus.Supported, ResearchSupportAnalyzer.GetStatus([new ResearchClaimEvidence(claim, first.Id, ResearchEvidenceStance.Support)], evidence, sources));
        Assert.Equal(ResearchClaimSupportStatus.Corroborated, ResearchSupportAnalyzer.GetStatus([new ResearchClaimEvidence(claim, first.Id, ResearchEvidenceStance.Support), new ResearchClaimEvidence(claim, second.Id, ResearchEvidenceStance.Support)], evidence, sources));
        Assert.Equal(ResearchClaimSupportStatus.Conflicted, ResearchSupportAnalyzer.GetStatus([new ResearchClaimEvidence(claim, first.Id, ResearchEvidenceStance.Support), new ResearchClaimEvidence(claim, second.Id, ResearchEvidenceStance.Contradict)], evidence, sources));
    }

    [Fact]
    public void Does_not_count_duplicate_links_to_one_source_as_corroboration()
    {
        var runId = Guid.NewGuid();
        var source = Source(runId, "https://example.org/a", "example.org", "same");
        var first = Evidence(runId, source.Id, "Fact one.");
        var second = Evidence(runId, source.Id, "Fact two.");
        var evidence = new Dictionary<Guid, ResearchEvidence> { [first.Id] = first, [second.Id] = second };
        var sources = new Dictionary<Guid, ResearchSource> { [source.Id] = source };
        var claim = Guid.NewGuid();

        Assert.Equal(ResearchClaimSupportStatus.Supported, ResearchSupportAnalyzer.GetStatus([new ResearchClaimEvidence(claim, first.Id, ResearchEvidenceStance.Support), new ResearchClaimEvidence(claim, second.Id, ResearchEvidenceStance.Support)], evidence, sources));
    }

    private static ResearchSource Source(Guid runId, string url, string domain, string hash) => new(runId, url, url, domain, "Source", domain, null, DateTimeOffset.UtcNow, ResearchSourceCategory.Institutional, ResearchSourceFetchStatus.Fetched, hash, null, null);
    private static ResearchEvidence Evidence(Guid runId, Guid sourceId, string fact) => new(runId, sourceId, ResearchEvidenceType.Fact, fact, fact, "body", 80, DateTimeOffset.UtcNow);
}
