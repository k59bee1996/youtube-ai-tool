# Product Scope

## Objective

YouTube AI Factory converts market and competitor evidence into structured, reusable content intelligence. V1 will guide creators from market research through ideas, pilots, research, scripts, and production packages; it will not create or upload complete videos.

## Current delivery: Phase 6

The repository provides the foundation plus the first end-to-end business slice:

- modular monolith, React client, API and worker hosts;
- PostgreSQL development/test services and EF Core migrations;
- project creation, listing, lookup, and persisted workspace selection;
- official YouTube Data API channel resolution and bounded recent-video collection;
- idempotent competitor refresh with channel and video metadata displayed from storage;
- structured logging, health checks, typed problem responses, and automated tests using provider fakes.
- bounded, background competitor AI analysis with deterministic performance features, validated typed output, immutable report versions, `AiRun` provenance, job progress, staleness detection, and a React intelligence report.
- bounded background cross-competitor opportunity analysis with immutable report versions, source provenance, validated evidence IDs, deterministic scoring, and a ranked React report.
- a bounded, worker-backed Idea Bank that requires an approved opportunity, retains immutable generation history, validates evidence provenance, avoids competitor-title and internal near-duplicates, calculates deterministic versioned scores, and supports idea approval/rejection.
- a worker-backed, versioned 12-video Pilot built only from ideas whose source opportunity is also approved. It has four Topic tests (1–4), four Packaging tests (5–8), and four Storytelling tests (9–12), each with an explicit hypothesis, variable, control strategy, planned metric, success signal, and rationale.

## Explicitly deferred

Project update/delete, competitor deletion, background collection, video projects, research, outlines, scripts, production packages, authentication, and alternate LLM providers remain future phases. Full video generation, uploads, billing, teams, microservices, and distributed infrastructure remain outside V1.

## Next product milestone

Phase 7 can consume an approved PilotVideo to create a production-oriented VideoProject; that workflow is intentionally not implemented yet.
