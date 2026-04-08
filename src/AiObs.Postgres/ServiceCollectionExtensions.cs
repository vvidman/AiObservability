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
using Microsoft.Extensions.DependencyInjection;

namespace AiObs.Postgres;

/// <summary>
/// Extension methods for registering AiObs.Postgres services.
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPostgresTraceStore(this IServiceCollection services, string connectionString, bool initializeSchema = true)
    {
        var options = new PostgresTraceStoreOptions { ConnectionString = connectionString };

        var store = new PostgresTraceStore(options);
        services.AddSingleton<ITraceStore>(store);

        if(initializeSchema)
        {
            services.AddHostedService(_ => new SchemaInitializerHostedService(store.SchemaInitializer));
        }

        return services;
    }
}
