using Cassandra;
using LagoVista;
using LagoVista.Core.Interfaces;
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
    /// creation/schema reconciliation, insert/batch insert, declared partition equality filters,
    /// declared SAI indexes, CreationDate ranges, optional time buckets, retention, and opaque
    /// provider paging cursors.
    /// </summary>
    [CriticalCoverage]
    public sealed class CassandraActivityRecordStore<TRecord> : IActivityRecordStore<TRecord>
        where TRecord : IActivityRecord, new()
    {
        private const int BatchInsertConcurrency = 16;
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

            for (var offset = 0; offset < materialized.Count; offset += BatchInsertConcurrency)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var count = Math.Min(BatchInsertConcurrency, materialized.Count - offset);
                var writes = new Task<RowSet>[count];
                for (var index = 0; index < count; index++)
                {
                    var record = materialized[offset + index];
                    writes[index] = session.ExecuteAsync(insert.Bind(_map.Values(record)));
                }

                await Task.WhenAll(writes).ConfigureAwait(false);
            }

            cancellationToken.ThrowIfCancellationRequested();
        }

        public async Task<StoragePageResult<TRecord>> QueryAsync(
            HistoryQuery<TRecord> query,
            CancellationToken cancellationToken = default)
        {
            if (query == null) throw new ArgumentNullException(nameof(query));
            cancellationToken.ThrowIfCancellationRequested();

            var partitionValues = ResolvePartitionValues(query);
            var indexedFilters = ResolveIndexedFilters(query);

            if (!_map.UsesTimeBuckets)
            {
                return await QueryPartitionAsync(query, partitionValues, indexedFilters, null, query.Page.ContinuationToken, cancellationToken)
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
                    indexedFilters,
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
            IReadOnlyList<IndexedFilter> indexedFilters,
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
                indexedFilters,
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
            IReadOnlyList<IndexedFilter> indexedFilters,
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

            foreach (var indexedFilter in indexedFilters)
            {
                clauses.Add($"{indexedFilter.Property.ColumnName} = ?");
                values.Add(_map.DriverValue(indexedFilter.Property, indexedFilter.Value));
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
                    await ReconcileSchemaAsync(session).ConfigureAwait(false);
                    await ReconcileIndexesAsync(session).ConfigureAwait(false);
                    await session.ExecuteAsync(new SimpleStatement(_map.ReconcileRetentionCql())).ConfigureAwait(false);
                    _schemaReady = true;
                }
            }
            finally
            {
                _schemaLock.Release();
            }

            return session;
        }

        private async Task ReconcileSchemaAsync(ISession session)
        {
            var prepared = await session.PrepareAsync(@"
SELECT column_name, type, kind, position
FROM system_schema.columns
WHERE keyspace_name = ? AND table_name = ?").ConfigureAwait(false);

            var rows = await session.ExecuteAsync(prepared.Bind(session.Keyspace, _map.TableName)).ConfigureAwait(false);
            var existing = rows.ToDictionary(
                row => row.GetValue<string>("column_name"),
                row => new ExistingColumn(
                    row.GetValue<string>("type"),
                    row.GetValue<string>("kind"),
                    row.GetValue<int>("position")),
                StringComparer.OrdinalIgnoreCase);

            foreach (var expected in ExpectedColumns())
            {
                if (!existing.TryGetValue(expected.Name, out var actual))
                {
                    if (expected.Kind != "regular")
                    {
                        throw new InvalidOperationException(
                            $"Cassandra activity table {_map.TableName} is missing required {expected.Kind} column {expected.Name}. Primary-key changes require an explicit migration.");
                    }

                    await session.ExecuteAsync(new SimpleStatement(
                        $"ALTER TABLE {_map.TableName} ADD {expected.Name} {expected.Type}")).ConfigureAwait(false);
                    continue;
                }

                if (!String.Equals(NormalizeCqlType(actual.Type), NormalizeCqlType(expected.Type), StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException(
                        $"Cassandra activity table {_map.TableName} column {expected.Name} has type {actual.Type}, but {typeof(TRecord).Name} requires {expected.Type}. Type changes require an explicit migration.");
                }

                if (!String.Equals(actual.Kind, expected.Kind, StringComparison.OrdinalIgnoreCase) ||
                    (expected.Kind != "regular" && actual.Position != expected.Position))
                {
                    throw new InvalidOperationException(
                        $"Cassandra activity table {_map.TableName} column {expected.Name} has key shape {actual.Kind}[{actual.Position}], but {typeof(TRecord).Name} requires {expected.Kind}[{expected.Position}]. Primary-key changes require an explicit migration.");
                }
            }
        }

        private async Task ReconcileIndexesAsync(ISession session)
        {
            if (_map.IndexedProperties.Count == 0) return;

            foreach (var property in _map.IndexedProperties)
            {
                var indexName = IndexName(property);
                var existing = await ReadIndexAsync(session, indexName).ConfigureAwait(false);

                if (existing == null)
                {
                    await session.ExecuteAsync(new SimpleStatement(
                        $"CREATE INDEX IF NOT EXISTS {indexName} ON {_map.TableName} ({property.ColumnName}) USING 'sai'")).ConfigureAwait(false);
                    existing = await ReadIndexAsync(session, indexName).ConfigureAwait(false);
                }

                if (existing == null)
                {
                    throw new InvalidOperationException(
                        $"Cassandra activity SAI index {indexName} could not be created for {_map.TableName}.{property.ColumnName}. The index name may conflict with another table in keyspace {session.Keyspace}.");
                }

                if (!String.Equals(existing.TableName, _map.TableName, StringComparison.OrdinalIgnoreCase) ||
                    !String.Equals(existing.Target, property.ColumnName, StringComparison.OrdinalIgnoreCase) ||
                    !existing.IsSai)
                {
                    throw new InvalidOperationException(
                        $"Cassandra activity index {indexName} does not match expected SAI target {_map.TableName}.{property.ColumnName}. Index changes require an explicit migration.");
                }
            }
        }

        private async Task<ExistingIndex> ReadIndexAsync(ISession session, string indexName)
        {
            var prepared = await session.PrepareAsync(@"
SELECT table_name, index_name, kind, options
FROM system_schema.indexes
WHERE keyspace_name = ?").ConfigureAwait(false);
            var rows = await session.ExecuteAsync(prepared.Bind(session.Keyspace)).ConfigureAwait(false);

            foreach (var row in rows)
            {
                if (!String.Equals(row.GetValue<string>("index_name"), indexName, StringComparison.OrdinalIgnoreCase)) continue;

                var options = row.GetValue<IDictionary<string, string>>("options");
                options.TryGetValue("target", out var target);
                options.TryGetValue("class_name", out var className);
                return new ExistingIndex(
                    row.GetValue<string>("table_name"),
                    target,
                    row.GetValue<string>("kind"),
                    className);
            }

            return null;
        }

        private IReadOnlyList<ExpectedColumn> ExpectedColumns()
        {
            var expected = new List<ExpectedColumn>();
            var partitionNames = new HashSet<string>(_map.PartitionProperties.Select(property => property.ColumnName), StringComparer.OrdinalIgnoreCase);

            for (var index = 0; index < _map.PartitionProperties.Count; index++)
            {
                var property = _map.PartitionProperties[index];
                expected.Add(new ExpectedColumn(property.ColumnName, property.CqlType, "partition_key", index));
            }

            if (_map.UsesTimeBuckets)
            {
                expected.Add(new ExpectedColumn(
                    CassandraRecordMap<TRecord>.BucketColumnName,
                    "text",
                    "partition_key",
                    _map.PartitionProperties.Count));
            }

            expected.Add(new ExpectedColumn(_map.Time.ColumnName, _map.Time.CqlType, "clustering", 0));
            expected.Add(new ExpectedColumn(_map.Key.ColumnName, _map.Key.CqlType, "clustering", 1));

            foreach (var property in _map.Properties)
            {
                if (partitionNames.Contains(property.ColumnName) ||
                    String.Equals(property.ColumnName, _map.Time.ColumnName, StringComparison.OrdinalIgnoreCase) ||
                    String.Equals(property.ColumnName, _map.Key.ColumnName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                expected.Add(new ExpectedColumn(property.ColumnName, property.CqlType, "regular", -1));
            }

            return expected;
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

                result[partition.Property.Name] = _map.DriverValue(partition, matches[0].Value);
            }

            return result;
        }

        private IReadOnlyList<IndexedFilter> ResolveIndexedFilters(HistoryQuery<TRecord> query)
        {
            var partitionNames = new HashSet<string>(
                _map.PartitionProperties.Select(property => property.Property.Name),
                StringComparer.OrdinalIgnoreCase);
            var indexedByName = _map.IndexedProperties.ToDictionary(
                property => property.Property.Name,
                StringComparer.OrdinalIgnoreCase);
            var result = new List<IndexedFilter>();

            foreach (var filter in query.Filters.Where(filter => !partitionNames.Contains(filter.Field)))
            {
                if (!indexedByName.TryGetValue(filter.Field, out var property))
                {
                    throw new NotSupportedException(
                        $"Filter {filter.Field} is not declared as an indexed Cassandra activity field. Register it with Index(...) before querying it.");
                }

                if (filter.Operator != StorageFilterOperator.Equal)
                {
                    throw new NotSupportedException(
                        $"Indexed Cassandra activity filter {filter.Field} currently supports equality only.");
                }

                result.Add(new IndexedFilter(property, filter.Value));
            }

            return result.AsReadOnly();
        }

        private string IndexName(CassandraRecordProperty property)
        {
            return $"{_map.TableName}_{property.ColumnName}_sai_idx";
        }

        private static string NormalizeCqlType(string type)
        {
            if (String.IsNullOrWhiteSpace(type)) return String.Empty;
            return type.Replace(" ", String.Empty).ToLowerInvariant();
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

        private sealed class ExistingColumn
        {
            public ExistingColumn(string type, string kind, int position)
            {
                Type = type;
                Kind = kind;
                Position = position;
            }

            public string Type { get; }
            public string Kind { get; }
            public int Position { get; }
        }

        private sealed class ExpectedColumn
        {
            public ExpectedColumn(string name, string type, string kind, int position)
            {
                Name = name;
                Type = type;
                Kind = kind;
                Position = position;
            }

            public string Name { get; }
            public string Type { get; }
            public string Kind { get; }
            public int Position { get; }
        }

        private sealed class ExistingIndex
        {
            public ExistingIndex(string tableName, string target, string kind, string className)
            {
                TableName = tableName;
                Target = target;
                Kind = kind;
                ClassName = className;
            }

            public string TableName { get; }
            public string Target { get; }
            public string Kind { get; }
            public string ClassName { get; }
            public bool IsSai => String.Equals(Kind, "CUSTOM", StringComparison.OrdinalIgnoreCase) &&
                (String.Equals(ClassName, "sai", StringComparison.OrdinalIgnoreCase) ||
                 String.Equals(ClassName, "StorageAttachedIndex", StringComparison.OrdinalIgnoreCase) ||
                 (!String.IsNullOrWhiteSpace(ClassName) && ClassName.EndsWith(".StorageAttachedIndex", StringComparison.OrdinalIgnoreCase)));
        }

        private sealed class IndexedFilter
        {
            public IndexedFilter(CassandraRecordProperty property, object value)
            {
                Property = property;
                Value = value;
            }

            public CassandraRecordProperty Property { get; }
            public object Value { get; }
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
