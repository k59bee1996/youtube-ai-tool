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

## Phase 2 request flow

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
