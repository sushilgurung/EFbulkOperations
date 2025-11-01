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
    public class SqlDataHandlerFactory
    {
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
                    DatabaseType.PostgreSql => "Gurung.BulkOperations.SqlDataHandler.PostgreSql.PostgreSqlDataHandler",
                    _ => throw new NotSupportedException("Unsupported provider")
                };

                Type handlerType = Type.GetType($"{typeName},{assemblyName}");
                if (handlerType is null)
                {
                    throw new InvalidOperationException($"Could not find type: {assemblyName}");
                }
                
                var dbServerInstance = Activator.CreateInstance(handlerType ?? typeof(int));
                ISqlDataHandler sqlDataHandler = dbServerInstance as ISqlDataHandler;
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

        private static DatabaseType GetDatabaseType(DbContext context)
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
