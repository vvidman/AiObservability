# Architecture — AiObservability

## Goals

1. Instrument AI pipelines (RAG, multi-agent, human-in-the-loop) with structured traces
2. Make traces inspectable by a human directly in the database — no special tooling required
3. Keep the abstraction layer dependency-free so any project can reference it without side effects
4. Support swappable storage backends via a single interface

---

## Domain model

### SpanStatus

```
SpanStatus
├── Ok     — span completed without errors
└── Error  — span completed with a recorded error or was force-closed by Dispose
```

### TraceSpan

Immutable record. Created exclusively by `SpanBuilder.Complete()`.

```
TraceSpan
├── Id              : string (GUID, no dashes)
├── Name            : string          — "embed_query", "retrieve_docs", "generate", etc.
├── StartedAt       : DateTimeOffset
├── CompletedAt     : DateTimeOffset
├── Duration        : TimeSpan        — computed: CompletedAt - StartedAt
├── Status          : SpanStatus
├── Input           : JsonNode?       — what the step received
├── Output          : JsonNode?       — what the step produced
├── ErrorMessage    : string?         — populated on Error status
├── Metadata        : IReadOnlyDictionary<string, JsonNode?>
│                     examples: model, input_tokens, output_tokens, chunk_count, top_k
└── Children        : IReadOnlyList<TraceSpan>   — completed child spans, ordered by StartedAt
```

### Trace

Immutable record. Created exclusively by `ITraceBuilder.CompleteAsync()`.

```
Trace
├── Id              : string (GUID, no dashes)
├── Name            : string          — "rag_query", "agent_task", "scaffold_step", etc.
├── StartedAt       : DateTimeOffset
├── CompletedAt     : DateTimeOffset
├── Duration        : TimeSpan        — computed
├── Tags            : IReadOnlyDictionary<string, string>
│                     examples: pipeline=RagLab, model=claude-sonnet-4-6, project=ChaosForge
└── RootSpans       : IReadOnlyList<TraceSpan>   — top-level spans, ordered by StartedAt
```

**Why `Tags` is `string, string` but `Metadata` is `string, JsonNode?`:**
Tags are used for filtering at the trace level (pipeline, model, environment). They are always simple scalar values. Metadata lives inside spans and can carry structured data (e.g. a chunk list, a token breakdown object).

---

## Interfaces

### ISpanBuilder

```csharp
public interface ISpanBuilder : IDisposable
{
    ISpanBuilder WithInput(object? value);
    ISpanBuilder WithOutput(object? value);
    ISpanBuilder WithMetadata(string key, object? value);
    ISpanBuilder RecordError(Exception exception);
    ISpanBuilder StartChildSpan(string name);    // returns the child's ISpanBuilder
    TraceSpan Complete();
}
```

`WithInput`, `WithOutput`, `WithMetadata` accept `object?` for caller convenience.
Internally, the builder serializes the value to `JsonNode` immediately on assignment.

**Strict child span rule:** `Complete()` throws `InvalidOperationException` if any child span opened via `StartChildSpan()` has not been completed. The caller is responsible for closing child spans before the parent.

**Dispose behaviour:** If `Dispose()` is called without a prior `Complete()`, the span is force-closed with `SpanStatus.Error` and a message indicating it was abandoned. All open children are also force-closed recursively. `Dispose()` never throws.

### ITraceBuilder

```csharp
public interface ITraceBuilder : IAsyncDisposable
{
    ISpanBuilder StartSpan(string name);
    ITraceBuilder WithTag(string key, string value);
    Task<Trace> CompleteAsync(CancellationToken cancellationToken = default);
}
```

`CompleteAsync()` applies the same strict rule as spans: throws if any root span is still open.
`DisposeAsync()` force-closes open spans and calls `CompleteAsync()` silently, swallowing exceptions.

### ITraceStore

```csharp
public interface ITraceStore
{
    ITraceBuilder StartTrace(string name);
    Task SaveAsync(Trace trace, CancellationToken cancellationToken = default);
    Task<Trace?> FindAsync(string traceId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Trace>> QueryAsync(TraceQuery query, CancellationToken cancellationToken = default);
}
```

`StartTrace()` returns a builder; `SaveAsync()` is called internally by `CompleteAsync()`.
`SaveAsync()` is public to allow manual persistence if needed (e.g. saving a pre-built Trace).

### TraceQuery

```csharp
public sealed class TraceQuery
{
    public string? NameContains { get; init; }
    public DateTimeOffset? From { get; init; }
    public DateTimeOffset? To { get; init; }
    public IReadOnlyDictionary<string, string>? TagFilters { get; init; }
    public int Limit { get; init; } = 100;
}
```

---

## Project responsibilities

### AiObs.Abstractions

- Domain models: `Trace`, `TraceSpan`, `SpanStatus`
- Interfaces: `ISpanBuilder`, `ITraceBuilder`, `ITraceStore`
- Query model: `TraceQuery`
- **Zero external NuGet dependencies**
- Referenced by consuming projects' innermost layers (Core, Domain, Application)

