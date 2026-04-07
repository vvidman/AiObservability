# ADR-006 — Strict child span validation: Complete() throws if children are open

**Date:** 2026-04-07
**Status:** Accepted

## Context

`ISpanBuilder.StartChildSpan()` creates a child span that must be completed before the parent. Two enforcement strategies were evaluated:

**A) Lenient:** if `Complete()` is called on a parent with open children, auto-close them and proceed.

**B) Strict:** if `Complete()` is called on a parent with open children, throw `InvalidOperationException`.

## Decision

Option B — strict. `Complete()` throws if any child span opened via `StartChildSpan()` is not yet completed.

## Reasons

- A forgotten `Complete()` call on a child span is a bug in the instrumentation code. Auto-closing it silently would hide the bug and produce a trace with incorrect latency data for the child (it would be closed at the parent's completion time, not when the step actually finished).
- Incorrect latency data in traces is worse than a thrown exception during development — the data looks valid but is misleading.
- The `using var` pattern makes it natural to complete spans in the right order. The strict rule enforces correct use of the API at development time, not silently at runtime.
- Errors surface during development and testing, not in production.

## Dispose behaviour (not lenient, not strict — pragmatic)

`Dispose()` without a prior `Complete()` is also a bug, but `Dispose()` must not throw (standard .NET contract). The resolution:

- `Dispose()` force-closes all open child spans recursively with `SpanStatus.Error`.
- The parent span is then force-closed with `SpanStatus.Error` and an error message indicating it was abandoned.
- The resulting `TraceSpan` is still saved — a visible Error span in the trace is more useful for debugging than no span at all.

This means the strict rule (Option B) applies only to the explicit `Complete()` call path. The `Dispose()` path is pragmatically handled to ensure no data is lost.

## Consequences

- Callers must structure their code so that child `Complete()` is called before the parent's `Complete()`. The `using` statement handles this naturally when scopes are correctly nested.
- Integration tests for the builder must cover the exception case.
