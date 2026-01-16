using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Gurung.BulkOperations.SqlServer
{
    public class SqlServerQueryBuilder
    {
        /// <summary>
        /// This method is used to set identity on off for identity insert
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="enable"></param>
        /// <returns></returns>
        public static string SetIdentityInsertQuery(string tableName, bool enable)
        {
            string value = enable ? "ON" : "OFF";
            return $"SET IDENTITY_INSERT {tableName} {value};";
        }
        /// <summary>
        /// This method Get the temporary table name
        /// </summary>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public static string GetTempTableName(string tableName)
        {
            return $"#temp_{tableName}";
        }

        /// <summary>
        /// This method is used to drop table if exists
        /// </summary>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public static string DropTableIfExistsQuery(string tableName)
        {
            return $@"IF OBJECT_ID('tempdb..{tableName}') IS NOT NULL DROP TABLE {tableName};";
        }
        /// <summary>
        /// This method is used to generate update merge query
        /// </summary>
        /// <param name="targetTable"></param>
        /// <param name="sourceTable"></param>
        /// <param name="dataTable"></param>
        /// <param name="tableInfo"></param>
        /// <returns></returns>
        public static string GenerateUpdateMergeQuery(string targetTable, string sourceTable, DataTable dataTable, TableDetails tableInfo)
        {
            int index = 0;
            StringBuilder sb = new StringBuilder();
            // Use primary key column names (not property names)
            foreach (var pkColumn in tableInfo.PrimaryKeyColumns)
            {
                if (index == 0)
                {
                    sb.Append($"target.[{pkColumn}] = source.[{pkColumn}]");
                }
                else
                {
                    sb.Append($" AND target.[{pkColumn}] = source.[{pkColumn}]");
                }
                index++;
            }
            // DataTable already has column names from ColumnMappings
            List<string> columns = dataTable.Columns.Cast<DataColumn>()
                         .Where(c => !tableInfo.PrimaryKeyColumns.Contains(c.ColumnName))
                         .Select(c => $"target.[{c.ColumnName}] = source.[{c.ColumnName}]")
            .ToList();

            var mergeQueryString = $@"
                                MERGE {tableInfo.FullTableName} AS target
                                USING {tableInfo.TempTableName} AS source
                                ON {sb.ToString()}
                                WHEN MATCHED THEN
                                    UPDATE SET {string.Join(", ", columns)};
                               ";

            return mergeQueryString;
        }

        /// <summary>
        /// This method is used to generate insert or update merge query
        /// </summary>
        /// <param name="targetTable"></param>
        /// <param name="sourceTable"></param>
        /// <param name="dataTable"></param>
        /// <param name="tableInfo"></param>
        /// <returns></returns>
        public static string GenerateInsertOrUpdateMergeQuery(string targetTable, string sourceTable, DataTable dataTable, TableDetails tableInfo)
        {
            int index = 0;
            StringBuilder sb = new StringBuilder();
            // Use primary key column names (not property names)
            foreach (var pkColumn in tableInfo.PrimaryKeyColumns)
            {
                if (index == 0)
                {
                    sb.Append($"target.[{pkColumn}] = source.[{pkColumn}]");
                }
                else
                {
                    sb.Append($" AND target.[{pkColumn}] = source.[{pkColumn}]");
                }
                index++;
            }
            // DataTable already has column names from ColumnMappings
            List<string> columns = dataTable.Columns.Cast<DataColumn>()
                         .Where(c => !tableInfo.PrimaryKeyColumns.Contains(c.ColumnName))
                         .Select(c => $"target.[{c.ColumnName}] = source.[{c.ColumnName}]")
            .ToList();

            var insertColumns = string.Join(", ", dataTable.Columns.Cast<DataColumn>().Where(c => !tableInfo.PrimaryKeyColumns.Contains(c.ColumnName)).Select(c => $"[{c.ColumnName}]"));
            var insertValues = string.Join(", ", dataTable.Columns.Cast<DataColumn>().Where(c => !tableInfo.PrimaryKeyColumns.Contains(c.ColumnName)).Select(c => $"source.[{c.ColumnName}]"));

            var mergeQueryString = $@"
                                MERGE {tableInfo.FullTableName} AS target
                                USING {tableInfo.TempTableName} AS source
                                ON {sb.ToString()}
                                WHEN MATCHED THEN
                                    UPDATE SET {string.Join(", ", columns)}
                                WHEN NOT MATCHED BY TARGET THEN
                                INSERT ({insertColumns})
                                VALUES ({insertValues});
                               ";

            return mergeQueryString;
        }
        /// <summary>
        /// This method is used to generate temperory table query
        /// </summary>
        /// <param name="tableDetails"></param>
        /// <returns></returns>
        public static string GenerateTemperoryTableQuery(TableDetails tableDetails)
        {
            string createTempTable = $"SELECT * INTO {tableDetails.TempTableName} FROM {tableDetails.FullTableName} WHERE 1 = 0";
            return createTempTable;
        }

    }
}
