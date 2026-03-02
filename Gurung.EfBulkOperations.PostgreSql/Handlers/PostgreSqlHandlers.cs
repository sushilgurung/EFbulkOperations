using Gurung.EfBulkOperations.PostgreSql.QueryBuilder;
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

namespace Gurung.EfBulkOperations.PostgreSql.Handlers
{
    /// <summary>
    /// Provides static methods for performing high-performance bulk operations with PostgreSQL, including bulk insert,
    /// update, and merge operations using the COPY command and temporary tables.
    /// </summary>
    /// <remarks>This class is designed to facilitate efficient data manipulation in PostgreSQL databases by
    /// leveraging native bulk operations. It supports scenarios such as bulk inserting entities, copying data to
    /// temporary tables, and executing upsert (merge) operations. All methods are thread-safe as they do not maintain
    /// internal state. Exceptions are thrown as InvalidOperationException with additional context for easier
    /// troubleshooting. The class requires Npgsql and assumes that the provided connection and transaction are valid
    /// and open.</remarks>
    public class PostgreSqlHandlers
    {
        /// <summary>
        /// Gets filtered properties based on identity columns and KeepIdentity configuration.
        /// Filters out identity columns when KeepIdentity is false.
        /// </summary>
        /// <param name="tableInfo">Table information containing property and entity metadata.</param>
        /// <param name="bulkConfig">Bulk configuration with KeepIdentity setting.</param>
        /// <returns>Array of filtered PropertyInfo objects.</returns>
        private static PropertyInfo[] GetFilteredProperties(TableDetails tableInfo, BulkConfig bulkConfig)
        {
            return tableInfo.PropertyInfo
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
        }

        /// <summary>
        /// Performs a bulk insert operation into a PostgreSQL table using binary import for the specified entities.
        /// </summary>
        /// <remarks>This method uses PostgreSQL's binary COPY protocol for efficient bulk data loading.
        /// Identity columns are handled according to the provided BulkConfig. The method does not automatically close the
        /// provided connection. Ensure that the connection remains open for the duration of the operation.</remarks>
        /// <typeparam name="T">The type of the entities to insert. Each entity must have properties that map to the columns defined in the
        /// target table.</typeparam>
        /// <param name="connection">The open NpgsqlConnection to the PostgreSQL database where the data will be inserted.</param>
        /// <param name="entities">The collection of entities to insert into the database. Each entity represents a row to be added.</param>
        /// <param name="tableInfo">The table details, including schema, table name, and column mappings, that define the target table for the
        /// bulk insert.</param>
        /// <param name="bulkConfig">The configuration options for the bulk insert operation. If null, default settings are used.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests. The operation is canceled if the token is triggered.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the NpgsqlBinaryImporter used for
        /// the bulk insert. The caller is responsible for disposing the importer after use.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the bulk copy operation fails due to an error during the insert process.</exception>
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
                var properties = GetFilteredProperties(tableInfo, bulkConfig);

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

                        // Check if this property is an identity column
                        bool isIdentity = tableInfo.EntityType
                            .FindProperty(propertyName.Name)?
                            .ValueGenerated == ValueGenerated.OnAdd;