### AiObs.Core

- `SpanBuilder` — internal, implements `ISpanBuilder`
- `TraceBuilder` — internal, implements `ITraceBuilder`, holds reference to `ITraceStore` for save-on-complete
- `InMemoryTraceStore` — thread-safe, for unit tests
- `JsonTraceStore` — writes one JSON file per trace to a configurable directory, for local dev without Docker
- Dependency: `System.Text.Json` (inbox in .NET 10, no NuGet needed)

### AiObs.Postgres

- `PostgresTraceStore` — implements `ITraceStore` via Npgsql
- `PostgresTraceStoreOptions` — connection string, schema name, table name
- `SchemaInitializer` — creates the `traces` table if it does not exist (called at startup)
- Dependency: `Npgsql`

### AiObs.Core.Tests

- Builder contract tests: fluent API, strict child validation, Dispose behaviour
- `InMemoryTraceStore` round-trip tests
- `JsonTraceStore` serialization/deserialization tests

---

## Database schema

### PostgreSQL table

```sql
CREATE TABLE IF NOT EXISTS traces (
    id            TEXT        PRIMARY KEY,
    name          TEXT        NOT NULL,
    started_at    TIMESTAMPTZ NOT NULL,
    completed_at  TIMESTAMPTZ NOT NULL,
    duration_ms   INTEGER     NOT NULL,
    tags          JSONB       NOT NULL DEFAULT '{}',
    root_spans    JSONB       NOT NULL DEFAULT '[]'
);

CREATE INDEX IF NOT EXISTS ix_traces_name       ON traces (name);
CREATE INDEX IF NOT EXISTS ix_traces_started_at ON traces (started_at DESC);
CREATE INDEX IF NOT EXISTS ix_traces_tags       ON traces USING GIN (tags);
```

`duration_ms` is stored explicitly (not computed) for simple ORDER BY and WHERE queries without JSONB overhead.

### Rationale for denormalization

The entire span tree is stored as a JSONB array in `root_spans`. Each span's children are nested arrays within it. This mirrors the in-memory `Trace` record exactly and eliminates the need for recursive CTEs to reconstruct the tree.

The tradeoff — inability to run span-level aggregate queries efficiently — is acceptable because:
- The primary use case is trace-level inspection and debug
- Span-level aggregation (latency histograms, token trends) will be handled by a separate `metrics` table in a future lab, not by querying `root_spans`

### Queryable JSON examples

```sql
-- All traces from a specific pipeline in the last 24 hours
SELECT id, name, duration_ms, tags
FROM traces
WHERE tags->>'pipeline' = 'RagLab'
  AND started_at > NOW() - INTERVAL '24 hours'
ORDER BY started_at DESC;

-- Traces where any root span has status Error
SELECT id, name, started_at
FROM traces
WHERE root_spans @> '[{"Status": "Error"}]';

-- Find slow traces
SELECT id, name, duration_ms
FROM traces
WHERE duration_ms > 3000
ORDER BY duration_ms DESC
LIMIT 20;
```

---

## Docker setup (NAS deployment)

```yaml
# docker/docker-compose.yml
services:
  postgres:
    image: postgres:16-alpine
    restart: unless-stopped
    environment:
      POSTGRES_DB: aiobs
      POSTGRES_USER: aiobs
      POSTGRES_PASSWORD: ${POSTGRES_PASSWORD}
    ports:
      - "5432:5432"
    volumes:
      - pgdata:/var/lib/postgresql/data

volumes:
  pgdata:
```

The Alpine image is ~80 MB and runs comfortably within the NAS's 1 GB RAM constraint.
Memory usage at idle is approximately 30–50 MB.

---

## Project reference map

```
RagLab.Core
    └── → AiObs.Abstractions

RagLab.Infrastructure
    └── → AiObs.Abstractions
    └── → AiObs.Core              (JsonTraceStore for dev)

RagLab.Console                    (composition root)
    └── → AiObs.Postgres          (PostgresTraceStore for prod)

─────────────────────────────────────────────────────

ChaosForge.Domain
    └── → AiObs.Abstractions

ChaosForge.Infrastructure
    └── → AiObs.Abstractions
    └── → AiObs.Core

ChaosForge.API                    (composition root)
    └── → AiObs.Postgres

─────────────────────────────────────────────────────

Dev_Scaffold / Scaffold.Application.Interfaces
    └── → AiObs.Abstractions

Dev_Scaffold / Scaffold.ServiceHost
    └── → AiObs.Abstractions
    └── → AiObs.Core

Dev_Scaffold / Scaffold.CLI       (composition root)
    └── → AiObs.Postgres
```

---

## What is not in scope

- Authentication or multi-tenancy on the trace store
- Real-time streaming of spans (traces are written atomically on completion)
- Distributed tracing across network boundaries (W3C TraceContext, OpenTelemetry)
- Automatic instrumentation (no source generators or IL weaving)
- A query UI — traces are inspected directly in PostgreSQL via any SQL client
