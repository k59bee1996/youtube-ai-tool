# Product Scope

## Objective

YouTube AI Factory converts market and competitor evidence into structured, reusable content intelligence. V1 will guide creators from market research through ideas, pilots, research, scripts, and production packages; it will not create or upload complete videos.

## Current delivery: Phase 2

The repository provides the foundation plus the first end-to-end business slice:

- modular monolith, React client, API and worker hosts;
- PostgreSQL development/test services and EF Core migrations;
- project creation, listing, lookup, and persisted workspace selection;
- official YouTube Data API channel resolution and bounded recent-video collection;
- idempotent competitor refresh with channel and video metadata displayed from storage;
- structured logging, health checks, typed problem responses, and automated tests using provider fakes.

## Explicitly deferred

Project update/delete, competitor deletion, background collection, competitor analysis, opportunity scoring, idea generation, pilots, video projects, research, outlines, scripts, production packages, authentication, and LLM provider implementations remain future phases. Full video generation, uploads, billing, teams, microservices, and distributed infrastructure remain outside V1.

## Next product milestone

Phase 3 should consume stored Phase 2 evidence through a provider-neutral analysis use case. Define versioned structured outputs, prompts, and provenance before connecting any real LLM provider.
