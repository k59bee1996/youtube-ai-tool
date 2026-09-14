# AI Workflows

## Competitor analysis (`competitor-analysis:v2`)

The first production AI workflow is a worker-executed, structured competitor analysis. `POST .../analysis:run` only creates a job; the worker builds a bounded context from persisted channel/video metadata and calls `ILlmProvider.GenerateStructuredAsync<CompetitorAnalysisResult>`.

The result contract contains audience, topic clusters, observed title, thumbnail and hook patterns, content formats, performance insights, potential weaknesses, transferable formats, evidence notes, and confidence/limitations. Each confidence is a 0–100 integer and all video references are checked against the selected source videos. Topic/title frequencies cannot exceed the analyzed sample. Invalid JSON, schema validation failures, missing structured members, or invalid references get one correction attempt (`CompetitorAnalysis:MaxStructuredOutputRetries`); failure is retained on the job and `AiRun`.

`competitor-analysis:v2` instructs the provider to use supplied evidence only, distinguish inference from observation, and never claim transcript, comments, or thumbnail-image evidence. It sends a strict JSON Schema to supported providers, requiring every nested object and array to match the typed result contract before deserialization. Thumbnail and hook fields must explicitly report insufficient evidence when pixel or transcript evidence is absent. It extracts mechanics, not copy instructions. Each completed result is immutable and retains source data timestamp, provider/model, prompt version, usage where available, latency, and retry count. Transient provider failures use bounded exponential job retries; stale running jobs are recovered by the configured lease. Normal tests use fakes and make no paid calls.

## Localized analysis reading aids (`competitor-analysis-localization:v2`, `opportunity-report-localization:v1`)

The UI remains English. Localization is not frontend i18n and it never changes navigation, controls, status labels, scores, IDs, URLs, timestamps, enums, version metadata, evidence references, model metadata, example titles, or title templates.

For completed competitor analyses, the UI can request the `EN | VI` reading-aid toggle. English is the canonical persisted `resultJson`. A Vietnamese selection checks `artifact_localizations` by `(artifact_type, artifact_id, artifact_version, locale)`; a completed entry is returned directly. On a miss, an `artifact-localization` background job translates only reader-facing analysis fields, validates that its list topology matches the canonical artifact, and persists a separate immutable JSON overlay with `AiRun` provenance. Concurrent selections coalesce to one active job. Translation failure leaves the canonical English artifact untouched and immediately usable.

The localization prompt explicitly excludes production-facing or evidence-derived title content. This keeps a working title such as `Why Owning a Castle Could Bankrupt You` canonical when the content target language is English. A future title-reading-aid field must be a separate localized representation and can never mutate that title. Content target language remains independent from analysis display language.

Opportunity reports use the same immutable artifact-localization mechanism. The Vietnamese overlay contains only reader-facing content: report and candidate limitations, candidate name, description, audience, topic, format, angle, rationale, evidence summary, risk, and limitation text. Candidate names are dynamic AI analysis content and are therefore translated for the reading aid. Scores, confidence, decision status, IDs, and evidence IDs remain canonical English/business data. The overlay preserves the exact score-sorted candidate and list order used by the report UI.

## Opportunity analysis (`opportunity-analysis:v1`)

The worker consumes only current persisted competitor analyses and sends a bounded context of high-confidence audience, topic, format, and transferable-mechanic evidence. Backend-issued evidence IDs are validated before persistence. The LLM assesses coherent adjacent opportunities, novelty, audience fit, transferability, story potential, complexity, confidence, risks, and limitations; it cannot claim global demand or provide a final score. `opportunity-analysis:v2` sends a strict JSON Schema to supported providers: all list fields, including `evidenceIds`, `risks`, and `limitations`, must be arrays of strings. An invalid response is retried once with its parse or validation diagnostic. C# calculates observed dataset demand, evidence strength, competitor diversity/competition risk, production ease, and the final `opportunity-score:v1` result. Reports preserve source-analysis versions and can be flagged stale after a newer analysis exists.

## Idea generation (`idea-generation:v1`)

Only an approved opportunity can start this worker-backed workflow. The bounded context includes project settings, the approved opportunity, its persisted evidence, up to 50 prior active ideas, and collected competitor titles. The structured candidate contract contains title, topic, angle, format, audience, viewer intent, hook and thumbnail concepts, viewer promise, core question, rationale, explicit hypothesis, risks, confidence, evidence IDs, and bounded subjective features.

The model must create original concepts and cannot copy competitor titles or scripts. Evidence IDs are validated; duplicate evidence IDs and out-of-context IDs are rejected. Jaccard token similarity and normalized topic/angle/format identity reject near duplicates within a batch, across active prior ideas, and against competitor titles. The worker makes at most two replacement requests and fails rather than calling a below-minimum result complete. `idea-score:v1` is calculated in C# from inherited opportunity fit/demand, AI-assessed novelty, title/thumbnail/story/audience potential and risks, deterministic evidence strength/production ease, and explicit risk penalties.

## Pilot generation (`pilot-generation:v1`)

The worker sends a bounded, structured set of ideas whose idea and source opportunity are both approved to the model. The model selects coherent learning experiments and explains their hypothesis, variable, control, planned metric, success signal, and rationale. It cannot invent idea or opportunity IDs, create new ideas, write scripts, or report analytics. C# validates exactly 12 unique existing approved ideas, fixed 4/4/4 experiment distribution and sequences, ownership, required top-level fields, and bounded slot text. One correction attempt is permitted for invalid structured output; semantic failures provide the prior candidate and validator failure to the retry. Soft balance analysis evaluates sequence blocks and warns about topic, opportunity, packaging, or early production-complexity concentration; it does not reject an otherwise valid plan.

## Video project creation (no AI workflow)

Phase 7 deliberately performs zero LLM calls and creates no `AiRun` or background Job. It converts already persisted, approved strategy into a local execution object. Research remains a future workflow and must begin from a Draft VideoProject rather than inferring that strategic pilot evidence is video-specific research.
