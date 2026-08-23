using Cassandra;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.CloudStorage.Storage
{
    /// <summary>
    /// Cassandra implementation for immutable activity records. Supports additive table
    /// creation, insert/batch insert, declared partition equality filters, CreationDate
    /// ranges, optional time buckets, and opaque provider paging cursors.
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

            if (!_map.UsesTimeBuckets)
            {
                return await QueryPartitionAsync(query, partitionValues, null, query.Page.ContinuationToken, cancellationToken)
                    .ConfigureAwait(false);
            }

            if (!query.StartUtc.HasValue || !query.EndUtc.HasValue)
            {
                throw new InvalidOperationException(
                    $"Bucketed Cassandra activity queries for {typeof(TRecord).Name} require both start and end dates.");
            }

            var buckets = _map.GetBuckets(query.StartUtc.Value, query.EndUtc.Value);
            var cursor = DecodeBucketCursor(query.Page.ContinuationToken, buckets.Count);
            var records = new List<TRecord>();

            for (var bucketIndex = cursor.BucketIndex; bucketIndex < buckets.Count && records.Count < query.Page.PageSize; bucketIndex++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var remaining = query.Page.PageSize - records.Count;
                var bucketPagingState = bucketIndex == cursor.BucketIndex ? cursor.PagingState : null;
                var bucketResult = await QueryBucketAsync(
                    query,
                    partitionValues,
                    buckets[bucketIndex],
                    remaining,
                    bucketPagingState,
                    cancellationToken).ConfigureAwait(false);

                records.AddRange(bucketResult.Items);

                if (bucketResult.PagingState != null && bucketResult.PagingState.Length > 0)
                {
                    return new StoragePageResult<TRecord>(
                        records,
                        EncodeBucketCursor(bucketIndex, bucketResult.PagingState));
                }

                if (records.Count == query.Page.PageSize && bucketIndex + 1 < buckets.Count)
                {
                    return new StoragePageResult<TRecord>(
                        records,
                        EncodeBucketCursor(bucketIndex + 1, null));
                }
            }

            return new StoragePageResult<TRecord>(records);
        }

        private async Task<StoragePageResult<TRecord>> QueryPartitionAsync(
            HistoryQuery<TRecord> query,
            Dictionary<string, object> partitionValues,
            string bucket,
            string continuationToken,
            CancellationToken cancellationToken)
        {
            byte[] pagingState = null;
            if (!String.IsNullOrWhiteSpace(continuationToken))
            {
                try
                {
                    pagingState = Convert.FromBase64String(continuationToken);
                }
                catch (FormatException ex)
                {
                    throw new ArgumentException("The activity query continuation token is invalid.", nameof(query), ex);
                }
            }

            var result = await QueryBucketAsync(
                query,
                partitionValues,
                bucket,
                query.Page.PageSize,
                pagingState,
                cancellationToken).ConfigureAwait(false);

            var next = result.PagingState == null || result.PagingState.Length == 0
                ? null
                : Convert.ToBase64String(result.PagingState);

            return new StoragePageResult<TRecord>(result.Items, next);
        }

        private async Task<BucketQueryResult> QueryBucketAsync(
            HistoryQuery<TRecord> query,
            Dictionary<string, object> partitionValues,
            string bucket,
            int pageSize,
            byte[] pagingState,
            CancellationToken cancellationToken)
        {
            var cql = $"SELECT {String.Join(", ", _map.Properties.Select(property => property.ColumnName))} FROM {_map.TableName} WHERE ";
            var clauses = new List<string>();
            var values = new List<object>();

            foreach (var partition in _map.PartitionProperties)
            {
                clauses.Add($"{partition.ColumnName} = ?");
                values.Add(partitionValues[partition.Property.Name]);
            }

            if (_map.UsesTimeBuckets)
            {
                clauses.Add($"{CassandraRecordMap<TRecord>.BucketColumnName} = ?");
                values.Add(bucket);
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
            var statement = prepared.Bind(values.ToArray())
                .SetPageSize(pageSize)
                .SetAutoPage(false);

            if (pagingState != null && pagingState.Length > 0)
            {
                statement.SetPagingState(pagingState);
            }

            var rows = await session.ExecuteAsync(statement).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            return new BucketQueryResult(rows.Select(_map.Read).ToList(), rows.PagingState);
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

        private static BucketCursor DecodeBucketCursor(string continuationToken, int bucketCount)
        {
            if (String.IsNullOrWhiteSpace(continuationToken)) return new BucketCursor(0, null);

            try
            {
                var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(continuationToken));
                var separator = decoded.IndexOf(':');
                if (separator < 1) throw new FormatException();

                var bucketIndex = Int32.Parse(decoded.Substring(0, separator));
                if (bucketIndex < 0 || bucketIndex >= bucketCount) throw new FormatException();

                var pagingText = decoded.Substring(separator + 1);
                var pagingState = String.IsNullOrWhiteSpace(pagingText) ? null : Convert.FromBase64String(pagingText);
                return new BucketCursor(bucketIndex, pagingState);
            }
            catch (Exception ex) when (ex is FormatException || ex is OverflowException)
            {
                throw new ArgumentException("The bucketed activity query continuation token is invalid.", nameof(continuationToken), ex);
            }
        }

        private static string EncodeBucketCursor(int bucketIndex, byte[] pagingState)
        {
            var pagingText = pagingState == null || pagingState.Length == 0
                ? String.Empty
                : Convert.ToBase64String(pagingState);
            return Convert.ToBase64String(Encoding.UTF8.GetBytes($"{bucketIndex}:{pagingText}"));
        }

        private sealed class BucketCursor
        {
            public BucketCursor(int bucketIndex, byte[] pagingState)
            {
                BucketIndex = bucketIndex;
                PagingState = pagingState;
            }

            public int BucketIndex { get; }
            public byte[] PagingState { get; }
        }

        private sealed class BucketQueryResult
        {
            public BucketQueryResult(IReadOnlyList<TRecord> items, byte[] pagingState)
            {
                Items = items;
                PagingState = pagingState;
            }

            public IReadOnlyList<TRecord> Items { get; }
            public byte[] PagingState { get; }
        }
    }
}
