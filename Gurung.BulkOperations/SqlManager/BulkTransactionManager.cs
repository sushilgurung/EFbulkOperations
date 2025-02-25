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
    public class BulkTransactionManager
    {
        public static async Task InsertAsync<T>(
            DbContext context,
            IEnumerable<T> entities,
            BulkConfig bulkConfig = null,
            CancellationToken cancellationToken = default
            )
        {
            ISqlDataHandler sqlDataHandler = bulkConfig.dataHandler;
            await sqlDataHandler.BulkInsertAsync(context, entities, bulkConfig, cancellationToken);
        }
    }
}
