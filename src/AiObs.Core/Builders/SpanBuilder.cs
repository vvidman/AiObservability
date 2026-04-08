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
using System.Text.Json.Nodes;
using AiObs.Abstractions.Builders;
using AiObs.Abstractions.Models;

namespace AiObs.Core.Builders;

internal sealed class SpanBuilder : ISpanBuilder
{
    private readonly string _name;
    private readonly DateTimeOffset _startedAt;
    private readonly List<SpanBuilder> _children = [];
    private readonly Dictionary<string, JsonNode?> _metadata = [];
    private readonly JsonSerializerOptions? _serializerOptions;

    private JsonNode? _input;
    private JsonNode? _output;
    private string? _errorMessage;
    private SpanStatus _status = SpanStatus.Ok;
    private bool _isCompleted;
    private TraceSpan? _completedSpan;

    internal SpanBuilder(string name, JsonSerializerOptions? serializerOptions = null)
    {
        _name = name;
        _startedAt = DateTimeOffset.UtcNow;
        _serializerOptions = serializerOptions;
    }

    internal string Name => _name;
    internal bool IsCompleted => _isCompleted;
    internal TraceSpan GetCompletedSpan() => _completedSpan ?? throw new InvalidOperationException($"Span '{_name}' has not been completed yet.");

    public ISpanBuilder WithInput(object? value)
    {
        _input = JsonSerializer.SerializeToNode(value, _serializerOptions);
        return this;
    }

    public ISpanBuilder WithOutput(object? value)
    {
        _output = JsonSerializer.SerializeToNode(value, _serializerOptions);
        return this;
    }

    public ISpanBuilder WithMetadata(string key, object? value)
    {
        _metadata[key] = JsonSerializer.SerializeToNode(value, _serializerOptions);
        return this;
    }

    public ISpanBuilder RecordError(Exception exception)
    {
        _errorMessage = exception.Message;
        _status = SpanStatus.Error;
        return this;
    }

    public ISpanBuilder StartChildSpan(string name)
    {
        var child = new SpanBuilder(name, _serializerOptions);
        _children.Add(child);
        return child;
    }

    public TraceSpan Complete()
    {
        if (_isCompleted)
            throw new InvalidOperationException($"Span '{_name}' has already been completed.");

        var openChildren = _children.Where(c => !c._isCompleted).Select(c => c._name).ToList();
        if (openChildren.Count > 0)
            throw new InvalidOperationException(
                $"Cannot complete span '{_name}': the following child spans are still open: {string.Join(", ", openChildren)}.");

        _isCompleted = true;
        _completedSpan = BuildSpan();
        return _completedSpan;
    }

    internal void ForceClose()
    {
        if (_isCompleted) return;

        foreach (var child in _children.Where(c => !c._isCompleted))
            child.ForceClose();

        _status = SpanStatus.Error;
        _errorMessage = "Span was abandoned (Dispose called without Complete).";
        _isCompleted = true;
        _completedSpan = BuildSpan();
    }

    public void Dispose()
    {
        if (_isCompleted) return;
        ForceClose();
    }

    private TraceSpan BuildSpan() => new()
    {
        Id = Guid.NewGuid().ToString("N"),
        Name = _name,
        StartedAt = _startedAt,
        CompletedAt = DateTimeOffset.UtcNow,
        Status = _status,
        Input = _input,
        Output = _output,
        ErrorMessage = _errorMessage,
        Metadata = _metadata.AsReadOnly(),
        Children = _children
            .Where(c => c._isCompleted)
            .Select(c => c.GetCompletedSpan())
            .OrderBy(s => s.StartedAt)
            .ToList()
    };
}
