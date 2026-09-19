# ADR-008: Evidence-grounded production packages

Status: Accepted for Phase 11.

## Decision

Model production planning as a versioned relational artifact sourced from exactly one approved Script. Persist ScriptBlock-to-Scene mapping separately from editable production instructions. Derive narration from the immutable Script, allocate timing in application code, and require a separate visual-grounding audit before approval.

Use controlled factuality, shot, asset, acquisition, text, purpose, and complexity taxonomies. Store ResearchClaim links for factual shots/assets/text. Require rights review for sourced media and reject generated media presented as archival. Use the existing SQL-backed job, lease, retry, AiRun, and provider-neutral model-routing infrastructure.

## Consequences

Production plans remain reproducible, reviewable, versioned, and traceable to evidence. Creator edits cannot silently change narration or lineage and invalidate grounding until audit-only validation passes. Exact timing and coverage are deterministic. The design supports future renderers and asset providers without adding them to Phase 11.

The relational shape is larger than a single JSON payload and requires explicit mapping code, but it makes ordering, uniqueness, ownership, and evidence integrity enforceable. Canonical production instructions do not receive localization overlays because translating them would mutate production semantics.
