# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

A lightweight .NET 10 observability library for AI pipelines. Every LLM call, retrieval step, and agent action becomes a **span** inside a **trace**, persisted to PostgreSQL (or in-memory/JSON for dev/tests). Designed for use by RagLab, ChaosForge, and Dev_Scaffold without coupling them to a storage backend.

## Repository layout (current vs. intended)

The repository folder names differ from the canonical .NET project names documented in README.md and architecture.md:

| Folder | Contents / project |
|---|---|
| `src/AiObs.Abstractions/` | Interfaces and domain models — zero NuGet dependencies |
| `src/AiObs.Core/` | Builder implementations + `InMemoryTraceStore` + `JsonTraceStore` |
| `src/AiObs.Postgres/` | `PostgresTraceStore` via Npgsql |
| `tests/AiObs.Core.Tests/` | Unit tests for builders and stores |
| `docker/` | `docker-compose.yml` for PostgreSQL 16 Alpine |
| `docs/` | Architecture doc + ADRs |

Design notes for each project are in the folder READMEs: `src/README.md` (Abstractions), `external dependencies/README.md` (Core), `memory stores/README.md` (Postgres).

## Build and test commands

```bash
# Build all projects
dotnet build

# Run all tests
dotnet test

# Run a single test (by name filter)
dotnet test --filter "FullyQualifiedName~SpanBuilder"

# Start PostgreSQL for local dev
cd contracts/docker && docker compose up -d
```

## Architecture constraints

### Dependency rules

- `AiObs.Abstractions` (`src/`) — **zero NuGet dependencies**. Only `System.Text.Json` (inbox in .NET 10). Safe to reference from any layer.
- `AiObs.Core` (`external dependencies/`) — depends on Abstractions + `System.Text.Json`. No Npgsql.
- `AiObs.Postgres` (`memory stores/`) — depends on Abstractions, Core, and `Npgsql`.

Consuming projects must only reference `AiObs.Postgres` at the composition root (Program.cs / CLI entrypoint). Inner layers reference only Abstractions.

### Immutable domain models

`Trace` and `TraceSpan` are immutable records. They are created **only** by builder implementations — never constructed directly. Do not add constructors or factory methods to the model classes.

### Strict child span validation

`ISpanBuilder.Complete()` throws `InvalidOperationException` if any child span opened via `StartChildSpan()` has not been completed. This is intentional (ADR-006) — silently auto-closing children produces traces with incorrect latency data. The `Dispose()` path is exempt (must not throw per .NET contract) and force-closes with `SpanStatus.Error`.

### Input/Output serialization

`WithInput`, `WithOutput`, and `WithMetadata` accept `object?` and serialize to `JsonNode` immediately at call time (not lazily). Serialization errors surface at the call site.

### Storage backends

| Class | Project | Use case |
|---|---|---|
| `InMemoryTraceStore` | Core | Unit tests — new instance per test |
| `JsonTraceStore` | Core | Local dev without Docker; one `.json` file per trace |
| `PostgresTraceStore` | Postgres | Production / NAS deployment |

### Database schema (denormalized by design — ADR-004)

The full span tree is stored as a JSONB `root_spans` column. Children are nested arrays. This avoids recursive CTEs for read and mirrors the in-memory model exactly. Span-level aggregate queries are explicitly out of scope.

```sql
CREATE TABLE traces (
    id            TEXT        PRIMARY KEY,
    name          TEXT        NOT NULL,
    started_at    TIMESTAMPTZ NOT NULL,
    completed_at  TIMESTAMPTZ NOT NULL,
    duration_ms   INTEGER     NOT NULL,   -- stored explicitly, not computed
    tags          JSONB       NOT NULL DEFAULT '{}',
    root_spans    JSONB       NOT NULL DEFAULT '[]'
);
```

`SchemaInitializer.EnsureCreatedAsync()` must be called at application startup.

## Key ADRs

- **ADR-001** — Standalone library (not coupled to any one consuming project)
- **ADR-004** — Denormalized schema (full span tree in one JSONB column)
- **ADR-005** — `JsonNode` for Input/Output (not `string`, not `object`)
- **ADR-006** — Strict child span validation (`Complete()` throws if children open)
- **ADR-007** — Postgres in a separate project (so Abstractions/Core stay Npgsql-free)

Full ADR text is in `docs/adr/`.
