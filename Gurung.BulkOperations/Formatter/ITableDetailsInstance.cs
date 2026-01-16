using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gurung.BulkOperations.Formatter
{
    public interface ITableDetailsInstance
    {
        TableDetails GenerateInstance<T>(DbContext context, IEnumerable<T> entities);
    }
}
