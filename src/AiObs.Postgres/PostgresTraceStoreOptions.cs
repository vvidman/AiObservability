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

namespace AiObs.Postgres;

/// <summary>Configuration options for <see cref="PostgresTraceStore"/>.</summary>
public sealed class PostgresTraceStoreOptions
{
    /// <summary>Npgsql connection string. Required.</summary>
    public required string ConnectionString { get; init; }

    /// <summary>PostgreSQL schema that contains the traces table. Defaults to <c>public</c>.</summary>
    public string SchemaName { get; init; } = "public";

    /// <summary>Name of the traces table. Defaults to <c>traces</c>.</summary>
    public string TableName { get; init; } = "traces";
}
