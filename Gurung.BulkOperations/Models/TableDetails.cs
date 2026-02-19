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
    /// <summary>
    /// Represents metadata and mapping details for a database table, including schema, table name, primary keys,
    /// property-to-column mappings, and related entity information.
    /// </summary>
    /// <remarks>This class is typically used to facilitate dynamic table operations, such as bulk inserts or
    /// schema introspection, by providing access to both .NET type information and Entity Framework Core metadata. It
    /// supports scenarios where table structure and mappings need to be determined at runtime. Thread safety is not
    /// guaranteed; instances are intended for use within a single operation or context.</remarks>
    public class TableDetails
    {
        #region Properties
        public string Schema { get; set; }
        public string SchemaFormated => Schema != null ? $"[{Schema}]." : "";
        public string TableName { get; set; }
        public string FullTableName { get; set; }
        public IEnumerable<string> PrimaryKeys { get; set; }
        public IEnumerable<string> PrimaryKeyColumns { get; set; }

        public Type Type { get; set; }
        public IEntityType EntityType { get; set; }
        public PropertyInfo[] PropertyInfo { get; set; }
        public string TempTableName { get; set; }

        /// <summary>
        /// Maps C# property names to database column names.
        /// Key: Property name (e.g., "UserId")
        /// Value: Column name (e.g., "user_id")
        /// </summary>
        public Dictionary<string, string> ColumnMappings { get; set; }

        #endregion
        public static TableDetails GenerateInstance<T>(DbContext context, IEnumerable<T> entities)
        {
            TableDetails tableInfo = new();
            Type type = GetEnumerableType(entities);
            
            var entityType = type is null ? null : context.Model.FindEntityType(type);
            if (entityType == null)
            {
                type = entities.FirstOrDefault()?.GetType() ?? throw new ArgumentNullException(nameof(type));
                entityType = context.Model.FindEntityType(type);
            }

            // Filter properties: exclude collections and navigation properties, but include all scalar/primitive types
            var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(prop =>
                {
                    // Exclude ICollection<> and IEnumerable<> (navigation collections)
                    if (prop.PropertyType.IsGenericType)
                    {
                        var genericType = prop.PropertyType.GetGenericTypeDefinition();
                        if (genericType == typeof(ICollection<>) || 
                            genericType == typeof(IEnumerable<>) ||
                            genericType == typeof(IList<>) ||
                            genericType == typeof(List<>))
                            return false;
                    }

                    // Check if EF Core recognizes this as a mapped property (column)
                    // Navigation properties (single references) won't be mapped as columns
                    var efProperty = entityType.FindProperty(prop.Name);
                    
                    // Include only if EF Core has it as a column mapping
                    // This will include: bool, int, string, DateTime, etc.
                    // This will exclude: navigation properties (QuestionType, QuestionCategory, etc.)
                    return efProperty != null;
                })
                .ToArray();

            tableInfo.TableName = entityType.GetTableName();
            tableInfo.Type = type;
            tableInfo.EntityType = entityType;
            tableInfo.PropertyInfo = properties;
            
            // Build column mappings from EF Core metadata (respects [Column] attributes)
            tableInfo.ColumnMappings = BuildColumnMappings(entityType, properties);
            
            // Get primary keys (both property names and column names)
            var pkInfo = GetPrimaryKeyInfo(entityType);
            tableInfo.PrimaryKeys = pkInfo.PropertyNames;
            tableInfo.PrimaryKeyColumns = pkInfo.ColumnNames;
           
            if (SqlDataHandlerFactory.GetDatabaseType(context) == DatabaseType.PostgreSql)
            {
                tableInfo.Schema = entityType.GetSchema() ?? "public";
                tableInfo.FullTableName = $"{tableInfo.Schema}.\"{tableInfo.TableName}\"";
                tableInfo.TempTableName = $"temp_{tableInfo.TableName}";
            }
            else
            {
                tableInfo.Schema = context.Model.GetDefaultSchema() ?? "dbo";
                tableInfo.FullTableName = $"{tableInfo.SchemaFormated}[{tableInfo.TableName}]";
                tableInfo.TempTableName = $"#temp_{tableInfo.TableName}";
            }
            return tableInfo;
        }

        public static Type GetEnumerableType<T>(IEnumerable<T> items)
        {
            return typeof(T);
        }

        /// <summary>
        /// Builds a mapping of C# property names to database column names using EF Core metadata.
        /// This respects [Column] attributes and other EF Core configurations.
        /// </summary>
        private static Dictionary<string, string> BuildColumnMappings(IEntityType entityType, PropertyInfo[] properties)
        {
            var mappings = new Dictionary<string, string>();
            
            foreach (var prop in properties)
            {
                var efProperty = entityType.FindProperty(prop.Name);
                if (efProperty != null)
                {
                    // Get the actual column name from EF Core (respects [Column] attribute)
                    var columnName = efProperty.GetColumnName();
                    mappings[prop.Name] = columnName;
                }
                else
                {
                    // Fallback to property name if not found in EF Core metadata
                    mappings[prop.Name] = prop.Name;
                }
            }
            
            return mappings;
        }

        /// <summary>
        /// Gets primary key information including both property names and column names.
        /// </summary>
        private static (List<string> PropertyNames, List<string> ColumnNames) GetPrimaryKeyInfo(IEntityType entityType)
        {
            var primaryKey = entityType.FindPrimaryKey();
            if (primaryKey == null)
            {
                return (new List<string>(), new List<string>());
            }

            var propertyNames = new List<string>();
            var columnNames = new List<string>();

            foreach (var property in primaryKey.Properties)
            {
                propertyNames.Add(property.Name);
                columnNames.Add(property.GetColumnName());
            }

            return (propertyNames, columnNames);
        }
    }
}
