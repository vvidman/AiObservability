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

using System.Text.Json;
using AiObs.Abstractions;
using AiObs.Abstractions.Builders;
using AiObs.Abstractions.Models;

namespace AiObs.Core.Builders;

internal sealed class TraceBuilder : ITraceBuilder
{
    private readonly string _name;
    private readonly ITraceStore _store;
    private readonly JsonSerializerOptions? _serializerOptions;
    private readonly DateTimeOffset _startedAt = DateTimeOffset.UtcNow;
    private readonly List<SpanBuilder> _rootSpans = [];
    private readonly Dictionary<string, string> _tags = [];
    private bool _isCompleted;

    internal TraceBuilder(string name, ITraceStore store, JsonSerializerOptions? serializerOptions = null)
    {
        _name = name;
        _store = store;
        _serializerOptions = serializerOptions;
    }

    public ISpanBuilder StartSpan(string name)
    {
        var span = new SpanBuilder(name, _serializerOptions);
        _rootSpans.Add(span);
        return span;
    }

    public ITraceBuilder WithTag(string key, string value)
    {
        _tags[key] = value;
        return this;
    }

    public async Task<Trace> CompleteAsync(CancellationToken cancellationToken = default)
    {
        if (_isCompleted)
            throw new InvalidOperationException($"Trace '{_name}' has already been completed.");

        var openSpans = _rootSpans.Where(s => !s.IsCompleted).Select(s => s.Name).ToList();
        if (openSpans.Count > 0)
            throw new InvalidOperationException(
                $"Cannot complete trace '{_name}': the following root spans are still open: {string.Join(", ", openSpans)}.");

        _isCompleted = true;

        var trace = new Trace
        {
            Id = Guid.NewGuid().ToString("N"),
            Name = _name,
            StartedAt = _startedAt,
            CompletedAt = DateTimeOffset.UtcNow,
            Tags = _tags.AsReadOnly(),
            RootSpans = _rootSpans
                .Select(s => s.GetCompletedSpan())
                .OrderBy(s => s.StartedAt)
                .ToList()
        };

        await _store.SaveAsync(trace, cancellationToken);
        return trace;
    }

    public async ValueTask DisposeAsync()
    {
        if (_isCompleted) return;

        foreach (var span in _rootSpans.Where(s => !s.IsCompleted))
            span.ForceClose();

        try { await CompleteAsync(); }
        catch { /* swallow — dispose must not throw */ }
    }
}
