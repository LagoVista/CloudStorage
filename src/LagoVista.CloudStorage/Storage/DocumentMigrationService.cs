using LagoVista.CloudStorage.Interfaces;
using Microsoft.Azure.Cosmos;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.CloudStorage.DocumentDB
{
    public sealed class DocumentMigrationService : IDocumentMigrationService
    {
        private static readonly string[] _cosmosSystemFields = { "_rid", "_self", "_etag", "_attachments", "_ts" };

        private readonly ICosmosClientProvider _cosmosClientProvider;
        private readonly IDocumentCollectionNameResolver _collectionNameResolver;

        public DocumentMigrationService(ICosmosClientProvider cosmosClientProvider, IDocumentCollectionNameResolver collectionNameResolver)
        {
            _cosmosClientProvider = cosmosClientProvider ?? throw new ArgumentNullException(nameof(cosmosClientProvider));
            _collectionNameResolver = collectionNameResolver ?? throw new ArgumentNullException(nameof(collectionNameResolver));
        }

        public async Task<CosmosToMongoMigrationResult> MigrateCosmosToMongoAsync(CosmosToMongoMigrationRequest request, CancellationToken cancellationToken = default)
        {
            ValidateRequest(request);

            var result = new CosmosToMongoMigrationResult { DryRun = request.DryRun };
            var sourceCollectionName = String.IsNullOrWhiteSpace(request.SourceCollectionName) ? $"{request.Source.DatabaseName}_Collections" : request.SourceCollectionName;
            var cosmosClient = _cosmosClientProvider.GetClient(request.Source.Endpoint, request.Source.SharedKey);
            var container = cosmosClient.GetContainer(request.Source.DatabaseName, sourceCollectionName);
            var query = CreateQuery(request.EntityType);
            var iterator = container.GetItemQueryIterator<JObject>(query, request.ContinuationToken, new QueryRequestOptions { MaxItemCount = request.BatchSize });
            var mongoClient = request.DryRun ? null : new MongoClient(request.Target.ConnectionString);
            var mongoDatabase = mongoClient?.GetDatabase(request.Target.DatabaseName);

            while (iterator.HasMoreResults && (request.MaxPages <= 0 || result.PagesRead < request.MaxPages))
            {
                var response = await iterator.ReadNextAsync(cancellationToken).ConfigureAwait(false);
                result.PagesRead++;
                result.ContinuationToken = response.ContinuationToken;

                var documentsByCollection = new Dictionary<string, List<BsonDocument>>(StringComparer.OrdinalIgnoreCase);
                foreach (var sourceDocument in response)
                {
                    result.DocumentsRead++;
                    var entityType = GetString(sourceDocument, "EntityType") ?? String.Empty;
                    var routeResolved = _collectionNameResolver.TryResolve(request.Target.DatabaseName, entityType, out var collectionName);
                    if (!routeResolved) result.UnresolvedRoutes++;

                    var route = GetRoute(result, entityType, collectionName);
                    route.Read++;
                    if (!routeResolved) route.UnresolvedRoute++;

                    if (!TryTransform(sourceDocument, out var targetDocument))
                    {
                        result.DocumentsSkipped++;
                        result.DocumentsFailed++;
                        route.Failed++;
                        continue;
                    }

                    if (request.DryRun) continue;
                    if (!documentsByCollection.TryGetValue(collectionName, out var collectionDocuments))
                    {
                        collectionDocuments = new List<BsonDocument>();
                        documentsByCollection[collectionName] = collectionDocuments;
                    }

                    collectionDocuments.Add(targetDocument);
                }

                if (!request.DryRun)
                {
                    foreach (var collectionBatch in documentsByCollection)
                    {
                        var collection = mongoDatabase.GetCollection<BsonDocument>(collectionBatch.Key);
                        var writes = collectionBatch.Value.Select(document => new ReplaceOneModel<BsonDocument>(Builders<BsonDocument>.Filter.Eq("_id", document["_id"]), document) { IsUpsert = true }).Cast<WriteModel<BsonDocument>>().ToList();
                        if (writes.Count == 0) continue;

                        await collection.BulkWriteAsync(writes, new BulkWriteOptions { IsOrdered = false }, cancellationToken).ConfigureAwait(false);
                        result.DocumentsWritten += writes.Count;
                        foreach (var document in collectionBatch.Value)
                        {
                            var entityType = document.Contains("EntityType") ? document["EntityType"].ToString() : String.Empty;
                            GetRoute(result, entityType, collectionBatch.Key).Written++;
                        }
                    }
                }
            }

            result.Completed = !iterator.HasMoreResults;
            if (result.Completed) result.ContinuationToken = null;
            return result;
        }

        private static QueryDefinition CreateQuery(string entityType)
        {
            if (String.IsNullOrWhiteSpace(entityType)) return new QueryDefinition("SELECT * FROM c");
            return new QueryDefinition("SELECT * FROM c WHERE c.EntityType = @entityType").WithParameter("@entityType", entityType);
        }

        private static bool TryTransform(JObject source, out BsonDocument target)
        {
            target = null;
            var id = GetString(source, "id");
            if (String.IsNullOrWhiteSpace(id)) return false;

            var copy = (JObject)source.DeepClone();
            RemoveProperty(copy, "id");
            foreach (var field in _cosmosSystemFields) RemoveProperty(copy, field);

            target = BsonDocument.Parse(copy.ToString(Formatting.None));
            target.InsertAt(0, new BsonElement("_id", id));
            return true;
        }

        private static DocumentMigrationRouteStatistics GetRoute(CosmosToMongoMigrationResult result, string entityType, string collectionName)
        {
            var route = result.Routes.FirstOrDefault(item => String.Equals(item.EntityType, entityType, StringComparison.OrdinalIgnoreCase) && String.Equals(item.CollectionName, collectionName, StringComparison.OrdinalIgnoreCase));
            if (route != null) return route;

            route = new DocumentMigrationRouteStatistics
            {
                EntityType = entityType,
                CollectionName = collectionName
            };
            result.Routes.Add(route);
            return route;
        }

        private static string GetString(JObject document, string propertyName)
        {
            var property = document.Properties().FirstOrDefault(item => String.Equals(item.Name, propertyName, StringComparison.OrdinalIgnoreCase));
            return property?.Value?.Type == JTokenType.Null ? null : property?.Value?.ToString();
        }

        private static void RemoveProperty(JObject document, string propertyName)
        {
            var property = document.Properties().FirstOrDefault(item => String.Equals(item.Name, propertyName, StringComparison.OrdinalIgnoreCase));
            property?.Remove();
        }

        private static void ValidateRequest(CosmosToMongoMigrationRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (request.Source == null) throw new InvalidOperationException("Cosmos source settings are required.");
            if (String.IsNullOrWhiteSpace(request.Source.Endpoint)) throw new InvalidOperationException("Cosmos source endpoint is required.");
            if (String.IsNullOrWhiteSpace(request.Source.SharedKey)) throw new InvalidOperationException("Cosmos source shared key is required.");
            if (String.IsNullOrWhiteSpace(request.Source.DatabaseName)) throw new InvalidOperationException("Cosmos source database name is required.");
            if (request.Target == null) throw new InvalidOperationException("Mongo target settings are required.");
            DocumentStorageSettingsResolver.Resolve(request.Source.Endpoint, request.Source.SharedKey, request.Source.DatabaseName, "mongo", request.Target);
            if (request.BatchSize <= 0) throw new InvalidOperationException("Migration batch size must be greater than zero.");
            if (request.MaxPages < 0) throw new InvalidOperationException("Migration maximum page count cannot be negative.");
        }
    }
}
