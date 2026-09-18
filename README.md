# YouTube AI Factory

YouTube AI Factory turns YouTube market information into structured content intelligence. Phase 10 converts an explicitly approved, evidence-grounded Video Outline into versioned viewer-facing narration, audits every Script against persisted ResearchClaims, and requires explicit user approval before production packaging.

## Repository layout

- `src/YoutubeAiFactory.Domain` — entities, value objects, invariants, and state transitions.
- `src/YoutubeAiFactory.Application` — use cases and provider-neutral contracts.
- `src/YoutubeAiFactory.Infrastructure` — EF Core, SQL Server, migrations, and the YouTube adapter.
- `src/YoutubeAiFactory.Api` — HTTP endpoints, problem responses, and health checks.
- `src/YoutubeAiFactory.Worker` — database-backed background workflow host.
- `src/YoutubeAiFactory.Web` — React and TypeScript project/competitor interface.
- `tests` — domain, application, adapter, API, and SQL Server integration tests.
- `docs` — product, architecture, domain, API, workflow, and setup notes.

## Quick start

Prerequisites: .NET SDK 10.0.400 or a compatible feature band, Node.js 22.12+, npm, Microsoft SQL Server, and a Google API key with YouTube Data API v3 access.

```powershell
dotnet tool restore
$env:ConnectionStrings__DefaultConnection = "Server=localhost;Database=YoutubeAiFactory;Trusted_Connection=True;TrustServerCertificate=True;Encrypt=True"
dotnet ef database update --project src/YoutubeAiFactory.Infrastructure --startup-project src/YoutubeAiFactory.Infrastructure
$env:YouTube__ApiKey = "your-google-api-key"
$env:AI__ApiKey = "your-ai-provider-key"
$env:AI__Models__Fast__Model = "gpt-5.6-luna"
$env:AI__Models__Reasoning__Model = "gpt-5.6-terra"
$env:AI__Models__Premium__Model = "gpt-5.6-sol"
dotnet run --project src/YoutubeAiFactory.Api --urls http://localhost:5050
```

In another terminal:

```powershell
cd src/YoutubeAiFactory.Web
npm install
npm run dev
```

Open `http://localhost:5173`. See [Local Development](docs/LOCAL_DEVELOPMENT.md) for tests, configuration, and database commands.

## CI

GitHub Actions runs for pull requests targeting `main` and pushes to `main`. It verifies .NET formatting, build, unit and SQL Server integration tests, then installs the web client with `npm ci` and runs its lint, test, and build checks. No GitHub repository secrets are required: CI uses an isolated ephemeral SQL Server service and provider fakes rather than the YouTube API.

Run the same checks locally using the commands in [Local Development](docs/LOCAL_DEVELOPMENT.md#validate), including the optional disposable SQL Server integration database.

## Phase boundary

Start the Worker as well as the API to process competitor, opportunity, idea-generation, pilot-generation, localization, research, outline, and Script jobs. An `OutlineApproved` VideoProject can queue Premium Script generation. The worker preserves exact Outline/Research lineage, writes ordered structured narration blocks in the project's content target language, validates block-to-Claim references, runs a separate Reasoning grounding audit, and persists only a safe version as `ScriptReady`. Any narration edit invalidates grounding and requires asynchronous revalidation before explicit approval moves the project to `ScriptApproved`. Phase 10 performs no research and generates no scenes, images, audio, or ProductionPackage.
