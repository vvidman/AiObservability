# AiObs.Core

Builder implementations and storage backends for local development and testing.

Depends on: `AiObs.Abstractions`, `System.Text.Json` (inbox, no NuGet needed).

---

## What lives here

```
AiObs.Core/
├── Builders/
│   ├── SpanBuilder.cs          # internal — implements ISpanBuilder
│   └── TraceBuilder.cs         # internal — implements ITraceBuilder
└── Stores/
    ├── InMemoryTraceStore.cs   # thread-safe, for unit tests
    └── JsonTraceStore.cs       # writes one .json file per trace to a directory
```

---

## SpanBuilder behaviour

- `WithInput`, `WithOutput`, `WithMetadata`: serialize `object?` to `JsonNode` immediately via `System.Text.Json`. Serialization errors surface at call time.
- `StartChildSpan(name)`: creates a new `SpanBuilder` and registers it as a child. Returns the child's `ISpanBuilder`.
- `Complete()`: validates that all child spans opened via `StartChildSpan()` have been completed. Throws `InvalidOperationException` if any are open. On success, builds the immutable `TraceSpan` record and returns it.
- `Dispose()`: if called without `Complete()`, force-closes all open children recursively with `SpanStatus.Error`, then force-closes self with `SpanStatus.Error`. Never throws.

## TraceBuilder behaviour

- Holds the `ITraceStore` reference passed from `ITraceStore.StartTrace()`.
- `CompleteAsync()`: validates root spans (same strict rule as `SpanBuilder`), builds the immutable `Trace` record, calls `ITraceStore.SaveAsync()`.
- `DisposeAsync()`: if called without `CompleteAsync()`, force-closes open spans and calls `CompleteAsync()` swallowing exceptions.

---

## InMemoryTraceStore

Stores traces in a `ConcurrentDictionary<string, Trace>`. Resets on each test run (new instance per test). `QueryAsync` supports all `TraceQuery` filters via LINQ.

Use for: unit tests, fast integration tests that do not require persistence.

## JsonTraceStore

Writes one JSON file per trace: `{OutputPath}/{traceName}/{traceId}.json`. Creates the directory if it does not exist. Reads are done by enumerating and deserializing files.

Not suitable for concurrent write-heavy workloads — intended for single-developer local debugging without a running database.

Configuration:

```csharp
services.AddSingleton<ITraceStore>(
    new JsonTraceStore(options =>
    {
        options.OutputPath = "./traces";
    }));
```
