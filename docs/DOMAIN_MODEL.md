# Domain Model

## Projects

`Project` is the aggregate root for a research workspace. It has a name and timestamps and owns `Market` (name, target language, target geography) and `AudienceProfile` (audience description). Domain constructors and methods enforce required fields and length limits.

## Competitors

`CompetitorChannel` belongs to one project. It keeps the submitted source URL separately from the resolved YouTube ID, handle, title, description, thumbnail, publication date, channel statistics, and collection timestamp. A resolved YouTube ID is immutable, and only a fully resolved channel may be persisted. `CompetitorVideo` belongs to one channel and records its stable YouTube ID, URL, descriptive metadata, duration, statistics, publication date, and collection timestamp.

Submitting the same resolved channel for a project refreshes its metadata and upserts videos by YouTube video ID. Existing videos outside the configured recent-video window are retained. Hidden or unavailable statistics are nullable rather than represented as zero. Database uniqueness constraints provide final protection against duplicate provider identities.

## Competitor intelligence

`CompetitorAnalysis` is an immutable completed report owned by a competitor channel. It stores a monotonically increasing version, structured-result JSON, `AiRun` link, prompt/provider/model provenance, analysis-video count, and source-data timestamp. It never overwrites an earlier report. `AiRun` additionally records the competitor ID for Phase 3 traceability.

## Operations

`Job` models database-backed background work with explicit state transitions and bounded retries. Phase 3 uses it for competitor analysis. `AiRun` records provider, model, prompt version, token usage, cost, latency and failure details.

## Deferred concepts

Content gaps, ideas, pilots, video projects, research reports, outlines, scripts, and production packages remain intentionally absent.
