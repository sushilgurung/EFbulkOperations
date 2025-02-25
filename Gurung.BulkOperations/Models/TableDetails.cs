using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Gurung.BulkOperations
{
    public class TableDetails
    {
        #region Properties
        public string Schema { get; set; }
        public string SchemaFormated => Schema != null ? $"[{Schema}]." : "";
        public string TableName { get; set; }
        public string FullTableName => $"{SchemaFormated}[{TableName}]";
        public IEnumerable<string> PrimaryKeys { get; set; }

        public Type Type { get; set; }

        public IEntityType EntityType { get; set; }

        public PropertyInfo[] PropertyInfo { get; set; }

        public string TempTableName { get; set; }

        #endregion
        public static TableDetails GenerateInstance<T>(DbContext context, IEnumerable<T> entities)
        {
            TableDetails tableInfo = new();
            Type type = GetEnumerableType(entities);
            var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
            var entityType = type is null ? null : context.Model.FindEntityType(type);
            if (entityType == null)
            {
                type = entities.FirstOrDefault()?.GetType() ?? throw new ArgumentNullException(nameof(type));
                entityType = context.Model.FindEntityType(type);
            }
            tableInfo.Schema = context.Model.GetDefaultSchema() ?? "dbo";
            tableInfo.TableName = entityType.GetTableName();
            tableInfo.PrimaryKeys = FindPrimaryKey(context, entities, tableInfo.TableName);
            tableInfo.Type = type;
            tableInfo.EntityType = entityType;
            tableInfo.PropertyInfo = properties;
            tableInfo.TempTableName = $"#temp_{tableInfo.TableName}";
            return tableInfo;
        }

        public static Type GetEnumerableType<T>(IEnumerable<T> items)
        {
            return typeof(T);
        }

        public static List<string> FindPrimaryKey<T>(DbContext context, IEnumerable<T> entities, string tableName)
        {
            var entityType = context.Model.GetEntityTypes().FirstOrDefault(e => e.GetTableName() == tableName);
            List<string> primaryEntity = entityType.FindPrimaryKey().Properties.Select(p => p.Name).ToList();
            return primaryEntity;
        }
    }
}
