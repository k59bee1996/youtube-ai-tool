# AI Workflows

## Model profiles and responsibility matrix

The application asks for an intelligence class; Infrastructure resolves the configured provider/model. Model identifiers are not selected by controllers, frontend code, or workflow business logic.

| Workflow | Profile | AI responsibility |
| --- | --- | --- |
| Competitor Analysis | Reasoning | Pattern and bounded-evidence interpretation |
| Opportunity Analysis | Premium | Strategic cross-competitor synthesis |
| Idea Generation | Reasoning | Original, evidence-backed hypotheses and packaging concepts |
| Pilot Generation | Premium | Controlled experiment and hypothesis planning |
| Artifact Localization | Fast | Meaning-preserving reader-facing translation |
| Research Query Planning | Reasoning | Bounded research questions and diverse search queries |
| Research Source Relevance | Fast | Relevance classification of untrusted retrieved text |
| Research Evidence Extraction | Reasoning | Source-bound fact and excerpt extraction |
| Research Contradiction Analysis | Premium | Cross-source disagreement interpretation |
| Research Synthesis | Premium | Evidence-limited report synthesis |
| Outline Generation | Reasoning | Evidence-grounded narrative and information architecture |
| Structured Output Repair | Fast | Mechanical JSON/schema correction without semantic changes |

Scores, rankings, ID/evidence validation, duplicate detection, versions, job state, VideoProject creation, and the Pilot 4/4/4 constraint remain deterministic C# responsibilities. A semantic validation failure reruns the originating workflow profile; structural repair uses `Fast` and cannot add reasoning or alter business meaning. Future policy is: production-package generation uses `Reasoning`; scripts use `Premium`. Those future workflows are not implemented.

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

## Idea generation (`idea-generation:v3`)

Only an approved opportunity can start this worker-backed workflow. The bounded context includes project settings, the approved opportunity, its persisted evidence, up to 50 prior active ideas, and collected competitor titles. The structured candidate contract contains title, topic, angle, format, audience, viewer intent, hook and thumbnail concepts, viewer promise, core question, rationale, explicit hypothesis, risks, confidence, evidence IDs, and bounded subjective features.

The model must create original concepts and cannot copy competitor titles or scripts. It must make every candidate in a response meaningfully distinct. `idea-generation:v3` submits a strict JSON Schema, so the provider must return every persisted field with its required JSON type. Evidence IDs are validated; duplicate evidence IDs and out-of-context IDs are rejected. Jaccard token similarity and normalized topic/angle/format identity reject near duplicates within a batch, across active prior ideas, and against competitor titles. The default generation is one bounded request for 15 candidates, using the resolved model output limit; it fails rather than completing a below-minimum result. `idea-score:v1` is calculated in C# from inherited opportunity fit/demand, AI-assessed novelty, title/thumbnail/story/audience potential and risks, deterministic evidence strength/production ease, and explicit risk penalties.

## Pilot generation (`pilot-generation:v3`)

The worker sends a bounded, structured set of ideas whose idea and source opportunity are both approved to the model. The model selects coherent learning experiments and explains their hypothesis, variable, control, planned metric, success signal, and rationale. `pilot-generation:v3` sends a strict JSON Schema for the complete Pilot result: every text field (including `hypothesis`) is a string, optional notes are explicitly nullable, and the C# experiment enum is represented by the integers Topic=0, Packaging=1, Storytelling=2. It cannot invent idea or opportunity IDs, create new ideas, write scripts, or report analytics. C# validates exactly 12 unique existing approved ideas, fixed 4/4/4 experiment distribution and sequences, ownership, required top-level fields, and bounded slot text. One correction attempt is permitted for invalid structured output; semantic failures provide the prior candidate and validator failure to the retry. Soft balance analysis evaluates sequence blocks and warns about topic, opportunity, packaging, or early production-complexity concentration; it does not reject an otherwise valid plan.

## Video project creation (no AI workflow)

