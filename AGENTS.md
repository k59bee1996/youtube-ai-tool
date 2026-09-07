**# CODEX MASTER EXECUTION PLAN**

**## Project: YouTube AI Factory**

**---**

**# 0. ROLE**

You are the principal software engineer responsible for implementing **\*\*YouTube AI Factory\*\***.

Act as a senior engineer working on a production-grade system, not as a prototype generator.

Your responsibilities include:

\- understand requirements before implementation;

\- preserve architectural boundaries;

\- implement features incrementally;

\- write maintainable code;

\- add automated tests;

\- validate changes before marking work complete;

\- document architectural decisions;

\- avoid unnecessary complexity;

\- never silently change product requirements.

Optimize for:

1\. correctness;

2\. maintainability;

3\. observability;

4\. testability;

5\. extensibility;

6\. developer experience.

Do not optimize prematurely for massive scale.

**---**

**# 1. PRODUCT OBJECTIVE**

YouTube AI Factory helps a creator go from:

**\*\*Market research → Competitor intelligence → Opportunity discovery → Video ideas → Pilot planning → Research → Script → Production package\*\***

The product is NOT initially intended to automatically create or upload complete YouTube videos.

The primary value of V1 is:

\> Transform raw YouTube market information into structured content intelligence and production-ready plans.

**---**

**# 2. V1 PRODUCT SCOPE**

V1 must support these major capabilities.

**## Module 1 — Project Management**

User can:

\- create project;

\- define market;

\- define target audience;

\- define target language;

\- define target geography;

\- store project settings.

**---**

**## Module 2 — Competitor Research**

User can provide:

\- YouTube channel URLs;

\- YouTube video URLs.

System can:

\- resolve channel/video identity;

\- collect metadata;

\- collect channel videos;

\- store metrics;

\- identify likely outlier videos;

\- persist raw source data.

**---**

**## Module 3 — Competitor Intelligence**

System analyzes competitor data and produces structured results:

\- target audience;

\- topic clusters;

\- title patterns;

\- thumbnail patterns;

\- hook patterns;

\- script patterns;

\- storytelling patterns;

\- successful formats;

\- weak/failed patterns;

\- audience signals;

\- reusable format mechanics.

The system must analyze mechanics, NOT produce instructions to clone content.

**---**

**## Module 4 — Market / Niche Gap Analysis**

Generate:

\- niche opportunities;

\- format × niche combinations;

\- saturation assessment;

\- competition assessment;

\- audience fit;

\- opportunity score;

\- rationale.

**---**

**## Module 5 — Idea Engine**

Generate 20–30 candidate video ideas.

Each idea should include structured attributes such as:

\- title;

\- topic;

\- angle;

\- audience;

\- content format;

\- hook concept;

\- thumbnail concept;

\- hypothesis;

\- demand score;

\- novelty score;

\- competition score;

\- story potential;

\- thumbnail potential;

\- production difficulty;

\- overall score.

**---**

**## Module 6 — 12-Video Pilot**

System can select and organize a 12-video validation plan.

Goal:

**### Videos 1–4**

Test topic hypotheses.

**### Videos 5–8**

Test packaging hypotheses.

**### Videos 9–12**

Test storytelling / production hypotheses.

Every pilot video must have an explicit hypothesis.

**---**

**## Module 7 — Video Research**

For an approved idea:

generate structured research containing:

\- facts;

\- claims;

\- sources;

\- uncertainties;

\- useful statistics;

\- examples;

\- conflicts;

\- story opportunities;

\- risk flags.

Research and script generation must remain separate steps.

**---**

**## Module 8 — Outline**

Generate:

\- hook;

\- setup;

\- acts/sections;

\- escalation;

\- pattern interrupts;

\- payoff;

\- CTA.

Outline requires explicit approval before script generation.

**---**

**## Module 9 — Script**

Generate an original script based on:

\- approved research;

\- approved outline;

\- channel settings;

\- audience;

\- tone;

\- video target length.

Scripts must be versioned.

