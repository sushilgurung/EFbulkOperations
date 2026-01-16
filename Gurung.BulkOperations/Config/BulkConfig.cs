using Gurung.BulkOperations.SqlDataHandler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gurung.BulkOperations
{
    public class BulkConfig
    {
        public BulkConfig()
        {
            WithHoldlock = true;
            KeepIdentity = false;
            BatchSize = 0;
        }
        public int BatchSize { get; set; } = 0;
        public bool WithHoldlock { get; set; } = true;
        /// <summary>
        /// 
        /// </summary>
        public bool KeepIdentity { get; set; } = false;
        public ISqlDataHandler dataHandler { get; set; }
        public int BulkCopyTimeout { get; set; }
        public int NotifyAfter { get; set; }
    }
}
