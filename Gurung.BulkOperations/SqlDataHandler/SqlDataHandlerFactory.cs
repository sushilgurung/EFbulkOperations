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
    //public class SqlDataHandlerFactory
    //{
    //    public static ISqlDataHandler Create(DbContext context)
    //    {
    //        try
    //        {
    //            DatabaseType databaseType = GetDatabaseType(context);

    //            string assemblyName = "Gurung.BulkOperations.SqlServer";
    //            //string assemblyName = databaseType switch
    //            //{
    //            //    DatabaseType.SqlServer => "Gurung.BulkOperations.SqlServer",
    //            //    DatabaseType.PostgreSql => "Gurung.BulkOperations.PostgreSql",
    //            //    _ => throw new NotSupportedException("Unsupported database type")
    //            //};
    //            string namespaceName = "Gurung.BulkOperations.SqlDataHandler";
    //            string typeName = databaseType switch
    //            {
    //                //"{namespace}.{class name}, "{assembly name}"
    //                DatabaseType.SqlServer => namespaceName + ".SqlServer.SqlServerBulkTransactionHandler, Gurung.BulkOperations.SqlServer",
    //                DatabaseType.PostgreSql => "Gurung.BulkOperations.PostgreSql,Gurung.BulkOperations.PostgreSql",
    //                _ => throw new NotSupportedException("Unsupported database type")
    //            };
    //            // Assembly assembly = Assembly.Load(assemblyName);
    //            //  Assembly assembly = Assembly.Load(assemblyName);

    //            //dynamic instance = Activator.CreateInstance(Type.GetType("Gurung.BulkOperations.SqlDataHandler.SqlServerBulkTransactionHandler, Gurung.BulkOperations.SqlDataHandler"), nonPublic: true);
    //            //instance.InternalMethod();


    //            Type? handlerType = Type.GetType(typeName);
    //            //Type? type = assembly.GetType(typeName);
    //            //if (type == null)
    //            //{
    //            //    throw new InvalidOperationException($"Could not find type: {typeName}");
    //            //}
    //            var dbServerInstance = Activator.CreateInstance(handlerType ?? typeof(int));
    //            ISqlDataHandler sqlDataHandler = dbServerInstance as ISqlDataHandler;
    //            return (ISqlDataHandler)Activator.CreateInstance(handlerType)!;
    //        }
    //        catch (Exception ex)
    //        {

    //            throw;
    //        }
    //    }


    //    private static DatabaseType GetDatabaseTypeFromOptions(DbContext context)
    //    {
    //        var extensions = context.Database.GetService<IDbContextOptions>().Extensions;
    //        foreach (var extension in extensions)
    //        {
    //            if (extension.GetType().Namespace.Contains("SqlServer"))
    //            {
    //                return DatabaseType.SqlServer;
    //            }
    //            if (extension.GetType().Namespace.Contains("Npgsql"))
    //            {
    //                return DatabaseType.PostgreSql;
    //            }
    //        }
    //        throw new NotSupportedException("Unknown database provider");
    //    }

    //    private static DatabaseType GetDatabaseType(DbContext context)
    //    {
    //        string providerName = context.Database.ProviderName;

    //        return providerName switch
    //        {
    //            "Microsoft.EntityFrameworkCore.SqlServer" => DatabaseType.SqlServer,
    //            "Npgsql.EntityFrameworkCore.PostgreSQL" => DatabaseType.PostgreSql,
    //            _ => throw new NotSupportedException($"Database provider '{providerName}' is not supported")
    //        };
    //    }
    //}
}
