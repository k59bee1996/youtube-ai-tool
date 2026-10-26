# Architecture

## Shape and dependency flow

The system is a .NET 10 modular monolith with two hosts and one React client.

```text
Web -> API -> Application -> Domain
                  ^
                  |
            Infrastructure
                  ^
                  |
                Worker
```

`Domain` has no framework or integration dependencies. `Application` defines use cases and external contracts. `Infrastructure` owns EF Core, SQL Server, provider adapters, and migrations. `Api` maps HTTP contracts to application use cases. `Worker` executes database-backed long-running workflows.

## Phase 12 observability

Phase 12 reuses `Job` as the logical workflow record and `AiRun` as the provider-request telemetry record. Each provider request may carry a `JobId`, VideoProject, workflow stage, model profile, resolved provider/model, token categories, cost source, pricing version, and sanitized failure metadata. `IObservabilityQueries` is a read-only Application contract implemented by SQL Server projections in Infrastructure; dashboard requests never invoke `ILlmProvider`, enqueue jobs, or mutate VideoProject state.

Cost accounting keeps shared project strategy work (competitor, opportunity, idea, and pilot workflows) separate from direct VideoProject work (research, outline, script, and production). Provider-reported cost and price-calculated estimates are distinct, and missing cost remains unavailable. Configured prices are optional and effective-dated; no provider prices are shipped by default. Cost totals are grouped by currency and never silently mixed.

## AI model routing

AI workflows request an application-level capability, never a concrete vendor model:

```text
Workflow -> AiModelProfile -> IAiModelResolver -> ILlmProvider -> configured provider/model
```

`Fast` is for mechanical transformation such as artifact localization and structural JSON repair. `Reasoning` is for bounded competitor interpretation, idea synthesis, and evidence-grounded narrative planning. `Premium` is reserved for high-leverage strategic synthesis and controlled experiment planning. The configuration-backed resolver is stateless and resolves each request independently; a missing or invalid required profile fails clearly and does not silently downgrade to another profile. Provider API keys remain in configuration only.

## Phase 2 collection flow

```text
React -> project/competitor endpoint -> application handler
                                      -> IYouTubeClient -> YouTube Data API
                                      -> IYoutubeAiFactoryStore -> SQL Server
```

Endpoints contain transport mapping only. `AddCompetitorHandler` coordinates URL parsing, provider calls, domain updates, and one database commit. The YouTube adapter owns HTTP details and retry/error classification. Collection is synchronous, bounded to 1–50 recent uploads, and cancellation-aware. This keeps the first vertical slice observable and testable without introducing queue processing early.

## Persistence and ownership

`YoutubeAiFactoryDbContext` uses the `yaf` SQL Server schema. `Project` owns `Market` and `AudienceProfile`; competitor channels belong to projects and videos belong to channels. Unique constraints on `(project_id, youtube_channel_id)` and `(competitor_channel_id, youtube_video_id)` make refreshes idempotent at the database boundary. YouTube IDs use a binary SQL Server collation so their case-sensitive provider semantics are not changed by a server-default collation. EF migrations are the only supported schema-change mechanism.

Only fully resolved competitors may be persisted: channel identity, title, and collection timestamp are required columns. A refresh cannot change an existing competitor's provider identity. One `SaveChangesAsync` call commits channel and video changes atomically; a uniqueness failure rolls back the full refresh.

## Runtime behavior

`/health/live` checks the process. `/health/ready` checks SQL Server and YouTube configuration. Structured collection logs include project, outcome, competitor/channel when resolved, video count, and elapsed time; API keys and response bodies are excluded. Job claiming and dispatch remain deferred.

See [ADR-001](adr/ADR-001-modular-monolith.md), [ADR-002](adr/ADR-002-postgresql-and-jobs.md), [ADR-003](adr/ADR-003-synchronous-competitor-collection.md), [ADR-004](adr/ADR-004-sql-server-persistence.md), [ADR-005](adr/ADR-005-evidence-backed-research.md), [ADR-006](adr/ADR-006-evidence-grounded-outlines.md), and [ADR-007](adr/ADR-007-evidence-grounded-scripts.md).

## Phase 3 competitor-analysis flow

```text
React -> API (202 Accepted) -> Job table -> Worker -> application workflow
                                                    -> context builder -> ILlmProvider
                                                    -> validator -> analysis/AiRun tables
```

The controller only queues or reads work. The application context builder selects a bounded mix of recent, high-, low-, and representative videos and calculates quantitative signals before the provider is called. `competitor-analysis:v2` is the current immutable prompt contract; it supplies a strict JSON Schema to supported providers, while historical analyses retain their recorded prompt version. The worker claims jobs through a short SQL Server transaction using `UPDLOCK`, `READPAST`, and `ROWLOCK`; the transaction runs inside EF Core's SQL Server retry strategy. A future lease-fencing or heartbeat mechanism is still required so that a slow worker cannot finalize work after its lease has expired and another worker has reclaimed the job.

