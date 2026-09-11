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

`Domain` has no framework or integration dependencies. `Application` defines use cases and external contracts. `Infrastructure` owns EF Core, PostgreSQL, provider adapters, and migrations. `Api` maps HTTP contracts to application use cases. `Worker` is reserved for long-running work.

## Phase 2 collection flow

```text
React -> project/competitor endpoint -> application handler
                                      -> IYouTubeClient -> YouTube Data API
                                      -> IYoutubeAiFactoryStore -> PostgreSQL
```

Endpoints contain transport mapping only. `AddCompetitorHandler` coordinates URL parsing, provider calls, domain updates, and one database commit. The YouTube adapter owns HTTP details and retry/error classification. Collection is synchronous, bounded to 1–50 recent uploads, and cancellation-aware. This keeps the first vertical slice observable and testable without introducing queue processing early.

## Persistence and ownership

`YoutubeAiFactoryDbContext` uses the `yaf` PostgreSQL schema. `Project` owns `Market` and `AudienceProfile`; competitor channels belong to projects and videos belong to channels. Unique constraints on `(project_id, youtube_channel_id)` and `(competitor_channel_id, youtube_video_id)` make refreshes idempotent at the database boundary. EF migrations are the only supported schema-change mechanism.

Only fully resolved competitors may be persisted: channel identity, title, and collection timestamp are required columns. A refresh cannot change an existing competitor's provider identity. One `SaveChangesAsync` call commits channel and video changes atomically; a uniqueness failure rolls back the full refresh.

## Runtime behavior

`/health/live` checks the process. `/health/ready` checks PostgreSQL and YouTube configuration. Structured collection logs include project, outcome, competitor/channel when resolved, video count, and elapsed time; API keys and response bodies are excluded. Job claiming and dispatch remain deferred.

See [ADR-001](adr/ADR-001-modular-monolith.md), [ADR-002](adr/ADR-002-postgresql-and-jobs.md), and [ADR-003](adr/ADR-003-synchronous-competitor-collection.md).

## Phase 3 competitor-analysis flow

```text
React -> API (202 Accepted) -> Job table -> Worker -> application workflow
                                                    -> context builder -> ILlmProvider
                                                    -> validator -> analysis/AiRun tables
```

The controller only queues or reads work. The application context builder selects a bounded mix of recent, high-, low-, and representative videos and calculates quantitative signals before the provider is called. `competitor-analysis:v1` is the first immutable prompt contract. The worker claims jobs with PostgreSQL row locking (`FOR UPDATE SKIP LOCKED`), which serializes normal concurrent claims. A future lease-fencing or heartbeat mechanism is still required so that a slow worker cannot finalize work after its lease has expired and another worker has reclaimed the job.

Completed `CompetitorAnalysis` records are immutable versions. Their nested typed result is stored as one `jsonb` aggregate because it is rendered and consumed as an analysis report; provenance, versions, source timestamp, provider and model remain relational columns. A refresh after `SourceDataAsOf` marks the latest report stale without automatically spending another AI call.

## Phase 4 opportunity-analysis flow

```text
React -> API (202) -> Job -> Worker -> bounded cross-competitor context
                                  -> ILlmProvider -> validation -> deterministic scoring
                                  -> immutable report, sources, candidates, evidence
```

The provider can assess novelty, audience fit, transferability, story potential and production complexity, but it cannot provide final scores or arbitrary evidence. C# validates backend-issued evidence IDs and derives observed demand, evidence strength, dataset competition risk, and the final `opportunity-score:v1` score. Each report records exact source-analysis versions, enabling stale detection.

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
