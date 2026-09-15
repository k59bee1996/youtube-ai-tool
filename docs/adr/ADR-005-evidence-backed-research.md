# ADR-005: Evidence-backed research as a worker-owned, versioned artifact

- Status: Accepted
- Date: 2026-09-14

## Context

Video research needs public-source discovery, untrusted-content retrieval, structured AI analysis, and provenance that a later outline can audit. A synchronous endpoint or a free-form model answer would make failures, citations, retrying, and version lineage unreliable.

## Decision

- Queue research as a database-backed `video-research` Job and execute it only in the Worker.
- Persist a `ResearchRun` for every attempt and create an immutable, versioned `ResearchReport` only after a successful run.
- Retain source metadata and bounded source-bound evidence, but not complete fetched pages. Search snippets remain discovery metadata, never evidence.
- Keep search and fetch behind Application interfaces. Infrastructure supplies the Tavily adapter and a redirect-aware, public-HTTP(S)-only HTML fetcher.
- Use structured AI outputs for query planning, relevance, evidence extraction, contradiction analysis, and synthesis. C# validates identifiers and excerpts, derives claim support, manages states, and rejects unsupported references.
- Treat `ResearchFailed` as an explicit VideoProject state. A new attempt remains separate from both failed runs and completed report versions.

## Consequences

The API returns `202 Accepted` and clients poll persisted state. Research is bounded and auditable, and Phase 9 can consume an approved report without rediscovering sources. The design adds relational tables and worker operations, while actual search access still requires a configured server-side provider key. V1 supports bounded HTML extraction only; PDF/document extraction is deferred.
