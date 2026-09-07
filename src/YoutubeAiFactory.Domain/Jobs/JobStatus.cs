namespace YoutubeAiFactory.Domain.Jobs;

public enum JobStatus
{
    Queued,
    Running,
    Retrying,
    Completed,
    Failed,
    Cancelled,
}
