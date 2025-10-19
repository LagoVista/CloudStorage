// --- BEGIN CODE INDEX META (do not edit) ---
// ContentHash: 2f8ef7b847ab12b6d0ddca187d91d7c6ce170c513bcda271452ce4998561c143
// IndexVersion: 0
// --- END CODE INDEX META ---
using LagoVista.CloudStorage.Storage;
using LagoVista.Core.Models.UIMetaData;
using LagoVista.Core.Models;
using LagoVista.IoT.Logging.Loggers;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace LagoVista.CloudStorage.Tests.Support
{
    public class TSEntityRepo : TableStorageBase<TSEntity>
    {
        public TSEntityRepo(string accountName, string accountKey, IAdminLogger logger) : base(accountName, accountKey, logger)
        {
        }
    
        public Task AddTSEtnityAsync(TSEntity entity)
        {
            return InsertAsync(entity);
        }

        public async Task<IEnumerable<TSEntity>> ReadMany(string id)
        {
            var results = await GetPagedResultsAsync(ListRequest.CreateForAll(), FilterOptions.Create(nameof(TSEntity.Value1), FilterOptions.Operators.Equals, "ONE"));
            return results.Model;

        }

    }
}
