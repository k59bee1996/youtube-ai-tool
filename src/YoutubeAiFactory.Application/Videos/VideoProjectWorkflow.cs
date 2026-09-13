using System.Text.Json;
using YoutubeAiFactory.Application.Common;
using YoutubeAiFactory.Application.Persistence;
using YoutubeAiFactory.Domain.Ideas;
using YoutubeAiFactory.Domain.Opportunities;
using YoutubeAiFactory.Domain.Pilots;
using YoutubeAiFactory.Domain.Videos;

namespace YoutubeAiFactory.Application.Videos;

public sealed class CreateVideoProjectHandler(IYoutubeAiFactoryStore store, TimeProvider timeProvider)
{
    public async Task<VideoProjectDto> HandleAsync(Guid projectId, Guid pilotId, CreateVideoProjectRequest request, CancellationToken cancellationToken)
    {
        if (await store.GetProjectAsync(projectId, cancellationToken) is null)
            throw new ResourceNotFoundException("Project was not found.");
        var pilot = await store.GetPilotAsync(projectId, pilotId, false, cancellationToken)
            ?? throw new ResourceNotFoundException("Pilot was not found.");
        if (pilot.Status != PilotStatus.Approved)
            throw new ApplicationValidationException("Only an approved pilot can start a video project.");
        var existing = await store.GetVideoProjectByPilotVideoAsync(request.PilotVideoId, cancellationToken);
        if (existing is not null)
            return await VideoProjectDtoMapper.MapAsync(store, existing, cancellationToken);
        var source = await store.GetVideoProjectSourceAsync(projectId, pilotId, request.PilotVideoId, cancellationToken)
            ?? throw new ResourceNotFoundException("Pilot video was not found in this pilot.");
        if (source.VideoIdea.DecisionStatus != IdeaDecisionStatus.Approved || source.Opportunity.DecisionStatus != OpportunityDecisionStatus.Approved)
            throw new ApplicationValidationException("This pilot video requires review because its source idea or opportunity is no longer approved.");
        var project = new VideoProject(projectId, pilot.Id, pilot.Version, source.PilotVideo.Id, source.VideoIdea.Id,
            source.Opportunity.Id, source.VideoIdea.WorkingTitle, source.VideoIdea.Topic, source.VideoIdea.Angle,
            source.VideoIdea.ContentFormat, source.VideoIdea.TargetAudience, source.VideoIdea.HookConcept,
            source.VideoIdea.ThumbnailConcept, source.VideoIdea.ViewerPromise, source.PilotVideo.ExperimentType,
            source.PilotVideo.Hypothesis, source.PilotVideo.VariableBeingTested, source.PilotVideo.PrimaryMetric,
            source.PilotVideo.SuccessSignal, pilot.WarningsJson, timeProvider.GetUtcNow());
        var persisted = await store.CreateVideoProjectIfAbsentAsync(project, cancellationToken);
        return await VideoProjectDtoMapper.MapAsync(store, persisted, cancellationToken);
    }
}

public sealed class ListVideoProjectsHandler(IYoutubeAiFactoryStore store)
{
    public async Task<IReadOnlyList<VideoProjectListItemDto>> HandleAsync(Guid projectId, CancellationToken cancellationToken)
    {
        if (await store.GetProjectAsync(projectId, cancellationToken) is null) throw new ResourceNotFoundException("Project was not found.");
        var projects = await store.ListVideoProjectsAsync(projectId, cancellationToken);
        var results = new List<VideoProjectListItemDto>(projects.Count);
        foreach (var project in projects)
        {
            var source = await store.GetVideoProjectSourceAsync(project.ProjectId, project.PilotId, project.PilotVideoId, cancellationToken);
            results.Add(new VideoProjectListItemDto(project.Id, project.PilotVideoId, project.WorkingTitle, project.Status.ToString(), project.VideoIdeaId,
                source?.PilotVideo.Sequence ?? 0, project.ExperimentType.ToString(), project.CreatedAt));
        }
        return results;
    }
}

public sealed class GetVideoProjectHandler(IYoutubeAiFactoryStore store)
{
    public async Task<VideoProjectDto> HandleAsync(Guid projectId, Guid videoProjectId, CancellationToken cancellationToken)
    {
        var project = await store.GetVideoProjectAsync(projectId, videoProjectId, false, cancellationToken)
            ?? throw new ResourceNotFoundException("Video project was not found.");
        return await VideoProjectDtoMapper.MapAsync(store, project, cancellationToken);
    }
}

public sealed class UpdateVideoProjectHandler(IYoutubeAiFactoryStore store, TimeProvider timeProvider)
{
    public async Task<VideoProjectDto> HandleAsync(Guid projectId, Guid videoProjectId, UpdateVideoProjectRequest request, CancellationToken cancellationToken)
    {
        var project = await store.GetVideoProjectAsync(projectId, videoProjectId, true, cancellationToken)
            ?? throw new ResourceNotFoundException("Video project was not found.");
        project.UpdateExecutionDetails(request.WorkingTitle, request.ExecutionNotes, timeProvider.GetUtcNow());
        await store.SaveChangesAsync(cancellationToken);
        return await VideoProjectDtoMapper.MapAsync(store, project, cancellationToken);
    }
}

internal static class VideoProjectDtoMapper
{
    public static async Task<VideoProjectDto> MapAsync(IYoutubeAiFactoryStore store, VideoProject project, CancellationToken cancellationToken)
    {
        var source = await store.GetVideoProjectSourceAsync(project.ProjectId, project.PilotId, project.PilotVideoId, cancellationToken)
            ?? throw new ResourceNotFoundException("Video project source context was not found.");
        var warnings = JsonSerializer.Deserialize<string[]>(project.SourceWarningsJson) ?? [];
        var requiresReview = source.Pilot.Status != PilotStatus.Approved || source.VideoIdea.DecisionStatus != IdeaDecisionStatus.Approved || source.Opportunity.DecisionStatus != OpportunityDecisionStatus.Approved;
        return new VideoProjectDto(project.Id, project.ProjectId, project.PilotId, project.PilotVersion, project.PilotVideoId,
            project.VideoIdeaId, project.OpportunityId, project.WorkingTitle, project.Status.ToString(), project.Topic,
            project.Angle, project.ContentFormat, project.TargetAudience, project.HookConcept, project.ThumbnailConcept,
            project.ViewerPromise, source.PilotVideo.Sequence, project.ExperimentType.ToString(), project.PilotHypothesis,
            project.VariableBeingTested, project.PrimaryMetric, project.SuccessSignal, source.Opportunity.Name,
            source.VideoIdea.OverallScore, project.ExecutionNotes, project.CreatedAt, project.UpdatedAt, requiresReview, warnings);
    }
}
