using LagoVista.CloudStorage.Interfaces;
using Microsoft.Azure.Cosmos;
using MongoDB.Bson;
using MongoDB.Driver;
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
            var targetCollectionName = _collectionNameResolver.GetFallback(request.Target.DatabaseName);
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
                    var route = GetRoute(result, entityType, targetCollectionName);
                    route.Read++;

                    if (!DocumentMigrationTransformer.TryTransform(sourceDocument, out var targetDocument))
                    {
                        result.DocumentsSkipped++;
                        result.DocumentsFailed++;
                        route.Failed++;
                        continue;
                    }

                    if (request.DryRun) continue;
                    if (!documentsByCollection.TryGetValue(targetCollectionName, out var collectionDocuments))
                    {
                        collectionDocuments = new List<BsonDocument>();
                        documentsByCollection[targetCollectionName] = collectionDocuments;
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

        public async Task<CosmosToMongoValidationResult> ValidateCosmosToMongoAsync(CosmosToMongoMigrationRequest request, CancellationToken cancellationToken = default)
        {
            ValidateRequest(request);

            var inventoryRequest = new CosmosToMongoMigrationRequest
            {
                Source = request.Source,
                Target = request.Target,
                SourceCollectionName = request.SourceCollectionName,
                EntityType = request.EntityType,
                BatchSize = request.BatchSize,
                DryRun = true
            };

            var inventory = await MigrateCosmosToMongoAsync(inventoryRequest, cancellationToken).ConfigureAwait(false);
            var mongoClient = new MongoClient(request.Target.ConnectionString);
            var mongoDatabase = mongoClient.GetDatabase(request.Target.DatabaseName);
            var result = new CosmosToMongoValidationResult();

            foreach (var route in inventory.Routes)
            {
                var collection = mongoDatabase.GetCollection<BsonDocument>(route.CollectionName);
                var filter = CreateMongoEntityTypeFilter(route.EntityType);
                var destinationCount = await collection.CountDocumentsAsync(filter, cancellationToken: cancellationToken).ConfigureAwait(false);
                var item = new DocumentMigrationValidationStatistics
                {
                    EntityType = route.EntityType,
                    CollectionName = route.CollectionName,
                    SourceCount = route.Read,
                    DestinationCount = destinationCount
                };
                result.Routes.Add(item);
            }

            result.SourceCount = result.Routes.Sum(item => item.SourceCount);
            result.DestinationCount = result.Routes.Sum(item => item.DestinationCount);
            result.Matches = result.Routes.All(item => item.Matches);
            return result;
        }

        private static QueryDefinition CreateQuery(string entityType)
        {
            if (String.IsNullOrWhiteSpace(entityType)) return new QueryDefinition("SELECT * FROM c");
            return new QueryDefinition("SELECT * FROM c WHERE c.EntityType = @entityType").WithParameter("@entityType", entityType);
        }

        private static FilterDefinition<BsonDocument> CreateMongoEntityTypeFilter(string entityType)
        {
            if (!String.IsNullOrWhiteSpace(entityType)) return Builders<BsonDocument>.Filter.Eq("EntityType", entityType);
            return Builders<BsonDocument>.Filter.Or(Builders<BsonDocument>.Filter.Exists("EntityType", false), Builders<BsonDocument>.Filter.Eq("EntityType", BsonNull.Value), Builders<BsonDocument>.Filter.Eq("EntityType", String.Empty));
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
