# Local Development

## Prerequisites

Install .NET SDK 10.0.400 (or a compatible later 10.0 feature band), Node.js 22.12+, npm, and Docker. Copy `.env.example` to `.env` only when overriding Compose defaults.

## Database and migrations

```powershell
docker compose up -d postgres
dotnet tool restore
dotnet ef database update --project src/YoutubeAiFactory.Infrastructure --startup-project src/YoutubeAiFactory.Infrastructure
```

The default database is on `localhost:5432`. Override it with `ConnectionStrings__Database`. Apply both `InitialFoundation` and `AddCompetitorCollectionMetadata` before running the API. Never add real credentials to tracked settings.

## YouTube configuration

Create a Google API key with YouTube Data API v3 access and provide it through configuration. Keep the value out of `appsettings*.json` and `.env.example`:

```powershell
$env:YouTube__ApiKey = "your-google-api-key"
$env:CompetitorCollection__VideoLimit = "30" # Allowed range: 1-50
$env:AI__ApiKey = "your-ai-provider-key"
$env:AI__Model = "gpt-4.1-mini"
$env:CompetitorAnalysis__MaxVideos = "30" # Allowed range: 1-50
```

Readiness reports `Degraded` when the key is absent. Project endpoints still work, while collection returns a `503` problem response. The adapter sends the key in the `X-Goog-Api-Key` header and logs no credentials.

## Run locally

```powershell
dotnet run --project src/YoutubeAiFactory.Api --urls http://localhost:5050
dotnet run --project src/YoutubeAiFactory.Worker
```

Run the client from `src/YoutubeAiFactory.Web` with `npm install` and `npm run dev`. Vite serves port 5173 and proxies `/api` and `/health` to port 5050. Create a project in the UI, then submit a supported channel URL; submit it again to refresh metadata.

For Phase 3 analysis, run the Worker in a separate terminal after setting `AI__ApiKey`. The UI queues work and polls job status; it never sends provider keys to the browser. The default OpenAI-compatible provider posts to the Chat Completions endpoint with JSON-mode output. No provider key is needed for routine tests.

## Validate

```powershell
dotnet build YoutubeAiFactory.sln
dotnet test YoutubeAiFactory.sln
dotnet format YoutubeAiFactory.sln --verify-no-changes
cd src/YoutubeAiFactory.Web
npm run lint
npm test
npm run build
```

PostgreSQL tests skip unless `YAF_TEST_POSTGRES` is set. Run them against the isolated `_tests` database:

```powershell
docker compose --profile tests up -d postgres-tests
$env:YAF_TEST_POSTGRES = "Host=localhost;Port=5433;Database=youtube_ai_factory_tests;Username=yaf;Password=yaf_test_password"
dotnet test tests/YoutubeAiFactory.IntegrationTests
docker compose --profile tests stop postgres-tests
```

Routine adapter and API tests use fake HTTP responses and never consume YouTube quota. The PostgreSQL suite deletes and recreates only a database whose name ends in `_tests`.
