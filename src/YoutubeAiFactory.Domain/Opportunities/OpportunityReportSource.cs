using YoutubeAiFactory.Domain.Common;

namespace YoutubeAiFactory.Domain.Opportunities;

public sealed class OpportunityReportSource
{
    private OpportunityReportSource() { }
    public OpportunityReportSource(Guid reportId, Guid competitorChannelId, Guid competitorAnalysisId, int competitorAnalysisVersion)
    {
        if (competitorAnalysisVersion < 1) throw new DomainException("Source analysis version must be positive.");
        Id = Guid.NewGuid(); ReportId = Guard.NotEmpty(reportId, nameof(reportId));
        CompetitorChannelId = Guard.NotEmpty(competitorChannelId, nameof(competitorChannelId));
        CompetitorAnalysisId = Guard.NotEmpty(competitorAnalysisId, nameof(competitorAnalysisId));
        CompetitorAnalysisVersion = competitorAnalysisVersion;
    }
    public Guid Id { get; private set; }
    public Guid ReportId { get; private set; }
    public Guid CompetitorChannelId { get; private set; }
    public Guid CompetitorAnalysisId { get; private set; }
    public int CompetitorAnalysisVersion { get; private set; }
}
