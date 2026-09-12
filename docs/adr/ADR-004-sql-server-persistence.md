# ADR-004: SQL Server persistence

- Status: Accepted
- Date: 2026-09-12
- Supersedes: ADR-002 for the concrete relational database provider only

## Context

Phases 0–6 were developed against PostgreSQL. Before Phase 7, the supported local database environment is Microsoft SQL Server already installed on the developer machine. The existing PostgreSQL schema migrations contain provider-specific annotations, types, JSONB storage, and job-claim SQL, so changing the provider configuration alone would not preserve persistence behavior.

## Decision

Use `Microsoft.EntityFrameworkCore.SqlServer` with an externally configured `ConnectionStrings:DefaultConnection`. API, Worker, design-time EF tooling, health checks, integration tests, and CI use SQL Server.

The repository is development-only and has no declared production PostgreSQL data set. Therefore, its PostgreSQL migration chain is replaced by one clean SQL Server baseline migration generated from the current Phase 6 model. Existing PostgreSQL databases and Docker volumes are not deleted, modified, or imported by this change.

Structured report and diagnostic payloads remain application-serialized JSON, now stored in `nvarchar(max)` rather than `jsonb` and protected by `ISJSON` check constraints. Business-queryable values remain relational. UTC event values stay `DateTimeOffset` and map to SQL Server `datetimeoffset`; IDs stay application-generated `Guid` values and map to `uniqueidentifier`. SQL Server filtered unique indexes preserve active-job uniqueness. Case-sensitive YouTube identifiers use binary collation.

## Consequences

Developers configure a local SQL Server connection through User Secrets or environment variables and apply migrations with EF Core; Docker is not required for normal runtime. CI provisions a disposable SQL Server service. Integration tests require an explicitly named disposable database ending in `Tests` and recreate only that database.

The worker uses SQL Server lock hints (`UPDLOCK`, `READPAST`, `ROWLOCK`) within a short transaction to claim jobs. The operation is executed through EF Core's SQL Server retry strategy. No transaction crosses an external AI or YouTube call.

If PostgreSQL data ever needs to be retained, it requires a separately approved export/transform/import process that preserves IDs, relationships, versions, timestamps, and scores. This provider migration intentionally does not perform destructive cross-engine data migration.
