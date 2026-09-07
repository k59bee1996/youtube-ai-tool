namespace YoutubeAiFactory.Application.AI;

public interface ILlmProvider
{
    Task<LlmResult<T>> GenerateStructuredAsync<T>(
        LlmRequest request,
        CancellationToken cancellationToken);
}
