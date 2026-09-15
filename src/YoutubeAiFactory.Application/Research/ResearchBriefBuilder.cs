using System.Security.Cryptography;
using System.Text;
using YoutubeAiFactory.Domain.Projects;
using YoutubeAiFactory.Domain.Videos;

namespace YoutubeAiFactory.Application.Research;

public sealed class ResearchBriefBuilder
{
    public static ResearchBrief Build(Project project, VideoProject videoProject, string opportunityName) => new(
        project.Id, videoProject.Id, videoProject.WorkingTitle, videoProject.Topic, videoProject.Angle,
        videoProject.ContentFormat, videoProject.TargetAudience, videoProject.ViewerPromise, videoProject.HookConcept,
        project.Market.TargetLanguage, project.Market.TargetGeography, videoProject.ExperimentType.ToString(),
        videoProject.PilotHypothesis, videoProject.VariableBeingTested, opportunityName);

    public static string CreateFingerprint(ResearchBrief brief)
    {
        var value = string.Join("\n", [brief.WorkingTitle, brief.Topic, brief.Angle, brief.ContentFormat,
            brief.TargetAudience, brief.ViewerPromise, brief.HookConcept, brief.TargetLanguage, brief.TargetGeography,
            brief.PilotExperimentType, brief.PilotHypothesis, brief.VariableBeingTested, brief.OpportunityName]);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    }
}