Completed `CompetitorAnalysis` records are immutable versions. Their nested typed result is stored as JSON in `nvarchar(max)` because it is rendered and consumed as an analysis report; provenance, versions, source timestamp, provider and model remain relational columns. A refresh after `SourceDataAsOf` marks the latest report stale without automatically spending another AI call.

## Phase 4 opportunity-analysis flow

```text
React -> API (202) -> Job -> Worker -> bounded cross-competitor context
                                  -> ILlmProvider -> validation -> deterministic scoring
                                  -> immutable report, sources, candidates, evidence
```

The provider can assess novelty, audience fit, transferability, story potential and production complexity, but it cannot provide final scores or arbitrary evidence. `opportunity-analysis:v2` supplies a strict JSON Schema to supported providers so typed list fields cannot be emitted as scalar strings; one correction request receives the failed parse or validation diagnostic. C# validates backend-issued evidence IDs and derives observed demand, evidence strength, dataset competition risk, and the final `opportunity-score:v1` score. Each report records exact source-analysis versions, enabling stale detection.

## Phase 5 idea-generation flow

```text
Approved opportunity -> API (202) -> Job -> Worker -> bounded context -> ILlmProvider
                                                   -> validation -> duplicate rejection -> deterministic scoring
                                                   -> immutable generation + ideas + evidence
```

The context uses project settings, one approved opportunity, its persisted `OpportunityEvidence`, a bounded active Idea Bank summary, and bounded collected competitor titles. The LLM proposes creative subjective features only. C# derives opportunity fit, observed-demand alignment, evidence strength and production ease; calculates `idea-score:v1`; and rejects close matches to competitor titles, prior ideas, and candidates in the same generation.

## Phase 6 pilot-generation flow

```text
Approved project ideas -> API (202) -> Job -> Worker -> bounded pilot context -> ILlmProvider
                                                          -> hard validation -> balance warnings -> immutable Pilot version
```

The context builder provides 12–40 ideas whose idea and source opportunity are both approved, and calculates topic, format, and opportunity frequency in C#. The model plans meaningful controlled variation and experiment prose only. C# enforces existing IDs/project ownership/approval, unique ideas, sequences 1–12, fixed Topic/Packaging/Storytelling blocks, and required experiment fields. `Pilot` versions are never overwritten; a source idea or source opportunity that is no longer approved makes a plan require review rather than deleting it.

## Phase 7 video-project flow

```text
React -> API -> CreateVideoProjectHandler -> IYoutubeAiFactoryStore -> SQL Server
```

This is a synchronous local transaction, not a job and not an AI workflow. The handler verifies the project, approved Pilot, slot membership, and still-approved source idea/opportunity before constructing the `VideoProject` aggregate. A unique SQL Server index on `pilot_video_id` makes repeated requests idempotent. The aggregate stores relational lineage IDs plus a narrow creation-time execution snapshot; future research, outline, script, and production artifacts will attach to VideoProject rather than to Pilot or VideoIdea.

## Phase 8 evidence-backed research flow

```text
React -> API (202) -> Job + ResearchRun -> Worker
                                        -> ResearchBrief -> Reasoning query plan
                                        -> provider-neutral search -> SSRF-safe source fetch
                                        -> Fast relevance -> Reasoning evidence extraction
                                        -> deterministic claim support -> Premium contradiction/synthesis
                                        -> immutable ResearchReport -> ResearchReady
```

Search snippets are discovery only; evidence is extracted only from successfully retrieved source content. `IResearchSearchClient` and `IResearchContentFetcher` are Application contracts. The Tavily search adapter and HTTP/HTML fetcher live in Infrastructure. The fetcher permits only public HTTP(S) destinations, validates every redirect target, blocks local/private/link-local IPv4 and IPv6 addresses plus metadata hosts, pins its connection to a validated public address, limits redirects/bytes/extracted characters, and never renders fetched HTML.

`ResearchRun` records every execution attempt, including failures. Each retry and stale-worker recovery creates a fresh run and job payload, so a partially persisted run is never rediscovered into. A completed `ResearchReport` is a separate immutable version and exists only after usable source/evidence and synthesis-reference validation pass. `ResearchQueued -> Researching -> ResearchReady` is centralized by `VideoProject`; terminal failure uses `ResearchFailed`, which can explicitly queue another run. Premium stages receive only bounded source metadata, source-bound evidence, claims, gaps, and conflicts; full fetched pages never reach Premium.

## Phase 9 evidence-grounded outline flow

```text
ResearchReady + current ResearchReport + approved Pilot context
    -> API (202) -> Job -> Worker -> bounded OutlineGenerationContext
    -> Reasoning narrative plan -> deterministic reference/experiment validation
    -> immutable VideoOutline version -> OutlineReady
    -> structured user edits/reorder -> explicit approval -> OutlineApproved
```

