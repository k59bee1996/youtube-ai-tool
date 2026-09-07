# ADR-003: Synchronous Competitor Collection

## Status

Accepted for Phase 2.

## Context

The first business slice must resolve one YouTube channel, fetch a configurable number of recent uploads, persist them, and return the stored result. The YouTube Data API supports at most 50 playlist items per request, which gives this phase a small and predictable workload. The repository already contains a `Job` model and worker host, but dispatch, claiming, and progress reporting are later concerns.

## Decision

Run collection inside the request through `AddCompetitorHandler`, with cancellation, a 30-second HTTP timeout, bounded transient retries, and a configurable video limit of 1–50. Commit the refreshed channel and videos together. Keep provider HTTP behavior behind `IYouTubeClient` and persistence behind `IYoutubeAiFactoryStore`.

## Consequences

Clients receive the collected result immediately and failures map to one problem response. Tests can replace the provider without network calls. Requests may take several seconds and cannot resume after process failure. A later phase may move this handler behind the existing database-backed job boundary while retaining the same domain and adapter contracts.
