# ADR-001: Modular monolith

- Status: Accepted
- Date: 2026-09-06

## Context

V1 spans several logical workflows but does not yet require independent deployment or distributed coordination.

## Decision

Use one .NET solution with Domain, Application, Infrastructure, API, and Worker projects plus a React client. Logical modules remain folders and namespaces until a proven boundary warrants stronger isolation.

## Consequences

The design preserves testable dependency direction and separate background execution while keeping local development and transactions simple. New code must not bypass Application to call infrastructure from API endpoints.