**---**

**## Module 10 — Production Package**

Convert approved script into structured production data.

Output contains:

\- narration;

\- scenes;

\- timestamps or timing estimates;

\- visual descriptions;

\- image prompts;

\- camera/motion suggestions;

\- transition suggestions;

\- subtitle-ready text;

\- thumbnail concepts;

\- metadata/title alternatives.

Example concept:

\`\`\`json

{

  "sceneNumber": 1,

  "purpose": "hook",

  "narration": "...",

  "visualDescription": "...",

  "imagePrompt": "...",

  "cameraMotion": "...",

  "transition": "..."

}

\`\`\`

**---**

**# 3. OUT OF SCOPE FOR V1**

Do NOT implement unless explicitly requested later:

\- full AI video generation;

\- automatic YouTube uploads;

\- automatic image generation;

\- automatic TTS;

\- CapCut automation;

\- billing;

\- subscriptions;

\- organization/team permissions;

\- complex RBAC;

\- mobile app;

\- microservices;

\- Kubernetes;

\- Kafka;

\- custom vector database infrastructure;

\- complex ML training.

Architect for future extensibility, but do not implement speculative infrastructure.

**---**

**# 4. REQUIRED TECHNOLOGY STACK**

**## Backend**

\- C#

\- ASP.NET Core

\- .NET 10

\- Entity Framework Core

\- PostgreSQL

**## Frontend**

\- React

\- TypeScript

Use a mainstream React stack with minimal unnecessary framework complexity.

**## Background work**

Separate .NET Worker process/project.

Start with database-backed jobs or another simple reliable queue abstraction.

Do not introduce distributed infrastructure until justified.

**## Realtime status**

Prefer SignalR when useful.

Polling is acceptable for an early vertical slice.

**## External integrations**

Abstract integrations behind interfaces:

\- YouTube;

\- LLM;

\- storage;

\- future search providers.

**---**

**# 5. ARCHITECTURE**

Build a:

**# MODULAR MONOLITH**

Suggested solution:

\`\`\`text

YoutubeAiFactory.sln

src/

  YoutubeAiFactory.Api/

  YoutubeAiFactory.Application/

  YoutubeAiFactory.Domain/

  YoutubeAiFactory.Infrastructure/

  YoutubeAiFactory.Worker/

tests/

  YoutubeAiFactory.Domain.Tests/

  YoutubeAiFactory.Application.Tests/

  YoutubeAiFactory.IntegrationTests/

docs/

\`\`\`

Do not create one project per small module.

Logical modules may include:

\`\`\`text

Projects

Competitors

Research

Markets

Ideas

Pilots

Videos

Production

AI

Jobs

\`\`\`

**---**

**# 6. ARCHITECTURAL DEPENDENCY RULES**

Dependencies must flow roughly:

\`\`\`text

API

 ↓

Application

 ↓

Domain

Infrastructure

 → implements Application/Domain abstractions

\`\`\`

Domain must NOT depend on:

\- Entity Framework;

\- ASP.NET;

\- OpenAI SDK;

\- YouTube SDK;

\- PostgreSQL;

\- HTTP;

\- UI concerns.

Application layer coordinates use cases.

Infrastructure implements external dependencies.

API handles:

\- HTTP concerns;

\- authentication;

\- validation boundary;

\- request/response mapping.

**---**

**# 7. DO NOT CALL LLMs FROM CONTROLLERS**

Never implement:

\`\`\`text

Controller

 → LLM API

\`\`\`

Required flow:

\`\`\`text

Controller

 ↓

Command / Use Case

 ↓

Application Service / Workflow

 ↓

Prompt Builder

 ↓

ILlmProvider

 ↓

Structured Result

 ↓

Validation

 ↓

Persistence

\`\`\`

Example:

\`\`\`text

GenerateIdeasCommand

 ↓

GenerateIdeasHandler

 ↓

IdeaGenerationWorkflow

 ↓

ILlmProvider

 ↓

IdeaGenerationResult

 ↓

IdeaScoringService

 ↓

Repository

\`\`\`

**---**

**# 8. AI PROVIDER ABSTRACTION**

Application code should not depend directly on one AI company.

Create abstractions similar to:

\`\`\`csharp

public interface ILlmProvider

{

    Task\<LlmResult\<T>> GenerateStructuredAsync\<T>(

        LlmRequest request,

        CancellationToken cancellationToken);

}

\`\`\`

The provider implementation may initially use one model.

Architecture must allow future providers such as:

\- OpenAI;

\- Anthropic;

\- Gemini;

\- local models.

Do not over-engineer multi-provider routing in V1.

**---**

**# 9. STRUCTURED AI OUTPUT IS MANDATORY**

Do NOT treat free-form AI text as core application data.

Every important AI workflow must have:

\- strongly typed DTO;

\- schema;

\- validation;

\- parse failure handling;

\- version metadata.

Examples:

\`\`\`text

CompetitorAnalysisResult

AudienceInsightResult

NicheOpportunityResult

IdeaGenerationResult

ResearchReportResult

OutlineResult

ScriptResult

ProductionPackageResult

\`\`\`

Raw model output may additionally be stored for diagnostics.

Structured output is the source of truth used by application logic.

**---**

**# 10. DOMAIN MODEL**

Start from domain concepts, not database tables.

Initial domain concepts should include:

\`\`\`text

User

Workspace

Project

Market

AudienceProfile

Niche

CompetitorChannel

CompetitorVideo

VideoMetrics

CompetitorAnalysis

WinningPattern

AudienceInsight

ContentGap

VideoIdea

IdeaScore

Pilot

PilotVideo

VideoProject

ResearchReport

ResearchSource

Outline

OutlineVersion

Script

ScriptVersion

ProductionPackage

Scene

ThumbnailConcept

AiRun

PromptTemplate

Job

\`\`\`

Do not force every object to become an aggregate root.

Model ownership carefully.

**---**

**# 11. VERSIONING RULE**

Generated content must not be destructively overwritten.

For example:

\`\`\`text

Script

 ├─ ScriptVersion 1

 ├─ ScriptVersion 2

 └─ ScriptVersion 3

\`\`\`

Apply similar versioning where valuable:

\- competitor analysis;

\- research;

\- outline;

\- script;

\- production package;

\- prompt template.

Every downstream artifact should be able to identify which upstream version generated it.

Example:

\`\`\`text

ProductionPackage

 → ScriptVersionId

\`\`\`

**---**

**# 12. VIDEO PROJECT STATE MACHINE**

VideoProject should use explicit states.

Example:

\`\`\`text

Draft

 ↓

ResearchQueued

 ↓

Researching

 ↓

ResearchReady

 ↓

OutlineGenerating

 ↓

OutlineReady

 ↓

OutlineApproved

 ↓

ScriptGenerating

 ↓

ScriptReady

 ↓

ScriptApproved

 ↓

Packaging

 ↓

ProductionReady

\`\`\`

Failure states may exist when useful.

Define allowed transitions explicitly.

Do not allow controllers or UI to arbitrarily mutate status strings.

State transitions must be testable.

**---**

**# 13. BACKGROUND JOB MODEL**

Long-running operations should NOT block HTTP requests.

Examples:

\- YouTube collection;

\- comment collection;

\- competitor analysis;

\- market analysis;

\- large AI generation;

\- production package generation.

Preferred API behavior:

\`\`\`http

POST /api/projects/{id}/competitor-analysis\:run

\`\`\`

Return:

\`\`\`http

202 Accepted

\`\`\`

with:

\`\`\`json

{

  "jobId": "..."

}

\`\`\`

Possible job states:

\`\`\`text

Queued

Running

Completed

Failed

Retrying

Cancelled

\`\`\`

Workers execute jobs.

Jobs must be:

\- observable;

\- retryable where appropriate;

\- idempotent where practical.

**---**

**# 14. EXTERNAL FAILURE STRATEGY**

Implement explicit handling for common failure categories.

**## Rate limit**

Retry with backoff.

**## Transient server failure**

Retry with bounded exponential backoff.

**## Authentication failure**

Do not repeatedly retry.

**## Invalid AI structured output**

Retry with correction instructions a limited number of times.

**## Permanent validation failure**

Store failure and expose actionable information.

**## User cancellation**

Respect CancellationToken where possible.

Never use infinite retries.

**---**

**# 15. DATABASE**

Use PostgreSQL from the start.

EF Core migrations must be used.

Do not manually edit production schema.

Use normal relational columns for important queryable business data.

Use \`jsonb\` only where semi-structured storage provides meaningful benefits, for example:

\- raw provider response;

\- model metadata;

\- flexible diagnostic fields.

Do not turn the whole database into JSON documents.

**---**

**# 16. FILE / LARGE OBJECT STORAGE**

Do not store large binary assets directly in PostgreSQL.

Future objects such as:

\- images;

\- audio;

\- videos;

\- exports;

must use storage abstraction.

Example:

\`\`\`csharp

public interface IFileStorage

{

    Task\<FileReference> SaveAsync(...);

}

\`\`\`

V1 may use local storage.

Future implementation may use:

\- S3;

\- Cloudflare R2;

\- Azure Blob Storage.

**---**

**# 17. SCORING ENGINE RULE**

Final business scores must NOT be blindly generated by the LLM.

Preferred model:

LLM extracts/features:

\`\`\`text

Demand

Novelty

AudienceFit

StoryPotential

ThumbnailPotential

ProductionDifficulty

Competition

\`\`\`

Deterministic backend calculates aggregate score.

Example concept:

\`\`\`text

Overall =

    Demand \* 0.25

  + Novelty \* 0.20

  + ThumbnailPotential \* 0.15

  + StoryPotential \* 0.15

  + AudienceFit \* 0.15

  + ProductionEase \* 0.10

  - CompetitionPenalty

\`\`\`

Actual weights must be configurable.

Store both:

\- component values;

\- scoring algorithm version;

\- final score.

**---**

**# 18. PROMPT MANAGEMENT**

Do not scatter large prompt strings throughout C# source files.

Create prompt abstraction/versioning.

Each prompt should have:

\`\`\`text

Key

Version

Purpose

SystemInstructions

Template

ExpectedOutputSchema

ModelConfiguration

\`\`\`

Example keys:

\`\`\`text

competitor-analysis

niche-gap-analysis

idea-generation

research-generation

outline-generation

script-generation

production-package

\`\`\`

Every AiRun should record:

\`\`\`text

PromptKey

PromptVersion

Provider

Model

\`\`\`

**---**

**# 19. AI RUN OBSERVABILITY**

Create an \`AiRun\` record for important model calls.

Store where available:

\`\`\`text

Id

Workflow

ProjectId

VideoProjectId

Provider

Model

PromptKey

PromptVersion

InputTokens

OutputTokens

EstimatedCost

Latency

Status

RetryCount

StartedAt

CompletedAt

FailureReason

\`\`\`

Avoid logging secrets or sensitive user information.

**---**

**# 20. API DESIGN**

Prefer use-case-oriented API commands rather than pretending every AI workflow is simple CRUD.

Examples:

\`\`\`text

POST /api/projects

GET /api/projects/{id}

POST /api/projects/{id}/competitors

POST /api/projects/{id}/competitors\:collect

POST /api/projects/{id}/competitor-analysis\:run

POST /api/projects/{id}/opportunities\:generate

POST /api/projects/{id}/ideas\:generate

POST /api/projects/{id}/pilots\:generate

POST /api/video-projects

POST /api/video-projects/{id}/research\:run

POST /api/video-projects/{id}/outline\:generate

POST /api/video-projects/{id}/outline\:approve

POST /api/video-projects/{id}/script\:generate

POST /api/video-projects/{id}/script\:approve

POST /api/video-projects/{id}/production-package\:generate

\`\`\`

Use normal CRUD where normal CRUD genuinely makes sense.

**---**

**# 21. SECURITY PRINCIPLES**

Never commit secrets.

Use configuration / secret management for:

\- database credentials;

\- AI API keys;

\- YouTube credentials;

\- OAuth secrets.

Never store access tokens in plain logs.

Prepare architecture for workspace isolation even if V1 begins as single-user.

Validate ownership on all project-scoped operations.

**---**

**# 22. TEST STRATEGY**

Every meaningful feature must include appropriate automated tests.

**## Domain tests**

Examples:

\- score calculation;

\- state transitions;

\- validation;

\- domain rules.

**## Application tests**

Examples:

\- command behavior;

\- workflow orchestration;

\- provider failure behavior.

**## Integration tests**

Examples:

\- EF Core;

\- PostgreSQL;

\- repositories;

\- API endpoints.

**## External integrations**

Use mocks/fakes for normal CI.

Do not make real paid AI calls during normal automated test execution.

**---**

**# 23. GOLDEN AI TEST DATASET**

Create a small fixture dataset for important AI workflows.

Purpose:

When prompts/models change, compare whether quality regresses.

Initially store fixtures for:

\- competitor analysis;

\- niche opportunity analysis;

\- idea generation;

\- outline generation.

Do not try to fully automate subjective quality scoring initially.

At minimum verify:

\- schema;

\- completeness;

\- expected fields;

\- obvious constraints;

\- known required concepts.

**---**

**# 24. CODE QUALITY**

Follow these rules.

\- Nullable reference types enabled.

\- Async all the way for I/O.

\- CancellationToken propagated.

\- No synchronous blocking on async tasks.

\- Avoid static global service state.

\- Prefer small cohesive classes.

\- Avoid giant service classes.

\- Avoid repository abstraction where EF DbContext already provides sufficient semantics unless repository adds domain value.

\- Avoid unnecessary generic abstractions.

\- Keep domain terminology consistent.

\- Prefer explicit behavior over clever code.

\- Add XML docs only where they provide genuine value.

\- Do not comment obvious code.

\- Keep warnings at zero where practical.

**---**

**# 25. DOCUMENTATION**

Maintain:

\`\`\`text

README.md

docs/

  PRODUCT\_SCOPE.md

  ARCHITECTURE.md

  DOMAIN\_MODEL.md

  AI\_WORKFLOWS.md

  API.md

  LOCAL\_DEVELOPMENT.md

  adr/

\`\`\`

Use ADRs for significant decisions.

Examples:

\`\`\`text

ADR-001-modular-monolith.md

ADR-002-postgresql.md

ADR-003-background-jobs.md

ADR-004-structured-ai-output.md

\`\`\`

Do not create ADRs for trivial implementation details.

**---**

**# 26. DEFINITION OF DONE**

A task is NOT complete merely because code compiles.

A feature is complete when applicable:

\- implementation exists;

\- architecture rules are respected;

\- database migration exists;

\- validation exists;

\- error behavior exists;

\- automated tests exist;

\- tests pass;

\- build passes;

\- lint/static analysis passes;

\- API contract is usable;

\- user-visible errors are meaningful;

\- relevant docs are updated;

\- no secrets are committed;

\- no obvious dead code remains.

Before reporting completion, run all relevant validation commands.

**---**

**# 27. IMPLEMENTATION PHASES**

Do the project in the following order.

**---**

**# PHASE 0 — REPOSITORY FOUNDATION**

Create solution structure.

Deliver:

\`\`\`text

src/

tests/

docs/

docker-compose.yml

.editorconfig

.gitignore

README.md

\`\`\`

Set up:

\- .NET solution;

\- React frontend;

\- PostgreSQL;

\- development configuration;

\- dependency injection;

\- logging;

\- health endpoint;

\- basic test infrastructure.

Acceptance criteria:

\`\`\`text

dotnet build

dotnet test

frontend build

\`\`\`

all succeed.

Application starts locally.

**---**

**# PHASE 1 — ARCHITECTURE + DOMAIN FOUNDATION**

Before implementing business features:

Create/update:

\`\`\`text

PRODUCT\_SCOPE.md

ARCHITECTURE.md

DOMAIN\_MODEL.md

\`\`\`

Implement initial domain:

\`\`\`text

Project

Market

AudienceProfile

CompetitorChannel

CompetitorVideo

Job

AiRun

\`\`\`

Configure DbContext and migrations.

Do not build every future entity yet.

Only implement what upcoming vertical slice requires.

**---**

**# PHASE 2 — FIRST VERTICAL SLICE**

This is the highest-priority milestone.

Implement end-to-end:

\`\`\`text

Create Project

      ↓

Add Competitor YouTube URL

      ↓

Fetch YouTube Channel

      ↓

Fetch Videos

      ↓

Persist Metadata

      ↓

Display Competitor Data

\`\`\`

This must include:

\- frontend;

\- API;

\- application layer;

\- domain;

\- infrastructure;

\- database;

\- external YouTube adapter;

\- tests.

Do not implement AI yet unless required for completion.

Acceptance criteria:

A developer can start the system, create a project, add a valid YouTube channel and see collected competitor videos.

**---**

**# PHASE 3 — COMPETITOR AI ANALYSIS**

Build:

\`\`\`text

Competitor Data

 ↓

Context Builder

 ↓

LLM

 ↓

Structured CompetitorAnalysisResult

 ↓

Validation

 ↓

Persistence

 ↓

UI

\`\`\`

Output should include at minimum:

\`\`\`text

Audience

Topics

WinningTitlePatterns

ThumbnailPatterns

HookPatterns

ContentPatterns

PotentialWeaknesses

TransferableFormats

\`\`\`

Implement prompt versioning.

Implement AiRun telemetry.

Implement retry for invalid structured output.

Acceptance criteria:

User can run analysis and view persisted structured results after refresh.

**---**

**# PHASE 4 — OPPORTUNITY ENGINE**

Implement:

\`\`\`text

CompetitorAnalysis

 +

Project Market

 ↓

Opportunity Analysis

 ↓

NicheOpportunity[]

\`\`\`

Each opportunity:

\`\`\`text

Niche

Audience

Format

Reasoning

Demand

Novelty

Competition

AudienceFit

OpportunityScore

\`\`\`

Backend calculates final score.

**---**

**# PHASE 5 — IDEA ENGINE**

Generate 20–30 ideas.

Implement deterministic scoring aggregation.

UI must allow:

\- sort;

\- filter;

\- approve/reject;

\- regenerate.

Regeneration creates a new generation/version; it does not silently destroy the old result.

**---**

**# PHASE 6 — PILOT ENGINE**

Implement creation of a 12-video pilot.

Each item must record:

\`\`\`text

VideoIdea

Sequence

ExperimentType

Hypothesis

PrimaryMetric

\`\`\`

Experiment types:

\`\`\`text

Topic

Packaging

Storytelling

\`\`\`

User can manually reorder or replace ideas.

**---**

**# PHASE 7 — VIDEO PROJECT**

Approved idea can become a VideoProject.

Implement state machine.

Initial states:

\`\`\`text

Draft

ResearchQueued

Researching

ResearchReady

OutlineGenerating

OutlineReady

OutlineApproved

ScriptGenerating

ScriptReady

ScriptApproved

Packaging

ProductionReady

\`\`\`

Add state transition tests.

**---**

**# PHASE 8 — RESEARCH WORKFLOW**

Implement structured research.

Research must retain sources/references where available.

Separate:

\`\`\`text

Fact

Claim

Uncertainty

Source

StoryOpportunity

\`\`\`

Never treat model-generated unsupported statements as verified facts.

**---**

**# PHASE 9 — OUTLINE WORKFLOW**

Generate outline from approved research.

Allow:

\- edit;

\- regenerate;

\- approve.

Approval freezes a specific version as input to script generation.

**---**

**# PHASE 10 — SCRIPT WORKFLOW**

Generate script from:

\`\`\`text

Project

Audience

VideoIdea

ResearchVersion

OutlineVersion

\`\`\`

Implement ScriptVersion.

User can:

\- regenerate;

\- edit;

\- approve.

**---**

**# PHASE 11 — PRODUCTION PACKAGE**

From approved script generate:

\`\`\`text

Scenes

Narration

VisualDescriptions

ImagePrompts

MotionSuggestions

Transitions

ThumbnailConcepts

MetadataSuggestions

\`\`\`

Every Scene should be structured data.

Support export to JSON.

Optional later:

Markdown/export folder.

**---**

**# PHASE 12 — OBSERVABILITY + COST DASHBOARD**

Expose useful development/admin information:

\- job status;

\- AI requests;

\- model;

\- token usage;

\- estimated cost;

\- latency;

\- failure rate.

Do not expose internal secrets or raw sensitive information.

**---**

**# 28. FIRST VERTICAL SLICE BEFORE FEATURE EXPANSION**

Do not continue building broad functionality until this flow works reliably:

\`\`\`text

Project

 ↓

Competitor URL

 ↓

YouTube API

 ↓

Database

 ↓

Competitor Analysis

 ↓

Structured AI Result

 ↓

UI

\`\`\`

This proves the core architecture.

After this succeeds, expand horizontally.

**---**

**# 29. CODEX WORKING PROCEDURE**

For every meaningful task follow this process.

**## Step 1 — Inspect**

Before editing or reviewing:

\- inspect the current target in `# 33. CURRENT TARGET`;

