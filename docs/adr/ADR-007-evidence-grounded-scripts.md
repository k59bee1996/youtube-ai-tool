# ADR-007: Persist evidence-grounded Scripts as relational structured narration

## Status

Accepted for Phase 10.

## Context

Viewer-facing narration must follow an explicitly Approved VideoOutline, remain auditable to persisted ResearchClaims, survive editing/versioning, and become a safe Phase 11 input. A single text blob cannot retain section identity, block-level Claim links, conflict handling, or reliable edit-time grounding state. The writing and evidence-review objectives also require different AI behavior.

## Decision

Persist each successful Script as an immutable version header with ordered relational `VideoScriptSection` and `VideoScriptBlock` rows plus block-to-Claim/Conflict links. Generation uses the Premium profile; an independent grounding audit uses Reasoning; evidence-safe correction uses Premium; mechanical malformed-output repair uses Fast. C# validates all IDs/order/support relationships, calculates words/runtime, controls retries/state/version/approval, and persists only an audit-passed candidate.

Any narration edit keeps structural IDs but invalidates the prior grounding pass. Revalidation is a background Reasoning audit. Approval freezes the latest current version and makes it the unambiguous Phase 11 source. The workflow consumes only persisted Outline/Research structures and has no search/fetch contracts.

## Consequences

- Phase 11 can consume ordered approved narration without parsing or regenerating a monolithic string.
- Evidence provenance and conflict semantics remain queryable per block.
- Generation, audit, correction, and repair cost/latency are independently observable through `AiRun`.
- Editing is deliberately conservative: every narration change requires revalidation.
- Runtime remains a deterministic WPM estimate, not measured audio duration.
- Additional tables and joins are accepted in exchange for integrity and downstream usability.
