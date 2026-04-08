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

using AiObs.Abstractions.Builders;
using AiObs.Abstractions.Models;

namespace AiObs.Abstractions;

/// <summary>Persistence and query contract for traces.</summary>
public interface ITraceStore
{
    /// <summary>Starts a new trace and returns a builder.</summary>
    ITraceBuilder StartTrace(string name);

    /// <summary>
    /// Persists a completed trace. Called internally by <see cref="ITraceBuilder.CompleteAsync"/>.
    /// Public to allow manual persistence of a pre-built <see cref="Trace"/>.
    /// </summary>
    Task SaveAsync(Trace trace, CancellationToken cancellationToken = default);

    /// <summary>Looks up a trace by its ID. Returns null if not found.</summary>
    Task<Trace?> FindAsync(string traceId, CancellationToken cancellationToken = default);

    /// <summary>Queries traces using the provided filter.</summary>
    Task<IReadOnlyList<Trace>> QueryAsync(TraceQuery query, CancellationToken cancellationToken = default);

    /// <summary>Deletes a trace by its ID. No-op if the trace does not exist.</summary>
    Task DeleteAsync(string traceId, CancellationToken cancellationToken = default);
}
