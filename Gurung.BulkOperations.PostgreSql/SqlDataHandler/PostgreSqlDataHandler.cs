using Gurung.BulkOperations.PostgreSql.Handlers;
using Gurung.BulkOperations.PostgreSql.QueryBuilder;
using Gurung.BulkOperations.SqlDataHandler;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using System.Data;

namespace Gurung.BulkOperations.PostgreDataHandler.PostgreSql
{
    public class PostgreSqlDataHandler : ISqlDataHandler
    {
        /// <summary>
        /// This method performs a bulk insert operation for a collection of entities into a PostgreSQL database using the provided DbContext.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="context"></param>
        /// <param name="entities"></param>
        /// <param name="bulkConfig"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="ApplicationException"></exception>
        public async Task BulkInsertAsync<T>(DbContext context, IEnumerable<T> entities, BulkConfig bulkConfig = null, CancellationToken cancellationToken = default)
        {
            if (entities == null || !entities.Any())
                return;

            var connection = (NpgsqlConnection)context.Database.GetDbConnection();
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
            using var transaction = await connection.BeginTransactionAsync(cancellationToken);
            try
            {
                TableDetails tableInfo = TableDetails.GenerateInstance(context, entities);
                var writer = await PostgreSqlHandlers.BulkInsertAsync((NpgsqlConnection)connection, entities, tableInfo, bulkConfig, cancellationToken).ConfigureAwait(false);
            
                await transaction.CommitAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                throw new ApplicationException("Invalid operation during PostgreSQL bulk insert.", ex);
            }
            finally
            {
                await connection.CloseAsync().ConfigureAwait(false);
            }
        }

        /// <summary>
        /// This method performs a bulk update operation for a collection of entities in a PostgreSQL database using the provided DbContext.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="context"></param>
        /// <param name="entities"></param>
        /// <param name="bulkConfig"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task BulkUpDateAsync<T>(DbContext context, IEnumerable<T> entities, BulkConfig bulkConfig = null, CancellationToken cancellationToken = default)
        {
            if (entities == null || !entities.Any())
                return;

            var connection = (NpgsqlConnection)context.Database.GetDbConnection();
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
            using var transaction = await connection.BeginTransactionAsync(cancellationToken);
            
            TableDetails tableInfo = TableDetails.GenerateInstance(context, entities);
            
            try
            {
                // Create temporary table
                bool hasTempTableCreated = await PostgreSqlHandlers.CreateTempTableAsync(connection, transaction, tableInfo, cancellationToken).ConfigureAwait(false);
                
                if (!hasTempTableCreated)
                {
                    throw new ApplicationException("Failed to create temporary table for bulk update.");
                }

                // Bulk copy data to temp table
                bool hasBulkCopy = await PostgreSqlHandlers.BulkCopyToTempTableAsync(connection, transaction, entities, tableInfo, bulkConfig, cancellationToken).ConfigureAwait(false);
                
                if (!hasBulkCopy)
                {
                    throw new ApplicationException("Failed to copy data to temporary table.");
                }

                // Get column names from mappings
                var columnNames = tableInfo.PropertyInfo
                    .Select(p => tableInfo.ColumnMappings[p.Name])
                    .ToList();

                // Generate and execute UPDATE query
                string updateQuery = PostgreQueryBuilder.GenerateUpdateQuery(
                    tableInfo.FullTableName, 
                    tableInfo.TempTableName, 
                    columnNames,
                    tableInfo.PrimaryKeyColumns.ToList());
                bool hasUpdateExecuted = await PostgreSqlHandlers.SqlCommandAsync(connection, transaction, updateQuery, cancellationToken).ConfigureAwait(false);
                
                if (!hasUpdateExecuted)
                {
                    throw new ApplicationException("Failed to execute bulk update query.");
                }

                await transaction.CommitAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                throw new ApplicationException("Invalid operation during PostgreSQL bulk update.", ex);
            }
            finally
            {
                var dropTempTableQuery = PostgreQueryBuilder.DropTempTableQuery(tableInfo.TempTableName);
                try
                {
                    await context.Database.ExecuteSqlRawAsync(dropTempTableQuery, cancellationToken).ConfigureAwait(false);
                }
                catch { }
                
                await connection.CloseAsync().ConfigureAwait(false);
            }
        }

        /// <summary>
        /// This method performs a bulk insert or update operation for a collection of entities in a PostgreSQL database using the provided DbContext.
        /// Handles both new records (with null/default PKs) and existing records (with valid PKs) in a single operation.
        /// Uses a split merge strategy: UPSERT for existing records, INSERT for new records.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="context"></param>
        /// <param name="entities"></param>
        /// <param name="bulkConfig"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task BulkInsertOrUpDateAsync<T>(DbContext context, IEnumerable<T> entities, BulkConfig bulkConfig = null, CancellationToken cancellationToken = default)
        {
            if (entities == null || !entities.Any())
                return;

            var connection = (NpgsqlConnection)context.Database.GetDbConnection();
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
            using var transaction = await connection.BeginTransactionAsync(cancellationToken);
            
            TableDetails tableInfo = TableDetails.GenerateInstance(context, entities);
            
            try
            {
                // Use the comprehensive BulkInsertOrUpdateAsync which handles split merge properly
                bool success = await PostgreSqlHandlers.BulkInsertOrUpdateAsync(
                    connection, 
                    transaction, 
                    entities, 
                    tableInfo, 
                    bulkConfig, 
                    cancellationToken
                ).ConfigureAwait(false);
                
                if (!success)
                {
                    throw new ApplicationException("Failed to execute bulk insert or update operation.");
                }

                await transaction.CommitAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                throw new ApplicationException("Invalid operation during PostgreSQL bulk upsert.", ex);
            }
            finally
            {
                await connection.CloseAsync().ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Converts a collection of entities to a DataTable
        /// </summary>
        private static DataTable ConvertToDataTable<T>(IEnumerable<T> entities, TableDetails tableInfo)
        {
            DataTable dataTable = new DataTable();

            var properties = tableInfo.PropertyInfo;
            foreach (var prop in properties)
            {
                Type propType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
                dataTable.Columns.Add(prop.Name, propType);
            }

            foreach (var entity in entities)
            {
                DataRow row = dataTable.NewRow();
                foreach (var prop in properties)
                {
                    row[prop.Name] = prop.GetValue(entity) ?? DBNull.Value;
                }
                dataTable.Rows.Add(row);
            }

            return dataTable;
        }
    }
}
