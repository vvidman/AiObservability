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

using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace AiObs.Abstractions.Models;

/// <summary>Immutable record representing a single pipeline step within a trace.</summary>
public sealed record TraceSpan
{
    /// <summary>Unique identifier (GUID without dashes).</summary>
    public required string Id { get; init; }

    /// <summary>Name of the pipeline step, e.g. "embed_query", "retrieve_docs".</summary>
    public required string Name { get; init; }

    /// <summary>UTC timestamp when this span was started.</summary>
    public required DateTimeOffset StartedAt { get; init; }

    /// <summary>UTC timestamp when this span was completed.</summary>
    public required DateTimeOffset CompletedAt { get; init; }

    /// <summary>Whether this span completed successfully or with an error.</summary>
    public required SpanStatus Status { get; init; }

    /// <summary>Input value serialized to JSON. Null if not set.</summary>
    public JsonNode? Input { get; init; }

    /// <summary>Output value serialized to JSON. Null if not set.</summary>
    public JsonNode? Output { get; init; }

    /// <summary>Error message if <see cref="Status"/> is <see cref="SpanStatus.Error"/>.</summary>
    public string? ErrorMessage { get; init; }

    /// <summary>Arbitrary structured metadata, e.g. token counts, chunk counts.</summary>
    public IReadOnlyDictionary<string, JsonNode?> Metadata { get; init; }
        = new Dictionary<string, JsonNode?>();

    /// <summary>Completed child spans, ordered by <see cref="StartedAt"/>.</summary>
    public IReadOnlyList<TraceSpan> Children { get; init; } = Array.Empty<TraceSpan>();

    /// <summary>Computed duration: <see cref="CompletedAt"/> - <see cref="StartedAt"/>.</summary>
    [JsonIgnore]
    public TimeSpan Duration => CompletedAt - StartedAt;
}
