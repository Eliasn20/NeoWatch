# NeoWatch

A .NET 10 background worker that ingests NASA's near-Earth object feed (NeoWs)
into SQLite: 90-day backfill, idempotent upserts, hazard alerts and daily summaries.

## Layout

| Project | Role |
|---|---|
| `src/NeoWatch.Core` | Models, API DTOs, pure pipeline logic. No dependencies. |
| `src/NeoWatch.Infrastructure` | NASA HTTP client, EF Core `DbContext`, repository. |
| `src/NeoWatch.Worker` | Host, dependency injection, timer loop, logging. |
| `tests/NeoWatch.Tests` | xUnit tests. |

## Setup (once)

1. Install the .NET 10 SDK and the C# Dev Kit extension for VS Code.
2. `bash setup.sh` (adds NuGet packages and the `dotnet ef` tool, then builds).
3. Get a free key at https://api.nasa.gov and store it outside the repo:
   `dotnet user-secrets set "NeoWs:ApiKey" "YOUR_KEY" --project src/NeoWatch.Worker`

## Build order

Search the code for `TODO (Elias)`. Tests exist for each step and start red.

| # | What | Where | Done when |
|---|---|---|---|
| 2a | Split a date range into 7-day windows | `Core/Pipeline/DateWindows.cs` | `DateWindowsTests` green |
| 2b | Flatten and validate the feed | `Core/Pipeline/NeoFeedMapper.cs` | `NeoFeedMapperTests` green |
| 3 | Idempotent upsert, then create the first migration | `Infrastructure/ApproachRepository.cs` | `ApproachRepositoryTests` green |
| 4 | First real run with backfill | `dotnet run` | rows in `neowatch.db`, second run inserts 0 |
| 5 | Daily summaries | `Core/Pipeline/DailySummaryCalculator.cs` | your own tests green |
| 6 | Push to GitHub | `.github/workflows/ci.yml` | CI green |
| 7 | Static `report.html` (optional) | new class | chart + table generated after each run |

Step 2b extra: save one real response as a second fixture:

```bash
curl "https://api.nasa.gov/neo/rest/v1/feed?start_date=2026-11-09&end_date=2026-11-15&api_key=YOUR_KEY" \
  -o tests/NeoWatch.Tests/Fixtures/feed-real.json
```

Step 3, after the tests pass:

```bash
dotnet ef migrations add InitialCreate \
  --project src/NeoWatch.Infrastructure --startup-project src/NeoWatch.Worker
```

## Commands

```bash
dotnet build
dotnet test
dotnet test --filter DateWindowsTests
dotnet run --project src/NeoWatch.Worker
```
