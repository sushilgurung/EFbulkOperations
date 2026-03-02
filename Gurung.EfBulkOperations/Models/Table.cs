using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Gurung.EfBulkOperations.Models
{
    /// <summary>
    /// Provides utility methods for converting entity collections to DataTable instances using Entity Framework Core
    /// metadata.
    /// </summary>
    /// <remarks>The TableService class is designed to facilitate interoperability between Entity Framework
    /// Core entities and ADO.NET DataTable objects. It ensures that column names and types in the resulting DataTable
    /// reflect EF Core configurations, including [Column] attributes and custom mappings. This is useful for scenarios
    /// where tabular data representation is required, such as exporting data or integrating with APIs that consume
    /// DataTable objects.</remarks>
    public class TableService
    {
        /// <summary>
        /// Converts entities to DataTable using database column names from EF Core metadata.
        /// Respects [Column] attributes and other EF Core column name configurations.
        /// </summary>
        public static DataTable ConvertToDataTable<T>(IEnumerable<T> data, TableDetails tableInfo)
        {
            DataTable dataTable = new DataTable();
            var properties = tableInfo.PropertyInfo;

            // Use column names from EF Core metadata (respects [Column] attributes)
            foreach (var prop in properties)
            {
                var columnName = tableInfo.ColumnMappings[prop.Name];
                dataTable.Columns.Add(columnName, Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType);
            }

            foreach (var item in data)
            {
                var row = dataTable.NewRow();
                foreach (var prop in properties)
                {
                    var columnName = tableInfo.ColumnMappings[prop.Name];
                    row[columnName] = prop.GetValue(item) ?? DBNull.Value;
                }
                dataTable.Rows.Add(row);
            }
            return dataTable;
        }
    }
}

