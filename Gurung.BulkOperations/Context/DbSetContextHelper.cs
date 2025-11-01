using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gurung.BulkOperations.Context
{
    internal static class DbSetContextHelper
    {
        internal static DbContext GetDbContext<T>(this DbSet<T> dbSet)
            where T : class
        {
            var infrastructure = dbSet as Microsoft.EntityFrameworkCore.Infrastructure.IInfrastructure<IServiceProvider>;
            var serviceProvider = infrastructure.Instance;
            var currentDbContext = serviceProvider.GetService(typeof(Microsoft.EntityFrameworkCore.Infrastructure.ICurrentDbContext))
                as Microsoft.EntityFrameworkCore.Infrastructure.ICurrentDbContext;
            return currentDbContext?.Context
                  ?? throw new InvalidOperationException("Unable to retrieve the current DbContext from DbSet.");
        }
    }
}
