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

using AiObs.Abstractions;
using AiObs.Abstractions.Models;

namespace AiObs.Api.Endpoints;

/// <summary>Minimal API endpoints for browsing and managing traces.</summary>
public static class TraceEndpoints
{
    /// <summary>Maps all trace endpoints onto the application.</summary>
    public static void Map(WebApplication app)
    {
        app.MapGet("/traces", GetTraces);
        app.MapGet("/traces/{id}", GetTrace);
        app.MapDelete("/traces/{id}", DeleteTrace);
    }

    /// <summary>
    /// Lists traces matching the provided filters.
    /// Supports optional query parameters: name, from, to, limit, tag_* (e.g. tag_pipeline=RagLab).
    /// Returns trace summaries without RootSpans.
    /// </summary>
    private static async Task<IResult> GetTraces(
        HttpContext context,
        ITraceStore store,
        CancellationToken ct)
    {
        var query = ParseQuery(context.Request.Query);
        var traces = await store.QueryAsync(query, ct);

        var result = traces.Select(t => new
        {
            id = t.Id,
            name = t.Name,
            startedAt = t.StartedAt,
            completedAt = t.CompletedAt,
            durationMs = (int)t.Duration.TotalMilliseconds,
            status = t.RootSpans.Any(s => s.Status == SpanStatus.Error) ? "Error" : "Ok",
            tags = t.Tags
        });

        return Results.Ok(result);
    }

    /// <summary>Returns the full trace by ID including its span tree. Returns 404 if not found.</summary>
    private static async Task<IResult> GetTrace(
        string id,
        ITraceStore store,
        CancellationToken ct)
    {
        var trace = await store.FindAsync(id, ct);
        return trace is null ? Results.NotFound() : Results.Ok(trace);
    }

    /// <summary>Deletes a trace by ID. Returns 404 if not found, 204 on success.</summary>
    private static async Task<IResult> DeleteTrace(
        string id,
        ITraceStore store,
        CancellationToken ct)
    {
        var trace = await store.FindAsync(id, ct);
        if (trace is null) return Results.NotFound();

        await store.DeleteAsync(id, ct);
        return Results.NoContent();
    }

    /// <summary>Parses trace filter query parameters from the request query string.</summary>
    internal static TraceQuery ParseQuery(IQueryCollection query)
    {
        var tagFilters = query
            .Where(kv => kv.Key.StartsWith("tag_", StringComparison.OrdinalIgnoreCase))
            .ToDictionary(
                kv => kv.Key[4..],
                kv => kv.Value.ToString());

        return new TraceQuery
        {
            NameContains = query.TryGetValue("name", out var name) ? name.ToString() : null,
            From = query.TryGetValue("from", out var from)
                   && DateTimeOffset.TryParse(from, out var fromDt) ? fromDt : null,
            To = query.TryGetValue("to", out var to)
                 && DateTimeOffset.TryParse(to, out var toDt) ? toDt : null,
            Limit = query.TryGetValue("limit", out var limit)
                    && int.TryParse(limit, out var limitInt) ? limitInt : 100,
            TagFilters = tagFilters.Count > 0 ? tagFilters : null
        };
    }
}
