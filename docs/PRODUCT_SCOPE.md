# Product Scope

## Objective

YouTube AI Factory converts market and competitor evidence into structured, reusable content intelligence. V1 will guide creators from market research through ideas, pilots, research, scripts, and production packages; it will not create or upload complete videos.

## Current delivery: Phase 4

The repository provides the foundation plus the first end-to-end business slice:

- modular monolith, React client, API and worker hosts;
- PostgreSQL development/test services and EF Core migrations;
- project creation, listing, lookup, and persisted workspace selection;
- official YouTube Data API channel resolution and bounded recent-video collection;
- idempotent competitor refresh with channel and video metadata displayed from storage;
- structured logging, health checks, typed problem responses, and automated tests using provider fakes.
- bounded, background competitor AI analysis with deterministic performance features, validated typed output, immutable report versions, `AiRun` provenance, job progress, staleness detection, and a React intelligence report.
- bounded background cross-competitor opportunity analysis with immutable report versions, source provenance, validated evidence IDs, deterministic scoring, and a ranked React report.

## Explicitly deferred

Project update/delete, competitor deletion, background collection, idea generation, pilots, video projects, research, outlines, scripts, production packages, authentication, and alternate LLM providers remain future phases. Full video generation, uploads, billing, teams, microservices, and distributed infrastructure remain outside V1.

## Next product milestone

Phase 5 may consume a structured approved opportunity without re-parsing report text.
