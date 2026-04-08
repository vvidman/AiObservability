# ADR-008 — Web UI: React + ASP.NET Core Minimal API

**Date:** 2026-04-08
**Status:** Accepted

## Context

Traces stored in PostgreSQL are queryable via SQL clients, but this requires developer tooling and knowledge of the schema. A web UI makes traces accessible without SQL knowledge and enables faster debug workflows.

## Decision

Two new projects are added to the AiObservability solution:

- `AiObs.Api` — ASP.NET Core Minimal API exposing trace query, delete, and export endpoints
- `AiObs.Web` — React (Vite) frontend served via nginx

Both run as Docker containers on the NAS alongside the existing PostgreSQL container.

## Reasons

**ASP.NET Core Minimal API:**
- Consistent with the existing .NET stack
- Minimal API surface matches the simple endpoint set (no controllers, no heavy scaffolding)
- Direct `ITraceStore` injection — no extra abstraction layer needed

**React (Vite):**
- Already used in ChaosForge — consistent across projects
- Vite produces optimised static assets suitable for nginx serving
- Component model fits the recursive span tree rendering requirement

**Separate API and Web containers:**
- The Web container serves only static files (nginx) — zero .NET runtime overhead for UI serving
- The API container handles all data access — clean separation of concerns
- Independent restartability: UI changes do not require API restart and vice versa

## Consequences

- Two additional Docker containers increase the NAS memory footprint by approximately 90 MB idle (80 MB API + 10 MB nginx)
- Total estimated memory: ~220 MB (Postgres 40 + API 80 + nginx 10 + OS overhead)
- This is within the NAS's 1 GB RAM budget
