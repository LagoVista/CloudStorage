using LagoVista.CloudStorage.Storage;
using LagoVista.IoT.Logging.Loggers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LagoVista.CloudStorage.Tests.Support
{
    public class TSEntityRepo : TableStorageBase<TSEntity>
    {
        public TSEntityRepo(string accountName, string accountKey, IAdminLogger logger) : base(accountName, accountKey, logger)
        {
        }
    }
}