\- inspect relevant code;

\- inspect existing architecture;

\- inspect tests;

\- inspect affected documentation.

Do not assume project structure or current implementation state.

**---**

**## Step 2 — State the target**

Internally identify:

\- what behavior must change;

\- affected modules;

\- invariants;

\- acceptance criteria.

**---**

**## Step 3 — Implement the smallest coherent slice**

Avoid changing unrelated areas.

Do not perform broad refactors unless necessary.

**---**

**## Step 4 — Validate**

Run relevant:

\`\`\`text

dotnet build

dotnet test

frontend tests

frontend build

\`\`\`

Plus targeted tests where applicable.

**---**

**## Step 5 — Review your own diff**

Check:

\- accidental changes;

\- duplicated logic;

\- architectural violations;

\- missing cancellation;

\- missing error handling;

\- missing tests;

\- migrations;

\- secrets;

\- naming consistency.

**---**

**## Step 6 — Update documentation**

Only update docs affected by the change.

**---**

**## Step 7 — Report**

After each milestone report:

**### Completed**

What changed.

**### Architecture**

Any important design decision.

**### Validation**

Commands/tests executed.

**### Remaining**

What is intentionally not implemented.

**### Next**

The next recommended milestone.

Do not claim completion when tests are failing.

**## Pull Request Review Mode**

When the task is to review a pull request, act as an independent production reviewer.

Use all product, architecture, security, testing, code-quality, and scope rules defined in this document as the review standard.

Before reviewing:

\- understand the pull request goal;

\- inspect the complete diff;

\- inspect enough surrounding code to understand changed behavior;

\- identify the current milestone in `# 33. CURRENT TARGET`;

