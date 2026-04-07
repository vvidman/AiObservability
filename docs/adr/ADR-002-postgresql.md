# ADR-002 — PostgreSQL 16 Alpine as the storage backend

**Date:** 2026-04-07
**Status:** Accepted

## Context

Traces need to be stored durably and inspectable by a human without special tooling. The storage backend must run in Docker on a NAS with an ARM Cortex-A55 CPU and 1 GB RAM.

Candidates evaluated:

| Option | Docker image size | ARM64 | JSON support | Notes |
|---|---|---|---|---|
| PostgreSQL 16 Alpine | ~80 MB | ✅ | JSONB — native, queryable | Selected |
| MongoDB | 400 MB+ | ✅ | Native BSON | Too heavy for NAS |
| SQLite | embedded | ✅ | JSON functions | Concurrent write issues with 3 projects |
| LiteDB | embedded | ✅ | BSON | Not directly inspectable via SQL clients |

## Decision

PostgreSQL 16 Alpine, deployed via Docker Compose on the NAS.

## Reasons

- JSONB columns allow the full span tree to be stored as structured JSON while remaining queryable with standard SQL operators (`->`, `->>`, `@>`, GIN indexes).
- The Alpine image fits comfortably within the NAS memory budget (~30–50 MB idle).
- Any standard SQL client (DBeaver, DataGrip, psql) can inspect traces without custom tooling.
- The ARM64 image is officially maintained by the PostgreSQL project.

## Consequences

- A running PostgreSQL instance is required for `PostgresTraceStore`. Development without Docker uses `JsonTraceStore` (file-based, ships in `AiObs.Core`).
- The NAS must expose port 5432 on the local network. This is an internal, trusted network — no TLS hardening is required for the initial setup.
