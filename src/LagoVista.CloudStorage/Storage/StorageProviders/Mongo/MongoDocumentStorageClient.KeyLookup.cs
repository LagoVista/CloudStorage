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
        public async Task<TProjection> GetDocumentProjectionByKeyAsync<TProjection>(string entityType, string key, string ownerOrganizationId, bool throwOnNotFound = true, CancellationToken cancellationToken = default) where TProjection : class
        {
            if (String.IsNullOrWhiteSpace(entityType)) throw new ArgumentException("Entity type is required.", nameof(entityType));
            if (String.IsNullOrWhiteSpace(key)) throw new ArgumentException("Document key is required.", nameof(key));
            if (String.IsNullOrWhiteSpace(ownerOrganizationId)) throw new ArgumentException("Owner organization id is required.", nameof(ownerOrganizationId));
            if (!_collectionNameResolver.TryResolve(_settings.DatabaseName, entityType, out var collectionName)) throw new InvalidOperationException($"Could not resolve Mongo collection for entity type '{entityType}'.");

            if (typeof(TProjection) == typeof(JObject))
            {
                var filter = Builders<BsonDocument>.Filter.And(Builders<BsonDocument>.Filter.Eq("EntityType", entityType), Builders<BsonDocument>.Filter.Eq("Key", key.Trim()), Builders<BsonDocument>.Filter.Eq("OwnerOrganization.Id", ownerOrganizationId));
                var document = await GetBsonCollection(collectionName).Find(filter).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
                if (document != null) return (TProjection)(object)ToJObject(document);
            }
            else
            {
                var filter = Builders<TProjection>.Filter.And(Builders<TProjection>.Filter.Eq("EntityType", entityType), Builders<TProjection>.Filter.Eq("Key", key.Trim()), Builders<TProjection>.Filter.Eq("OwnerOrganization.Id", ownerOrganizationId));
                var projection = await GetProjectionCollection<TProjection>(entityType).Find(filter).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
                if (projection != null) return projection;
            }

            if (throwOnNotFound) throw new RecordNotFoundException(entityType, key);
            return null;
        }
    }
}