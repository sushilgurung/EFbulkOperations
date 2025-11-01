using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gurung.BulkOperations.SqlDataHandler
{
    public interface ISqlDataHandler
    {
        public Task BulkInsertAsync<T>(DbContext context, IEnumerable<T> entities, BulkConfig bulkConfig = null, CancellationToken cancellationToken = default);

        public Task BulkUpDateAsync<T>(
                DbContext context,
                IEnumerable<T> entities,
                BulkConfig bulkConfig = null,
                CancellationToken cancellationToken = default);

        public Task BulkInsertOrUpDateAsync<T>(
            DbContext context,
            IEnumerable<T> entities,
            BulkConfig bulkConfig = null,
            CancellationToken cancellationToken = default);
    }
}
