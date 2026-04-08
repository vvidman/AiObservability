# AiObs.Postgres

PostgreSQL-backed `ITraceStore` implementation via Npgsql.

Depends on: `AiObs.Abstractions`, `AiObs.Core`, `Npgsql`.

---

## What lives here

```
AiObs.Postgres/
├── PostgresTraceStore.cs           # ITraceStore implementation
├── PostgresTraceStoreOptions.cs    # Connection string, schema, table name
└── SchemaInitializer.cs            # CREATE TABLE IF NOT EXISTS at startup
```

---

## Schema

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

`duration_ms` is stored explicitly for simple ORDER BY and WHERE without JSONB overhead.
`root_spans` contains the full span tree as nested JSON — human-readable via any SQL client.

---

## Registration

```csharp
// Program.cs / composition root
builder.Services.AddPostgresTraceStore(
    connectionsString: builder.Configuration.GetConnectionString("AiObs")
        ?? throw new InvalidOperationException("AiObs connection string is required"),
    initializeSchema: true);
```

Or, if registered via extension method (to be added):

```csharp
services.AddPostgresTraceStore(configuration.GetConnectionString("AiObs")!);
```

---

## Connection string

```json
// appsettings.json
{
  "ConnectionStrings": {
    "AiObs": "Host=nas.local;Database=aiobs;Username=aiobs;Password=<secret>"
  }
}
```

Keep the password in `appsettings.Development.json` (gitignored) or in an environment variable — never in the committed `appsettings.json`.

---

## Docker setup

See `docker/docker-compose.yml` in the repository root. Start the database:

```bash
cd docker
docker compose up -d
```

Connect with any SQL client (DBeaver, DataGrip, psql):

```
Host:     nas.local (or localhost in dev)
Port:     5432
Database: aiobs
Username: aiobs
```

---

## Useful queries

```sql
-- Browse recent traces
SELECT id, name, duration_ms, tags, started_at
FROM traces
ORDER BY started_at DESC
LIMIT 50;

-- Inspect a specific trace (full span tree)
SELECT root_spans
FROM traces
WHERE id = 'abc123';

-- Find slow traces
SELECT id, name, duration_ms
FROM traces
WHERE duration_ms > 3000
ORDER BY duration_ms DESC;

-- Find error traces
SELECT id, name, started_at
FROM traces
WHERE root_spans @> '[{"Status": "Error"}]';

-- Filter by pipeline tag
SELECT id, name, duration_ms
FROM traces
WHERE tags->>'pipeline' = 'RagLab'
  AND started_at > NOW() - INTERVAL '1 day';
```
