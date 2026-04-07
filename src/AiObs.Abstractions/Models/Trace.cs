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

using System.Text.Json.Serialization;

namespace AiObs.Abstractions.Models;

/// <summary>Immutable record representing a complete pipeline execution.</summary>
public sealed record Trace
{
    /// <summary>Unique identifier (GUID without dashes).</summary>
    public required string Id { get; init; }

    /// <summary>Name of the pipeline, e.g. "rag_query", "agent_task".</summary>
    public required string Name { get; init; }

    /// <summary>UTC timestamp when this trace was started.</summary>
    public required DateTimeOffset StartedAt { get; init; }

    /// <summary>UTC timestamp when this trace was completed.</summary>
    public required DateTimeOffset CompletedAt { get; init; }

    /// <summary>Scalar key-value tags for filtering, e.g. pipeline, model, environment.</summary>
    public IReadOnlyDictionary<string, string> Tags { get; init; }
        = new Dictionary<string, string>();

    /// <summary>Top-level spans, ordered by <see cref="TraceSpan.StartedAt"/>.</summary>
    public IReadOnlyList<TraceSpan> RootSpans { get; init; } = Array.Empty<TraceSpan>();

    /// <summary>Computed duration: <see cref="CompletedAt"/> - <see cref="StartedAt"/>.</summary>
    [JsonIgnore]
    public TimeSpan Duration => CompletedAt - StartedAt;
}
