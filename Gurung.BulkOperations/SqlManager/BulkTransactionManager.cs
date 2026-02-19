using Gurung.BulkOperations.Context;
using Gurung.BulkOperations.SqlDataHandler;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace Gurung.BulkOperations
{
    /// <summary>
    /// Provides static methods for performing bulk insert, update, and insert-or-update operations on collections of
    /// entities using Entity Framework Core contexts and DbSets. These methods enable efficient processing of large
    /// data sets by leveraging optimized bulk operations.
    /// </summary>
    /// <remarks>Bulk operations performed by this class are designed to improve performance when handling
    /// large numbers of entities, compared to executing individual insert or update commands. The methods support
    /// asynchronous execution and allow customization through bulk configuration options. Thread safety and transaction
    /// management depend on the underlying DbContext and configuration provided. For best results, ensure that the
    /// entities and configuration options are compatible with the database provider in use.</remarks>
    public static class BulkTransactionManager
    {
        /// <summary>
        /// This method performs a bulk insert operation for a collection of entities into the database using the provided DbContext.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="context"></param>
        /// <param name="entities"></param>
        /// <param name="bulkConfig"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static async Task BulkInsertAsync<T>(
           this DbContext context,
            IEnumerable<T> entities,
            BulkConfig bulkConfig = null,
            CancellationToken cancellationToken = default
            )
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context), "DbContext cannot be null.");
            
            if (entities == null)
                throw new ArgumentNullException(nameof(entities), "Entities collection cannot be null.");

            try
            {
                ISqlDataHandler sqlDataHandler = SqlDataHandlerFactory.CreateDataHandler(context);
                bulkConfig = bulkConfig ?? new BulkConfig() { dataHandler = sqlDataHandler };
                bulkConfig.dataHandler = sqlDataHandler;
                await sqlDataHandler.BulkInsertAsync(context, entities, bulkConfig, cancellationToken);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Bulk insert operation failed for entity type '{typeof(T).Name}'. See inner exception for details.", ex);
            }
        }

        /// <summary>
        /// This method performs a bulk insert operation for a collection of entities into the database using the provided DbSet.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="dbSet"></param>
        /// <param name="entities"></param>
        /// <param name="bulkConfig"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static async Task BulkInsertAsync<T>(
           this DbSet<T> dbSet,
           IEnumerable<T> entities,
           BulkConfig bulkConfig = null,
           CancellationToken cancellationToken = default
       ) where T : class
        {
            if (dbSet == null)
                throw new ArgumentNullException(nameof(dbSet), "DbSet cannot be null.");
            
            if (entities == null)
                throw new ArgumentNullException(nameof(entities), "Entities collection cannot be null.");

            try
            {
                var context = dbSet.GetDbContext();
                ISqlDataHandler sqlDataHandler = SqlDataHandlerFactory.CreateDataHandler(context);

                bulkConfig = bulkConfig ?? new BulkConfig() { dataHandler = sqlDataHandler };
                bulkConfig.dataHandler = sqlDataHandler;
                await sqlDataHandler.BulkInsertAsync(context, entities, bulkConfig, cancellationToken);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Bulk insert operation failed for entity type '{typeof(T).Name}'. See inner exception for details.", ex);
            }
        }

        /// <summary>
        /// This method performs a bulk update operation for a collection of entities in the database using the provided DbContext.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="context"></param>
        /// <param name="entities"></param>
        /// <param name="bulkConfig"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static async Task BulkUpDateAsync<T>(
          this DbContext context,
          IEnumerable<T> entities,
          BulkConfig bulkConfig = null,
          CancellationToken cancellationToken = default
          )
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context), "DbContext cannot be null.");
            
            if (entities == null)
                throw new ArgumentNullException(nameof(entities), "Entities collection cannot be null.");

            try
            {
                ISqlDataHandler sqlDataHandler = SqlDataHandlerFactory.CreateDataHandler(context);
                bulkConfig = bulkConfig ?? new BulkConfig() { dataHandler = sqlDataHandler };
                bulkConfig.dataHandler = sqlDataHandler;
                await sqlDataHandler.BulkUpDateAsync(context, entities, bulkConfig, cancellationToken);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Bulk update operation failed for entity type '{typeof(T).Name}'. See inner exception for details.", ex);
            }
        }

        /// <summary>
        /// This method performs a bulk update operation for a collection of entities in the database using the provided DbSet.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="dbSet"></param>
        /// <param name="entities"></param>
        /// <param name="bulkConfig"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static async Task BulkUpDateAsync<T>(
        this DbSet<T> dbSet,
       IEnumerable<T> entities,
       BulkConfig bulkConfig = null,
       CancellationToken cancellationToken = default
       ) where T : class
        {
            if (dbSet == null)
                throw new ArgumentNullException(nameof(dbSet), "DbSet cannot be null.");
            
            if (entities == null)
                throw new ArgumentNullException(nameof(entities), "Entities collection cannot be null.");

            try
            {
                var context = dbSet.GetDbContext();
                ISqlDataHandler sqlDataHandler = SqlDataHandlerFactory.CreateDataHandler(context);
                bulkConfig = bulkConfig ?? new BulkConfig() { dataHandler = sqlDataHandler };
                bulkConfig.dataHandler = sqlDataHandler;
                await sqlDataHandler.BulkUpDateAsync(context, entities, bulkConfig, cancellationToken);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Bulk update operation failed for entity type '{typeof(T).Name}'. See inner exception for details.", ex);
            }
        }
        /// <summary>
        /// This method performs a bulk insert or update operation for a collection of entities in the database using the provided DbContext.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="context"></param>
        /// <param name="entities"></param>
        /// <param name="bulkConfig"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static async Task BulkInsertOrUpDateAsync<T>(
          this DbContext context,
           IEnumerable<T> entities,
           BulkConfig bulkConfig = null,
           CancellationToken cancellationToken = default)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context), "DbContext cannot be null.");
            
            if (entities == null)
                throw new ArgumentNullException(nameof(entities), "Entities collection cannot be null.");

            try
            {
                ISqlDataHandler sqlDataHandler = SqlDataHandlerFactory.CreateDataHandler(context);
                bulkConfig = bulkConfig ?? new BulkConfig() { dataHandler = sqlDataHandler };
                bulkConfig.dataHandler = sqlDataHandler;
                await sqlDataHandler.BulkInsertOrUpDateAsync(context, entities, bulkConfig, cancellationToken);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Bulk insert or update operation failed for entity type '{typeof(T).Name}'. See inner exception for details.", ex);
            }
        }

        /// <summary>
        /// Performs a bulk insert or update operation for the specified entities in the database asynchronously.
        /// </summary>
        /// <remarks>This method uses a bulk operation to efficiently insert new entities or update
        /// existing ones in the database. It is suitable for scenarios where large numbers of records need to be
        /// processed with improved performance compared to individual insert or update operations.</remarks>
        /// <typeparam name="T">The type of the entities in the DbSet. Must be a reference type.</typeparam>
        /// <param name="dbSet">The DbSet representing the table in which to insert or update entities.</param>
        /// <param name="entities">The collection of entities to be inserted or updated. Cannot be null.</param>
        /// <param name="bulkConfig">The configuration options for the bulk operation. If null, default settings are used.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests. The default value is None.</param>
        /// <returns>A task that represents the asynchronous bulk insert or update operation.</returns>
        public static async Task BulkInsertOrUpDateAsync<T>(
         this DbSet<T> dbSet,
          IEnumerable<T> entities,
          BulkConfig bulkConfig = null,
          CancellationToken cancellationToken = default) where T : class
        {
            if (dbSet == null)
                throw new ArgumentNullException(nameof(dbSet), "DbSet cannot be null.");
            
            if (entities == null)
                throw new ArgumentNullException(nameof(entities), "Entities collection cannot be null.");

            try
            {
                var context = dbSet.GetDbContext();
                ISqlDataHandler sqlDataHandler = SqlDataHandlerFactory.CreateDataHandler(context);
                bulkConfig = bulkConfig ?? new BulkConfig() { dataHandler = sqlDataHandler };
                bulkConfig.dataHandler = sqlDataHandler;
                await sqlDataHandler.BulkInsertOrUpDateAsync(context, entities, bulkConfig, cancellationToken);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Bulk insert or update operation failed for entity type '{typeof(T).Name}'. See inner exception for details.", ex);
            }
        }

    }
}
