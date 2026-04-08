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
using Npgsql;

namespace AiObs.Postgres;

/// <summary>PostgreSQL-backed <see cref="ITraceStore"/> implementation via Npgsql.</summary>
public sealed class PostgresTraceStore : ITraceStore
{
    private readonly PostgresTraceStoreOptions _options;
    private static readonly JsonSerializerOptions SerializerOptions = new();

    /// <summary>Initialises the store and exposes a <see cref="SchemaInitializer"/> for startup use.</summary>
    public PostgresTraceStore(PostgresTraceStoreOptions options)
    {
        _options = options;
        SchemaInitializer = new SchemaInitializer(options);
    }

    /// <summary>Creates and ensures the database schema on startup.</summary>
    public SchemaInitializer SchemaInitializer { get; }

    /// <inheritdoc />
    public ITraceBuilder StartTrace(string name) => new TraceBuilder(name, this);

    /// <inheritdoc />
    public async Task SaveAsync(Trace trace, CancellationToken cancellationToken = default)
    {
        await using var conn = new NpgsqlConnection(_options.ConnectionString);
        await conn.OpenAsync(cancellationToken);

        var sql = $"""
            INSERT INTO {PostgresTraceStoreOptions.SchemaName}.{PostgresTraceStoreOptions.TableName}
                (id, name, started_at, completed_at, duration_ms, tags, root_spans)
            VALUES (@id, @name, @startedAt, @completedAt, @durationMs, @tags::jsonb, @rootSpans::jsonb)
            ON CONFLICT (id) DO NOTHING
            """;

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("id", trace.Id);
        cmd.Parameters.AddWithValue("name", trace.Name);
        cmd.Parameters.AddWithValue("startedAt", trace.StartedAt);
        cmd.Parameters.AddWithValue("completedAt", trace.CompletedAt);
        cmd.Parameters.AddWithValue("durationMs", (int)trace.Duration.TotalMilliseconds);
        cmd.Parameters.AddWithValue("tags", JsonSerializer.Serialize(trace.Tags, SerializerOptions));
        cmd.Parameters.AddWithValue("rootSpans", JsonSerializer.Serialize(trace.RootSpans, SerializerOptions));

        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Trace?> FindAsync(string traceId, CancellationToken cancellationToken = default)
    {
        await using var conn = new NpgsqlConnection(_options.ConnectionString);
        await conn.OpenAsync(cancellationToken);

        var sql = $"""
            SELECT id, name, started_at, completed_at, tags, root_spans
            FROM {PostgresTraceStoreOptions.SchemaName}.{PostgresTraceStoreOptions.TableName}
            WHERE id = @id
            """;

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("id", traceId);

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken)) return null;

        return ReadTrace(reader);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(string traceId, CancellationToken cancellationToken = default)
    {
        await using var conn = new NpgsqlConnection(_options.ConnectionString);
        await conn.OpenAsync(cancellationToken);

        var sql = $"DELETE FROM {PostgresTraceStoreOptions.SchemaName}.{PostgresTraceStoreOptions.TableName} WHERE id = @id";
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("id", traceId);
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Trace>> QueryAsync(TraceQuery query, CancellationToken cancellationToken = default)
    {
        await using var conn = new NpgsqlConnection(_options.ConnectionString);
        await conn.OpenAsync(cancellationToken);

        var conditions = new List<string>();
        var parameters = new Dictionary<string, object>();

        if (query.NameContains is not null)
        {
            conditions.Add("name ILIKE @name");
            parameters["name"] = $"%{query.NameContains}%";
        }
        if (query.From is not null)
        {
            conditions.Add("started_at >= @from");
            parameters["from"] = query.From.Value;
        }
        if (query.To is not null)
        {
            conditions.Add("started_at <= @to");
            parameters["to"] = query.To.Value;
        }
        if (query.TagFilters is not null)
        {
            conditions.Add("tags @> @tags::jsonb");
            parameters["tags"] = JsonSerializer.Serialize(query.TagFilters, SerializerOptions);
        }
        parameters["limit"] = query.Limit;

        var whereClause = conditions.Count > 0
            ? "WHERE " + string.Join(" AND ", conditions)
            : string.Empty;

        var sql = $"""
            SELECT id, name, started_at, completed_at, tags, root_spans
            FROM {PostgresTraceStoreOptions.SchemaName}.{PostgresTraceStoreOptions.TableName}
            {whereClause}
            ORDER BY started_at DESC
            LIMIT @limit
            """;

        await using var cmd = new NpgsqlCommand(sql, conn);
        foreach (var (key, value) in parameters)
            cmd.Parameters.AddWithValue(key, value);

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        var traces = new List<Trace>();
        while (await reader.ReadAsync(cancellationToken))
            traces.Add(ReadTrace(reader));

        return traces;
    }

    private static Trace ReadTrace(NpgsqlDataReader reader) => new()
    {
        Id = reader.GetString(reader.GetOrdinal("id")),
        Name = reader.GetString(reader.GetOrdinal("name")),
        StartedAt = reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("started_at")),
        CompletedAt = reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("completed_at")),
        Tags = JsonSerializer.Deserialize<Dictionary<string, string>>(reader.GetString(reader.GetOrdinal("tags")), SerializerOptions)
               ?? new Dictionary<string, string>(),
        RootSpans = JsonSerializer.Deserialize<List<TraceSpan>>(reader.GetString(reader.GetOrdinal("root_spans")), SerializerOptions)
                    ?? []
    };
}
