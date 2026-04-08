# AiObs.Api

ASP.NET Core Minimal API for querying, deleting, and exporting traces stored in PostgreSQL.

Depends on: `AiObs.Abstractions`, `AiObs.Core`, `AiObs.Postgres`.

---

## What lives here

```
AiObs.Api/
├── Program.cs              # Composition root, DI wiring, endpoint definitions
├── Endpoints/
│   ├── TraceEndpoints.cs   # GET /traces, GET /traces/{id}, DELETE /traces/{id}
│   └── ExportEndpoints.cs  # GET /traces/{id}/export, GET /traces/export
└── AiObs.Api.csproj
```

---

## Endpoints

### Query traces

```
GET /traces
```

Query parameters (all optional):

| Parameter | Type | Description |
|---|---|---|
| `name` | string | Partial match on trace name (case-insensitive) |
| `from` | ISO 8601 | Filter traces started at or after this time |
| `to` | ISO 8601 | Filter traces started at or before this time |
| `tag_{key}` | string | Filter by tag value, e.g. `tag_pipeline=RagLab` |
| `limit` | int | Maximum results (default: 100) |

Response: `200 OK` — array of trace summary objects (no `root_spans`).

### Get trace detail

```
GET /traces/{id}
```

Response: `200 OK` — full trace including nested span tree. `404` if not found.

### Delete trace

```
DELETE /traces/{id}
```

Response: `204 No Content`. `404` if not found.

### Export single trace as JSON

```
GET /traces/{id}/export
```

Response: `200 OK` with `Content-Disposition: attachment; filename={id}.json` and full trace JSON.

### Export filtered traces as JSON

```
GET /traces/export
```

Accepts the same query parameters as `GET /traces`.
Response: `200 OK` with `Content-Disposition: attachment; filename=traces-export.json` and JSON array.

---

## Configuration

```json
// appsettings.json
{
  "ConnectionStrings": {
    "AiObs": "Host=postgres;Port=5432;Database=aiobs;Username=aiobs;Password=..."
  },
  "Kestrel": {
    "Endpoints": {
      "Http": {
        "Url": "http://0.0.0.0:5000"
      }
    }
  }
}
```

Note: inside Docker, `Host=postgres` refers to the Postgres service name in docker-compose.yml, not `localhost`.

---

## Running locally (without Docker)

```bash
dotnet run --project src/AiObs.Api
```

The API will be available at `http://localhost:5000`.
Set the `AiObs` connection string to point to your local or NAS PostgreSQL instance.

---

## Docker

Built via `docker/Dockerfile.api`. See `docker/docker-compose.yml` for the full setup.
The API container is not exposed directly — all traffic goes through the nginx reverse proxy on port 8080.
