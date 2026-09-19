# API

All business responses use JSON. Failures use RFC 7807 problem details without provider secrets or raw upstream bodies.

## Projects

- `POST /api/projects` creates a project. Example body: `{"name":"Creator Research","marketName":"Education","targetLanguage":"English","targetGeography":"Global","audienceDescription":"Independent video creators"}`.
- `GET /api/projects` lists projects.
- `GET /api/projects/{projectId}` returns one project.

## Competitors

- `POST /api/projects/{projectId}/competitors` resolves and collects a channel. Body: `{"youtubeUrl":"https://youtube.com/@handle"}`. A new competitor returns `201`; refreshing an existing channel returns `200` with the same ID.
- `GET /api/projects/{projectId}/competitors` lists collected channels.
- `GET /api/projects/{projectId}/competitors/{competitorId}` returns channel metadata and collected videos.
- `POST /api/projects/{projectId}/competitors/{competitorId}/analysis:run` queues competitor intelligence and returns `202 Accepted` with `jobId`, `status`, and whether an equivalent active job was reused.
- `GET /api/projects/{projectId}/competitors/{competitorId}/analysis` returns the newest persisted report plus active/latest job status. The report contains version and provenance metadata, source-data timestamp/count, stale flag, and typed structured intelligence; it does not regenerate analysis.
- `POST /api/projects/{projectId}/competitors/{competitorId}/analysis/{analysisId}/localizations/vi` returns a persisted Vietnamese reading aid when available, otherwise queues one translation job and returns `202`. It never changes the canonical analysis.
- `GET /api/projects/{projectId}/competitors/{competitorId}/analysis/{analysisId}/localizations/vi` returns the persisted Vietnamese overlay and its job state for polling. English is returned from the canonical analysis endpoint and does not require localization.

Supported inputs are absolute HTTP(S) URLs no longer than 2,048 characters in the forms `youtube.com/@handle` and `youtube.com/channel/{24-character-channel-id}`, with optional `www`/`m`, trailing slash, or query string. Valid international handles are accepted. Malformed or unsupported URLs return `400`; missing projects or channels return `404`; concurrent duplicates return `409`. YouTube quota exhaustion returns `429`, configuration/authentication/transient failures return `503`, and malformed upstream data returns `502`.

## Security boundary

Phase 2 is intentionally single-user and has no authentication or workspace authorization. Project IDs scope every competitor read/write and prevent accidental cross-project access, but they are not an authorization boundary. Authentication and workspace isolation must be implemented before exposing the API to multiple users or an untrusted network.

## Operations

- `GET /` returns the service name and phase marker.
- `GET /health/live` confirms the process can answer requests.
- `GET /health/ready` checks SQL Server connectivity and YouTube API-key configuration.

## AI analysis behavior

Competitor IDs are always scoped to their route project. A competitor without collected videos returns a validation problem instead of consuming an AI call. Repeated run commands while a matching job is queued/running return the existing job. A failed job preserves a safe failure message and may be retried by submitting a new run command. Start `YoutubeAiFactory.Worker` to execute queued jobs.

## Opportunities

- `POST /api/projects/{projectId}/opportunities:generate` queues one project-scoped opportunity analysis and returns `202` with a job ID, status, and active-job reuse indicator.
- `GET /api/projects/{projectId}/opportunities/latest` returns the latest immutable report, status, analyzed-competitor count, score components, sources, validated evidence, risks, limitations, and stale flag.
- `POST /api/projects/{projectId}/opportunities/{reportId}/localizations/vi` returns a persisted Vietnamese reading aid when available, otherwise queues one translation job and returns `202`. It never changes the canonical opportunity report, including candidate names, scores, decisions, or evidence references.
- `GET /api/projects/{projectId}/opportunities/{reportId}/localizations/vi` returns the persisted Vietnamese overlay and its job state. English continues to come from the canonical opportunity report endpoint.

At least one completed competitor analysis is required. Generation may use completed analyses even when some collected competitors are not analyzed; that limitation is exposed by the response and UI.

## Ideas