`OutlineGenerationContextBuilder` consumes Phase 8's immutable report, supported/corroborated/conflicted claims, limited evidence excerpts, conflicts, gaps, and Pilot control strategy. It rejects stale research, unsupported critical premises, and cross-report relationships. It never calls `IResearchSearchClient` or `IResearchContentFetcher`, and full webpages cannot enter this workflow.

The AI selects narrative structure, information order, planning-level section objectives, viewer questions, transitions, payoff, pacing, and claim placement. C# owns project/report identity, source freshness, input fingerprint, version, workflow/artifact status, bounded retries, section sequence, claim ownership/support, conflict/gap references, duration totals, active-job idempotency, and approval. Malformed JSON may receive one Fast mechanical repair; semantic evidence or experiment errors receive a bounded Reasoning correction.

`VideoOutline`, `VideoOutlineSection`, and relational Section-to-Claim/Conflict/Gap links preserve the exact ResearchReport version and the path `Section -> ResearchClaim -> ResearchClaimEvidence -> ResearchEvidence -> ResearchSource`. Reordering is transactional and requires transition review before approval. A newer ResearchReport or changed material execution context makes an Outline stale without deleting it; stale ready Outlines cannot be approved. An approved Outline is immutable and is the unambiguous Phase 10 source.

## Phase 10 evidence-grounded Script flow

```text
OutlineApproved + approved VideoOutline + exact ResearchReport
    -> API (202) -> Job -> Worker -> bounded ScriptGenerationContext
    -> Premium structured narration -> deterministic structure/reference/length validation
    -> Reasoning grounding audit -> bounded Premium correction and re-audit when required
    -> relational VideoScript version -> ScriptReady
    -> structured block edit -> Grounding Pending -> async revalidation
    -> explicit approval -> ScriptApproved
```

`ScriptGenerationContextBuilder` loads the explicitly Approved Outline and its exact source ResearchReport. Every Outline Section receives only its own supported/corroborated/conflicted Claims, source-bound evidence excerpts, conflicts, and gaps plus audience, ViewerPromise, Narrative Strategy, content language, and Pilot experiment controls. It rejects stale fingerprints and invalid ownership. The Script module has no search/fetch dependency, never creates Research entities, and never mutates WorkingTitle or upstream artifacts.

The writer, auditor, correction, and structural-repair calls are separate `AiRun` records. C# owns IDs, exact section/block order, reference validation, versions, word counts, runtime estimates, length policy, job/state transitions, staleness, and approval. The worker renews and fences the job lease; the final artifact and `ScriptReady` transition commit together. Terminal generation failure restores `OutlineApproved` (or the previous `ScriptReady` during regeneration) and creates no successful Script version.

`VideoScript`, `VideoScriptSection`, `VideoScriptBlock`, and relational block-to-Claim/Conflict links are the Phase 11 boundary. An approved Script is immutable and unambiguously queryable. Phase 11 may consume its ordered narration and runtime metadata without parsing one text blob or rerunning Research/Script generation.

## Phase 11 production-package flow

`ScriptApproved -> Packaging -> ProductionReady` is the approval boundary. Initial terminal generation failure returns the VideoProject to `ScriptApproved`; regeneration failure leaves `Packaging` and retains prior package versions. A unique filtered job index coalesces active production work per VideoProject, while worker leases, stale-job recovery, bounded retries, and fenced completion follow the existing durable SQL job pattern.

The Application layer builds a bounded `ProductionGenerationContext` from the current approved Script and its exact Outline/Research lineage. It includes ordered narration blocks plus only their persisted Claims, evidence locators, and conflicts; it performs no search or fetch. Reasoning-class generation produces typed scenes, shots, assets, and overlays. Deterministic validation owns identity/order/limits/claim reachability/rights rules and allocates all seconds so package runtime equals the approved Script estimate. A separate Reasoning grounding audit may trigger one bounded Reasoning correction. Fast is reserved for mechanical malformed-JSON repair.

Persistence is relational: `ProductionPackage`, `ProductionScene`, `ProductionSceneScriptBlock`, `ProductionShot`, `ProductionAssetRequirement`, `ProductionOnScreenText`, and Claim link tables. `(ProductionPackageId, ScriptBlockId)` is unique, package versions are append-only, and only one package may be Approved per VideoProject. A SQL `rowversion` on `ProductionPackage` prevents racing edit/approve operations from approving stale audited state. Original shot relative-duration weights are persisted separately from allocated integer seconds so validation is idempotent across reads. Package instruction edits are allowed only on the latest Ready version and reset grounding to Pending. Script mapping, claim links, factuality classifications, acquisition modes, duration, and lineage are read-only to the user.

The production-package Vietnamese reading aid reuses the shared `artifact_localizations` queue/cache. Its fingerprint covers only selected reader-facing explanatory fields and validators preserve exact Scene/Shot/Asset identity and order. Canonical production instructions and exports are never replaced by the overlay.
