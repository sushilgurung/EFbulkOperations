using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gurung.BulkOperations.SqlDataHandler
{
    /// <summary>
    /// Defines methods for performing bulk data operations on a SQL database using an Entity Framework Core context.
    /// </summary>
    /// <remarks>Implementations of this interface provide efficient mechanisms for inserting, updating, or
    /// upserting large collections of entities in a SQL database. These operations are typically optimized for
    /// performance and are intended for scenarios where processing large volumes of data is required. Thread safety and
    /// transaction management depend on the specific implementation.</remarks>
    public interface ISqlDataHandler
    {
        /// <summary>
        /// Asynchronously inserts a collection of entities into the database in bulk, using the specified configuration
        /// options.
        /// </summary>
        /// <remarks>Bulk insert operations can significantly improve performance when adding large
        /// numbers of entities compared to individual inserts. The operation is performed within the context's current
        /// transaction, if one exists.</remarks>
        /// <typeparam name="T">The type of the entities to insert. Must be a class that is mapped by the provided DbContext.</typeparam>
        /// <param name="context">The DbContext instance used to access the database. Cannot be null.</param>
        /// <param name="entities">The collection of entities to insert. Cannot be null or contain null elements.</param>
        /// <param name="bulkConfig">The configuration options for the bulk insert operation. If null, default options are used.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the asynchronous operation.</param>
        /// <returns>A task that represents the asynchronous bulk insert operation.</returns>
        public Task BulkInsertAsync<T>(DbContext context, IEnumerable<T> entities, BulkConfig bulkConfig = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Performs a bulk update operation asynchronously for the specified entities in the given DbContext.
        /// </summary>
        /// <remarks>This method is optimized for updating large numbers of entities efficiently. The
        /// operation is performed asynchronously and may improve performance compared to updating entities
        /// individually. Ensure that the entities are attached to the provided DbContext and that any required
        /// configuration is specified in the bulkConfig parameter.</remarks>
        /// <typeparam name="T">The type of the entities to update. Must be a class that is tracked by the DbContext.</typeparam>
        /// <param name="context">The DbContext instance used to track and update the entities. Cannot be null.</param>
        /// <param name="entities">The collection of entities to update in bulk. Cannot be null or contain null elements.</param>
        /// <param name="bulkConfig">An optional configuration object that specifies bulk operation options. If null, default settings are used.</param>
        /// <param name="cancellationToken">A CancellationToken that can be used to cancel the asynchronous operation.</param>
        /// <returns>A task that represents the asynchronous bulk update operation.</returns>
        public Task BulkUpDateAsync<T>(
                DbContext context,
                IEnumerable<T> entities,
                BulkConfig bulkConfig = null,
                CancellationToken cancellationToken = default);
        /// <summary>
        /// Performs a bulk insert or update operation for the specified entities in the given DbContext asynchronously.
        /// </summary>
        /// <remarks>This method efficiently inserts new entities or updates existing ones in bulk, which
        /// can significantly improve performance compared to individual operations. The behavior of the operation can
        /// be customized using the bulkConfig parameter. The method does not track changes to the entities after the
        /// operation completes.</remarks>
        /// <typeparam name="T">The type of the entities to insert or update. Must be a class that is mapped in the DbContext.</typeparam>
        /// <param name="context">The DbContext instance used to perform the bulk operation. Cannot be null.</param>
        /// <param name="entities">The collection of entities to insert or update. Cannot be null or empty.</param>
        /// <param name="bulkConfig">An optional configuration object that specifies options for the bulk operation. If null, default settings
        /// are used.</param>
        /// <param name="cancellationToken">A CancellationToken that can be used to cancel the asynchronous operation.</param>
        /// <returns>A task that represents the asynchronous bulk insert or update operation.</returns>
        public Task BulkInsertOrUpDateAsync<T>(
            DbContext context,
            IEnumerable<T> entities,
            BulkConfig bulkConfig = null,
            CancellationToken cancellationToken = default);
    }
}
