# ADR-003 — Npgsql direct instead of EF Core

**Date:** 2026-04-07
**Status:** Accepted

## Context

`AiObs.Postgres` needs to persist and query `Trace` records in PostgreSQL. Two approaches were considered: raw Npgsql with explicit SQL, or EF Core with an Npgsql provider.

## Decision

Npgsql direct — no EF Core in the observability library.

## Reasons

- The data access pattern is simple: one INSERT per trace, one SELECT by ID, one filtered SELECT for queries. There is no complex object graph, no lazy loading, no migrations beyond the initial `CREATE TABLE`.
- EF Core would add a significant dependency to a library that is otherwise lightweight. Consuming projects that already use EF Core (ChaosForge) would pull in a second DbContext and potentially conflicting package versions.
- The `Trace` record is immutable. EF Core's change tracking provides no value for immutable records.
- JSONB handling with Npgsql direct is straightforward: read as `string`, deserialize with `System.Text.Json`. EF Core's JSONB mapping adds complexity without benefit here.
- SQL is explicit and reviewable — no LINQ translation surprises.

## Consequences

- SQL statements live in `PostgresTraceStore` as constants. Schema changes require manual SQL updates.
- `SchemaInitializer.EnsureCreatedAsync()` runs `CREATE TABLE IF NOT EXISTS` at startup. There is no migration framework — schema changes in future versions must be handled with manual `ALTER TABLE` statements.
- Npgsql is the only external dependency of `AiObs.Postgres`.
