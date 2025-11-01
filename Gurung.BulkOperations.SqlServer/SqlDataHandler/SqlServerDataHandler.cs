using Gurung.BulkOperations.SqlServer.Handlers;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Gurung.BulkOperations.Models;
using Gurung.BulkOperations.SqlServer;

namespace Gurung.BulkOperations.SqlDataHandler.SqlServer
{
    public class SqlServerDataHandler : ISqlDataHandler
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        /// <param name="entities"></param>
        /// <param name="bulkConfig"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task BulkInsertAsync<T>(
            DbContext context,
            IEnumerable<T> entities,
            BulkConfig bulkConfig = null,
            CancellationToken cancellationToken = default)
        {
            await context.Database.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
            var connection = context.Database.GetDbConnection();
            using var transaction = await connection.BeginTransactionAsync();

            try
            {
                TableDetails tableInfo = TableDetails.GenerateInstance(context, entities);
                SqlBulkCopy bulkCopy = SQLHandlers.SetSqlBulkCopy((SqlConnection)connection, (SqlTransaction)transaction, tableInfo, bulkConfig);
                DataTable dataTable = TableService.ConvertToDataTable(entities, tableInfo);
                await bulkCopy.WriteToServerAsync(dataTable);
                transaction.Commit();
            }
            catch (Exception ex)
            {
                throw new ApplicationException("Invalid operation during bulk insert.", ex);
            }
            finally
            {
                await context.Database.CloseConnectionAsync().ConfigureAwait(false);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        /// <param name="entities"></param>
        /// <param name="bulkConfig"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task BulkUpDateAsync<T>(
            DbContext context,
            IEnumerable<T> entities,
            BulkConfig bulkConfig = null,
            CancellationToken cancellationToken = default)
        {
            await context.Database.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
            var connection = context.Database.GetDbConnection();

            using var transaction = await connection.BeginTransactionAsync();
            TableDetails tableInfo = TableDetails.GenerateInstance(context, entities);
            try
            {
                #region Create Temp Table
                bool hasTempTableCreated = await SQLHandlers.CreateTempTableAsync((SqlConnection)connection, (SqlTransaction)transaction, tableInfo, cancellationToken);
                #endregion

                // Add Data on Temp Table
                using SqlBulkCopy bulkCopy = SQLHandlers.SetSqlBulkCopy((SqlConnection)connection, (SqlTransaction)transaction, tableInfo, bulkConfig, true);
                bulkCopy.DestinationTableName = tableInfo.TempTableName;
                DataTable dataTable = TableService.ConvertToDataTable(entities, tableInfo);
                await bulkCopy.WriteToServerAsync(dataTable, cancellationToken).ConfigureAwait(false);

                string mergeQuery = SqlServerQueryBuilder.GenerateUpdateMergeQuery(tableInfo.FullTableName, tableInfo.TempTableName, dataTable, tableInfo);
                bool hasMergeQueryExecuted = await SQLHandlers.SqlCommandAsync((SqlConnection)connection, (SqlTransaction)transaction, mergeQuery);
                transaction.Commit();
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                throw new ApplicationException("Invalid operation during bulk update.", ex);
            }
            finally
            {
                var dropTempTableQuery = SqlServerQueryBuilder.DropTableIfExistsQuery(tableInfo.TempTableName);
                await context.Database.ExecuteSqlRawAsync(dropTempTableQuery, cancellationToken).ConfigureAwait(false);
                await context.Database.CloseConnectionAsync().ConfigureAwait(false);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        /// <param name="entities"></param>
        /// <param name="bulkConfig"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task BulkInsertOrUpDateAsync<T>(
           DbContext context,
           IEnumerable<T> entities,
           BulkConfig bulkConfig = null,
           CancellationToken cancellationToken = default)
        {
            await context.Database.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
            var connection = context.Database.GetDbConnection();

            using var transaction = await connection.BeginTransactionAsync();
            TableDetails tableInfo = TableDetails.GenerateInstance(context, entities);
            try
            {
                #region Create Temp Table
                bool hasTempTableCreated = await SQLHandlers.CreateTempTableAsync((SqlConnection)connection, (SqlTransaction)transaction, tableInfo, cancellationToken);
                #endregion

                using SqlBulkCopy bulkCopy = SQLHandlers.SetSqlBulkCopy((SqlConnection)connection, (SqlTransaction)transaction, tableInfo, bulkConfig, true);
                bulkCopy.DestinationTableName = tableInfo.TempTableName;
                DataTable dataTable = TableService.ConvertToDataTable(entities, tableInfo);
                await bulkCopy.WriteToServerAsync(dataTable, cancellationToken).ConfigureAwait(false);
                // DataTable dt = await GetTempTableData((SqlConnection)connection, (SqlTransaction)transaction, tableInfo.TempTableName).ConfigureAwait(false);
                string mergeQuery = SqlServerQueryBuilder.GenerateInsertOrUpdateMergeQuery(tableInfo.FullTableName, tableInfo.TempTableName, dataTable, tableInfo);
                bool hasMergeQueryExecuted = await SQLHandlers.SqlCommandAsync((SqlConnection)connection, (SqlTransaction)transaction, mergeQuery);
                transaction.Commit();
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                throw new ApplicationException("Invalid operation during bulk Merge.", ex);
            }
            finally
            {
                var dropTempTableQuery = SqlServerQueryBuilder.DropTableIfExistsQuery(tableInfo.TempTableName);
                await context.Database.ExecuteSqlRawAsync(dropTempTableQuery, cancellationToken).ConfigureAwait(false);
                await context.Database.CloseConnectionAsync().ConfigureAwait(false);
            }
        }

    }
}
