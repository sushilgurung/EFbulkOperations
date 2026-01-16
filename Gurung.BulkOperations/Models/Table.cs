using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Gurung.BulkOperations.Models
{
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

