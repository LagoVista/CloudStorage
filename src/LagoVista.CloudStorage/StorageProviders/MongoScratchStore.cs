using LagoVista.CloudStorage.Storage;
using LagoVista.Core.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.CloudStorage.StorageProviders
{
    /// <summary>
    /// MongoDB implementation of deterministic durable scratch storage.
    /// Retention is provider-owned and materialized through an internal Mongo TTL field.
    /// </summary>
    public sealed class MongoScratchStore : IScratchStore
    {
        private readonly MongoMutableRecordStore _store;

        public MongoScratchStore(
            IScratchStorageSettings settings,
            IMongoStorageClientFactory clientFactory,
            IServiceProvider serviceProvider)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            if (clientFactory == null) throw new ArgumentNullException(nameof(clientFactory));

            var database = clientFactory.GetDatabase(settings.ConnectionString, settings.DatabaseName);
            _store = new MongoMutableRecordStore(database, serviceProvider);
        }

        public Task<TRecord> GetAsync<TRecord>(StorageKey key, CancellationToken cancellationToken = default)
            where TRecord : class, IScratchDataRecord
        {
            return _store.GetAsync<TRecord>(key, cancellationToken);
        }

        public Task UpsertAsync<TRecord>(TRecord record, CancellationToken cancellationToken = default)
            where TRecord : class, IScratchDataRecord
        {
            ValidateRecord(record);
            var key = new StorageKey(record.Id.Value, record.Organization.Id);
            var retention = _store.GetScratchRetention<TRecord>();
            return _store.ReplaceScratchAsync(key, record, retention, cancellationToken);
        }

        public Task DeleteAsync<TRecord>(StorageKey key, CancellationToken cancellationToken = default)
            where TRecord : class, IScratchDataRecord
        {
            return _store.DeleteAsync<TRecord>(key, cancellationToken);
        }

        public Task<StoragePageResult<TRecord>> QueryAsync<TRecord>(StorageQuery<TRecord> query, CancellationToken cancellationToken = default)
            where TRecord : class, IScratchDataRecord
        {
            return _store.QueryAsync(query, cancellationToken);
        }

        private static void ValidateRecord<TRecord>(TRecord record)
            where TRecord : class, IScratchDataRecord
        {
            if (record == null) throw new ArgumentNullException(nameof(record));
            if (String.IsNullOrWhiteSpace(record.Id.Value)) throw new ArgumentException("Scratch data record Id is required.", nameof(record));
            if (EntityHeader.IsNullOrEmpty(record.Organization)) throw new ArgumentException("Scratch data record Organization is required.", nameof(record));
        }
    }
}
