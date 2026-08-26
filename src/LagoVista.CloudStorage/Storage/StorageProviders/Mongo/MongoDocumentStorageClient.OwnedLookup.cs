using LagoVista.Core.Exceptions;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json.Linq;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.CloudStorage.StorageProviders
{
    public sealed partial class MongoDocumentStorageClient
    {
        public async Task<TProjection> GetOwnedDocumentProjectionAsync<TProjection>(string id, string ownerOrganizationId, bool throwOnNotFound = true, CancellationToken cancellationToken = default) where TProjection : class
        {
            if (String.IsNullOrWhiteSpace(id)) throw new ArgumentException("Document id is required.", nameof(id));
            if (String.IsNullOrWhiteSpace(ownerOrganizationId)) throw new ArgumentException("Owner organization id is required.", nameof(ownerOrganizationId));

            var collectionName = _collectionNameResolver.GetFallback(_settings.DatabaseName);
            if (typeof(TProjection) == typeof(JObject))
            {
                var filter = Builders<BsonDocument>.Filter.And(Builders<BsonDocument>.Filter.Eq("_id", id.Trim()), Builders<BsonDocument>.Filter.Eq("OwnerOrganization.Id", ownerOrganizationId));
                var document = await GetBsonCollection(collectionName).Find(filter).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
                if (document != null) return (TProjection)(object)ToJObject(document);
            }
            else
            {
                var filter = Builders<TProjection>.Filter.And(Builders<TProjection>.Filter.Eq("_id", id.Trim()), Builders<TProjection>.Filter.Eq("OwnerOrganization.Id", ownerOrganizationId));
                var projection = await GetProjectionCollection<TProjection>().Find(filter).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
                if (projection != null) return projection;
            }

            if (throwOnNotFound) throw new RecordNotFoundException(typeof(TProjection).Name, id);
            return null;
        }
    }
}