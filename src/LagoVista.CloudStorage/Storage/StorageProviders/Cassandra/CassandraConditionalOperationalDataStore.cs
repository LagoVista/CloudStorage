using Cassandra;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.CloudStorage.Storage.StorageProviders.Cassandra
{
    /// <summary>
    /// Cassandra LWT implementation of conditional operational storage.
    /// The normal operational store owns schema creation/reconciliation and reads;
    /// this class adds atomic IF NOT EXISTS / IF version = ? mutations.
    /// </summary>
    public sealed class CassandraConditionalOperationalDataStore<TRecord> : IConditionalOperationalDataStore<TRecord>
        where TRecord : class, IConditionalOperationalDataRecord, new()
    {
        private readonly ICassandraSessionFactory _sessionFactory;
        private readonly CassandraOperationalRecordMap<TRecord> _map;
        private readonly CassandraOperationalDataStore<TRecord> _baseStore;
        private PreparedStatement _create;
        private PreparedStatement _replace;
        private PreparedStatement _delete;

        public CassandraConditionalOperationalDataStore(
            ICassandraSessionFactory sessionFactory,
            OperationalDataStoreOptions<TRecord> options)
        {
            _sessionFactory = sessionFactory ?? throw new ArgumentNullException(nameof(sessionFactory));
            _map = new CassandraOperationalRecordMap<TRecord>(options ?? throw new ArgumentNullException(nameof(options)));
            _baseStore = new CassandraOperationalDataStore<TRecord>(sessionFactory, options);
        }

        public Task<TRecord> GetAsync(
            string organizationId,
            string id,
            CancellationToken cancellationToken = default)
        {
            return _baseStore.GetAsync(organizationId, id, cancellationToken);
        }

        public async Task<ConditionalMutationResult<TRecord>> TryCreateAsync(
            TRecord record,
            CancellationToken cancellationToken = default)
        {
            if (record == null) throw new ArgumentNullException(nameof(record));
            ValidateIdentity(record.OrganizationId, record.Id);
            cancellationToken.ThrowIfCancellationRequested();

            StampForCreate(record);

            // Force the normal operational store to create/reconcile the physical table first.
            await _baseStore.GetAsync(record.OrganizationId, record.Id, cancellationToken).ConfigureAwait(false);

            var session = await _sessionFactory.GetSessionAsync().ConfigureAwait(false);
            var prepared = await GetCreateAsync(session).ConfigureAwait(false);
            var rows = await session.ExecuteAsync(prepared.Bind(_map.Values(record))).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();

            if (WasApplied(rows))
                return new ConditionalMutationResult<TRecord>(true, record);

            var current = await _baseStore.GetAsync(record.OrganizationId, record.Id, cancellationToken).ConfigureAwait(false);
            return new ConditionalMutationResult<TRecord>(false, current);
        }

        public async Task<ConditionalMutationResult<TRecord>> TryReplaceAsync(
            TRecord record,
            long expectedVersion,
            CancellationToken cancellationToken = default)
        {
            if (record == null) throw new ArgumentNullException(nameof(record));
            ValidateIdentity(record.OrganizationId, record.Id);
            ValidateExpectedVersion(expectedVersion);
            cancellationToken.ThrowIfCancellationRequested();

            await _baseStore.GetAsync(record.OrganizationId, record.Id, cancellationToken).ConfigureAwait(false);

            record.LastUpdatedDate = DateTime.UtcNow;
            record.Version = checked(expectedVersion + 1);

            var session = await _sessionFactory.GetSessionAsync().ConfigureAwait(false);
            var prepared = await GetReplaceAsync(session).ConfigureAwait(false);
            var values = BuildReplaceValues(record, expectedVersion);
            var rows = await session.ExecuteAsync(prepared.Bind(values)).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();

            if (WasApplied(rows))
                return new ConditionalMutationResult<TRecord>(true, record);

            var current = await _baseStore.GetAsync(record.OrganizationId, record.Id, cancellationToken).ConfigureAwait(false);
            return new ConditionalMutationResult<TRecord>(false, current);
        }

        public async Task<ConditionalMutationResult<TRecord>> TryDeleteAsync(
            string organizationId,
            string id,
            long expectedVersion,
            CancellationToken cancellationToken = default)
        {
            ValidateIdentity(organizationId, id);
            ValidateExpectedVersion(expectedVersion);
            cancellationToken.ThrowIfCancellationRequested();

            await _baseStore.GetAsync(organizationId, id, cancellationToken).ConfigureAwait(false);

            var session = await _sessionFactory.GetSessionAsync().ConfigureAwait(false);
            var prepared = await GetDeleteAsync(session).ConfigureAwait(false);
            var values = BuildIdentityValues(organizationId, id)
                .Concat(new object[] { expectedVersion })
                .ToArray();

            var rows = await session.ExecuteAsync(prepared.Bind(values)).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();

            if (WasApplied(rows))
                return new ConditionalMutationResult<TRecord>(true, null);

            var current = await _baseStore.GetAsync(organizationId, id, cancellationToken).ConfigureAwait(false);
            return new ConditionalMutationResult<TRecord>(false, current);
        }

        private async Task<PreparedStatement> GetCreateAsync(ISession session)
        {
            if (_create != null) return _create;

            var columns = _map.Properties.Select(property => property.ColumnName).ToList();
            var markers = String.Join(", ", columns.Select(_ => "?"));
            _create = await session.PrepareAsync(
                $"INSERT INTO {_map.TableName} ({String.Join(", ", columns)}) VALUES ({markers}) IF NOT EXISTS")
                .ConfigureAwait(false);
            return _create;
        }

        private async Task<PreparedStatement> GetReplaceAsync(ISession session)
        {
            if (_replace != null) return _replace;

            var mutable = MutableProperties();
            var assignments = String.Join(", ", mutable.Select(property => $"{property.ColumnName} = ?"));
            var partition = _map.PartitionProperties[0];

            _replace = await session.PrepareAsync(
                $"UPDATE {_map.TableName} SET {assignments} WHERE {partition.ColumnName} = ? AND {_map.Key.ColumnName} = ? IF version = ?")
                .ConfigureAwait(false);
            return _replace;
        }

        private async Task<PreparedStatement> GetDeleteAsync(ISession session)
        {
            if (_delete != null) return _delete;

            var partition = _map.PartitionProperties[0];
            _delete = await session.PrepareAsync(
                $"DELETE FROM {_map.TableName} WHERE {partition.ColumnName} = ? AND {_map.Key.ColumnName} = ? IF version = ?")
                .ConfigureAwait(false);
            return _delete;
        }

        private object[] BuildReplaceValues(TRecord record, long expectedVersion)
        {
            var values = new List<object>();
            foreach (var property in MutableProperties())
            {
                values.Add(_map.DriverValue(property, property.Property.GetValue(record)));
            }

            values.AddRange(BuildIdentityValues(record.OrganizationId, record.Id));
            values.Add(expectedVersion);
            return values.ToArray();
        }

        private IReadOnlyList<CassandraRecordProperty> MutableProperties()
        {
            var partitionNames = new HashSet<string>(
                _map.PartitionProperties.Select(property => property.Property.Name),
                StringComparer.OrdinalIgnoreCase);

            return _map.Properties
                .Where(property =>
                    !partitionNames.Contains(property.Property.Name) &&
                    !String.Equals(property.Property.Name, _map.Key.Property.Name, StringComparison.OrdinalIgnoreCase))
                .ToList()
                .AsReadOnly();
        }

        private object[] BuildIdentityValues(string organizationId, string id)
        {
            return new[]
            {
                _map.DriverValue(_map.PartitionProperties[0], organizationId),
                _map.DriverValue(_map.Key, id)
            };
        }

        private static bool WasApplied(RowSet rows)
        {
            var row = rows?.FirstOrDefault();
            return row != null && row.GetValue<bool>("[applied]");
        }

        private static void StampForCreate(TRecord record)
        {
            var now = DateTime.UtcNow;

            if (record.CreationDate == default)
                record.CreationDate = now;
            else if (record.CreationDate.Kind == DateTimeKind.Local)
                record.CreationDate = record.CreationDate.ToUniversalTime();
            else if (record.CreationDate.Kind == DateTimeKind.Unspecified)
                record.CreationDate = DateTime.SpecifyKind(record.CreationDate, DateTimeKind.Utc);

            record.LastUpdatedDate = now;
            record.Version = 1;
        }

        private static void ValidateExpectedVersion(long expectedVersion)
        {
            if (expectedVersion <= 0)
                throw new ArgumentOutOfRangeException(nameof(expectedVersion), "Expected version must be greater than zero.");
        }

        private static void ValidateIdentity(string organizationId, string id)
        {
            if (String.IsNullOrWhiteSpace(organizationId)) throw new ArgumentNullException(nameof(organizationId));
            if (String.IsNullOrWhiteSpace(id)) throw new ArgumentNullException(nameof(id));
        }
    }
}
