using LagoVista.CloudStorage.Interfaces;
using LagoVista.CloudStorage.Models;
using LagoVista.CloudStorage.Storage.ConnectionSettings;
using LagoVista.Core.Models.UIMetaData;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.CloudStorage.Storage.StorageProviders.Mongo
{
    public sealed class MongoAdminRepo : IMongoAdminRepo
    {
        private static readonly HashSet<string> SystemDatabases =
            new HashSet<string>(new[] { "admin", "config", "local" }, StringComparer.OrdinalIgnoreCase);

        private readonly IMongoDocumentStorageConnectionSettings _settings;
        private readonly IMongoStorageClientFactory _clientFactory;
        private readonly JsonWriterSettings _jsonWriterSettings =
            new JsonWriterSettings { OutputMode = JsonOutputMode.RelaxedExtendedJson };

        public MongoAdminRepo(
            IMongoDocumentStorageConnectionSettings settings,
            IMongoStorageClientFactory clientFactory)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _clientFactory = clientFactory ?? throw new ArgumentNullException(nameof(clientFactory));
        }

        public async Task<ListResponse<MongoDatabaseInfo>> GetDatabasesAsync(
            ListRequest listRequest,
            CancellationToken cancellationToken = default)
        {
            var request = NormalizeListRequest(listRequest);
            var client = GetClient();

            using (var cursor = await client.ListDatabaseNamesAsync(cancellationToken: cancellationToken).ConfigureAwait(false))
            {
                var databases = (await cursor.ToListAsync(cancellationToken).ConfigureAwait(false))
                    .Where(name => !SystemDatabases.Contains(name))
                    .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                    .Select(name => new MongoDatabaseInfo { Name = name })
                    .ToList();

                return CreateListResponse(databases, request);
            }
        }

        public async Task<ListResponse<MongoCollectionInfo>> GetCollectionsAsync(
            string databaseName,
            ListRequest listRequest,
            CancellationToken cancellationToken = default)
        {
            var request = NormalizeListRequest(listRequest);
            var database = GetDatabase(databaseName);

            using (var cursor = await database.ListCollectionNamesAsync(cancellationToken: cancellationToken).ConfigureAwait(false))
            {
                var names = (await cursor.ToListAsync(cancellationToken).ConfigureAwait(false))
                    .Where(name => !name.StartsWith("system.", StringComparison.OrdinalIgnoreCase))
                    .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                var all = new List<MongoCollectionInfo>();
                foreach (var name in names)
                {
                    var collection = database.GetCollection<BsonDocument>(name);
                    var count = await collection.EstimatedDocumentCountAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
                    all.Add(new MongoCollectionInfo
                    {
                        Name = name,
                        EstimatedDocumentCount = count
                    });
                }

                return CreateListResponse(all, request);
            }
        }

        public async Task<ListResponse<MongoDocumentInfo>> QueryAsync(
            string databaseName,
            string collectionName,
            MongoQueryRequest request,
            CancellationToken cancellationToken = default)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));

            NormalizeListRequest(request);

            var collection = GetCollection(databaseName, collectionName);
            var filter = ParseDocument(request.Filter, "filter", allowEmpty: true);
            var pageSize = Math.Min(request.PageSize, 500);
            var skip = (request.PageIndex - 1) * pageSize;

            var matchedCount = await collection.CountDocumentsAsync(
                filter,
                cancellationToken: cancellationToken).ConfigureAwait(false);

            var find = collection.Find(filter).Skip(skip).Limit(pageSize);

            if (!String.IsNullOrWhiteSpace(request.Sort))
            {
                find = find.Sort(ParseDocument(request.Sort, "sort", allowEmpty: false));
            }

            List<BsonDocument> documents;
            if (!String.IsNullOrWhiteSpace(request.Projection))
            {
                documents = await find
                    .Project<BsonDocument>(ParseDocument(request.Projection, "projection", allowEmpty: false))
                    .ToListAsync(cancellationToken)
                    .ConfigureAwait(false);
            }
            else
            {
                documents = await find.ToListAsync(cancellationToken).ConfigureAwait(false);
            }

            var items = documents.Select(document => new MongoDocumentInfo
            {
                Id = GetDocumentId(document),
                Json = ToJson(document)
            }).ToList();

            var response = ListResponse<MongoDocumentInfo>.Create(items, request);
            response.RecordCount = matchedCount > Int32.MaxValue ? Int32.MaxValue : (int)matchedCount;
            response.PageSize = pageSize;
            response.PageIndex = request.PageIndex;
            response.PageCount = matchedCount == 0 ? 0 : (int)Math.Ceiling(matchedCount / (double)pageSize);
            response.HasMoreRecords = (long)skip + items.Count < matchedCount;
            return response;
        }

        public async Task<string> GetDocumentAsync(
            string databaseName,
            string collectionName,
            string id,
            CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(id)) throw new ArgumentNullException(nameof(id));

            var collection = GetCollection(databaseName, collectionName);
            FilterDefinition<BsonDocument> filter = Builders<BsonDocument>.Filter.Eq("_id", id.Trim());

            if (ObjectId.TryParse(id.Trim(), out var objectId))
            {
                filter = Builders<BsonDocument>.Filter.Or(
                    filter,
                    Builders<BsonDocument>.Filter.Eq("_id", objectId));
            }

            var document = await collection.Find(filter).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
            return document == null ? null : ToJson(document);
        }

        public async Task<MongoPatchResult> PatchManyAsync(
            string databaseName,
            string collectionName,
            MongoPatchRequest request,
            CancellationToken cancellationToken = default)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));

            var filter = ParseDocument(request.Filter, "filter", allowEmpty: true);
            var patch = ParseDocument(request.Patch, "patch", allowEmpty: false);

            if (patch.ElementCount == 0 || patch.Elements.Any(element => !element.Name.StartsWith("$", StringComparison.Ordinal)))
                throw new InvalidOperationException("Patch must contain Mongo update operators such as $set or $unset.");

            var collection = GetCollection(databaseName, collectionName);
            var matchedCount = await collection.CountDocumentsAsync(filter, cancellationToken: cancellationToken).ConfigureAwait(false);

            if (request.ExpectedMatchCount.HasValue && request.ExpectedMatchCount.Value != matchedCount)
                throw new InvalidOperationException(
                    $"Patch expected {request.ExpectedMatchCount.Value} matching documents but found {matchedCount}. No documents were changed.");

            var result = await collection.UpdateManyAsync(
                filter,
                new BsonDocumentUpdateDefinition<BsonDocument>(patch),
                cancellationToken: cancellationToken).ConfigureAwait(false);

            return new MongoPatchResult
            {
                MatchedCount = result.MatchedCount,
                ModifiedCount = result.ModifiedCount
            };
        }

        private static ListResponse<T> CreateListResponse<T>(IReadOnlyList<T> items, ListRequest request)
            where T : class
        {
            var skip = (request.PageIndex - 1) * request.PageSize;
            var page = items.Skip(skip).Take(request.PageSize).ToList();
            var response = ListResponse<T>.Create(page, request);
            response.RecordCount = items.Count;
            response.PageCount = items.Count == 0 ? 0 : (int)Math.Ceiling(items.Count / (double)request.PageSize);
            response.HasMoreRecords = skip + page.Count < items.Count;
            return response;
        }

        private static ListRequest NormalizeListRequest(ListRequest request)
        {
            if (request == null)
                request = ListRequest.Create(1, 100);

            if (request.PageIndex <= 0)
                request.PageIndex = 1;

            if (request.PageSize <= 0)
                request.PageSize = 100;

            request.PageSize = Math.Min(request.PageSize, 500);
            return request;
        }

        private static string GetDocumentId(BsonDocument document)
        {
            if (!document.TryGetValue("_id", out var id) || id == null || id.IsBsonNull)
                return null;

            return id.IsString ? id.AsString : id.ToString();
        }

        private IMongoClient GetClient()
        {
            return _clientFactory.GetClient(_settings.BuildConnectionString());
        }

        private IMongoDatabase GetDatabase(string databaseName)
        {
            if (String.IsNullOrWhiteSpace(databaseName)) throw new ArgumentNullException(nameof(databaseName));
            if (SystemDatabases.Contains(databaseName.Trim()))
                throw new InvalidOperationException($"Database '{databaseName}' is not available through Mongo admin tools.");

            return _clientFactory.GetDatabase(_settings.BuildConnectionString(), databaseName.Trim());
        }

        private IMongoCollection<BsonDocument> GetCollection(string databaseName, string collectionName)
        {
            if (String.IsNullOrWhiteSpace(collectionName)) throw new ArgumentNullException(nameof(collectionName));
            if (collectionName.StartsWith("system.", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("System collections are not available through Mongo admin tools.");

            return GetDatabase(databaseName).GetCollection<BsonDocument>(collectionName.Trim());
        }

        private static BsonDocument ParseDocument(string json, string fieldName, bool allowEmpty)
        {
            if (String.IsNullOrWhiteSpace(json))
            {
                if (allowEmpty) return new BsonDocument();
                throw new InvalidOperationException($"{fieldName} is required.");
            }

            try
            {
                return BsonDocument.Parse(json);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Invalid Mongo {fieldName} JSON: {ex.Message}", ex);
            }
        }

        private string ToJson(BsonDocument document)
        {
            return document.ToJson(_jsonWriterSettings);
        }
    }
}
