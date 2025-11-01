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
            //  bulkConfig.dataHandler = SqlDataHandlerFactory.CreateDataHandler(context);
            ISqlDataHandler sqlDataHandler = SqlDataHandlerFactory.CreateDataHandler(context);
            await sqlDataHandler.BulkInsertAsync(context, entities, bulkConfig, cancellationToken);
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
            var context = dbSet.GetDbContext();
            ISqlDataHandler sqlDataHandler = SqlDataHandlerFactory.CreateDataHandler(context);
            await sqlDataHandler.BulkInsertAsync(context, entities, bulkConfig, cancellationToken);
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
            ISqlDataHandler sqlDataHandler = bulkConfig.dataHandler;
            await sqlDataHandler.BulkUpDateAsync(context, entities, bulkConfig, cancellationToken);
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
            var context = dbSet.GetDbContext();
            ISqlDataHandler sqlDataHandler = bulkConfig.dataHandler;
            await sqlDataHandler.BulkUpDateAsync(context, entities, bulkConfig, cancellationToken);
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
            ISqlDataHandler sqlDataHandler = bulkConfig.dataHandler;
            await sqlDataHandler.BulkInsertOrUpDateAsync(context, entities, bulkConfig, cancellationToken);
        }

        public static async Task BulkInsertOrUpDateAsync<T>(
         this DbSet<T> dbSet,
          IEnumerable<T> entities,
          BulkConfig bulkConfig = null,
          CancellationToken cancellationToken = default) where T : class
        {
            var context = dbSet.GetDbContext();
            ISqlDataHandler sqlDataHandler = bulkConfig.dataHandler;
            await sqlDataHandler.BulkInsertOrUpDateAsync(context, entities, bulkConfig, cancellationToken);
        }

    }
}
