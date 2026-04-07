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

using AiObs.Abstractions.Models;
using AiObs.Core.Builders;
using AiObs.Core.Stores;
using FluentAssertions;

namespace AiObs.Core.Tests;

public sealed class SpanBuilderTests
{
    [Fact]
    public void Complete_returns_span_with_correct_name_status_and_positive_duration()
    {
        var builder = new SpanBuilder("embed_query");

        var span = builder.Complete();

        span.Name.Should().Be("embed_query");
        span.Status.Should().Be(SpanStatus.Ok);
        span.Duration.Should().BeGreaterThan(TimeSpan.Zero);
        span.Id.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void Complete_with_open_child_throws_listing_child_name()
    {
        var builder = new SpanBuilder("parent");
        builder.StartChildSpan("child_one");

        var act = builder.Invoking(b => b.Complete());

        act.Should().Throw<InvalidOperationException>()
           .WithMessage("*child_one*");
    }

    [Fact]
    public void Complete_twice_throws()
    {
        var builder = new SpanBuilder("span");
        builder.Complete();

        var act = builder.Invoking(b => b.Complete());

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void WithInput_serializes_value_to_JsonNode()
    {
        var builder = new SpanBuilder("span");
        builder.WithInput(new { question = "hello" });

        var span = builder.Complete();

        span.Input.Should().NotBeNull();
        span.Input!["question"]!.GetValue<string>().Should().Be("hello");
    }

    [Fact]
    public void WithOutput_serializes_value_to_JsonNode()
    {
        var builder = new SpanBuilder("span");
        builder.WithOutput(42);

        var span = builder.Complete();

        span.Output.Should().NotBeNull();
        span.Output!.GetValue<int>().Should().Be(42);
    }

    [Fact]
    public void RecordError_sets_status_error_and_error_message()
    {
        var builder = new SpanBuilder("span");
        builder.RecordError(new InvalidOperationException("something went wrong"));

        var span = builder.Complete();

        span.Status.Should().Be(SpanStatus.Error);
        span.ErrorMessage.Should().Be("something went wrong");
    }

    [Fact]
    public async Task Dispose_without_complete_saves_error_span_via_store()
    {
        var store = new InMemoryTraceStore();
        await using var traceBuilder = store.StartTrace("test_trace");

        using (traceBuilder.StartSpan("abandoned_span"))
        {
            // intentionally not calling Complete
        }

        // DisposeAsync on traceBuilder saves the trace
        await ((IAsyncDisposable)traceBuilder).DisposeAsync();

        var results = await store.QueryAsync(new AiObs.Abstractions.TraceQuery());
        results.Should().HaveCount(1);

        var savedTrace = results[0];
        savedTrace.RootSpans.Should().HaveCount(1);
        savedTrace.RootSpans[0].Status.Should().Be(SpanStatus.Error);
        savedTrace.RootSpans[0].ErrorMessage.Should().Contain("abandoned");
    }

    [Fact]
    public void Child_span_complete_before_parent_complete_succeeds()
    {
        var parent = new SpanBuilder("parent");
        var child = parent.StartChildSpan("child");

        var childSpan = child.Complete();
        var parentSpan = parent.Complete();

        childSpan.Status.Should().Be(SpanStatus.Ok);
        parentSpan.Status.Should().Be(SpanStatus.Ok);
        parentSpan.Children.Should().HaveCount(1);
        parentSpan.Children[0].Name.Should().Be("child");
    }
}
