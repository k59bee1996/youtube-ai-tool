# Domain Model

## Projects

`Project` is the aggregate root for a research workspace. It has a name and timestamps and owns `Market` (name, target language, target geography) and `AudienceProfile` (audience description). Domain constructors and methods enforce required fields and length limits.

## Competitors

`CompetitorChannel` belongs to one project. It keeps the submitted source URL separately from the resolved YouTube ID, handle, title, description, thumbnail, publication date, channel statistics, and collection timestamp. A resolved YouTube ID is immutable, and only a fully resolved channel may be persisted. `CompetitorVideo` belongs to one channel and records its stable YouTube ID, URL, descriptive metadata, duration, statistics, publication date, and collection timestamp.

Submitting the same resolved channel for a project refreshes its metadata and upserts videos by YouTube video ID. Existing videos outside the configured recent-video window are retained. Hidden or unavailable statistics are nullable rather than represented as zero. Database uniqueness constraints provide final protection against duplicate provider identities.

## Competitor intelligence

`CompetitorAnalysis` is an immutable completed report owned by a competitor channel. It stores a monotonically increasing version, structured-result JSON, `AiRun` link, prompt/provider/model provenance, analysis-video count, and source-data timestamp. It never overwrites an earlier report. `AiRun` additionally records the competitor ID for Phase 3 traceability.

## Operations

`Job` models database-backed background work with explicit state transitions and bounded retries. Phase 3 uses it for competitor analysis. `AiRun` records the requested logical model profile separately from the resolved provider/model, plus prompt version, token usage, cost, latency and failure details. This is provenance metadata rather than a domain model-selection dependency; pre-routing records are retained as `Unspecified`.

## Opportunities

`OpportunityReport` is an immutable, project-owned generation version. `OpportunityReportSource` captures exact competitor-analysis IDs and versions. Relational `OpportunityCandidate` rows retain searchable target fields and scores, while `OpportunityEvidence` points to the source analysis, competitor, and optional video. Candidate decision states are `Candidate`, `Approved`, and `Rejected`.

## Ideas

`IdeaGeneration` is an immutable batch version scoped to one approved `OpportunityCandidate`, retaining its source report/version, prompt/provider/model provenance, scoring version, and `AiRun`. `VideoIdea` is a concrete structured content hypothesis, not a pilot or video project. It stores packaging concepts, audience and viewer intent, hypothesis, score components, confidence, risks, and decision state (`Candidate`, `Approved`, `Rejected`). `IdeaEvidence` links each idea to persisted `OpportunityEvidence`, preserving the path to competitor intelligence. A newer opportunity report can mark a generation stale without destroying it.

## Pilots

`Pilot` is a project-owned generation version of a learning plan, not a `VideoProject`. Its draft/approved state records provenance, prompt and planning versions, limitations, and balance warnings; a draft may be adjusted, while approval freezes it. A `PilotVideo` directly references one `VideoIdea` and its source opportunity, and records sequence, experiment type, hypothesis, variable, control strategy, planned primary metric, success signal, and rationale. Database constraints make `(PilotId, Sequence)` and `(PilotId, VideoIdeaId)` unique, and enforce 1–4 Topic, 5–8 Packaging, and 9–12 Storytelling blocks. A source idea or source opportunity becoming unapproved makes the Pilot require review.

## Deferred concepts

## Video projects

`VideoProject` is a durable execution aggregate, not another idea or a pilot slot. One approved `PilotVideo` can create at most one VideoProject. It preserves live relational references to `Project`, exact `Pilot` version, `PilotVideo`, `VideoIdea`, and `OpportunityCandidate`; it snapshots the initial title, topic, angle, format, audience, hook, thumbnail concept, viewer promise, and experiment brief so future upstream edits cannot silently rewrite production intent. Only working title and execution notes are editable in Phase 7.

The production-state vocabulary is `Draft`, `ResearchQueued`, `Researching`, `ResearchFailed`, `ResearchReady`, `OutlineGenerating`, `OutlineReady`, `OutlineApproved`, `ScriptGenerating`, `ScriptReady`, `ScriptApproved`, `Packaging`, and `ProductionReady`. The aggregate centralizes its transition matrix. Research failure is explicit so a worker failure cannot leave a VideoProject reporting `Researching` forever; `ResearchFailed` can explicitly queue another run.

## Research

`ResearchRun` is a project/video-project execution attempt. It records `research-engine:v1`, a deterministic execution-brief fingerprint, timestamps, source/evidence/claim/conflict counts, external failure counts, status, and a safe failure reason. A failed run never pretends to be a completed report.

`ResearchReport` is an immutable successful artifact with a per-VideoProject version, exact input fingerprint, algorithm version, synthesis provenance, and structured presentation JSON. It owns relational `ResearchClaim` records, explicit `ResearchClaimEvidence` support/contradiction links, and `ResearchConflict` records. `ResearchSource` and `ResearchEvidence` belong to the run so retrieval remains observable even for failed attempts. Evidence is source-bound and retains only an extracted fact, short source excerpt, locator, type, and confidence.

Support describes the retained research dataset, not universal truth: no supporting link is `Unsupported`; one independent support source is `Supported`; two or more independent source keys are `Corroborated`; any valid contradiction link is `Conflicted`. V1 uses source domain plus content hash/canonical URL as conservative independence signals.
