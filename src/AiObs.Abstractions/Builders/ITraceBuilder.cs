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

namespace AiObs.Abstractions.Builders;

/// <summary>Fluent builder for a full pipeline trace.</summary>
public interface ITraceBuilder : IAsyncDisposable
{
    /// <summary>Opens a new root-level span and returns its builder.</summary>
    ISpanBuilder StartSpan(string name);

    /// <summary>Adds a scalar tag to the trace for filtering.</summary>
    ITraceBuilder WithTag(string key, string value);

    /// <summary>
    /// Completes the trace, persists it via <see cref="ITraceStore.SaveAsync"/>,
    /// and returns the immutable <see cref="Trace"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown if any root span opened via <see cref="StartSpan"/> has not yet been completed,
    /// or if this trace has already been completed.
    /// </exception>
    Task<Trace> CompleteAsync(CancellationToken cancellationToken = default);
}
