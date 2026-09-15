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

`Domain` has no framework or integration dependencies. `Application` defines use cases and external contracts. `Infrastructure` owns EF Core, SQL Server, provider adapters, and migrations. `Api` maps HTTP contracts to application use cases. `Worker` is reserved for long-running work.

## AI model routing

AI workflows request an application-level capability, never a concrete vendor model:

```text
Workflow -> AiModelProfile -> IAiModelResolver -> ILlmProvider -> configured provider/model
```

`Fast` is for mechanical transformation such as artifact localization and future structural repair. `Reasoning` is for bounded competitor interpretation and idea synthesis. `Premium` is reserved for high-leverage strategic synthesis and controlled experiment planning. The configuration-backed resolver is stateless and resolves each request independently; a missing or invalid required profile fails clearly and does not silently downgrade to another profile. Provider API keys remain in configuration only.

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

See [ADR-001](adr/ADR-001-modular-monolith.md), [ADR-002](adr/ADR-002-postgresql-and-jobs.md), [ADR-003](adr/ADR-003-synchronous-competitor-collection.md), and [ADR-004](adr/ADR-004-sql-server-persistence.md).

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
