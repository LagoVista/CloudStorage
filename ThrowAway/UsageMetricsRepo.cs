// --- BEGIN CODE INDEX META (do not edit) ---
// ContentHash: aa035a006cde54719524e79db696f6faa24b444cb8bd8c21e48684ea5273a4a5
// IndexVersion: 0
// --- END CODE INDEX META ---
using LagoVista.CloudStorage.Storage;
using LagoVista.IoT.Logging.Loggers;
using System;
using LagoVista.Core.PlatformSupport;
using LagoVista.Core.Validation;
using LagoVista.IoT.Logging;
using System.Collections.Generic;
using LagoVista.Core.Models.UIMetaData;
using System.Threading.Tasks;

namespace ThrowAway
{
    public class UsageMetricsRepo : TableStorageBase<UsageMetrics>
    {
        public UsageMetricsRepo(String accountName, string accountKey, IAdminLogger logger) : base(accountName, accountKey, logger)
        {

        }

        public Task<ListResponse<UsageMetrics>> GetByPage(String id, ListRequest listRequest)
        {
            return GetPagedResultsAsync(id, listRequest);
        }
    }

}