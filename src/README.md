# AiObs.Abstractions

Domain models and interfaces for structured AI pipeline tracing. Zero external dependencies.

---

## What lives here

```
AiObs.Abstractions/
├── Models/
│   ├── SpanStatus.cs       # Ok | Error
│   ├── TraceSpan.cs        # Immutable record: one pipeline step
│   └── Trace.cs            # Immutable record: one full pipeline execution
├── Builders/
│   ├── ISpanBuilder.cs     # Fluent builder for a single span
│   └── ITraceBuilder.cs    # Fluent builder for a trace; calls ITraceStore.SaveAsync on complete
├── ITraceStore.cs          # Persistence contract
└── TraceQuery.cs           # Filter model for ITraceStore.QueryAsync
```

---

## Dependency rule

This project has **zero external NuGet dependencies**. The only dependency is `System.Text.Json`, which is an inbox library in .NET 10 (no NuGet package required).

It is safe to reference from any layer, including domain and core layers that must remain dependency-free from infrastructure concerns.

---

## Key design decisions

**`TraceSpan` and `Trace` are immutable records.** They are created only by builder implementations, never by hand. Do not add constructors or factory methods here.

**`Input` and `Output` on `TraceSpan` are `JsonNode?`.** The builder accepts `object?` and converts to `JsonNode` at assignment time. See ADR-005.

**`Tags` on `Trace` is `IReadOnlyDictionary<string, string>`.** Tags are simple scalar key-value pairs used for filtering (pipeline name, model name, environment). For structured values inside a span, use `Metadata`.

**`Metadata` on `TraceSpan` is `IReadOnlyDictionary<string, JsonNode?>`.** Metadata can carry structured data: token counts, chunk counts, model parameters.

---

## Interface summary

### ISpanBuilder

```csharp
ISpanBuilder WithInput(object? value);
ISpanBuilder WithOutput(object? value);
ISpanBuilder WithMetadata(string key, object? value);
ISpanBuilder RecordError(Exception exception);
ISpanBuilder StartChildSpan(string name);   // returns the child builder
TraceSpan Complete();                        // throws if any child is still open
```

`Complete()` throws `InvalidOperationException` if any child span opened via `StartChildSpan()` has not been completed. See ADR-006.

### ITraceBuilder

```csharp
ISpanBuilder StartSpan(string name);
ITraceBuilder WithTag(string key, string value);
Task<Trace> CompleteAsync(CancellationToken cancellationToken = default);
```

### ITraceStore

```csharp
ITraceBuilder StartTrace(string name);
Task SaveAsync(Trace trace, CancellationToken cancellationToken = default);
Task<Trace?> FindAsync(string traceId, CancellationToken cancellationToken = default);
Task<IReadOnlyList<Trace>> QueryAsync(TraceQuery query, CancellationToken cancellationToken = default);
```
