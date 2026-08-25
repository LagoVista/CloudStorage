using LagoVista.CloudStorage.Storage;
using LagoVista.Core.Models;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.CloudStorage.StorageProviders
{
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
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

            var collection = await GetCollectionAsync<TRecord>(cancellationToken).ConfigureAwait(false);
            return await collection.Find(BuildKeyFilter<TRecord>(key)).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
        }

        public async Task InsertAsync<TRecord>(TRecord record, CancellationToken cancellationToken)
        {
            if (record == null) throw new ArgumentNullException(nameof(record));

            var collection = await GetCollectionAsync<TRecord>(cancellationToken).ConfigureAwait(false);
            await collection.InsertOneAsync(record, null, cancellationToken).ConfigureAwait(false);
        }

        public async Task ReplaceAsync<TRecord>(StorageKey key, TRecord record, bool upsert, CancellationToken cancellationToken)
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

        public async Task DeleteAsync<TRecord>(StorageKey key, CancellationToken cancellationToken)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

            var collection = await GetCollectionAsync<TRecord>(cancellationToken).ConfigureAwait(false);
            await collection.DeleteOneAsync(BuildKeyFilter<TRecord>(key), cancellationToken).ConfigureAwait(false);
        }

        public async Task<StoragePageResult<TRecord>> QueryAsync<TRecord>(StorageQuery<TRecord> query, CancellationToken cancellationToken)
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
                    var next = item.Direction == StorageSortDirection.Ascending
                        ? Builders<TRecord>.Sort.Ascending(item.Field)
                        : Builders<TRecord>.Sort.Descending(item.Field);
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

        public async Task ApplyScratchExpirationAsync<TRecord>(StorageKey key, TimeSpan retention, CancellationToken cancellationToken)
            where TRecord : IScratchDataRecord
        {
            var collection = await GetCollectionAsync<TRecord>(cancellationToken).ConfigureAwait(false);
            var expiration = DateTime.UtcNow.Add(retention);
            var update = Builders<TRecord>.Update.Set(ScratchExpirationField, expiration);
            await collection.UpdateOneAsync(BuildKeyFilter<TRecord>(key), update, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        private async Task<IMongoCollection<TRecord>> GetCollectionAsync<TRecord>(CancellationToken cancellationToken)
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
        {
            var collection = _database.GetCollection<TRecord>(collectionName);
            var indexes = new List<CreateIndexModel<TRecord>>();

            if (typeof(IApplicationDataRecord).IsAssignableFrom(typeof(TRecord)))
            {
                var options = _serviceProvider.GetService<ApplicationDataStoreOptions<TRecord>>();
                AddCommonAndConfiguredIndexes(indexes, options?.Definition);
            }
            else if (typeof(IScratchDataRecord).IsAssignableFrom(typeof(TRecord)))
            {
                var options = _serviceProvider.GetService<ScratchStoreOptions<TRecord>>();
                AddCommonAndConfiguredIndexes(indexes, options?.Definition);

                if (options?.Definition.Retention != null)
                {
                    indexes.Add(new CreateIndexModel<TRecord>(
                        Builders<TRecord>.IndexKeys.Ascending(ScratchExpirationField),
                        new CreateIndexOptions
                        {
                            Name = ScratchExpirationIndexName,
                            ExpireAfter = TimeSpan.Zero
                        }));
                }
            }

            if (indexes.Count > 0)
                await collection.Indexes.CreateManyAsync(indexes, cancellationToken).ConfigureAwait(false);
        }

        private static void AddCommonAndConfiguredIndexes<TRecord>(List<CreateIndexModel<TRecord>> indexes, FlatStorageDefinition<TRecord> definition)
        {
            AddIndex(indexes, StorageRecordIdentity.OrganizationIdPath);

            if (definition == null)
                return;

            foreach (var field in definition.IndexedFields)
                AddIndex(indexes, field);
        }

        private static void AddIndex<TRecord>(List<CreateIndexModel<TRecord>> indexes, string field)
        {
            if (String.IsNullOrWhiteSpace(field) || indexes.Any(existing => String.Equals(existing.Options?.Name, BuildIndexName(field), StringComparison.Ordinal)))
                return;

            indexes.Add(new CreateIndexModel<TRecord>(
                Builders<TRecord>.IndexKeys.Ascending(field),
                new CreateIndexOptions { Name = BuildIndexName(field) }));
        }

        private static string BuildIndexName(string field)
        {
            return "ix_" + field.Replace('.', '_').ToLowerInvariant();
        }

        private static FilterDefinition<TRecord> BuildKeyFilter<TRecord>(StorageKey key)
        {
            var filter = Builders<TRecord>.Filter.Eq(StorageRecordIdentity.IdPath, key.Id);
            if (!String.IsNullOrWhiteSpace(key.Scope))
                filter &= Builders<TRecord>.Filter.Eq(StorageRecordIdentity.OrganizationIdPath, key.Scope);
            return filter;
        }

        private static FilterDefinition<TRecord> BuildFilter<TRecord>(StorageFilter<TRecord> filter)
        {
            var value = NormalizeScalar(filter.Value);
            switch (filter.Operator)
            {
                case StorageFilterOperator.Equal:
                    return Builders<TRecord>.Filter.Eq<object>(filter.Field, value);
                case StorageFilterOperator.NotEqual:
                    return Builders<TRecord>.Filter.Ne<object>(filter.Field, value);
                case StorageFilterOperator.LessThan:
                    return Builders<TRecord>.Filter.Lt<object>(filter.Field, value);
                case StorageFilterOperator.LessThanOrEqual:
                    return Builders<TRecord>.Filter.Lte<object>(filter.Field, value);
                case StorageFilterOperator.GreaterThan:
                    return Builders<TRecord>.Filter.Gt<object>(filter.Field, value);
                case StorageFilterOperator.GreaterThanOrEqual:
                    return Builders<TRecord>.Filter.Gte<object>(filter.Field, value);
                default:
                    throw new NotSupportedException($"Storage filter operator '{filter.Operator}' is not supported by Mongo storage.");
            }
        }

        private static object NormalizeScalar(object value)
        {
            if (value == null) return null;
            if (value is NormalizedId32 normalizedId) return normalizedId.Value;
            if (value is UtcTimestamp timestamp) return timestamp.ToString();
            return value;
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
