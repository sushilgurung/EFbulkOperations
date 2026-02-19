using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gurung.BulkOperations
{
    /// <summary>
    /// Specifies the supported types of database engines.
    /// </summary>
    /// <remarks>Use this enumeration to indicate which database provider is being targeted when configuring
    /// database connections or operations. The values correspond to commonly used relational database
    /// systems.</remarks>
    public enum DatabaseType
    {
        /// <summary>
        /// Specifies the SQL Server database provider.
        /// </summary>
        SqlServer,
        /// <summary>
        /// Represents the PostgreSQL database provider or connection type.
        /// </summary>
        PostgreSql
    }
}
