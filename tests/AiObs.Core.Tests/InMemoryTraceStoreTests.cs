/*
   Copyright 2026 Viktor Vidman (vvidman)

   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.
*/

using AiObs.Abstractions;
using AiObs.Core.Stores;
using FluentAssertions;

namespace AiObs.Core.Tests;

public sealed class InMemoryTraceStoreTests
{
    [Fact]
    public async Task FindAsync_returns_null_for_unknown_id()
    {
        var store = new InMemoryTraceStore();

        var result = await store.FindAsync("does-not-exist");

        result.Should().BeNull();
    }

    [Fact]
    public async Task SaveAsync_then_FindAsync_round_trip_preserves_all_fields()
    {
        var store = new InMemoryTraceStore();
        var traceBuilder = store.StartTrace("rag_query");
        traceBuilder.WithTag("pipeline", "RagLab");

        var spanBuilder = traceBuilder.StartSpan("embed");
        spanBuilder.WithInput("hello").WithOutput("vec").WithMetadata("model", "text-embedding-3");
        spanBuilder.Complete();

        var saved = await traceBuilder.CompleteAsync();
        var found = await store.FindAsync(saved.Id);

        found.Should().NotBeNull();
        found!.Id.Should().Be(saved.Id);
        found.Name.Should().Be("rag_query");
        found.Tags["pipeline"].Should().Be("RagLab");
        found.RootSpans.Should().HaveCount(1);
        found.RootSpans[0].Name.Should().Be("embed");
        found.RootSpans[0].Metadata.Should().ContainKey("model");
    }

    [Fact]
    public async Task QueryAsync_filters_by_NameContains()
    {
        var store = new InMemoryTraceStore();
        await CompleteTrace(store, "rag_query");
        await CompleteTrace(store, "agent_task");

        var results = await store.QueryAsync(new TraceQuery { NameContains = "rag" });

        results.Should().HaveCount(1);
        results[0].Name.Should().Be("rag_query");
    }

    [Fact]
    public async Task QueryAsync_filters_by_From_and_To()
    {
        var store = new InMemoryTraceStore();
        await CompleteTrace(store, "early");
        await Task.Delay(10);
        var mid = DateTimeOffset.UtcNow;
        await Task.Delay(10);
        await CompleteTrace(store, "late");

        var results = await store.QueryAsync(new TraceQuery { From = mid });

        results.Should().HaveCount(1);
        results[0].Name.Should().Be("late");
    }

    [Fact]
    public async Task QueryAsync_filters_by_TagFilters()
    {
        var store = new InMemoryTraceStore();

        var t1 = store.StartTrace("pipeline_a");
        t1.WithTag("env", "prod");
        t1.StartSpan("s").Complete();
        await t1.CompleteAsync();

        var t2 = store.StartTrace("pipeline_b");
        t2.WithTag("env", "dev");
        t2.StartSpan("s").Complete();
        await t2.CompleteAsync();

        var results = await store.QueryAsync(new TraceQuery
        {
            TagFilters = new Dictionary<string, string> { ["env"] = "prod" }
        });

        results.Should().HaveCount(1);
        results[0].Name.Should().Be("pipeline_a");
    }

    [Fact]
    public async Task QueryAsync_respects_Limit()
    {
        var store = new InMemoryTraceStore();
        for (var i = 0; i < 5; i++)
            await CompleteTrace(store, $"trace_{i}");

        var results = await store.QueryAsync(new TraceQuery { Limit = 2 });

        results.Should().HaveCount(2);
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    private static async Task CompleteTrace(InMemoryTraceStore store, string name)
    {
        var tb = store.StartTrace(name);
        tb.StartSpan("step").Complete();
        await tb.CompleteAsync();
    }
}
