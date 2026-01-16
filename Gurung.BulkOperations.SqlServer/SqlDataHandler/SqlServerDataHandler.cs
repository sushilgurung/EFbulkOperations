using Gurung.BulkOperations.SqlServer.Handlers;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;
using Gurung.BulkOperations.Models;
using Gurung.BulkOperations.SqlServer;

namespace Gurung.BulkOperations.SqlDataHandler.SqlServer
{
    public class SqlServerDataHandler : ISqlDataHandler
    {
        /// <summary>
        /// Asynchronously inserts a collection of entities into the database in bulk using the specified DbContext.
        /// </summary>
        /// <remarks>This method uses a database transaction to ensure that all entities are inserted as a single atomic
        /// operation. The connection is opened and closed automatically. For large collections, bulk insert can significantly
        /// improve performance compared to inserting entities individually.</remarks>
        /// <typeparam name="T">The type of the entities to insert. Must be a class that is mapped by the DbContext.</typeparam>
        /// <param name="context">The DbContext instance used to access the database. Must not be null and must be configured for the target entity
        /// type.</param>
        /// <param name="entities">The collection of entities to insert. Must not be null and must contain at least one entity.</param>
        /// <param name="bulkConfig">An optional configuration object that specifies bulk insert options, such as batch size or column mappings. If null,
        /// default settings are used.</param>
        /// <param name="cancellationToken">A CancellationToken that can be used to cancel the asynchronous operation.</param>
        /// <returns>A task that represents the asynchronous bulk insert operation.</returns>
        /// <exception cref="ApplicationException">Thrown if an error occurs during the bulk insert operation.</exception>
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
                await transaction.CommitAsync();
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
        /// Performs a bulk update operation on the specified entities in the database asynchronously using a temporary
        /// table and a merge statement.
        /// </summary>
        /// <remarks>This method uses a temporary table and a SQL MERGE statement to efficiently update multiple
        /// records in a single database operation. The operation is performed within a transaction to ensure atomicity.
        /// After completion, the temporary table is dropped and the database connection is closed. This method is suitable
        /// for scenarios where updating a large number of records individually would be inefficient.</remarks>
        /// <typeparam name="T">The type of the entities to update. Must be a class that is mapped to a table in the provided DbContext.</typeparam>
        /// <param name="context">The DbContext instance used to access the database and track changes.</param>
        /// <param name="entities">The collection of entities to update in bulk. Each entity must correspond to a row in the target database table.</param>
        /// <param name="bulkConfig">An optional configuration object that specifies settings for the bulk operation, such as batch size or column
        /// mappings. If null, default settings are used.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the asynchronous operation.</param>
        /// <returns>A task that represents the asynchronous bulk update operation.</returns>
        /// <exception cref="ApplicationException">Thrown if an error occurs during the bulk update process.</exception>
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
                await transaction.CommitAsync();
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
        /// Performs a bulk insert or update operation for a collection of entities in the specified DbContext using
        /// optimized SQL Server techniques.
        /// </summary>
        /// <remarks>This method uses temporary tables and SQL Server MERGE statements to efficiently insert new
        /// records or update existing ones in bulk. The operation is performed within a transaction to ensure atomicity.
        /// After completion, the temporary table is dropped and the database connection is closed. This method is intended
        /// for scenarios where large numbers of entities need to be synchronized with the database efficiently.</remarks>
        /// <typeparam name="T">The type of the entities to insert or update. Must be a class mapped to a database table in the provided
        /// DbContext.</typeparam>
        /// <param name="context">The DbContext instance used to access the database and track changes.</param>
        /// <param name="entities">The collection of entities to be inserted or updated in bulk. Cannot be null.</param>
        /// <param name="bulkConfig">An optional configuration object that specifies bulk operation settings such as batch size or column mappings.
        /// If null, default settings are used.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests. The operation is canceled if the token is triggered.</param>
        /// <returns>A task that represents the asynchronous bulk insert or update operation.</returns>
        /// <exception cref="ApplicationException">Thrown if an error occurs during the bulk insert or update process. The inner exception contains details about
        /// the underlying failure.</exception>
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
                await transaction.CommitAsync();
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
