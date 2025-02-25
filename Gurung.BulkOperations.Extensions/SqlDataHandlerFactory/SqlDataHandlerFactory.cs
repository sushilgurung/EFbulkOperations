using Gurung.BulkOperations.SqlDataHandler;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Gurung.BulkOperations.SqlDataHandler.SqlServer;
using Gurung.BulkOperations.PostgreSql;

namespace Gurung.BulkOperations.Extensions
{
    public class SqlDataHandlerFactory
    {
        public static ISqlDataHandler CreateDataHandler(DbContext context)
        {
            try
            {
                DatabaseType databaseType = GetDatabaseType(context);

                ISqlDataHandler dataHandler = databaseType switch
                {
                    DatabaseType.SqlServer => new SqlServerBulkTransactionHandler(),
                    DatabaseType.PostgreSql => new PostgreSqlDataHandler(),
                    _ => throw new NotSupportedException("Unsupported database type")
                };
                return dataHandler;
            }
            catch (Exception ex)
            {

                throw;
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
