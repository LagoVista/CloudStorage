using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using LagoVista.Core;
using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.CloudStorage.Storage.StorageProviders.Mongo
{
    [CriticalCoverage]
    internal sealed class MongoMutableRecordStore
    {
        private const string ScratchExpirationField = "_storageExpiresUtc";
        private const string ScratchExpirationIndexName = "ix_storage_expires_utc";

        private readonly IMongoDatabase _database;
        private readonly IServiceProvider _serviceProvider;
        private readonly ConcurrentDictionary<string, Task> _initializers = new ConcurrentDictionary<string, Task>(StringComparer.Ordinal);

        public MongoMutableRecordStore(IMongoDatabase database, IServiceProvider serviceProvider)
        {
            _database = database ?? throw new ArgumentNullException(nameof(database));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            MongoBsonSerialization.Configure();
        }

        public async Task<TRecord> GetAsync<TRecord>(StorageKey key, CancellationToken cancellationToken)
            where TRecord : class
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

            var collection = await GetCollectionAsync<TRecord>(cancellationToken).ConfigureAwait(false);
            return await collection.Find(BuildKeyFilter<TRecord>(key)).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
        }

        public async Task InsertAsync<TRecord>(TRecord record, CancellationToken cancellationToken)
            where TRecord : class
        {
            if (record == null) throw new ArgumentNullException(nameof(record));

            var collection = await GetCollectionAsync<TRecord>(cancellationToken).ConfigureAwait(false);
            await collection.InsertOneAsync(record, null, cancellationToken).ConfigureAwait(false);
        }

        public async Task ReplaceAsync<TRecord>(StorageKey key, TRecord record, bool upsert, CancellationToken cancellationToken)
            where TRecord : class
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            if (record == null) throw new ArgumentNullException(nameof(record));

            var collection = await GetCollectionAsync<TRecord>(cancellationToken).ConfigureAwait(false);
            var result = await collection.ReplaceOneAsync(
                BuildKeyFilter<TRecord>(key),
                record,
                new ReplaceOptions { IsUpsert = upsert },
                cancellationToken).ConfigureAwait(false);

            if (!upsert && result.MatchedCount == 0)
                throw new KeyNotFoundException($"{typeof(TRecord).Name} record '{key.Id}' was not found.");
        }

        public async Task ReplaceScratchAsync<TRecord>(StorageKey key, TRecord record, TimeSpan? retention, CancellationToken cancellationToken)
            where TRecord : class, IScratchDataRecord
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            if (record == null) throw new ArgumentNullException(nameof(record));

            await GetCollectionAsync<TRecord>(cancellationToken).ConfigureAwait(false);

            if (!retention.HasValue)
            {
                await ReplaceAsync(key, record, true, cancellationToken).ConfigureAwait(false);
                return;
            }

            // Serialize through the normal TRecord class map so the application POCO remains
            // the canonical representation, then attach the provider-owned TTL field before
            // a single replacement/upsert. This avoids a crash window between persistence and TTL.
            var document = record.ToBsonDocument();
            document[ScratchExpirationField] = new BsonDateTime(DateTime.UtcNow.Add(retention.Value));

            var collectionName = StorageRecordIdentity.GetCollectionName<TRecord>();
            var collection = _database.GetCollection<BsonDocument>(collectionName);
            var filter = BuildBsonKeyFilter(key);
            await collection.ReplaceOneAsync(
                filter,
                document,
                new ReplaceOptions { IsUpsert = true },
                cancellationToken).ConfigureAwait(false);
        }

        public async Task DeleteAsync<TRecord>(StorageKey key, CancellationToken cancellationToken)
            where TRecord : class
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

            var collection = await GetCollectionAsync<TRecord>(cancellationToken).ConfigureAwait(false);
            await collection.DeleteOneAsync(BuildKeyFilter<TRecord>(key), cancellationToken).ConfigureAwait(false);
        }

        public async Task<StoragePageResult<TRecord>> QueryAsync<TRecord>(StorageQuery<TRecord> query, CancellationToken cancellationToken)
            where TRecord : class
        {
            if (query == null) throw new ArgumentNullException(nameof(query));

            var collection = await GetCollectionAsync<TRecord>(cancellationToken).ConfigureAwait(false);
            var filter = Builders<TRecord>.Filter.Empty;

            foreach (var item in query.Filters)
                filter &= BuildFilter(item);

            var find = collection.Find(filter);

            if (query.Sorts.Count > 0)
            {
                SortDefinition<TRecord> sort = null;
                foreach (var item in query.Sorts)
                {
                    var field = TranslateField(item.Field);
                    var next = item.Direction == StorageSortDirection.Ascending
                        ? Builders<TRecord>.Sort.Ascending(field)
                        : Builders<TRecord>.Sort.Descending(field);
                    sort = sort == null ? next : Builders<TRecord>.Sort.Combine(sort, next);
                }

                find = find.Sort(sort);
            }

            var offset = DecodeOffset(query.Page.ContinuationToken);
            var pageSize = query.Page.PageSize;
            var records = await find.Skip(offset).Limit(pageSize + 1).ToListAsync(cancellationToken).ConfigureAwait(false);

            var hasMore = records.Count > pageSize;
            if (hasMore)
                records.RemoveAt(records.Count - 1);

            return new StoragePageResult<TRecord>(records, hasMore ? EncodeOffset(offset + pageSize) : null);
        }

        public TimeSpan? GetScratchRetention<TRecord>()
            where TRecord : class, IScratchDataRecord
        {
            return GetDefinition<TRecord>(typeof(ScratchStoreOptions<>))?.Retention;
        }

        private async Task<IMongoCollection<TRecord>> GetCollectionAsync<TRecord>(CancellationToken cancellationToken)
            where TRecord : class
        {
            EnsureScratchExtraElementCompatibility<TRecord>();
            var collectionName = StorageRecordIdentity.GetCollectionName<TRecord>();
            var initializationKey = $"{typeof(TRecord).AssemblyQualifiedName}|{collectionName}";
            var initializer = _initializers.GetOrAdd(initializationKey, _ => InitializeCollectionAsync<TRecord>(collectionName, CancellationToken.None));
            await initializer.ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            return _database.GetCollection<TRecord>(collectionName);
        }

        private async Task InitializeCollectionAsync<TRecord>(string collectionName, CancellationToken cancellationToken)
            where TRecord : class
        {
            var collection = _database.GetCollection<TRecord>(collectionName);
            var indexes = new List<CreateIndexModel<TRecord>>();
            StorageDefinition<TRecord> definition = null;

            if (typeof(IApplicationDataRecord).IsAssignableFrom(typeof(TRecord)))
                definition = GetDefinition<TRecord>(typeof(ApplicationDataStoreOptions<>));
            else if (typeof(IScratchDataRecord).IsAssignableFrom(typeof(TRecord)))
                definition = GetDefinition<TRecord>(typeof(ScratchStoreOptions<>));

            AddCommonAndConfiguredIndexes(indexes, definition);

            if (typeof(IScratchDataRecord).IsAssignableFrom(typeof(TRecord)) && definition?.Retention != null)
            {
                indexes.Add(new CreateIndexModel<TRecord>(
                    Builders<TRecord>.IndexKeys.Ascending(ScratchExpirationField),
                    new CreateIndexOptions
                    {
                        Name = ScratchExpirationIndexName,
                        ExpireAfter = TimeSpan.Zero
                    }));
            }

            if (indexes.Count > 0)
                await collection.Indexes.CreateManyAsync(indexes, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        private StorageDefinition<TRecord> GetDefinition<TRecord>(Type openOptionsType)
            where TRecord : class
        {
            var closedOptionsType = openOptionsType.MakeGenericType(typeof(TRecord));
            var options = _serviceProvider.GetService(closedOptionsType);
            if (options == null)
                return null;

            var definitionProperty = closedOptionsType.GetProperty("Definition", BindingFlags.Instance | BindingFlags.Public);
            return definitionProperty?.GetValue(options) as StorageDefinition<TRecord>;
        }

        private static void AddCommonAndConfiguredIndexes<TRecord>(List<CreateIndexModel<TRecord>> indexes, StorageDefinition<TRecord> definition)
            where TRecord : class
        {
            AddIndex(indexes, StorageRecordIdentity.OrganizationIdPath);

            if (definition == null)
                return;

            foreach (var field in definition.IndexedFields)
                AddIndex(indexes, field);
        }

        private static void AddIndex<TRecord>(List<CreateIndexModel<TRecord>> indexes, string field)
            where TRecord : class
        {
            field = TranslateField(field);
            if (String.IsNullOrWhiteSpace(field) || field == "_id" || indexes.Any(existing => String.Equals(existing.Options?.Name, BuildIndexName(field), StringComparison.Ordinal)))
                return;

            indexes.Add(new CreateIndexModel<TRecord>(
                Builders<TRecord>.IndexKeys.Ascending(field),
                new CreateIndexOptions { Name = BuildIndexName(field) }));
        }

        private static string BuildIndexName(string field)
        {
            return "ix_" + field.Replace('.', '_').TrimStart('_').ToLowerInvariant();
        }

        private static FilterDefinition<TRecord> BuildKeyFilter<TRecord>(StorageKey key)
            where TRecord : class
        {
            return new BsonDocumentFilterDefinition<TRecord>(BuildBsonKeyFilter(key));
        }

        private static BsonDocument BuildBsonKeyFilter(StorageKey key)
        {
            var document = new BsonDocument("_id", key.Id);
            if (!String.IsNullOrWhiteSpace(key.Scope))
                document.Add(StorageRecordIdentity.OrganizationIdPath, key.Scope);
            return document;
        }

        private static FilterDefinition<TRecord> BuildFilter<TRecord>(StorageFilter<TRecord> filter)
            where TRecord : class
        {
            var field = TranslateField(filter.Field);
            var value = ToBsonValue(filter.Value);

            if (filter.Operator == StorageFilterOperator.Equal)
                return new BsonDocumentFilterDefinition<TRecord>(new BsonDocument(field, value));

            var mongoOperator = filter.Operator switch
            {
                StorageFilterOperator.NotEqual => "$ne",
                StorageFilterOperator.LessThan => "$lt",
                StorageFilterOperator.LessThanOrEqual => "$lte",
                StorageFilterOperator.GreaterThan => "$gt",
                StorageFilterOperator.GreaterThanOrEqual => "$gte",
                _ => throw new NotSupportedException($"Storage filter operator '{filter.Operator}' is not supported by Mongo storage.")
            };

            return new BsonDocumentFilterDefinition<TRecord>(
                new BsonDocument(field, new BsonDocument(mongoOperator, value)));
        }

        private static BsonValue ToBsonValue(object value)
        {
            if (value == null) return BsonNull.Value;
            if (value is NormalizedId32 normalizedId) return new BsonString(normalizedId.Value);
            if (value is UtcTimestamp timestamp) return new BsonString(timestamp.ToString());
            return BsonValue.Create(value);
        }

        private static string TranslateField(string field)
        {
            return String.Equals(field, StorageRecordIdentity.IdPath, StringComparison.Ordinal) ? "_id" : field;
        }

        private static int DecodeOffset(string continuationToken)
        {
            if (String.IsNullOrWhiteSpace(continuationToken)) return 0;

            try
            {
                var value = Encoding.UTF8.GetString(Convert.FromBase64String(continuationToken));
                if (Int32.TryParse(value, out var offset) && offset >= 0)
                    return offset;
            }
            catch (FormatException)
            {
            }

            throw new ArgumentException("Invalid storage continuation token.", nameof(continuationToken));
        }

        private static string EncodeOffset(int offset)
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(offset.ToString()));
        }

        private static void EnsureScratchExtraElementCompatibility<TRecord>()
            where TRecord : class
        {
            if (!typeof(IScratchDataRecord).IsAssignableFrom(typeof(TRecord)))
                return;

            if (BsonClassMap.IsClassMapRegistered(typeof(TRecord)))
                return;

            BsonClassMap.RegisterClassMap<TRecord>(classMap =>
            {
                classMap.AutoMap();
                classMap.SetIgnoreExtraElements(true);
            });
        }
    }
}
