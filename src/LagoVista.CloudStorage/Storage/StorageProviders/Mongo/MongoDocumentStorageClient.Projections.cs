using LagoVista.Core.Exceptions;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.CloudStorage.StorageProviders
{
    public sealed partial class MongoDocumentStorageClient
    {
        public async Task<TProjection> GetDocumentProjectionAsync<TProjection>(string id, bool throwOnNotFound = true, CancellationToken cancellationToken = default)
            where TProjection : class
        {
            if (String.IsNullOrWhiteSpace(id)) throw new ArgumentException("Document id is required.", nameof(id));

            if (typeof(TProjection) == typeof(JObject))
            {
                var collectionName = _collectionNameResolver.GetFallback(_settings.DatabaseName);
                var document = await GetBsonCollection(collectionName)
                    .Find(Builders<BsonDocument>.Filter.Eq("_id", id))
                    .FirstOrDefaultAsync(cancellationToken)
                    .ConfigureAwait(false);

                if (document == null)
                {
                    if (throwOnNotFound) throw new RecordNotFoundException(typeof(TProjection).Name, id);
                    return null;
                }

                return (TProjection)(object)ToJObject(document);
            }

            var projection = await GetProjectionCollection<TProjection>()
                .Find(Builders<TProjection>.Filter.Eq("_id", id))
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);

            if (projection == null && throwOnNotFound)
                throw new RecordNotFoundException(typeof(TProjection).Name, id);

            return projection;
        }

        public async Task<TProjection> GetDocumentProjectionAsync<TProjection>(string entityType, string id, bool throwOnNotFound = true, CancellationToken cancellationToken = default)
            where TProjection : class
        {
            if (String.IsNullOrWhiteSpace(entityType)) throw new ArgumentException("Entity type is required.", nameof(entityType));
            if (String.IsNullOrWhiteSpace(id)) throw new ArgumentException("Document id is required.", nameof(id));

            if (typeof(TProjection) == typeof(JObject))
            {
                if (!_collectionNameResolver.TryResolve(_settings.DatabaseName, entityType, out var collectionName))
                    throw new InvalidOperationException($"Could not resolve Mongo collection for entity type '{entityType}'.");

                var filter = Builders<BsonDocument>.Filter.And(
                    Builders<BsonDocument>.Filter.Eq("_id", id),
                    Builders<BsonDocument>.Filter.Eq("EntityType", entityType));

                var document = await GetBsonCollection(collectionName)
                    .Find(filter)
                    .FirstOrDefaultAsync(cancellationToken)
                    .ConfigureAwait(false);

                if (document == null)
                {
                    if (throwOnNotFound) throw new RecordNotFoundException(entityType, id);
                    return null;
                }

                return (TProjection)(object)ToJObject(document);
            }

            var typedFilter = Builders<TProjection>.Filter.And(
                Builders<TProjection>.Filter.Eq("_id", id),
                Builders<TProjection>.Filter.Eq("EntityType", entityType));

            var projection = await GetProjectionCollection<TProjection>(entityType)
                .Find(typedFilter)
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);

            if (projection == null && throwOnNotFound)
                throw new RecordNotFoundException(entityType, id);

            return projection;
        }

        public async Task<IEnumerable<TProjection>> GetDocumentProjectionsAsync<TProjection>(string entityType, Expression<Func<TProjection, bool>> query, CancellationToken cancellationToken = default)
            where TProjection : class
        {
            if (String.IsNullOrWhiteSpace(entityType)) throw new ArgumentException("Entity type is required.", nameof(entityType));
            if (query == null) throw new ArgumentNullException(nameof(query));

            var filter = Builders<TProjection>.Filter.And(
                Builders<TProjection>.Filter.Eq("EntityType", entityType),
                Builders<TProjection>.Filter.Where(query));

            return await GetProjectionCollection<TProjection>(entityType)
                .Find(filter)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
        }

        private IMongoCollection<TProjection> GetProjectionCollection<TProjection>()
            where TProjection : class
        {
            var collectionName = _collectionNameResolver.GetFallback(_settings.DatabaseName);
            return _clientFactory
                .GetDatabase(_settings.BuildConnectionString(), _settings.DatabaseName)
                .GetCollection<TProjection>(collectionName);
        }

        private IMongoCollection<TProjection> GetProjectionCollection<TProjection>(string entityType)
            where TProjection : class
        {
            if (!_collectionNameResolver.TryResolve(_settings.DatabaseName, entityType, out var collectionName))
                throw new InvalidOperationException($"Could not resolve Mongo collection for entity type '{entityType}'.");

            return _clientFactory
                .GetDatabase(_settings.BuildConnectionString(), _settings.DatabaseName)
                .GetCollection<TProjection>(collectionName);
        }
    }
}
