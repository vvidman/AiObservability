# AiObservability

A lightweight, cross-project observability library for .NET 10 AI pipelines.

Designed to instrument **RagLab**, **ChaosForge**, and **Dev_Scaffold** with structured tracing — without coupling any of them to a specific storage backend.

---

## What it does

Every LLM call, retrieval step, and agent action becomes a **span** inside a **trace**. Each span captures input, output, latency, and arbitrary metadata as queryable JSON. Traces are persisted to PostgreSQL and can be inspected directly with standard SQL tooling.

---

## Solution structure

```
AiObservability/
├── src/
│   ├── AiObs.Abstractions/     # Interfaces and domain models — zero external dependencies
│   ├── AiObs.Core/             # Builder implementations + file/in-memory stores
│   └── AiObs.Postgres/         # PostgreSQL store via Npgsql
├── tests/
│   └── AiObs.Core.Tests/       # Unit tests for builders and store contracts
├── docker/
│   └── docker-compose.yml      # PostgreSQL 16 Alpine for local/NAS deployment
└── docs/
    ├── architecture.md
    └── adr/
        ├── ADR-001-standalone-library.md
        ├── ADR-002-postgresql.md
        ├── ADR-003-npgsql-direct.md
        ├── ADR-004-denormalized-schema.md
        ├── ADR-005-jsonnode-input-output.md
        ├── ADR-006-strict-child-span-validation.md
        └── ADR-007-postgres-separate-project.md
```

---

## How consuming projects reference it

All three consuming projects follow the same pattern:

```
<project>.Core / Domain / Application.Interfaces
    └── → AiObs.Abstractions   (ProjectReference)

<project>.Infrastructure / ServiceHost
    └── → AiObs.Core           (ProjectReference)

<project>.Console / API / CLI   (composition root)
    └── → AiObs.Postgres       (ProjectReference)
         registers: PostgresTraceStore as ITraceStore
```

The composition root is the only layer that knows which `ITraceStore` implementation is active.

---

## Quick integration example (RagLab)

```csharp
// RagLab.Console / Program.cs
services.AddSingleton<ITraceStore>(
    new PostgresTraceStore("Host=nas.local;Database=aiobs;Username=aiobs;Password=..."));

// RagLab.Infrastructure / QueryPipeline.cs
public class QueryPipeline(ITraceStore traceStore, ...)
{
    public async Task<string> QueryAsync(string question, CancellationToken ct)
    {
        await using var trace = traceStore.StartTrace("rag_query");
        trace.WithTag("pipeline", "RagLab").WithTag("model", _modelName);

        using var embedSpan = trace.StartSpan("embed_query");
        var embedding = await _embedder.EmbedAsync(question, ct);
        embedSpan.WithInput(question).WithOutput(embedding).Complete();

        using var retrieveSpan = trace.StartSpan("retrieve_docs");
        var chunks = await _vectorStore.SearchAsync(embedding, topK: 5, ct);
        retrieveSpan.WithInput(embedding)
                    .WithOutput(chunks)
                    .WithMetadata("chunk_count", chunks.Count)
                    .Complete();

        using var genSpan = trace.StartSpan("generate");
        var answer = await _generator.GenerateAsync(BuildPrompt(question, chunks), ct);
        genSpan.WithInput(question)
               .WithOutput(answer.Text)
               .WithMetadata("input_tokens", answer.InputTokens)
               .WithMetadata("output_tokens", answer.OutputTokens)
               .Complete();

        await trace.CompleteAsync(ct);
        return answer.Text;
    }
}
```

---

## Storage backends

| Backend | Project | Use case |
|---|---|---|
| `InMemoryTraceStore` | `AiObs.Core` | Unit tests |
| `JsonTraceStore` | `AiObs.Core` | Local development without Docker |
| `PostgresTraceStore` | `AiObs.Postgres` | Production / NAS deployment |

---

## Prerequisites

- .NET 10 SDK
- Docker (for PostgreSQL on NAS)
- A PostgreSQL 16 instance (see `docker/docker-compose.yml`)
