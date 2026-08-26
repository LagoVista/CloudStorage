using LagoVista.CloudStorage.Exceptions;
using LagoVista.CloudStorage.Models;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.CloudStorage.StorageProviders
{
    public sealed partial class MongoDocumentStorageClient
    {
        public async Task<SyncUpsertResult> UpsertDocumentAsync(JObject document, string expectedETag = null, CancellationToken cancellationToken = default)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            var id = document.Value<string>("id");
            var entityType = document.Value<string>("EntityType");
            if (String.IsNullOrWhiteSpace(id)) throw new ArgumentException("Document id is required.", nameof(document));
            if (String.IsNullOrWhiteSpace(entityType)) throw new ArgumentException("Document EntityType is required.", nameof(document));
            if (!_collectionNameResolver.TryResolve(_settings.DatabaseName, entityType, out var collectionName)) throw new InvalidOperationException($"Could not resolve Mongo collection for entity type '{entityType}'.");

            var bson = BsonDocument.Parse(document.ToString(Formatting.None));
            bson.Remove("id");
            bson["_id"] = id;
            bson.Remove("_etag");
            var newETag = CreateETag();
            bson["ETag"] = newETag;

            var filter = Builders<BsonDocument>.Filter.And(Builders<BsonDocument>.Filter.Eq("_id", id), Builders<BsonDocument>.Filter.Eq("EntityType", entityType));
            if (!String.IsNullOrWhiteSpace(expectedETag)) filter &= Builders<BsonDocument>.Filter.Eq("ETag", expectedETag);

            var result = await GetBsonCollection(collectionName).ReplaceOneAsync(filter, bson, new ReplaceOptions { IsUpsert = String.IsNullOrWhiteSpace(expectedETag) }, cancellationToken).ConfigureAwait(false);
            if (!String.IsNullOrWhiteSpace(expectedETag) && result.MatchedCount == 0) throw new ContentModifiedException { EntityType = entityType, Id = id };

            return new SyncUpsertResult { Id = id, ETag = newETag, StatusCode = result.UpsertedId != null ? 201 : 200 };
        }
    }
}