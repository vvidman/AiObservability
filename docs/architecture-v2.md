# Architecture — AiObservability

## Goals

1. Instrument AI pipelines (RAG, multi-agent, human-in-the-loop) with structured traces
2. Make traces inspectable by a human directly in the database — no special tooling required
3. Keep the abstraction layer dependency-free so any project can reference it without side effects
4. Support swappable storage backends via a single interface
5. Provide a web UI for browsing, filtering, and exporting traces without SQL knowledge

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
├── Duration        : TimeSpan        — computed: CompletedAt - StartedAt (JsonIgnore)
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
├── Duration        : TimeSpan        — computed (JsonIgnore)
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
    ISpanBuilder StartChildSpan(string name);
    TraceSpan Complete();
}
```

`WithInput`, `WithOutput`, `WithMetadata` accept `object?` for caller convenience.
Internally, the builder serializes the value to `JsonNode` immediately on assignment.

**Strict child span rule:** `Complete()` throws `InvalidOperationException` if any child span opened via `StartChildSpan()` has not been completed.

**Dispose behaviour:** Force-closes all open children recursively with `SpanStatus.Error`. Never throws.

### ITraceBuilder

```csharp
public interface ITraceBuilder : IAsyncDisposable
{
    ISpanBuilder StartSpan(string name);
    ITraceBuilder WithTag(string key, string value);
    Task<Trace> CompleteAsync(CancellationToken cancellationToken = default);
}
```

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

### AiObs.Core

- `SpanBuilder` — internal, implements `ISpanBuilder`
- `TraceBuilder` — internal, implements `ITraceBuilder`
- `InMemoryTraceStore` — thread-safe, for unit tests
- `JsonTraceStore` — writes one JSON file per trace, for local dev without Docker
- Dependency: `System.Text.Json` (inbox in .NET 10)

### AiObs.Postgres

- `PostgresTraceStore` — implements `ITraceStore` via Npgsql
- `PostgresTraceStoreOptions` — connection string only; schema and table names are internal constants
- `SchemaInitializer` — creates the `traces` table and indexes on startup
- `SchemaInitializerHostedService` — `IHostedService` wrapper for startup execution
- `ServiceCollectionExtensions` — `AddPostgresTraceStore()` extension method
- Dependencies: `Npgsql`, `Microsoft.Extensions.DependencyInjection.Abstractions`, `Microsoft.Extensions.Hosting.Abstractions`

### AiObs.Api

- ASP.NET Core Minimal API
- Endpoints: `GET /traces`, `GET /traces/{id}`, `DELETE /traces/{id}`, `GET /traces/{id}/export`, `GET /traces/export`
- No authentication — local network, trusted environment (see ADR-010)
- Not exposed directly on host network — traffic arrives via nginx reverse proxy
- Dependencies: `AiObs.Abstractions`, `AiObs.Postgres`

### AiObs.Web

- React (Vite) single-page application
- Pages: Trace list with filter bar, Trace detail with span tree
- All API calls use relative URLs (`/api/...`) — no hardcoded IP addresses (see ADR-009)
- Served as static files by nginx in production
- nginx proxies `/api/*` to the `api` container on the internal Docker network

### AiObs.Core.Tests

- Builder contract tests
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

Schema and table names are hardcoded constants — not configurable at runtime (see ADR-003 amendment).

---

## Docker setup

### Services

```
docker-compose.yml
├── postgres    — PostgreSQL 16 Alpine (internal only)
├── api         — AiObs.Api, ASP.NET Core (internal only)
└── web         — nginx:alpine, port 8080 (only publicly exposed port)
```

### Network topology

```
Host network
    └── :8080 → web (nginx)
                    ├── /          → serves React static files
                    └── /api/*     → proxy → api:5000
                                              └── → postgres:5432
```

No direct host access to `api` or `postgres` — all traffic enters through nginx on port 8080.

### Dockerfiles

**`Dockerfile.api`** — multi-stage .NET build:
```
Stage 1 (sdk:10-alpine):     dotnet publish → /app/publish
Stage 2 (aspnet:10-alpine):  copies /app/publish, ENTRYPOINT dotnet AiObs.Api.dll
```

**`Dockerfile.web`** — multi-stage Node + nginx:
```
Stage 1 (node:20-alpine):  npm ci && npm run build → /app/dist
Stage 2 (nginx:alpine):    copies dist to /usr/share/nginx/html, applies nginx.conf
```

### nginx.conf (key section)

```nginx
server {
    listen 80;

    location /api/ {
        proxy_pass http://api:5000/;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
    }

    location / {
        root /usr/share/nginx/html;
        try_files $uri $uri/ /index.html;
    }
}
```

`try_files ... /index.html` ensures React client-side routing works correctly.

---

## Project reference map

### Library consumers (external projects)

```
RagLab.Core / ChaosForge.Domain / Scaffold.Application.Interfaces
    └── → AiObs.Abstractions

RagLab.Infrastructure / ChaosForge.Infrastructure / Scaffold.ServiceHost
    └── → AiObs.Abstractions
    └── → AiObs.Core

RagLab.Console / ChaosForge.API / Scaffold.CLI   (composition roots)
    └── → AiObs.Postgres
```

### Internal (AiObservability solution)

```
AiObs.Api
    └── → AiObs.Abstractions
    └── → AiObs.Postgres

AiObs.Web
    └── (standalone React — no .NET project references)

AiObs.Core.Tests
    └── → AiObs.Core
    └── → AiObs.Abstractions
```

---

## Estimated memory footprint on NAS

| Container | Idle RAM |
|---|---|
| postgres | ~40 MB |
| api | ~80 MB |
| web (nginx) | ~10 MB |
| **Total** | **~130 MB** |

Well within the NAS's 1 GB RAM budget.

---

## What is not in scope

- Authentication or multi-tenancy
- Real-time streaming of spans
- Distributed tracing (W3C TraceContext, OpenTelemetry)
- Automatic instrumentation
- Span-level aggregate analytics (future `metrics` table)
- Internet exposure of any endpoint
