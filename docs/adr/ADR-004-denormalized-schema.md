# ADR-004 — Denormalized schema: full span tree as JSONB

**Date:** 2026-04-07
**Status:** Accepted

## Context

`TraceSpan` is a recursive structure — a span can have child spans, which can have their own children. Three schema options were evaluated:

**A) Denormalized:** one row per trace, span tree stored as a JSONB array in `root_spans`.

**B) Normalized:** separate `traces` and `spans` tables, with `parent_span_id` foreign key. Tree reconstruction via recursive CTE.

**C) Hybrid:** separate tables, but with `children` as a JSONB array of IDs.

## Decision

Option A — denormalized, one row per trace, full span tree in `root_spans JSONB`.

## Reasons

- The primary access pattern is: retrieve one trace by ID and display the full span tree. Denormalization makes this a single `SELECT` with no joins or recursive CTEs.
- The library is an observability tool, not an analytics platform. Span-level aggregation (e.g. "average embed_query latency across 10,000 traces") is not an initial requirement. If it becomes one, it will be addressed by a separate `metrics` table, not by querying `root_spans`.
- The `Trace` record in memory is already a nested structure. JSONB storage mirrors this exactly — serialization is a direct `JsonSerializer.Serialize(trace)`, deserialization is the reverse. No mapping layer needed.
- JSONB GIN indexes and containment operators (`@>`) support the ad-hoc queries that are actually needed: filter by tag, filter by error status, find slow traces.

## Consequences

- Span-level queries (e.g. "find all traces where the generate span took > 2s") require JSONB path expressions and will not benefit from B-tree indexes on span fields.
- The accepted tradeoff: this is a debug and inspection tool. Query performance on span internals is not a priority. Human-readable output is.
