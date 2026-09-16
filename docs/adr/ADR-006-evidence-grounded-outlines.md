# ADR-006: Evidence-grounded, versioned Video Outlines

## Status

Accepted for Phase 9.

## Context

The Script workflow needs a stable narrative plan without collapsing Research directly into generated prose. Narrative ordering requires model reasoning, but all factual material must remain traceable to the immutable Phase 8 ResearchReport and its support/conflict semantics. Pilot experiments also require the Outline to preserve the variable and controls selected earlier.

## Decision

Introduce `VideoOutline` as a versioned artifact owned by a VideoProject and tied to one exact ResearchReport ID/version and deterministic input fingerprint. Store Narrative Strategy and important metadata relationally. Store ordered `VideoOutlineSection` rows and relational joins to ResearchClaim and ResearchConflict; reference Research gaps by stable position in the immutable report payload.

Run generation asynchronously through the existing SQL Server Job/Worker infrastructure. Use the Reasoning model profile for narrative design and Fast only for one mechanical structured-output repair. C# validates ownership, freshness, support state, sequence, references, experiment fidelity, versions, status, and approval. The Outline workflow cannot call search/fetch providers or create Research artifacts.

Ready Outlines may receive structured planning-text edits and transactional reorder. Reorder requires explicit transition review. Approval freezes the latest current version and advances the VideoProject to `OutlineApproved`. A new ResearchReport makes prior Outlines stale but does not delete them. Vietnamese is a separately persisted reader overlay with exact identity/order preservation.

## Consequences

- Phase 10 has one explicit approved source with ordered narrative intent and complete evidence traceability.
- Failed jobs remain observable in Job/AiRun without consuming an artifact version.
- Claim text and source metadata are not duplicated into Outline storage; historical meaning depends on immutable ResearchReport history.
- V1 intentionally does not provide post-approval revision/supersession or Script generation.
