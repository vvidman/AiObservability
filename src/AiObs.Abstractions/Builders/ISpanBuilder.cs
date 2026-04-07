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

/// <summary>Fluent builder for a single pipeline span.</summary>
public interface ISpanBuilder : IDisposable
{
    /// <summary>Sets the input value for this span. Serialized to JSON immediately.</summary>
    ISpanBuilder WithInput(object? value);

    /// <summary>Sets the output value for this span. Serialized to JSON immediately.</summary>
    ISpanBuilder WithOutput(object? value);

    /// <summary>Adds a metadata entry to this span. Value is serialized to JSON immediately.</summary>
    ISpanBuilder WithMetadata(string key, object? value);

    /// <summary>Records an error on this span, setting its status to <see cref="SpanStatus.Error"/>.</summary>
    ISpanBuilder RecordError(Exception exception);

    /// <summary>Opens a new child span nested under this span and returns its builder.</summary>
    ISpanBuilder StartChildSpan(string name);

    /// <summary>
    /// Completes this span and returns the immutable <see cref="TraceSpan"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown if any child span opened via <see cref="StartChildSpan"/> has not yet been completed,
    /// or if this span has already been completed.
    /// </exception>
    TraceSpan Complete();
}
