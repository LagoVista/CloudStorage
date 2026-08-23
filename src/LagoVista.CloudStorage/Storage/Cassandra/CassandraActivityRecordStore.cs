using Cassandra;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.CloudStorage.Storage
{
    /// <summary>
    /// Cassandra implementation for immutable activity records. The initial implementation
    /// supports additive table creation, insert/batch insert, partition equality filters,
    /// CreationDate ranges, and opaque Cassandra paging state.
    /// </summary>
    public sealed class CassandraActivityRecordStore<TRecord> : IActivityRecordStore<TRecord>
        where TRecord : IActivityRecord, new()
    {
        private readonly ICassandraSessionFactory _sessionFactory;
        private readonly CassandraRecordMap<TRecord> _map;
        private readonly SemaphoreSlim _schemaLock = new SemaphoreSlim(1, 1);
        private volatile bool _schemaReady;
        private PreparedStatement _insert;

        public CassandraActivityRecordStore(
            ICassandraSessionFactory sessionFactory,
            ActivityRecordStoreOptions<TRecord> options)
        {
            _sessionFactory = sessionFactory ?? throw new ArgumentNullException(nameof(sessionFactory));
            _map = new CassandraRecordMap<TRecord>(options);
        }

        public async Task InsertAsync(TRecord record, CancellationToken cancellationToken = default)
        {
            if (record == null) throw new ArgumentNullException(nameof(record));
            cancellationToken.ThrowIfCancellationRequested();

            var session = await GetReadySessionAsync().ConfigureAwait(false);
            var insert = await GetInsertAsync(session).ConfigureAwait(false);
            await session.ExecuteAsync(insert.Bind(_map.Values(record))).ConfigureAwait(false);

            cancellationToken.ThrowIfCancellationRequested();
        }

        public async Task InsertBatchAsync(IEnumerable<TRecord> records, CancellationToken cancellationToken = default)
        {
            if (records == null) throw new ArgumentNullException(nameof(records));
            cancellationToken.ThrowIfCancellationRequested();

            var materialized = records.ToList();
            if (materialized.Count == 0) return;
            if (materialized.Any(record => record == null)) throw new ArgumentException("Activity record batches cannot contain null records.", nameof(records));

            var session = await GetReadySessionAsync().ConfigureAwait(false);
            var insert = await GetInsertAsync(session).ConfigureAwait(false);
            var batch = new BatchStatement();

            foreach (var record in materialized)
            {
                cancellationToken.ThrowIfCancellationRequested();
                batch.Add(insert.Bind(_map.Values(record)));
            }

            await session.ExecuteAsync(batch).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
        }

        public async Task<StoragePageResult<TRecord>> QueryAsync(
            HistoryQuery<TRecord> query,
            CancellationToken cancellationToken = default)
        {
            if (query == null) throw new ArgumentNullException(nameof(query));
            cancellationToken.ThrowIfCancellationRequested();

            var partitionValues = ResolvePartitionValues(query);
            ValidateUnsupportedFilters(query);

            var cql = $"SELECT {String.Join(", ", _map.Properties.Select(property => property.ColumnName))} FROM {_map.TableName} WHERE ";
            var clauses = new List<string>();
            var values = new List<object>();

            foreach (var partition in _map.PartitionProperties)
            {
                clauses.Add($"{partition.ColumnName} = ?");
                values.Add(partitionValues[partition.Property.Name]);
            }

            if (query.StartUtc.HasValue)
            {
                clauses.Add($"{_map.Time.ColumnName} >= ?");
                values.Add(new DateTimeOffset(query.StartUtc.Value));
            }

            if (query.EndUtc.HasValue)
            {
                clauses.Add($"{_map.Time.ColumnName} <= ?");
                values.Add(new DateTimeOffset(query.EndUtc.Value));
            }

            cql += String.Join(" AND ", clauses);

            var session = await GetReadySessionAsync().ConfigureAwait(false);
            var prepared = await session.PrepareAsync(cql).ConfigureAwait(false);
            var statement = prepared.Bind(values.ToArray()).SetPageSize(query.Page.PageSize);

            if (!String.IsNullOrWhiteSpace(query.Page.ContinuationToken))
            {
                try
                {
                    statement.SetPagingState(Convert.FromBase64String(query.Page.ContinuationToken));
                }
                catch (FormatException ex)
                {
                    throw new ArgumentException("The activity query continuation token is invalid.", nameof(query), ex);
                }
            }

            var rows = await session.ExecuteAsync(statement).ConfigureAwait(false);
            var records = rows.Select(_map.Read).ToList();
            var continuationToken = rows.PagingState == null || rows.PagingState.Length == 0
                ? null
                : Convert.ToBase64String(rows.PagingState);

            cancellationToken.ThrowIfCancellationRequested();
            return new StoragePageResult<TRecord>(records, continuationToken);
        }

        private async Task<ISession> GetReadySessionAsync()
        {
            var session = await _sessionFactory.GetSessionAsync().ConfigureAwait(false);
            if (_schemaReady) return session;

            await _schemaLock.WaitAsync().ConfigureAwait(false);
            try
            {
                if (!_schemaReady)
                {
                    await session.ExecuteAsync(new SimpleStatement(_map.CreateTableCql())).ConfigureAwait(false);
                    _schemaReady = true;
                }
            }
            finally
            {
                _schemaLock.Release();
            }

            return session;
        }

        private async Task<PreparedStatement> GetInsertAsync(ISession session)
        {
            if (_insert != null) return _insert;
            _insert = await session.PrepareAsync(_map.InsertCql()).ConfigureAwait(false);
            return _insert;
        }

        private Dictionary<string, object> ResolvePartitionValues(HistoryQuery<TRecord> query)
        {
            var result = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

            foreach (var partition in _map.PartitionProperties)
            {
                var matches = query.Filters
                    .Where(filter => String.Equals(filter.Field, partition.Property.Name, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                if (matches.Count != 1 || matches[0].Operator != StorageFilterOperator.Equal)
                {
                    throw new InvalidOperationException(
                        $"Cassandra activity queries require exactly one equality filter for partition field {partition.Property.Name}.");
                }

                result[partition.Property.Name] = matches[0].Value;
            }

            return result;
        }

        private void ValidateUnsupportedFilters(HistoryQuery<TRecord> query)
        {
            var partitionNames = new HashSet<string>(
                _map.PartitionProperties.Select(property => property.Property.Name),
                StringComparer.OrdinalIgnoreCase);

            var unsupported = query.Filters.FirstOrDefault(filter => !partitionNames.Contains(filter.Field));
            if (unsupported != null)
            {
                throw new NotSupportedException(
                    $"Filter {unsupported.Field} is not supported by the core Cassandra activity query path yet. Declare/query indexes explicitly in the indexed-query increment.");
            }
        }
    }
}
