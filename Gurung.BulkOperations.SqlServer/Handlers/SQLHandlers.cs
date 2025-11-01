using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gurung.BulkOperations.SqlServer.Handlers
{
    public class SQLHandlers
    {
        /// <summary>
        /// Set SqlBulkCopy
        /// </summary>
        /// <param name="sqlConnection"></param>
        /// <param name="sqlTransaction"></param>
        /// <param name="tableInfo"></param>
        /// <param name="bulkConfig"></param>
        /// <param name="isTempTable"></param>
        /// <returns></returns>
        public static SqlBulkCopy SetSqlBulkCopy(SqlConnection sqlConnection, SqlTransaction sqlTransaction, TableDetails tableInfo, BulkConfig bulkConfig, bool isTempTable = false)
        {
            SqlBulkCopyOptions sqlBulkCopyOptions = new SqlBulkCopyOptions();
            if (bulkConfig is not null)
            {
                sqlBulkCopyOptions = bulkConfig.KeepIdentity ? SqlBulkCopyOptions.KeepIdentity : SqlBulkCopyOptions.Default;
            }

            if (isTempTable)
            {
                sqlBulkCopyOptions = SqlBulkCopyOptions.KeepIdentity;
            }

            SqlBulkCopy bulkCopy = new SqlBulkCopy(sqlConnection, sqlBulkCopyOptions, sqlTransaction);
            bulkCopy.DestinationTableName = isTempTable ? tableInfo.TempTableName : tableInfo.FullTableName;

            if (bulkConfig is not null)
            {
                if (bulkConfig.BatchSize > 0)
                {
                    bulkCopy.BatchSize = bulkConfig.BatchSize;
                    bulkCopy.NotifyAfter = bulkConfig.NotifyAfter > 0 ? bulkConfig.NotifyAfter : bulkConfig.BatchSize;
                }
                bulkCopy.BulkCopyTimeout = bulkConfig.BulkCopyTimeout > 0 ? bulkConfig.BulkCopyTimeout : bulkCopy.BulkCopyTimeout;
            }

            var properties = tableInfo.PropertyInfo;
            if (properties is not null)
            {
                foreach (var prop in properties)
                {
                    bulkCopy.ColumnMappings.Add(prop.Name, prop.Name);
                }
            }
            return bulkCopy;
        }


        /// <summary>
        /// Create Temp Table
        /// </summary>
        /// <param name="sqlConnection"></param>
        /// <param name="sqlTransaction"></param>
        /// <param name="tableInfo"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static async Task<bool> CreateTempTableAsync(SqlConnection sqlConnection, SqlTransaction sqlTransaction, TableDetails tableInfo, CancellationToken cancellationToken = default)
        {
            try
            {
                string createTempTable = $"SELECT * INTO {tableInfo.TempTableName} FROM {tableInfo.FullTableName} WHERE 1 = 0";
                var command = new SqlCommand(SqlServerQueryBuilder.DropTableIfExistsQuery(tableInfo.TempTableName), sqlConnection, sqlTransaction);
                {
                    command.CommandText = createTempTable;
                    await command.ExecuteNonQueryAsync(cancellationToken);
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Execute Sql Command
        /// </summary>
        /// <param name="sqlConnection"></param>
        /// <param name="sqlTransaction"></param>
        /// <param name="sqlCommand"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static async Task<bool> SqlCommandAsync(SqlConnection sqlConnection, SqlTransaction sqlTransaction, string sqlCommand, CancellationToken cancellationToken = default)
        {
            try
            {
                var command = new SqlCommand(sqlCommand, sqlConnection, sqlTransaction);
                {
                    await command.ExecuteNonQueryAsync(cancellationToken);
                }
                return true;
            }
            catch
            {
                return false;
            }

        }

    }
}
