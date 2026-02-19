using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gurung.BulkOperations.Formatter
{
    /// <summary>
    /// Defines a contract for generating detailed table information for a collection of entities within a specified
    /// database context.
    /// </summary>
    public interface ITableDetailsInstance
    {
        /// <summary>
        /// Generates a new instance of the table details for the specified entities within the given database context.
        /// </summary>
        /// <typeparam name="T">The type of the entities for which to generate table details.</typeparam>
        /// <param name="context">The database context that provides access to the underlying data store. Cannot be null.</param>
        /// <param name="entities">The collection of entities to include in the table details. Cannot be null.</param>
        /// <returns>A TableDetails object representing the schema and data information for the specified entities.</returns>
        TableDetails GenerateInstance<T>(DbContext context, IEnumerable<T> entities);
    }
}
