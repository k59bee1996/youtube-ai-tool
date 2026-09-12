# ADR-002: PostgreSQL persistence and database-backed jobs

- Status: Superseded by ADR-004 for the database provider; retained as historical context for the database-backed job decision
- Date: 2026-09-06

## Context

The product needs durable relational business data and observable long-running operations, but distributed queue infrastructure is premature.

## Decision

Use PostgreSQL through EF Core migrations. Store primary business fields relationally and reserve `jsonb` for variable job payloads. Model jobs with explicit states, availability time, bounded retry counts, and failure details. Add claiming and dispatch only when the first asynchronous workflow is implemented.

## Consequences

PostgreSQL is required for readiness and integration validation. API requests will later enqueue work and return `202 Accepted`; the worker will claim records safely without adding a message broker in V1.
