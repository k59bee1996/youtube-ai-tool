# AI Workflows

## Competitor analysis (`competitor-analysis:v1`)

The first production AI workflow is a worker-executed, structured competitor analysis. `POST .../analysis:run` only creates a job; the worker builds a bounded context from persisted channel/video metadata and calls `ILlmProvider.GenerateStructuredAsync<CompetitorAnalysisResult>`.

The result contract contains audience, topic clusters, observed title, thumbnail and hook patterns, content formats, performance insights, potential weaknesses, transferable formats, evidence notes, and confidence/limitations. Each confidence is a 0–100 integer and all video references are checked against the selected source videos. Topic/title frequencies cannot exceed the analyzed sample. Invalid JSON, schema validation failures, missing structured members, or invalid references get one correction attempt (`CompetitorAnalysis:MaxStructuredOutputRetries`); failure is retained on the job and `AiRun`.

`competitor-analysis:v1` instructs the provider to use supplied evidence only, distinguish inference from observation, and never claim transcript, comments, or thumbnail-image evidence. Thumbnail and hook fields must explicitly report insufficient evidence when pixel or transcript evidence is absent. It extracts mechanics, not copy instructions. Each completed result is immutable and retains source data timestamp, provider/model, prompt version, usage where available, latency, and retry count. Transient provider failures use bounded exponential job retries; stale running jobs are recovered by the configured lease. Normal tests use fakes and make no paid calls.
