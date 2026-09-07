# AI Workflows

No AI workflow or provider is implemented in Phase 2.

The application layer defines `ILlmProvider.GenerateStructuredAsync<T>`, `LlmRequest`, and `LlmResult<T>` so future workflows can use a provider-neutral structured-output contract. Requests identify a prompt key and positive version; results retain provider/model identity, usage, and optional raw output. The current project and competitor handlers have no dependency on this contract.

Phase 3 should add the first workflow through an application handler and workflow service, validate its typed result, record an `AiRun`, and persist the structured result. API endpoints must not call providers directly. Real paid calls must remain outside routine automated tests.