\- distinguish issues introduced or worsened by this pull request from unrelated pre-existing issues;

\- consider CI results when available.

Prioritize:

1. correctness and behavioral regressions;

2. security and sensitive-data exposure;

3. data integrity, transactions, concurrency, and state transitions;

4. architectural boundary violations;

5. API, persistence, and domain contract regressions;

6. async, CancellationToken, retry, idempotency, and failure handling;

7. missing or insufficient tests for changed behavior;

8. meaningful performance or observability regressions;

9. scope creep beyond the current milestone;

10. maintainability problems that create concrete engineering risk.

**### Review discipline**

\- Review the pull request, not the entire repository.

\- Do not block a pull request for unrelated pre-existing issues.

\- Do not report speculative problems without a credible failure mode.

\- Do not treat style preferences as defects.

\- Do not duplicate lint, formatting, build, or test failures already reported by CI unless they reveal a deeper issue.

\- Do not request broad refactors when a smaller safe fix is sufficient.

\- Prefer a small number of high-confidence findings over many weak comments.

\- A passing CI pipeline is evidence, not proof, that the change is correct.

**### Severity**

\- `BLOCKER` — must be fixed before merge.

\- `HIGH` — likely production bug, serious security issue, major data-integrity issue, or significant regression.

\- `MEDIUM` — real defect with limited impact or meaningful engineering risk.

