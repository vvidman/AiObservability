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

namespace AiObs.Abstractions;

/// <summary>Filter model for <see cref="ITraceStore.QueryAsync"/>.</summary>
public sealed class TraceQuery
{
    /// <summary>Case-insensitive substring match on trace name.</summary>
    public string? NameContains { get; init; }

    /// <summary>Inclusive lower bound on <c>StartedAt</c>.</summary>
    public DateTimeOffset? From { get; init; }

    /// <summary>Inclusive upper bound on <c>StartedAt</c>.</summary>
    public DateTimeOffset? To { get; init; }

    /// <summary>All specified key-value pairs must be present in the trace's tags.</summary>
    public IReadOnlyDictionary<string, string>? TagFilters { get; init; }

    /// <summary>Maximum number of results to return. Defaults to 100.</summary>
    public int Limit { get; init; } = 100;
}
