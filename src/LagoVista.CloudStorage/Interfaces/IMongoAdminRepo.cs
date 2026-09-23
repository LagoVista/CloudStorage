using LagoVista.CloudStorage.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.CloudStorage.Interfaces
{
    public interface IMongoAdminRepo
    {
        Task<IReadOnlyList<string>> GetDatabasesAsync(CancellationToken cancellationToken = default);
        Task<IReadOnlyList<MongoCollectionInfo>> GetCollectionsAsync(string databaseName, CancellationToken cancellationToken = default);
        Task<MongoQueryResult> QueryAsync(string databaseName, string collectionName, MongoQueryRequest request, CancellationToken cancellationToken = default);
        Task<string> GetDocumentAsync(string databaseName, string collectionName, string id, CancellationToken cancellationToken = default);
        Task<MongoPatchResult> PatchManyAsync(string databaseName, string collectionName, MongoPatchRequest request, CancellationToken cancellationToken = default);
    }
}