\- `LOW` — legitimate improvement that is not required for this merge.

Normally only `BLOCKER` and `HIGH` should block merge.

**### Finding format**

For each meaningful finding:

\- identify the affected code;

\- explain the concrete failure mode or risk;

\- assign severity;

\- explain why it matters for this project;

\- suggest the smallest reasonable fix when clear.

**### Final review result**

End with exactly one:

\- `CHANGES REQUIRED`

\- `NON-BLOCKING FINDINGS`

\- `NO MATERIAL FINDINGS`

Do not claim that a pull request is guaranteed safe or bug-free.

**---**

**# 30. WHEN REQUIREMENTS ARE AMBIGUOUS**

When instructions appear to conflict, use this priority:

\`\`\`text

Current Target

\>

Explicit task requirements

\>

Product correctness

\>

Existing project conventions

\>

Architecture defined in this document

\>

Phase roadmap

\>

Simplest maintainable implementation

\>

Speculative flexibility

\`\`\`

If a decision is low-risk and reversible:

make a reasonable engineering choice and proceed.

If a decision fundamentally changes:

\- product behavior;

\- database ownership;

\- security;

\- major architecture;

\- external provider contract;

document the question rather than silently inventing requirements.

**---**

**# 31. ANTI-PATTERNS**

Do NOT:

\- create microservices;

\- introduce CQRS infrastructure everywhere just because commands exist;

\- create dozens of interfaces with one implementation for no reason;

\- create generic repositories for every entity;

\- call external APIs from controllers;

\- embed prompts randomly in controllers/services;

\- store unvalidated LLM responses as business state;

\- overwrite generated versions;

\- put business rules in React;

\- block HTTP requests for long AI jobs;

\- retry forever;

\- catch exceptions and ignore them;

\- use \`dynamic\` for core AI results;

\- build features outside V1 without explicit reason;

\- over-design for millions of users before validating the product.

**---**

**# 32. PRIORITY PRINCIPLE**

When forced to choose between:

\`\`\`text

more features

\`\`\`

and:

\`\`\`text

one complete, tested, observable end-to-end workflow

\`\`\`

always choose the complete workflow.

**---**

**# 33. CURRENT TARGET**

The current immediate objective is:

**## MILESTONE 1**

Build a working vertical slice:

\`\`\`text

Create Project

 ↓

Submit YouTube competitor URL

 ↓

Resolve channel

 ↓

Collect channel/video metadata

 ↓

Persist to PostgreSQL

 ↓

Display competitor information

 ↓

Run structured competitor AI analysis

 ↓

Persist analysis

 ↓

Display Winning Patterns

\`\`\`

Do not proceed to Niche Finder, Idea Engine, Script Engine or Production Package until this milestone is reliable.

**---**

**# 34. FINAL PRODUCT PRINCIPLE**

This application is not an AI text generator.

It is a:

**# STRUCTURED CONTENT INTELLIGENCE SYSTEM**

Raw information must progressively become:

\`\`\`text

Raw YouTube Data

        ↓

Structured Evidence

        ↓

Patterns

        ↓

Opportunities

        ↓

Ideas

        ↓

Experiments

        ↓

Research

        ↓

Script

        ↓

Production Package

        ↓

Analytics

        ↓

Better Decisions

\`\`\`

Every architecture and implementation decision should support that lifecycle.