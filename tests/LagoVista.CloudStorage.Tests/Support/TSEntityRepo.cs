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
