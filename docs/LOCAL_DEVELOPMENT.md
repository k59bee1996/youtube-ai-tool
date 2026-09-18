# Local Development

## Prerequisites

Install .NET SDK 10.0.400 (or a compatible later 10.0 feature band), Node.js 22.12+, npm, and Microsoft SQL Server. Docker is not required for normal local development. The application supports a default or named SQL Server instance, Windows authentication, and SQL authentication.

## Configure SQL Server

Set `ConnectionStrings__DefaultConnection` for both the API and Worker. Do not add machine-specific connection strings or credentials to tracked settings.

Windows authentication:

```powershell
$env:ConnectionStrings__DefaultConnection = "Server=localhost;Database=YoutubeAiFactory;Trusted_Connection=True;TrustServerCertificate=True;Encrypt=True"
```

For a named instance, replace `localhost` with (for example) `localhost\SQLEXPRESS`. SQL authentication is also supported by supplying `User Id` and `Password`; use User Secrets or environment variables for those values. `TrustServerCertificate=True` is appropriate only for local development when the local instance has no trusted certificate.

Apply the provider-specific SQL Server baseline migration:

```powershell
dotnet tool restore
dotnet ef database update --project src/YoutubeAiFactory.Infrastructure --startup-project src/YoutubeAiFactory.Infrastructure
```

If `dotnet ef` cannot find configuration, set `ConnectionStrings__DefaultConnection` in the same terminal. The design-time factory intentionally contains no fallback connection string or credentials.

## YouTube configuration

Create a Google API key with YouTube Data API v3 access and provide it through configuration. Keep the value out of `appsettings*.json` and `.env.example`:

```powershell
$env:YouTube__ApiKey = "your-google-api-key"
$env:CompetitorCollection__VideoLimit = "30" # Allowed range: 1-50
$env:AI__ApiKey = "your-ai-provider-key"
$env:AI__Models__Fast__Model = "gpt-5.6-luna"
$env:AI__Models__Reasoning__Model = "gpt-5.6-terra"
$env:AI__Models__Reasoning__TimeoutSeconds = "300" # Default: 300 seconds for structured analysis/idea generation
$env:AI__Models__Reasoning__MaxOutputTokens = "8000" # Default: 8,000 tokens for one 15-idea generation response
$env:AI__Models__Premium__Model = "gpt-5.6-sol"
$env:ResearchSearch__ApiKey = "your-tavily-api-key"
$env:CompetitorAnalysis__MaxVideos = "30" # Allowed range: 1-50
$env:OpportunityAnalysis__MaxCompetitors = "8"
$env:OpportunityAnalysis__MaxEvidenceItems = "60"
$env:IdeaGeneration__TargetIdeaCount = "15"
$env:IdeaGeneration__MinIdeaCount = "15"
$env:IdeaGeneration__IdeasPerRequest = "15"
$env:PilotGeneration__MaxCandidateIdeas = "40" # Allowed range: 12-40
$env:Outline__MaxClaimsForOutline = "30"
$env:Outline__MaxEvidenceExcerptsForOutline = "40"
$env:Outline__MinOutlineSections = "4"
$env:Outline__MaxOutlineSections = "12"
$env:Script__PlanningWordsPerMinute = "150"
$env:Script__MinimumLengthRatio = "0.8"
$env:Script__MaximumLengthRatio = "1.2"
$env:Script__MaxLengthCorrectionAttempts = "1"
$env:Script__MaxGroundingCorrectionAttempts = "1"
```

Readiness reports `Degraded` when the key is absent. Project endpoints still work, while collection returns a `503` problem response. The adapter sends the key in the `X-Goog-Api-Key` header and logs no credentials.

## Run locally

```powershell
dotnet run --project src/YoutubeAiFactory.Api --urls http://localhost:5050
dotnet run --project src/YoutubeAiFactory.Worker
```

Run the client from `src/YoutubeAiFactory.Web` with `npm install` and `npm run dev`. Vite serves port 5173 and proxies `/api` and `/health` to port 5050. Create a project in the UI, then submit a supported channel URL; submit it again to refresh metadata.

Phase 10 Script generation and edited-Script revalidation require the Worker and `AI__ApiKey`. They consume the Approved Outline and persisted Research only; they never use `ResearchSearch__ApiKey`, fetch pages, or make new Research entities. Routine tests use provider fakes.

For Phase 3–6 analysis, Phase 8 research, and Phase 9 outline generation, run the Worker in a separate terminal after setting `AI__ApiKey`. Research additionally requires server-side `ResearchSearch__ApiKey`; it is never sent to the browser. Outline generation consumes persisted Research only and never uses that search key. Phase 7 VideoProject creation is synchronous and needs neither service. The default OpenAI-compatible provider posts to the Chat Completions endpoint with JSON-mode output. No provider key is needed for routine tests.

`Research` provides bounded query/result/source/evidence/claim limits. `ResearchFetch` controls request timeout, redirect count, response bytes, and normalized source characters. `ResearchSearch` controls the Tavily endpoint and timeout. `Outline` bounds generation/repair retries, Section count, Claims, evidence excerpts, conflicts, gaps, findings, and worker lease age. Keep real keys in environment variables or User Secrets; normal tests use fakes and do not call live search, web pages, or paid models.

`AI:Models:Fast`, `AI:Models:Reasoning`, and `AI:Models:Premium` each require `Provider`, `Model`, `TimeoutSeconds`, and `MaxOutputTokens`. Environment variables such as `AI__Models__Premium__Model` may map profiles differently by environment, or intentionally map all profiles to one local-development model. Missing profiles fail clearly; routing never silently downgrades a Premium workload.

`Script` configuration bounds generation/repair retries, independent length and grounding correction attempts, block/narration size, WPM, fallback target duration, length ratios, and the worker lease. Defaults use 150 WPM and a 0.8-1.2 target-word tolerance.

## Validate

```powershell
dotnet restore YoutubeAiFactory.sln
dotnet build YoutubeAiFactory.sln
dotnet test YoutubeAiFactory.sln
dotnet format YoutubeAiFactory.sln --verify-no-changes
cd src/YoutubeAiFactory.Web
npm ci
npm run lint
npm test
npm run build
```

SQL Server integration tests are isolated and skipped unless `YAF_TEST_SQLSERVER` is set. The configured database name must end in `Tests`; the suite deletes and recreates only that disposable database.

```powershell
$env:YAF_TEST_SQLSERVER = "Server=localhost;Database=YoutubeAiFactoryIntegrationTests;Trusted_Connection=True;TrustServerCertificate=True;Encrypt=True"
dotnet test tests/YoutubeAiFactory.IntegrationTests
```

CI provisions its own ephemeral SQL Server service and does not use a developer machine database. Routine adapter and API tests use fake HTTP responses and never consume YouTube quota.

## Troubleshooting

- Confirm the SQL Server service is running and that the configured server/instance name is correct.
- For `Login failed`, check whether the connection uses Windows or SQL authentication and that the account has permission to create/update the database.
- For certificate errors during local development, use `TrustServerCertificate=True`; do not apply that setting globally to production.
- For connection failures to a named instance, confirm SQL Server Browser/TCP-IP configuration or use the explicit server and port.
- For a missing database, run `dotnet ef database update` after setting the connection string.