Phase 7 deliberately performs zero LLM calls and creates no `AiRun` or background Job. It converts already persisted, approved strategy into a local execution object. Research begins from that specific execution brief rather than inferring strategic pilot evidence is video-specific research.

## Evidence-backed research (`research-engine:v1`)

Research is a worker workflow, not an LLM request from a controller. `research-query-plan:v1` uses Reasoning to make a bounded plan in the content target language. Provider-neutral search discovers URLs; snippets are never evidence. The fetcher treats source content as untrusted, applies redirect-aware SSRF protection and content limits, and retains no complete webpage by default. `research-source-relevance:v1` uses Fast; `research-evidence-extraction:v1` uses Reasoning and must emit an exact, short source excerpt.

C# validates every AI-issued ID and calculates claim support. `research-contradiction-analysis:v1` and `research-synthesis:v1` use Premium over bounded evidence/claim representations only. Synthesis cannot create factual claims, sources, evidence IDs, or citation IDs and is rejected if its references do not exist in the current run. A successful report requires at least one relevant source and one source-bound evidence item.

`research-report-localization:v1` is a Fast, lazy `EN | VI` overlay. It translates only the executive summary, finding/gap explanations, warnings, and limitations. Indexed entries must preserve exact canonical order before persistence. Claims, excerpts, source metadata/URLs, numbers/dates, IDs, support statuses, versions, and metrics remain canonical. Toggling reading language never re-runs research or changes source selection.

## Evidence-grounded outline (`outline-generation:v1`, `outline-engine:v1`)

Outline generation is a worker workflow available only from `ResearchReady` or, for an unapproved regeneration, `OutlineReady`. It loads the current non-stale ResearchReport and builds a bounded context from its synthesis, supported/corroborated/conflicted claims, limited evidence excerpts, conflicts, gaps, and immutable Pilot experiment context. Unsupported claims are excluded from factual candidates. An unsupported critical premise, stale fingerprint, missing report, or invalid cross-report relationship blocks generation before an AI call. No search or content-fetch contract is injected into the outline workflow.

The Reasoning model produces only a canonical English structured Narrative Strategy and planning-level ordered Sections; project TargetLanguage remains contextual input and does not translate the artifact. Every outline contains a Hook, a mid-outline `PatternInterrupt`, and exactly one final `CTA` planning section. Both are intent-only fields, never finished spoken copy. Each context claim exposes its `Support` or `Contradict` evidence stance, so conflicting evidence cannot be presented as support. The model may select a controlled structure, core question/tension, hook concept, information progression, payoff, pacing, section objective/summary/viewer question/transition intent, and supplied claim placement. It cannot change WorkingTitle or ViewerPromise, add evidence or IDs, present Unsupported claims as facts, hide conflicts, create narration, or write a script. A Storytelling `Rise-and-Fall` variable must be implemented by both `RiseAndFall` strategy and meaningful escalation/turn/payoff section purposes; Topic control strategies reject selected storytelling confounds; Packaging always persists the existing ViewerPromise.

C# validates required fields, bounded section count, contiguous sequence, Hook/PatternInterrupt/final-CTA placement, all claim/conflict/gap identities, claim ownership/support, conflict co-location, and experiment structure. C# computes the version, input fingerprint, status, duration total, and warnings. The outline worker renews its lease and lease-fences the final persistence transaction. Invalid semantic output receives a bounded Reasoning correction. Only malformed output carrying raw JSON may use `structured-output-repair:v1` on Fast, once. Each call receives an `AiRun` tied to the ResearchReport; failed calls and jobs remain observable but create no Outline version.

`video-outline-localization:v1` uses the shared lazy Fast localization queue. It translates only narrative/alignment/warning and Section planning text and must preserve the canonical-content fingerprint, exact Section IDs, order, count, list indexes, and nullable-field topology. A Ready-outline edit invalidates an older overlay even if a concurrent translation finishes later. Claim/conflict/gap references, enums, status, versions, provider/model data, timestamps, URLs, metrics, and canonical production content never change. Switching `EN | VI` creates no Outline version and performs no research.
