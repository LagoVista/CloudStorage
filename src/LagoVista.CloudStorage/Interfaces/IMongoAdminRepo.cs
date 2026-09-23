using LagoVista.CloudStorage.Models;
using LagoVista.Core.Models.UIMetaData;
using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.CloudStorage.Interfaces
{
    public interface IMongoAdminRepo
    {
        Task<ListResponse<MongoDatabaseInfo>> GetDatabasesAsync(ListRequest listRequest, CancellationToken cancellationToken = default);
        Task<ListResponse<MongoCollectionInfo>> GetCollectionsAsync(string databaseName, ListRequest listRequest, CancellationToken cancellationToken = default);
        Task<ListResponse<MongoDocumentInfo>> QueryAsync(string databaseName, string collectionName, MongoQueryRequest request, CancellationToken cancellationToken = default);
        Task<string> GetDocumentAsync(string databaseName, string collectionName, string id, CancellationToken cancellationToken = default);
        Task<MongoPatchResult> PatchManyAsync(string databaseName, string collectionName, MongoPatchRequest request, CancellationToken cancellationToken = default);
    }
}
