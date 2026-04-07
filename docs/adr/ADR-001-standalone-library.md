# ADR-001 — Standalone observability library

**Date:** 2026-04-07
**Status:** Accepted

## Context

Three separate .NET projects (RagLab, ChaosForge, Dev_Scaffold) all need structured tracing and a trace store. The question was whether to build observability inside each project independently or extract it into a shared library.

## Decision

A standalone solution (`AiObservability`) with its own projects (`AiObs.Abstractions`, `AiObs.Core`, `AiObs.Postgres`) is the chosen approach. Consuming projects reference it via `<ProjectReference>`.

## Reasons

- Tracing logic (builder pattern, strict child validation, JSON serialization) is non-trivial. Duplicating it across three projects would mean three places to fix bugs.
- The `ITraceStore` abstraction is the same contract in all three projects. A single definition eliminates the risk of divergence.
- A `ProjectReference` keeps the feedback loop tight during development — no NuGet publish required.
- If the library matures, it can be published as a NuGet package with no changes to the consuming projects.

## Consequences

- The `AiObservability` solution must be checked out alongside the consuming projects, or the path references must be maintained.
- Breaking changes to `AiObs.Abstractions` interfaces affect all three consuming projects simultaneously — this is a feature, not a bug (forces consistency).
