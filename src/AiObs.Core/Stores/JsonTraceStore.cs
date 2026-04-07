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
using AiObs.Core.Builders;

namespace AiObs.Core.Stores;

/// <summary>Options for <see cref="JsonTraceStore"/>.</summary>
public sealed class JsonTraceStoreOptions
{
    /// <summary>Root directory where trace files are written. Defaults to <c>./traces</c>.</summary>
    public string OutputPath { get; set; } = "./traces";
}

/// <summary>
/// File-backed <see cref="ITraceStore"/> that writes one JSON file per trace to
/// <c>{OutputPath}/{traceName}/{traceId}.json</c>. Intended for local development
/// without a running database.
/// </summary>
public sealed class JsonTraceStore : ITraceStore
{
    private readonly JsonTraceStoreOptions _options;

    private static readonly JsonSerializerOptions SerializerOptions = new() { WriteIndented = true };

    /// <summary>Creates a new <see cref="JsonTraceStore"/> using the provided configuration delegate.</summary>
    public JsonTraceStore(Action<JsonTraceStoreOptions> configure)
    {
        _options = new JsonTraceStoreOptions();
        configure(_options);
    }

    /// <inheritdoc />
    public ITraceBuilder StartTrace(string name) => new TraceBuilder(name, this);

    /// <inheritdoc />
    public async Task SaveAsync(Trace trace, CancellationToken cancellationToken = default)
    {
        var dir = Path.Combine(_options.OutputPath, trace.Name);
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, $"{trace.Id}.json");
        var json = JsonSerializer.Serialize(trace, SerializerOptions);
        await File.WriteAllTextAsync(path, json, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Trace?> FindAsync(string traceId, CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(_options.OutputPath)) return null;

        foreach (var file in Directory.EnumerateFiles(_options.OutputPath, "*.json", SearchOption.AllDirectories))
        {
            var json = await File.ReadAllTextAsync(file, cancellationToken);
            var trace = JsonSerializer.Deserialize<Trace>(json, SerializerOptions);
            if (trace?.Id == traceId) return trace;
        }

        return null;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Trace>> QueryAsync(TraceQuery query, CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(_options.OutputPath)) return [];

        var traces = new List<Trace>();
        foreach (var file in Directory.EnumerateFiles(_options.OutputPath, "*.json", SearchOption.AllDirectories))
        {
            var json = await File.ReadAllTextAsync(file, cancellationToken);
            var trace = JsonSerializer.Deserialize<Trace>(json, SerializerOptions);
            if (trace is not null) traces.Add(trace);
        }

        IEnumerable<Trace> result = traces;

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

        return result.ToList();
    }
}