- `POST /api/projects/{projectId}/opportunities/{opportunityId}:approve` and `:reject` persist an opportunity decision.
- `POST /api/projects/{projectId}/opportunities/{opportunityId}/ideas:generate` queues a generation for an approved opportunity and returns `202`; a matching active job is reused.
- `GET /api/projects/{projectId}/opportunities/{opportunityId}/ideas` returns ranked persisted ideas, generation history, staleness, and job state without generating anything.
- `GET /api/projects/{projectId}/opportunities/{opportunityId}/idea-generations` lists immutable generation history; `GET /api/projects/{projectId}/idea-generations/{generationId}` returns one generation and its ideas.
- `POST /api/projects/{projectId}/ideas/{ideaId}:approve` and `:reject` persist the individual idea decision.

All routes remain project-scoped; non-approved opportunities return a validation problem when generation is requested.

## Pilots

- `POST /api/projects/{projectId}/pilots:generate` queues a project-level Pilot from ideas whose idea and source opportunity are approved, and returns `202`. A matching queued/running job is reused.
- `GET /api/projects/{projectId}/pilots/latest` returns eligibility count, active/latest job state, and the latest persisted Pilot.
- `GET /api/projects/{projectId}/pilots` lists immutable Pilot versions, newest first.
- `GET /api/projects/{projectId}/pilots/eligible-ideas` lists approved project ideas available for a draft-slot replacement.
- `GET /api/projects/{projectId}/pilots/{pilotId}` returns one Pilot version.
- `POST /api/projects/{projectId}/pilots/{pilotId}:approve` approves a draft Pilot unless it requires review because a source idea or source opportunity is no longer approved.
- `POST /api/projects/{projectId}/pilots/{pilotId}/slots/{sequence}:replace` replaces a draft slot with an unused approved project idea while preserving its fixed experiment block.
- `POST /api/projects/{projectId}/pilots/{pilotId}/slots/{sequence}:move` moves a draft slot within its existing experiment block.

At least 12 ideas with approved source opportunities are required. The API does not generate ideas automatically or ingest analytics.

## Video Projects

- `POST /api/projects/{projectId}/pilots/{pilotId}/video-projects` creates (or idempotently returns) the Draft VideoProject for an approved pilot slot. Body: `{"pilotVideoId":"..."}`.
- `GET /api/projects/{projectId}/video-projects` lists project execution workspaces.
- `GET /api/projects/{projectId}/video-projects/{videoProjectId}` returns its execution brief, exact lineage, experiment context, current source-review signal, and snapshot warnings.
- `PATCH /api/projects/{projectId}/video-projects/{videoProjectId}` updates only `workingTitle` and `executionNotes`.

Creation returns `400` when the Pilot is not approved or the source idea/opportunity requires review, and `404` for missing or cross-project resources. The API never accepts a status field.

## Video Project Research

- `POST /api/projects/{projectId}/video-projects/{videoProjectId}/research:run` queues evidence-backed research and returns `202 Accepted` with `jobId`, `status`, and `existing`. Calls while an active job exists reuse it.
- `GET /api/projects/{projectId}/video-projects/{videoProjectId}/research/latest` returns the latest report, active/latest job, and latest run status for polling; it does not generate research.
- `GET /api/projects/{projectId}/video-projects/{videoProjectId}/research` lists immutable report versions; `GET .../research/{reportId}` returns a structured report and run history.
- `POST` / `GET .../research/{reportId}/localizations/vi` lazily creates or reads the Vietnamese reading overlay. It changes only reader-facing synthesis text, never sources, claims, evidence, or report version.

Research is valid from `Draft`, `ResearchReady`, `ResearchFailed`, or an unapproved `OutlineReady` state. Rerunning it from `OutlineReady` preserves historical Outlines, which become stale against the new current report. A bad state is a validation problem; missing or cross-project video projects/reports return `404`. Search/fetch/AI failure is persisted on the job/run and returns a safe actionable message through polling. A failed run creates no report version.

## Video Project Outlines

