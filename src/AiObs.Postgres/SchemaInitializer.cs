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

using Npgsql;

namespace AiObs.Postgres;

/// <summary>Creates the traces table and its indexes if they do not already exist.</summary>
public sealed class SchemaInitializer(PostgresTraceStoreOptions options)
{
    /// <summary>
    /// Ensures the traces table and its indexes exist. Safe to call on every startup
    /// (uses <c>CREATE TABLE IF NOT EXISTS</c> and <c>CREATE INDEX IF NOT EXISTS</c>).
    /// </summary>
    public async Task EnsureCreatedAsync(CancellationToken cancellationToken = default)
    {
        await using var conn = new NpgsqlConnection(options.ConnectionString);
        await conn.OpenAsync(cancellationToken);

        var sql = $$"""
            CREATE TABLE IF NOT EXISTS {{PostgresTraceStoreOptions.SchemaName}}.{{PostgresTraceStoreOptions.TableName}} (
                id            TEXT        PRIMARY KEY,
                name          TEXT        NOT NULL,
                started_at    TIMESTAMPTZ NOT NULL,
                completed_at  TIMESTAMPTZ NOT NULL,
                duration_ms   INTEGER     NOT NULL,
                tags          JSONB       NOT NULL DEFAULT '{}',
                root_spans    JSONB       NOT NULL DEFAULT '[]'
            );
            CREATE INDEX IF NOT EXISTS ix_{{PostgresTraceStoreOptions.TableName}}_name
                ON {{PostgresTraceStoreOptions.SchemaName}}.{{PostgresTraceStoreOptions.TableName}} (name);
            CREATE INDEX IF NOT EXISTS ix_{{PostgresTraceStoreOptions.TableName}}_started_at
                ON {{PostgresTraceStoreOptions.SchemaName}}.{{PostgresTraceStoreOptions.TableName}} (started_at DESC);
            CREATE INDEX IF NOT EXISTS ix_{{PostgresTraceStoreOptions.TableName}}_tags
                ON {{PostgresTraceStoreOptions.SchemaName}}.{{PostgresTraceStoreOptions.TableName}} USING GIN (tags);
            """;

        await using var cmd = new NpgsqlCommand(sql, conn);
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }
}
