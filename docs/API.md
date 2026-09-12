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

At least 12 ideas with approved source opportunities are required. The API does not generate ideas automatically, ingest analytics, create VideoProjects, or start production.
