using LagoVista.CloudStorage.Storage;
using LagoVista.Core.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LagoVista.Core;

namespace LagoVista.CloudStorage.StorageProviders
{
    /// <summary>
    /// MongoDB implementation of deterministic mutable application-data storage.
    /// One record type maps to one collection through StorageRecordIdentity.
    /// </summary>
    public sealed class MongoApplicationDataStore : IApplicationDataStore
    {
        private readonly MongoMutableRecordStore _store;

        public MongoApplicationDataStore(
            IApplicationDataStorageSettings settings,
            IMongoStorageClientFactory clientFactory,
            IServiceProvider serviceProvider)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            if (clientFactory == null) throw new ArgumentNullException(nameof(clientFactory));

            var database = clientFactory.GetDatabase(settings.ConnectionString, settings.DatabaseName);
            _store = new MongoMutableRecordStore(database, serviceProvider);
        }

        public Task<TRecord> GetAsync<TRecord>(StorageKey key, CancellationToken cancellationToken = default)
            where TRecord : class, IApplicationDataRecord
        {
            return _store.GetAsync<TRecord>(key, cancellationToken);
        }

        public async Task InsertAsync<TRecord>(TRecord record, CancellationToken cancellationToken = default)
            where TRecord : class, IApplicationDataRecord
        {
            ValidateRecord(record);
            var now = UtcTimestamp.Now;
            record.CreationDate = now;
            record.LastUpdatedDate = now;
            await _store.InsertAsync(record, cancellationToken).ConfigureAwait(false);
        }

        public async Task UpdateAsync<TRecord>(TRecord record, CancellationToken cancellationToken = default)
            where TRecord : class, IApplicationDataRecord
        {
            ValidateRecord(record);
            var key = BuildKey(record);
            var existing = await _store.GetAsync<TRecord>(key, cancellationToken).ConfigureAwait(false);
            if (existing == null)
                throw new KeyNotFoundException($"{typeof(TRecord).Name} record '{record.Id.Value}' was not found.");

            record.CreationDate = existing.CreationDate;
            record.LastUpdatedDate = UtcTimestamp.Now;
            await _store.ReplaceAsync(key, record, false, cancellationToken).ConfigureAwait(false);
        }

        public Task DeleteAsync<TRecord>(StorageKey key, CancellationToken cancellationToken = default)
            where TRecord : class, IApplicationDataRecord
        {
            return _store.DeleteAsync<TRecord>(key, cancellationToken);
        }

        public Task<StoragePageResult<TRecord>> QueryAsync<TRecord>(StorageQuery<TRecord> query, CancellationToken cancellationToken = default)
            where TRecord : class, IApplicationDataRecord
        {
            return _store.QueryAsync(query, cancellationToken);
        }

        private static StorageKey BuildKey<TRecord>(TRecord record)
            where TRecord : class, IApplicationDataRecord
        {
            return new StorageKey(record.Id.Value, record.Organization.Id);
        }

        private static void ValidateRecord<TRecord>(TRecord record)
            where TRecord : class, IApplicationDataRecord
        {
            if (record == null) throw new ArgumentNullException(nameof(record));
            if (String.IsNullOrWhiteSpace(record.Id.Value)) throw new ArgumentException("Application data record Id is required.", nameof(record));
            if (EntityHeader.IsNullOrEmpty(record.Organization)) throw new ArgumentException("Application data record Organization is required.", nameof(record));
        }
    }
}
