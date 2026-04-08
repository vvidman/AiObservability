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

using System.Text;
using System.Text.Json;
using AiObs.Abstractions;

namespace AiObs.Api.Endpoints;

/// <summary>Minimal API endpoints for downloading traces as JSON files.</summary>
public static class ExportEndpoints
{
    private static readonly JsonSerializerOptions IndentedOptions = new() 
    { 
        WriteIndented = true, 
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() } 
    };

    /// <summary>Maps all export endpoints onto the application.</summary>
    public static void Map(WebApplication app)
    {
        app.MapGet("/traces/export", ExportTraces);
        app.MapGet("/traces/{id}/export", ExportTrace);
    }

    /// <summary>
    /// Downloads a single trace as an indented JSON file.
    /// Returns 404 if the trace is not found.
    /// </summary>
    private static async Task<IResult> ExportTrace(
        string id,
        ITraceStore store,
        CancellationToken ct)
    {
        var trace = await store.FindAsync(id, ct);
        if (trace is null) return Results.NotFound();

        var json = JsonSerializer.Serialize(trace, IndentedOptions);
        var bytes = Encoding.UTF8.GetBytes(json);
        return Results.File(bytes, "application/json", $"{id}.json");
    }

    /// <summary>
    /// Downloads all traces matching the provided filters as an indented JSON file.
    /// Accepts the same query parameters as GET /traces.
    /// </summary>
    private static async Task<IResult> ExportTraces(
        HttpContext context,
        ITraceStore store,
        CancellationToken ct)
    {
        var query = TraceEndpoints.ParseQuery(context.Request.Query);
        var traces = await store.QueryAsync(query, ct);

        var json = JsonSerializer.Serialize(traces, IndentedOptions);
        var bytes = Encoding.UTF8.GetBytes(json);
        return Results.File(bytes, "application/json", "traces-export.json");
    }
}