- `POST /api/projects/{projectId}/video-projects/{videoProjectId}/outline:generate` queues a Reasoning outline job and returns `202 Accepted` with `jobId`, `status`, and `existing`. An active job for the same VideoProject is reused.
- `GET /api/projects/{projectId}/video-projects/{videoProjectId}/outline/latest` returns the latest Outline, active/latest job, `canGenerate`, and an actionable block reason.
- `GET /api/projects/{projectId}/video-projects/{videoProjectId}/outlines` lists immutable Outline versions; `GET .../outlines/{outlineId}` returns a specific version with ordered Sections and claim/evidence/source traceability.
- `PATCH .../outlines/{outlineId}` edits Ready Section headings, objectives, summaries, viewer questions, transition intents, and timing estimates. It does not accept claim IDs.
- `PUT .../outlines/{outlineId}/sections/order` transactionally reorders every Section ID exactly once. The resulting Outline requires transition review through a subsequent structured edit before approval.
- `POST .../outlines/{outlineId}:approve` approves only the latest Ready, non-stale, fully validated version and transitions the VideoProject to `OutlineApproved`.
- `POST` / `GET .../outlines/{outlineId}/localizations/vi` lazily creates or reads a Vietnamese reading overlay for the same Outline ID/version.

Generation requires a current ResearchReport and `ResearchReady` or `OutlineReady` state. Missing/stale research, unsupported critical premise, stale approval, invalid ownership, or immutable-approved edits return business validation problems; missing or cross-project resources return `404`. Generation failure restores the prior retryable workflow state, preserves earlier Outline versions, and exposes safe Job/AiRun failure details. These routes do not perform research, create claims/evidence/sources, update WorkingTitle/ViewerPromise, or generate a Script.

## Video Project Scripts

- `POST /api/projects/{projectId}/video-projects/{videoProjectId}/script:generate` queues Premium generation plus Reasoning grounding and returns `202 Accepted` with `jobId`, `status`, and `existing`; one active Script workflow per VideoProject is reused.
- `GET /api/projects/{projectId}/video-projects/{videoProjectId}/script/latest` returns the latest Script, active/latest job, `canGenerate`, and an actionable block reason.
- `GET /api/projects/{projectId}/video-projects/{videoProjectId}/scripts` lists version history; `GET .../scripts/{scriptId}` returns ordered Sections/Blocks with Claim, Evidence, Source, and Conflict traceability.
- `PATCH .../scripts/{scriptId}` updates narration text for identified Blocks of the latest Ready version. Every edit recalculates metrics and changes grounding to `Pending`; Claim IDs and structural identities are not accepted from the client.
- `POST .../scripts/{scriptId}:validate` queues a Reasoning audit of current edited narration and returns `202`.
- `POST .../scripts/{scriptId}:approve` approves only the latest Ready, current, structurally valid, grounding-passed version and transitions the VideoProject to `ScriptApproved`.

Initial generation requires `OutlineApproved`; regeneration may create another immutable version while the project is `ScriptReady`. The source Approved Outline and its exact ResearchReport must still be current. Terminal generation failure restores the prior retryable state and preserves earlier Script versions. Approved Scripts are immutable. These routes never search/fetch, create or mutate Research, change WorkingTitle, or generate production scenes/assets/audio.

## Phase 11 production package

- `POST /api/projects/{projectId}/video-projects/{videoProjectId}/production-package:generate` returns `202` with an idempotent job identity. Allowed for a current approved Script while `ScriptApproved` or for regeneration while `Packaging`.
- `GET /api/projects/{projectId}/video-projects/{videoProjectId}/production-package/latest` returns latest package, active/latest job, and generation eligibility.
- `GET /api/projects/{projectId}/video-projects/{videoProjectId}/production-packages` lists immutable version history.
- `GET /api/projects/{projectId}/video-projects/{videoProjectId}/production-packages/{packageId}` returns one project-scoped version with narration derived from ScriptBlock mappings and Claim source locators.
- `PATCH /api/projects/{projectId}/video-projects/{videoProjectId}/production-packages/{packageId}` edits only creator-facing instruction text on the latest Ready version and changes grounding to Pending. Mapping, lineage, claims, timing, classifications, and approved packages are immutable.
- `POST /api/projects/{projectId}/video-projects/{videoProjectId}/production-packages/{packageId}:validate` returns `202` and queues audit-only revalidation.
- `POST /api/projects/{projectId}/video-projects/{videoProjectId}/production-packages/{packageId}:approve` approves only the latest, current, Passed version and atomically moves the VideoProject to `ProductionReady`.
- `GET /api/projects/{projectId}/video-projects/{videoProjectId}/production-package:export` returns approved-only JSON with `schemaVersion: production-package-export:v1`.

All routes verify the parent Project/VideoProject scope. Generation and validation are asynchronous. Failed generation creates no package version. Existing versions remain readable after failed regeneration.
