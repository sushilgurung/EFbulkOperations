using Gurung.BulkOperations.PostgreSql.QueryBuilder;
using Microsoft.EntityFrameworkCore.Metadata;
using Npgsql;
using Npgsql.EntityFrameworkCore.PostgreSQL.Storage.Internal.Mapping;
using NpgsqlTypes;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Gurung.BulkOperations.PostgreSql.Handlers
{
    public class PostgreSqlHandlers
    {
        /// <summary>
        /// Bulk insert using PostgreSQL's COPY command.
        /// </summary>
        public static async Task<NpgsqlBinaryImporter> BulkInsertAsync<T>(
            NpgsqlConnection connection,
            IEnumerable<T> entities,
            TableDetails tableInfo,
            BulkConfig bulkConfig = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                bulkConfig ??= new BulkConfig();

                var fullTableName = tableInfo.FullTableName;
                var properties = tableInfo.PropertyInfo
                                  .Where(p =>
                                  {
                                      bool isIdentity = tableInfo.EntityType
                                          .FindProperty(p.Name)?
                                          .ValueGenerated == ValueGenerated.OnAdd;

                                      if (isIdentity && !bulkConfig.KeepIdentity)
                                          return false;

                                      return true;
                                  })
                                  .ToArray();

                // Get column names from mappings
                var columnNames = properties.Select(p => tableInfo.ColumnMappings[p.Name]);
                var copyCommand = PostgreQueryBuilder.CopyCommand(fullTableName, columnNames);
                using NpgsqlBinaryImporter writer = await connection.BeginBinaryImportAsync(copyCommand, cancellationToken);

                foreach (var entity in entities)
                {
                    await writer.StartRowAsync(cancellationToken)
                        .ConfigureAwait(false);

                    foreach (var propertyName in properties)
                    {
                        var value = propertyName.GetValue(entity);

                        // Check if this property is a primary key column
                        bool isPrimaryKey = tableInfo.PrimaryKeys != null &&
                                           tableInfo.PrimaryKeys.Contains(propertyName.Name);

                        // For primary key columns with null/default values in PostgreSQL, use DBNull
                        // PostgreSQL will handle auto-generated values through IDENTITY or SERIAL
                        if (isPrimaryKey && (value == null || IsDefaultValue(value)))
                        {
                            await writer.WriteAsync(DBNull.Value, cancellationToken).ConfigureAwait(false);
                        }
                        else
                        {
                            await writer.WriteAsync(value ?? DBNull.Value, cancellationToken).ConfigureAwait(false);
                        }
                    }
                }
                await writer.CompleteAsync(cancellationToken);
                await writer.CloseAsync()
                    .ConfigureAwait(false);
                return writer;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PostgreSQL BulkInsert] Error: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Bulk copy entities to temp table using COPY command
        /// </summary>
        public static async Task<bool> BulkCopyToTempTableAsync<T>(
            NpgsqlConnection connection,
            NpgsqlTransaction transaction,
            IEnumerable<T> entities,
            TableDetails tableInfo,
            BulkConfig bulkConfig = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                bulkConfig ??= new BulkConfig();

                var properties = tableInfo.PropertyInfo
                    .Where(p =>
                    {
                        bool isIdentity = tableInfo.EntityType
                            .FindProperty(p.Name)?
                            .ValueGenerated == ValueGenerated.OnAdd;

                        if (isIdentity && !bulkConfig.KeepIdentity)
                            return false;

                        return true;
                    })
                    .ToArray();

                var columnNames = properties.Select(p => tableInfo.ColumnMappings[p.Name]);
                var copyCommand = PostgreQueryBuilder.CopyCommand(tableInfo.TempTableName, columnNames);
                NpgsqlBinaryImporter writer = await connection.BeginBinaryImportAsync(
                    copyFromCommand: copyCommand, 
                    cancellationToken: cancellationToken
                    );

                foreach (var entity in entities)
                {
                    await writer.StartRowAsync(cancellationToken)
                        .ConfigureAwait(false);

                    foreach (var propertyName in properties)
                    {
                        var value = propertyName.GetValue(entity);

                        // Check if this property is a primary key column
                        bool isPrimaryKey = tableInfo.PrimaryKeys != null &&
                                           tableInfo.PrimaryKeys.Contains(propertyName.Name);

                        // For primary key columns with null/default values in PostgreSQL, use DBNull
                        // PostgreSQL will handle auto-generated values through IDENTITY or SERIAL
                        if (isPrimaryKey && (value == null || IsDefaultValue(value)))
                        {
                            await writer.WriteAsync(DBNull.Value, cancellationToken).ConfigureAwait(false);
                        }
                        else
                        {
                            await writer.WriteAsync(value ?? DBNull.Value, cancellationToken).ConfigureAwait(false);
                        }
                    }
                }
                await writer.CompleteAsync(cancellationToken);
                await writer.CloseAsync()
                    .ConfigureAwait(false);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PostgreSQL BulkCopyToTemp] Error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Creates a temporary table from the source table structure.
        /// </summary>
        public static async Task<bool> CreateTempTableAsync(
            NpgsqlConnection connection,
            NpgsqlTransaction transaction,
            TableDetails tableInfo,
            CancellationToken cancellationToken = default)
        {
            try
            {
                string createTempTable =
                    $"CREATE TEMP TABLE {tableInfo.TempTableName} AS TABLE {tableInfo.FullTableName} WITH NO DATA;";

                var command = new NpgsqlCommand(createTempTable, connection, transaction);
                await command.ExecuteNonQueryAsync(cancellationToken);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PostgreSQL Temp Table] Error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Checks if a value is the default value for its type.
        /// </summary>
        private static bool IsDefaultValue(object value)
        {
            if (value == null) return true;

            var type = value.GetType();

            // For value types, check against default
            if (type.IsValueType)
            {
                var defaultValue = Activator.CreateInstance(type);
                return value.Equals(defaultValue);
            }

            return false;
        }

        /// <summary>
        /// Executes a raw SQL command.
        /// </summary>
        public static async Task<bool> SqlCommandAsync(
            NpgsqlConnection connection,
            NpgsqlTransaction transaction,
            string sqlCommand,
            CancellationToken cancellationToken = default)
        {
            try
            {
                using var command = new NpgsqlCommand(sqlCommand, connection, transaction);
                await command.ExecuteNonQueryAsync(cancellationToken);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PostgreSQL SqlCommand] Error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Executes the merge operation (InsertOrUpdate) using split queries.
        /// Handles records with NULL/default PKs (for INSERT) separately from records with existing PKs (for UPSERT).
        /// </summary>
        public static async Task<bool> ExecuteMergeAsync(
            NpgsqlConnection connection,
            NpgsqlTransaction transaction,
            TableDetails tableInfo,
            bool hasIdentityColumn,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Get column names from mappings for SQL generation
                var columnNames = tableInfo.PropertyInfo
                    .Select(p => tableInfo.ColumnMappings[p.Name])
                    .ToList();
                
                var mergeQueries = PostgreQueryBuilder.GenerateSplitMergeQueries(
                    tableInfo.FullTableName,
                    tableInfo.TempTableName,
                    columnNames,
                    tableInfo.PrimaryKeyColumns.ToList(),
                    hasIdentityColumn
                );

                Console.WriteLine($"[PostgreSQL Merge] Executing UPSERT query for existing records...");
                Console.WriteLine($"[PostgreSQL Merge] UPSERT Query: {mergeQueries.UpsertQuery}");

                // Execute UPSERT for records with existing PKs first
                int upsertCount = 0;
                using (var upsertCommand = new NpgsqlCommand(mergeQueries.UpsertQuery, connection, transaction))
                {
                    upsertCount = await upsertCommand.ExecuteNonQueryAsync(cancellationToken);
                }
                Console.WriteLine($"[PostgreSQL Merge] UPSERT completed: {upsertCount} rows affected");

                Console.WriteLine($"[PostgreSQL Merge] Executing INSERT query for new records...");
                Console.WriteLine($"[PostgreSQL Merge] INSERT Query: {mergeQueries.InsertNewQuery}");

                // Then INSERT new records with NULL/default PKs
                int insertCount = 0;
                using (var insertCommand = new NpgsqlCommand(mergeQueries.InsertNewQuery, connection, transaction))
                {
                    insertCount = await insertCommand.ExecuteNonQueryAsync(cancellationToken);
                }
                Console.WriteLine($"[PostgreSQL Merge] INSERT completed: {insertCount} rows affected");
                Console.WriteLine($"[PostgreSQL Merge] Total operation completed successfully");

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PostgreSQL Merge] Error: {ex.Message}");
                Console.WriteLine($"[PostgreSQL Merge] Stack Trace: {ex.StackTrace}");
                throw;
            }
        }

        /// <summary>
        /// Executes the complete InsertOrUpdate operation:
        /// 1. Creates temp table
        /// 2. Copies data to temp table
        /// 3. Executes merge (split into UPSERT + INSERT)
        /// 4. Drops temp table
        /// </summary>
        public static async Task<bool> BulkInsertOrUpdateAsync<T>(
            NpgsqlConnection connection,
            NpgsqlTransaction transaction,
            IEnumerable<T> entities,
            TableDetails tableInfo,
            BulkConfig bulkConfig = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                bulkConfig ??= new BulkConfig();

                // Step 1: Create temp table
                bool tempTableCreated = await CreateTempTableAsync(connection, transaction, tableInfo, cancellationToken);
                if (!tempTableCreated)
                {
                    return false;
                }

                // Step 2: Copy data to temp table (include PKs even if null/default)
                bool dataCopied = await BulkCopyToTempTableWithPKAsync(connection, transaction, entities, tableInfo, bulkConfig, cancellationToken);
                if (!dataCopied)
                {
                    return false;
                }

                // Step 3: Determine if entity has identity column
                bool hasIdentityColumn = tableInfo.EntityType
                    .FindPrimaryKey()?.Properties
                    .Any(p => p.ValueGenerated == ValueGenerated.OnAdd) ?? false;

                // Step 4: Execute merge
                bool mergeSuccess = await ExecuteMergeAsync(connection, transaction, tableInfo, hasIdentityColumn, cancellationToken);
                if (!mergeSuccess)
                {
                    return false;
                }

                // Step 5: Drop temp table
                var dropQuery = PostgreQueryBuilder.DropTempTableQuery(tableInfo.TempTableName);
                await SqlCommandAsync(connection, transaction, dropQuery, cancellationToken);

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PostgreSQL BulkInsertOrUpdate] Error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Bulk copy entities to temp table, including PK columns (even if null/default).
        /// This is specifically for InsertOrUpdate operations.
        /// </summary>
        public static async Task<bool> BulkCopyToTempTableWithPKAsync<T>(
            NpgsqlConnection connection,
            NpgsqlTransaction transaction,
            IEnumerable<T> entities,
            TableDetails tableInfo,
            BulkConfig bulkConfig = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                bulkConfig ??= new BulkConfig();

                // For InsertOrUpdate, we need ALL columns including PKs
                var properties = tableInfo.PropertyInfo;

                var columnNames = properties.Select(p => tableInfo.ColumnMappings[p.Name]);
                var copyCommand = PostgreQueryBuilder.CopyCommand(tableInfo.TempTableName, columnNames);
                NpgsqlBinaryImporter writer = await connection.BeginBinaryImportAsync(
                    copyFromCommand: copyCommand,
                    cancellationToken: cancellationToken
                );

                foreach (var entity in entities)
                {
                    await writer.StartRowAsync(cancellationToken).ConfigureAwait(false);

                    foreach (var property in properties)
                    {
                        var value = property.GetValue(entity);
                        // Write the value as-is (including null for new records)
                        await writer.WriteAsync(value ?? DBNull.Value, cancellationToken).ConfigureAwait(false);
                    }
                }

                await writer.CompleteAsync(cancellationToken);
                await writer.CloseAsync().ConfigureAwait(false);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PostgreSQL BulkCopyToTempWithPK] Error: {ex.Message}");
                return false;
            }
        }

    
    }
}