                        // When KeepIdentity = true, insert the actual ID value
                        // When KeepIdentity = false, identity columns are already filtered out in properties
                        // So we always write the actual value here
                        await writer.WriteAsync(value ?? DBNull.Value, cancellationToken).ConfigureAwait(false);
                    }
                }
                await writer.CompleteAsync(cancellationToken);
                await writer.CloseAsync()
                    .ConfigureAwait(false);
                return writer;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to perform bulk copy operation in PostgreSQL.", ex);
            }
        }

        /// <summary>
        /// Asynchronously copies a collection of entities to a temporary table in PostgreSQL using binary import for
        /// efficient bulk operations.
        /// </summary>
        /// <remarks>This method uses PostgreSQL's binary import to efficiently insert large numbers of
        /// entities into a temporary table. The operation is optimized for performance and is suitable for scenarios
        /// where high-throughput data loading is required. The method respects identity column handling based on the
        /// provided BulkConfig. The caller is responsible for ensuring that the connection is open and that the
        /// temporary table schema matches the entity structure.</remarks>
        /// <typeparam name="T">The type of the entities to be copied to the temporary table.</typeparam>
        /// <param name="connection">The open NpgsqlConnection to the PostgreSQL database where the temporary table resides.</param>
        /// <param name="transaction">The NpgsqlTransaction to associate with the bulk copy operation. Can be null if no transaction is required.</param>
        /// <param name="entities">The collection of entities to copy into the temporary table. Each entity represents a row to be inserted.</param>
        /// <param name="tableInfo">The TableDetails object containing metadata about the target temporary table, including column mappings and
        /// entity type information.</param>
        /// <param name="bulkConfig">The BulkConfig object specifying options for the bulk copy operation. If null, default configuration is used.</param>
        /// <param name="cancellationToken">A CancellationToken that can be used to cancel the asynchronous operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result is true if the bulk copy succeeds.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the bulk copy operation fails due to an error during the import process.</exception>
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

                var properties = GetFilteredProperties(tableInfo, bulkConfig);

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

                        // Check if this property is an identity column
                        bool isIdentity = tableInfo.EntityType
                            .FindProperty(propertyName.Name)?
                            .ValueGenerated == ValueGenerated.OnAdd;

                        // When KeepIdentity = true, insert the actual ID value
                        // When KeepIdentity = false, identity columns are already filtered out in properties
                        // So we always write the actual value here
                        await writer.WriteAsync(value ?? DBNull.Value, cancellationToken).ConfigureAwait(false);
                    }
                }
                await writer.CompleteAsync(cancellationToken);
                await writer.CloseAsync()
                    .ConfigureAwait(false);
                return true;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to bulk copy data to temporary table in PostgreSQL.", ex);
            }
        }

        /// <summary>
        /// Asynchronously creates a temporary table in PostgreSQL based on the schema of an existing table, without
        /// copying any data.
        /// </summary>
        /// <remarks>The temporary table is created with the same schema as the specified source table but
        /// contains no data. The operation must be performed within an active transaction and connection.</remarks>
        /// <param name="connection">The open NpgsqlConnection to the PostgreSQL database where the temporary table will be created. Must not be
        /// null.</param>
        /// <param name="transaction">The NpgsqlTransaction within which the temporary table creation will be executed. Must not be null.</param>
        /// <param name="tableInfo">The TableDetails object containing information about the source table and the name for the temporary table.
        /// Must not be null.</param>
        /// <param name="cancellationToken">A CancellationToken that can be used to cancel the asynchronous operation. Optional.</param>
        /// <returns>A task that represents the asynchronous operation. The task result is true if the temporary table is created
        /// successfully.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the temporary table cannot be created in PostgreSQL.</exception>
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
                throw new InvalidOperationException("Failed to create temporary table in PostgreSQL.", ex);
            }
        }

        /// <summary>
        /// Determines whether the specified object is equal to the default value for its type.
        /// </summary>
        /// <remarks>For reference types, this method returns true if the value is null. For value types, it
        /// returns true if the value is equal to the result of Activator.CreateInstance for the type.</remarks>
        /// <param name="value">The object to compare against the default value of its type. Can be null.</param>
        /// <returns>true if the specified object is null or equal to the default value of its type; otherwise, false.</returns>
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
        /// Asynchronously executes a SQL command using the specified PostgreSQL connection and transaction.
        /// </summary>
        /// <param name="connection">The open NpgsqlConnection to use for executing the command. Must not be null.</param>
        /// <param name="transaction">The NpgsqlTransaction within which the command is executed. Must be associated with the provided connection.</param>
        /// <param name="sqlCommand">The SQL command text to execute. Cannot be null or empty.</param>
        /// <param name="cancellationToken">A CancellationToken that can be used to cancel the asynchronous operation. The default value is None.</param>
        /// <returns>A task that represents the asynchronous operation. The task result is true if the command executes
        /// successfully.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the SQL command fails to execute.</exception>
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
                throw new InvalidOperationException("Failed to execute SQL command in PostgreSQL.", ex);
            }
        }

        /// <summary>
        /// Executes a merge (upsert) operation on a PostgreSQL table using the specified connection, transaction, and
        /// bulk configuration.
        /// </summary>
        /// <remarks>This method performs an upsert by first updating existing records with matching
        /// primary keys and then inserting new records. The operation is executed within the provided transaction
        /// context. The method is intended for use with bulk data operations and relies on the configuration and table
        /// metadata supplied.</remarks>
        /// <param name="connection">The open NpgsqlConnection to the PostgreSQL database where the merge operation will be performed.</param>
        /// <param name="transaction">The NpgsqlTransaction within which the merge operation will be executed. The operation is committed or
        /// rolled back with this transaction.</param>
        /// <param name="tableInfo">The TableDetails object containing metadata about the target table, including column mappings and primary
        /// key information.</param>
        /// <param name="hasIdentityColumn">true if the target table includes an identity (auto-increment) column; otherwise, false.</param>
        /// <param name="bulkConfig">The BulkConfig object specifying options and settings for the bulk merge operation.</param>
        /// <param name="cancellationToken">A CancellationToken that can be used to cancel the asynchronous operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result is true if the merge operation completes
        /// successfully.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the merge (upsert) operation fails to execute in PostgreSQL.</exception>
        public static async Task<bool> ExecuteMergeAsync(
            NpgsqlConnection connection,
            NpgsqlTransaction transaction,
            TableDetails tableInfo,
            bool hasIdentityColumn,
            BulkConfig bulkConfig,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Get column names from mappings for SQL generation
                var properties = GetFilteredProperties(tableInfo, bulkConfig);

                // Get column names from mappings
                var columnNames = properties.Select(p => tableInfo.ColumnMappings[p.Name]);

                var mergeQueries = PostgreQueryBuilder.GenerateSplitMergeQueries(
                    tableInfo.FullTableName,
                    tableInfo.TempTableName,
                    columnNames,
                    tableInfo.PrimaryKeyColumns.ToList(),
                    hasIdentityColumn
                );

                // Execute UPSERT for records with existing PKs first
                int upsertCount = 0;
                using (var upsertCommand = new NpgsqlCommand(mergeQueries.UpsertQuery, connection, transaction))
                {
                    upsertCount = await upsertCommand.ExecuteNonQueryAsync(cancellationToken);
                }
                ;

                // Then INSERT new records with NULL/default PKs
                int insertCount = 0;
                using (var insertCommand = new NpgsqlCommand(mergeQueries.InsertNewQuery, connection, transaction))
                {
                    insertCount = await insertCommand.ExecuteNonQueryAsync(cancellationToken);
                }

                return true;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to execute merge (upsert) operation in PostgreSQL.", ex);
            }
        }

        /// <summary>
        /// Performs a bulk insert or update operation for a collection of entities in a PostgreSQL database using a temporary
        /// table and merge strategy.
        /// </summary>
        /// <remarks>This method uses a temporary table and a merge operation to efficiently insert new records or update
        /// existing ones based on primary key values. The operation is executed within the provided transaction and is atomic
        /// with respect to that transaction. The method does not commit or roll back the transaction; transaction management is
        /// the caller's responsibility.</remarks>
        /// <typeparam name="T">The type of the entities to insert or update. Must correspond to the target table schema.</typeparam>
        /// <param name="connection">The open NpgsqlConnection to the PostgreSQL database where the operation will be performed.</param>
        /// <param name="transaction">The NpgsqlTransaction within which the bulk operation will execute. The transaction must be valid and open.</param>
        /// <param name="entities">The collection of entities to insert or update in the target table. Cannot be null.</param>
        /// <param name="tableInfo">The TableDetails object containing metadata about the target table, such as schema and column mappings.</param>
        /// <param name="bulkConfig">The BulkConfig object specifying options for the bulk operation, such as batch size or column mappings. If null,
        /// default configuration is used.</param>
        /// <param name="cancellationToken">A CancellationToken that can be used to cancel the asynchronous operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result is true if the bulk insert or update succeeds;
        /// otherwise, false.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the bulk insert or update operation fails due to an error in the process.</exception>
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
                bool mergeSuccess = await ExecuteMergeAsync(connection, transaction, tableInfo, hasIdentityColumn, bulkConfig, cancellationToken);
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
                throw new InvalidOperationException("Failed to execute bulk insert or update operation in PostgreSQL.", ex);
            }
        }

        /// <summary>
        /// Asynchronously copies a collection of entities, including primary key values, to a temporary table in
        /// PostgreSQL using binary import.
        /// </summary>
        /// <remarks>This method is intended for scenarios where primary key values must be preserved in the
        /// temporary table, such as for insert-or-update operations. The method uses PostgreSQL's binary import for
        /// efficient data transfer. The caller is responsible for ensuring that the connection is open and that the
        /// temporary table schema matches the entity properties.</remarks>
        /// <typeparam name="T">The type of the entities to be copied to the temporary table.</typeparam>
        /// <param name="connection">The open NpgsqlConnection to the PostgreSQL database where the temporary table resides.</param>
        /// <param name="transaction">The NpgsqlTransaction to associate with the bulk copy operation. Can be null if no transaction is required.</param>
        /// <param name="entities">The collection of entities to copy to the temporary table. Each entity's properties are mapped to table
        /// columns.</param>
        /// <param name="tableInfo">The TableDetails object containing metadata about the target temporary table, including column mappings and
        /// property information.</param>
        /// <param name="bulkConfig">The BulkConfig object that specifies options for the bulk copy operation. If null, default configuration is
        /// used.</param>
        /// <param name="cancellationToken">A CancellationToken that can be used to cancel the asynchronous operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result is true if the bulk copy succeeds.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the bulk copy operation fails due to an error during the import process.</exception>
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
                throw new InvalidOperationException("Failed to bulk copy data with primary keys to temporary table in PostgreSQL.", ex);
            }
        }


    }
}
