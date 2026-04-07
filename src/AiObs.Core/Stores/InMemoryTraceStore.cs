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

using System.Collections.Concurrent;
using AiObs.Abstractions;
using AiObs.Abstractions.Builders;
using AiObs.Abstractions.Models;
using AiObs.Core.Builders;

namespace AiObs.Core.Stores;

/// <summary>
/// Thread-safe in-memory <see cref="ITraceStore"/> backed by a <see cref="ConcurrentDictionary{TKey,TValue}"/>.
/// Intended for unit tests; data is lost when the instance is discarded.
/// </summary>
public sealed class InMemoryTraceStore : ITraceStore
{
    private readonly ConcurrentDictionary<string, Trace> _traces = new();

    /// <inheritdoc />
    public ITraceBuilder StartTrace(string name) => new TraceBuilder(name, this);

    /// <inheritdoc />
    public Task SaveAsync(Trace trace, CancellationToken cancellationToken = default)
    {
        _traces[trace.Id] = trace;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<Trace?> FindAsync(string traceId, CancellationToken cancellationToken = default)
    {
        _traces.TryGetValue(traceId, out var trace);
        return Task.FromResult(trace);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<Trace>> QueryAsync(TraceQuery query, CancellationToken cancellationToken = default)
    {
        IEnumerable<Trace> result = _traces.Values;

        if (query.NameContains is not null)
            result = result.Where(t => t.Name.Contains(query.NameContains, StringComparison.OrdinalIgnoreCase));

        if (query.From is not null)
            result = result.Where(t => t.StartedAt >= query.From.Value);

        if (query.To is not null)
            result = result.Where(t => t.StartedAt <= query.To.Value);

        if (query.TagFilters is not null)
            result = result.Where(t =>
                query.TagFilters.All(kv => t.Tags.TryGetValue(kv.Key, out var v) && v == kv.Value));

        result = result.Take(query.Limit);

        return Task.FromResult<IReadOnlyList<Trace>>(result.ToList());
    }
}
