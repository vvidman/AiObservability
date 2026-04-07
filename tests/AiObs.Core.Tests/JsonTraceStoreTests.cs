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

using AiObs.Core.Stores;
using FluentAssertions;

namespace AiObs.Core.Tests;

public sealed class JsonTraceStoreTests : IDisposable
{
    private readonly string _outputPath = Path.Combine(Path.GetTempPath(), $"aiobs_tests_{Guid.NewGuid():N}");

    [Fact]
    public async Task SaveAsync_creates_file_at_expected_path()
    {
        var store = MakeStore();
        var tb = store.StartTrace("rag_query");
        tb.StartSpan("step").Complete();
        var trace = await tb.CompleteAsync();

        var expectedPath = Path.Combine(_outputPath, "rag_query", $"{trace.Id}.json");
        File.Exists(expectedPath).Should().BeTrue();
    }

    [Fact]
    public async Task FindAsync_deserializes_saved_file_correctly()
    {
        var store = MakeStore();
        var tb = store.StartTrace("rag_query");
        tb.StartSpan("embed").Complete();
        var saved = await tb.CompleteAsync();

        var found = await store.FindAsync(saved.Id);

        found.Should().NotBeNull();
        found!.Id.Should().Be(saved.Id);
        found.Name.Should().Be("rag_query");
    }

    [Fact]
    public async Task Round_trip_preserves_all_fields()
    {
        var store = MakeStore();
        var tb = store.StartTrace("pipeline");
        tb.WithTag("env", "test");

        var span = tb.StartSpan("step");
        span.WithInput("in").WithOutput("out").WithMetadata("tokens", 42);
        span.Complete();

        var saved = await tb.CompleteAsync();
        var found = await store.FindAsync(saved.Id);

        found.Should().NotBeNull();
        found!.Name.Should().Be(saved.Name);
        found.Tags.Should().ContainKey("env").WhoseValue.Should().Be("test");
        found.RootSpans.Should().HaveCount(1);

        var foundSpan = found.RootSpans[0];
        foundSpan.Name.Should().Be("step");
        foundSpan.Input.Should().NotBeNull();
        foundSpan.Output.Should().NotBeNull();
        foundSpan.Metadata.Should().ContainKey("tokens");
    }

    public void Dispose()
    {
        if (Directory.Exists(_outputPath))
            Directory.Delete(_outputPath, recursive: true);
    }

    private JsonTraceStore MakeStore() =>
        new(o => o.OutputPath = _outputPath);
}
