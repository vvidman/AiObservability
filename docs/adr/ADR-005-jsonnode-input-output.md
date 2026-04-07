# ADR-005 — JsonNode? for Input and Output instead of object?

**Date:** 2026-04-07
**Status:** Accepted

## Context

`TraceSpan.Input` and `TraceSpan.Output` need to hold values from three different projects. In RagLab, a span input might be a `string` query or a `float[]` embedding. In ChaosForge, it might be a `WorkTask` entity. In Dev_Scaffold, it might be a `ValidationOutcome` record.

Two options were considered:

**A) `object?`** — accept anything, serialize lazily at persistence time.

**B) `JsonNode?`** — serialize eagerly at builder assignment time, store as structured JSON.

## Decision

`JsonNode?` in the domain model (`TraceSpan`, and `Metadata` values). The builder's `WithInput(object? value)` method accepts `object?` for caller convenience and converts to `JsonNode` immediately.

## Reasons

- **Human readability in the database:** JSONB columns in PostgreSQL render as structured, readable JSON. If `Input` were stored as a serialized `object?`, it would require an extra deserialization step to become readable.
- **No runtime type surprises:** `object?` at the model level means the type information is lost after construction. A `JsonNode?` carries the structure and can be safely passed around, compared, and logged without knowing the original type.
- **Separation of concerns:** the `SpanBuilder` owns the serialization concern. Consuming code (RagLab pipeline, ChaosForge agent) passes its domain object; the builder converts it. The `TraceSpan` record never deals with raw `object?`.
- **Consistency:** `Metadata` values follow the same pattern. A span's metadata might carry `{ "chunk_count": 5 }` or `{ "token_breakdown": { "prompt": 120, "completion": 85 } }` — both are naturally `JsonNode`.

## Consequences

- `AiObs.Abstractions` depends on `System.Text.Json` (for `JsonNode`). In .NET 10, `System.Text.Json` is an inbox library — no NuGet package required, but it is a conceptual dependency.
- Serialization errors (e.g. a type with a circular reference) surface at `WithInput()` / `WithOutput()` call time, not at persistence time. This is preferable — the error is closer to its source.
- Custom `JsonSerializerOptions` can be passed to `SpanBuilder` if consuming projects need specific converters (e.g. for `float[]` embeddings or EF Core entities).
