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
using AiObs.Abstractions.Models;
using AiObs.Core.Stores;
using FluentAssertions;

namespace AiObs.Core.Tests;

public sealed class TraceBuilderTests
{
    [Fact]
    public async Task CompleteAsync_with_open_root_span_throws_listing_span_name()
    {
        var store = new InMemoryTraceStore();
        var traceBuilder = store.StartTrace("my_trace");
        traceBuilder.StartSpan("open_span");

        var act = async () => await traceBuilder.CompleteAsync();

        await act.Should().ThrowAsync<InvalidOperationException>()
                 .WithMessage("*open_span*");
    }

    [Fact]
    public async Task CompleteAsync_calls_save_on_store()
    {
        var store = new InMemoryTraceStore();
        var traceBuilder = store.StartTrace("pipeline");

        var span = traceBuilder.StartSpan("step");
        span.Complete();

        var trace = await traceBuilder.CompleteAsync();

        var found = await store.FindAsync(trace.Id);
        found.Should().NotBeNull();
        found!.Id.Should().Be(trace.Id);
    }

    [Fact]
    public async Task WithTag_values_appear_in_saved_trace()
    {
        var store = new InMemoryTraceStore();
        var traceBuilder = store.StartTrace("rag_query");
        traceBuilder.WithTag("pipeline", "RagLab").WithTag("model", "claude-sonnet-4-6");

        var span = traceBuilder.StartSpan("step");
        span.Complete();

        var trace = await traceBuilder.CompleteAsync();

        trace.Tags["pipeline"].Should().Be("RagLab");
        trace.Tags["model"].Should().Be("claude-sonnet-4-6");
    }

    [Fact]
    public async Task DisposeAsync_without_complete_still_saves_trace_with_error_spans()
    {
        var store = new InMemoryTraceStore();
        await using var traceBuilder = store.StartTrace("failing_pipeline");

        using (traceBuilder.StartSpan("broken_span"))
        {
            // intentionally not completing span or trace
        }

        // DisposeAsync force-closes and saves
        await ((IAsyncDisposable)traceBuilder).DisposeAsync();

        var results = await store.QueryAsync(new TraceQuery());
        results.Should().HaveCount(1);

        var trace = results[0];
        trace.RootSpans.Should().HaveCount(1);
        trace.RootSpans[0].Status.Should().Be(SpanStatus.Error);
    }
}
