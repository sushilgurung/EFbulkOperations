using Gurung.BulkOperations.SqlDataHandler;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Gurung.BulkOperations
{
    /// <summary>
    /// Provides factory methods for creating SQL data handler instances based on the database provider used by a given
    /// Entity Framework Core context.
    /// </summary>
    /// <remarks>This class supports dynamic creation of data handlers for different database providers, such
    /// as SQL Server and PostgreSQL, by inspecting the specified DbContext. It is intended for use in scenarios where
    /// bulk operations or provider-specific data handling are required. The factory methods throw exceptions if the
    /// database provider is not supported.</remarks>
    public class SqlDataHandlerFactory
    {
        /// <summary>
        /// Creates an instance of an SQL data handler that is compatible with the database provider used by the
        /// specified Entity Framework context.
        /// </summary>
        /// <remarks>Currently supports SQL Server and PostgreSQL database providers. The method uses
        /// reflection to load the appropriate handler implementation based on the context's provider. Ensure that the
        /// required assemblies are available at runtime.</remarks>
        /// <param name="context">The Entity Framework database context for which to create a data handler. The context's provider determines
        /// the type of data handler returned. Cannot be null.</param>
        /// <returns>An implementation of ISqlDataHandler that supports the database provider associated with the specified
        /// context.</returns>
        /// <exception cref="Exception">Thrown if the database provider is not supported, if the required handler type cannot be found or
        /// instantiated, or if an error occurs during handler creation.</exception>
        public static ISqlDataHandler CreateDataHandler(DbContext context)
        {
            try
            {
                DatabaseType databaseType = GetDatabaseType(context);

                string assemblyName = databaseType switch
                {
                    DatabaseType.SqlServer => "Gurung.BulkOperations.SqlServer",
                    DatabaseType.PostgreSql => "Gurung.BulkOperations.PostgreSql",
                    _ => throw new NotSupportedException("Unsupported database type")
                };

                string typeName = databaseType switch
                {
                    DatabaseType.SqlServer => "Gurung.BulkOperations.SqlDataHandler.SqlServer.SqlServerDataHandler",
                    DatabaseType.PostgreSql => "Gurung.BulkOperations.PostgreDataHandler.PostgreSql.PostgreSqlDataHandler",
                    _ => throw new NotSupportedException("Unsupported provider")
                };

                // Load the assembly first, then get the type
                Assembly assembly = Assembly.Load(assemblyName);
                Type? handlerType = assembly.GetType(typeName);

                if (handlerType is null)
                {
                    throw new InvalidOperationException($"Could not find type: {typeName} in assembly {assemblyName}");
                }

                var dbServerInstance = Activator.CreateInstance(handlerType);
                ISqlDataHandler sqlDataHandler = (ISqlDataHandler)(dbServerInstance ?? throw new InvalidOperationException("Failed to create handler instance"));
                return sqlDataHandler;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }


        private static DatabaseType GetDatabaseTypeFromOptions(DbContext context)
        {
            var extensions = context.Database.GetService<IDbContextOptions>().Extensions;
            foreach (var extension in extensions)
            {
                if (extension.GetType().Namespace.Contains("SqlServer"))
                {
                    return DatabaseType.SqlServer;
                }
                if (extension.GetType().Namespace.Contains("Npgsql"))
                {
                    return DatabaseType.PostgreSql;
                }
            }
            throw new NotSupportedException("Unknown database provider");
        }

        /// <summary>
        /// Determines the type of database provider used by the specified Entity Framework Core context.
        /// </summary>
        /// <remarks>Currently supports SQL Server and PostgreSQL providers. Additional providers may
        /// require updates to this method.</remarks>
        /// <param name="context">The Entity Framework Core database context for which to determine the database provider type. Cannot be null.</param>
        /// <returns>A value of the DatabaseType enumeration that represents the type of database provider used by the context.</returns>
        /// <exception cref="NotSupportedException">Thrown if the database provider used by the context is not supported.</exception>
        public static DatabaseType GetDatabaseType(DbContext context)
        {
            string providerName = context.Database.ProviderName;

            return providerName switch
            {
                "Microsoft.EntityFrameworkCore.SqlServer" => DatabaseType.SqlServer,
                "Npgsql.EntityFrameworkCore.PostgreSQL" => DatabaseType.PostgreSql,
                _ => throw new NotSupportedException($"Database provider '{providerName}' is not supported")
            };
        }
    }
}
