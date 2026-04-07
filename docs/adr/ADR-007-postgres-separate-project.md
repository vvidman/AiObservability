# ADR-007 — AiObs.Postgres as a separate project

**Date:** 2026-04-07
**Status:** Accepted

## Context

The PostgreSQL store implementation could live in `AiObs.Core` alongside `JsonTraceStore` and `InMemoryTraceStore`, or in a dedicated `AiObs.Postgres` project.

## Decision

`AiObs.Postgres` is a separate project with its own `.csproj`.

## Reasons

- `AiObs.Core` has no external NuGet dependencies (only inbox `System.Text.Json`). Adding Npgsql would break this property for all consumers of `AiObs.Core`, even those that do not use PostgreSQL.
- Consuming projects that run unit tests or local development without Docker should not be forced to reference Npgsql. `JsonTraceStore` and `InMemoryTraceStore` in `AiObs.Core` cover those scenarios.
- The separation mirrors the pattern already established in RagLab (`RagLab.Core` has no external deps; `RagLab.Infrastructure` does) and follows Clean Architecture conventions the team is already comfortable with.
- A future `AiObs.Litedb` or `AiObs.SqlServer` variant could be added as additional projects without modifying `AiObs.Core`.

## Consequences

- The composition root of each consuming project (RagLab.Console, ChaosForge.API, Scaffold.CLI) must reference both `AiObs.Core` and `AiObs.Postgres` to get the full set of implementations.
- `AiObs.Core.Tests` references only `AiObs.Core` — no PostgreSQL dependency in the test project.
- If PostgreSQL integration tests are added in the future, they belong in a separate `AiObs.Postgres.Tests` project that can be excluded from CI runs that do not have a live database.
