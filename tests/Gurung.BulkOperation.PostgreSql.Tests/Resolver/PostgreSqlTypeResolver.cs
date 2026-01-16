using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Gurung.BulkOperation.PostgreSql.Tests.Resolver
{
    internal class PostgreSqlTypeResolver
    {
        public static string GetPostgresColumnType<TContext>(PropertyInfo property)
       where TContext : DbContext, new()
        {
            using var context = new TContext();

            // EF Core's built-in relational type mapping service
            var typeMappingSource = context.GetService<IRelationalTypeMappingSource>();

            // Find the mapping for this property type
            var mapping = typeMappingSource.FindMapping(property.PropertyType);

            // This contains the actual PostgreSQL type name
            return mapping.StoreType;
        }
    }
}
