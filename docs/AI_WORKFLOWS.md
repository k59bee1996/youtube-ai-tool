# AI Workflows

## Competitor analysis (`competitor-analysis:v1`)

The first production AI workflow is a worker-executed, structured competitor analysis. `POST .../analysis:run` only creates a job; the worker builds a bounded context from persisted channel/video metadata and calls `ILlmProvider.GenerateStructuredAsync<CompetitorAnalysisResult>`.

The result contract contains audience, topic clusters, observed title, thumbnail and hook patterns, content formats, performance insights, potential weaknesses, transferable formats, evidence notes, and confidence/limitations. Each confidence is a 0–100 integer and all video references are checked against the selected source videos. Topic/title frequencies cannot exceed the analyzed sample. Invalid JSON, schema validation failures, missing structured members, or invalid references get one correction attempt (`CompetitorAnalysis:MaxStructuredOutputRetries`); failure is retained on the job and `AiRun`.

`competitor-analysis:v1` instructs the provider to use supplied evidence only, distinguish inference from observation, and never claim transcript, comments, or thumbnail-image evidence. Thumbnail and hook fields must explicitly report insufficient evidence when pixel or transcript evidence is absent. It extracts mechanics, not copy instructions. Each completed result is immutable and retains source data timestamp, provider/model, prompt version, usage where available, latency, and retry count. Transient provider failures use bounded exponential job retries; stale running jobs are recovered by the configured lease. Normal tests use fakes and make no paid calls.

## Opportunity analysis (`opportunity-analysis:v1`)

The worker consumes only current persisted competitor analyses and sends a bounded context of high-confidence audience, topic, format, and transferable-mechanic evidence. Backend-issued evidence IDs are validated before persistence. The LLM assesses coherent adjacent opportunities, novelty, audience fit, transferability, story potential, complexity, confidence, risks, and limitations; it cannot claim global demand or provide a final score. C# calculates observed dataset demand, evidence strength, competitor diversity/competition risk, production ease, and the final `opportunity-score:v1` result. Reports preserve source-analysis versions and can be flagged stale after a newer analysis exists.

## Idea generation (`idea-generation:v1`)

Only an approved opportunity can start this worker-backed workflow. The bounded context includes project settings, the approved opportunity, its persisted evidence, up to 50 prior active ideas, and collected competitor titles. The structured candidate contract contains title, topic, angle, format, audience, viewer intent, hook and thumbnail concepts, viewer promise, core question, rationale, explicit hypothesis, risks, confidence, evidence IDs, and bounded subjective features.

The model must create original concepts and cannot copy competitor titles or scripts. Evidence IDs are validated; duplicate evidence IDs and out-of-context IDs are rejected. Jaccard token similarity and normalized topic/angle/format identity reject near duplicates within a batch, across active prior ideas, and against competitor titles. The worker makes at most two replacement requests and fails rather than calling a below-minimum result complete. `idea-score:v1` is calculated in C# from inherited opportunity fit/demand, AI-assessed novelty, title/thumbnail/story/audience potential and risks, deterministic evidence strength/production ease, and explicit risk penalties.

## Pilot generation (`pilot-generation:v1`)

The worker sends a bounded, structured set of approved project ideas to the model. The model selects coherent learning experiments and explains their hypothesis, variable, control, planned metric, success signal, and rationale. It cannot invent idea or opportunity IDs, create new ideas, write scripts, or report analytics. C# validates exactly 12 unique existing approved ideas, fixed 4/4/4 experiment distribution and sequences, ownership, and all required fields. One correction attempt is permitted for invalid structured output. Soft balance analysis warns about topic, opportunity, packaging, or early production-complexity concentration; it does not reject an otherwise valid plan.
