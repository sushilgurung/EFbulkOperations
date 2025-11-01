//using Microsoft.EntityFrameworkCore;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace Gurung.BulkOperations.Extensions
//{
//    public static class DbContextBulkExtensions
//    {
//        public static Task BulkInsertAsync<T>(this DbContext context, IEnumerable<T> entities, BulkConfig bulkConfig = null)
//        {
//            bulkConfig.dataHandler= SqlDataHandlerFactory.CreateDataHandler(context);
//            return BulkTransactionManager.InsertAsync(context, entities, bulkConfig, CancellationToken.None);
//        }
//    }
//}
