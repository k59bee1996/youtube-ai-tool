# Product Scope

## Objective

YouTube AI Factory converts market and competitor evidence into structured, reusable content intelligence. V1 will guide creators from market research through ideas, pilots, research, scripts, and production packages; it will not create or upload complete videos.

## Current delivery: Phase 10

The repository provides the foundation plus the first end-to-end business slice:

- modular monolith, React client, API and worker hosts;
- SQL Server EF Core migrations and SQL Server-backed integration tests;
- project creation, listing, lookup, and persisted workspace selection;
- official YouTube Data API channel resolution and bounded recent-video collection;
- idempotent competitor refresh with channel and video metadata displayed from storage;
- structured logging, health checks, typed problem responses, and automated tests using provider fakes.
- bounded, background competitor AI analysis with deterministic performance features, validated typed output, immutable report versions, `AiRun` provenance, job progress, staleness detection, and a React intelligence report.
- bounded background cross-competitor opportunity analysis with immutable report versions, source provenance, validated evidence IDs, deterministic scoring, and a ranked React report.
- a bounded, worker-backed Idea Bank that requires an approved opportunity, retains immutable generation history, validates evidence provenance, avoids competitor-title and internal near-duplicates, calculates deterministic versioned scores, and supports idea approval/rejection.
- a worker-backed, versioned 12-video Pilot built only from ideas whose source opportunity is also approved. It has four Topic tests (1–4), four Packaging tests (5–8), and four Storytelling tests (9–12), each with an explicit hypothesis, variable, control strategy, planned metric, success signal, and rationale.

An approved PilotVideo can now be explicitly started as one idempotent Draft VideoProject. The execution workspace preserves immutable project/pilot/slot/idea/opportunity lineage and a creation-time execution brief; it allows only a working title and execution notes to change.

- a bounded, worker-backed research engine for a Draft/ResearchReady/ResearchFailed VideoProject. It creates a `ResearchRun` attempt, discovers public sources through a provider-neutral search client, retrieves only SSRF-safe bounded HTML, preserves short source-bound evidence, derives claim support deterministically, retains conflicts and gaps, and creates immutable successful `ResearchReport` versions.
- a Research workspace report with source links, claim/evidence inspection, conflicts, gaps, staleness indication, retry, report history API, and a lazy Vietnamese reading overlay for explanatory synthesis only.
- a bounded, worker-backed Outline Engine that consumes only the current immutable ResearchReport, creates structured Narrative Strategy and ordered Sections, validates claim/conflict/gap traceability and Pilot experiment fidelity, preserves immutable outline versions, and supports structured editing, transactional reordering, and explicit approval.
- an Outline workspace that exposes version history, evidence inspection, conflict/gap warnings, Pilot alignment, immutable approval, staleness, and a lazy Vietnamese reading overlay that never changes canonical IDs, references, order, or workflow state.
- a bounded, worker-backed Script Engine that requires the explicitly approved current Outline, sends only each Section's assigned claims/evidence/conflicts/gaps, uses Premium generation plus a separate Reasoning grounding audit, and persists ordered Script Sections/Blocks with exact Outline and ResearchReport lineage.
- a Script workspace with immutable generation history, narration reading, Claim/Evidence/Source inspection, structured grounding issues, deterministic word/runtime metadata, block editing, grounding invalidation/revalidation, and explicit immutable approval.

## Explicitly deferred

Project update/delete, competitor deletion, background collection, production packages, authentication, and alternate LLM providers remain future phases. Full video generation, uploads, billing, teams, microservices, and distributed infrastructure remain outside V1.

## Next product milestone

Phase 11 can consume the one approved `VideoScript`, its ordered Sections/Blocks, narration, content language, Claim traceability, and runtime metadata. Production packaging must not regenerate Research or Script and remains out of scope for Phase 10.
