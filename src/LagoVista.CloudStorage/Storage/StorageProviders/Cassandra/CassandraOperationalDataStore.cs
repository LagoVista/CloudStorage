using Cassandra;
using LagoVista;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.CloudStorage.Storage
{
    /// <summary>
    /// Cassandra implementation for mutable operational records. Operational data
    /// is intentionally key-oriented rather than time-oriented: organization scope
    /// is the partition key and Id is the clustering key by convention.
    /// </summary>
    [CriticalCoverage]
    public sealed class CassandraOperationalDataStore<TRecord> : IOperationalDataStore<TRecord>
        where TRecord : class, IOperationalDataRecord, new()
    {
        private const int BatchWriteConcurrency = 16;
        private readonly ICassandraSessionFactory _sessionFactory;
        private readonly CassandraOperationalRecordMap<TRecord> _map;
        private readonly SemaphoreSlim _schemaLock = new SemaphoreSlim(1, 1);
        private volatile bool _schemaReady;
        private PreparedStatement _upsert;
        private PreparedStatement _get;
        private PreparedStatement _delete;

        public CassandraOperationalDataStore(
            ICassandraSessionFactory sessionFactory,
            OperationalDataStoreOptions<TRecord> options)
        {
            _sessionFactory = sessionFactory ?? throw new ArgumentNullException(nameof(sessionFactory));
            _map = new CassandraOperationalRecordMap<TRecord>(options);
        }

        public async Task<TRecord> GetAsync(string organizationId, string id, CancellationToken cancellationToken = default)
        {
            ValidateIdentity(organizationId, id);
            cancellationToken.ThrowIfCancellationRequested();

            var session = await GetReadySessionAsync().ConfigureAwait(false);
            var statement = await GetGetAsync(session).ConfigureAwait(false);
            var rows = await session.ExecuteAsync(statement.Bind(BuildIdentityValues(organizationId, id))).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            var row = rows.FirstOrDefault();
            return row == null ? null : _map.Read(row);
        }

        public async Task UpsertAsync(TRecord record, CancellationToken cancellationToken = default)
        {
            if (record == null) throw new ArgumentNullException(nameof(record));
            ValidateIdentity(record.OrganizationId, record.Id);
            cancellationToken.ThrowIfCancellationRequested();

            Stamp(record);
            var session = await GetReadySessionAsync().ConfigureAwait(false);
            var statement = await GetUpsertAsync(session).ConfigureAwait(false);
            await session.ExecuteAsync(statement.Bind(_map.Values(record))).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
        }

        public async Task UpsertBatchAsync(IEnumerable<TRecord> records, CancellationToken cancellationToken = default)
        {
            if (records == null) throw new ArgumentNullException(nameof(records));
            cancellationToken.ThrowIfCancellationRequested();

            var materialized = records.ToList();
            if (materialized.Count == 0) return;
            if (materialized.Any(record => record == null)) throw new ArgumentException("Operational record batches cannot contain null records.", nameof(records));

            foreach (var record in materialized)
            {
                ValidateIdentity(record.OrganizationId, record.Id);
                Stamp(record);
            }

            var session = await GetReadySessionAsync().ConfigureAwait(false);
            var statement = await GetUpsertAsync(session).ConfigureAwait(false);

            for (var offset = 0; offset < materialized.Count; offset += BatchWriteConcurrency)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var count = Math.Min(BatchWriteConcurrency, materialized.Count - offset);
                var writes = new Task<RowSet>[count];
                for (var index = 0; index < count; index++)
                {
                    writes[index] = session.ExecuteAsync(statement.Bind(_map.Values(materialized[offset + index])));
                }
                await Task.WhenAll(writes).ConfigureAwait(false);
            }

            cancellationToken.ThrowIfCancellationRequested();
        }

        public async Task DeleteAsync(string organizationId, string id, CancellationToken cancellationToken = default)
        {
            ValidateIdentity(organizationId, id);
            cancellationToken.ThrowIfCancellationRequested();

            var session = await GetReadySessionAsync().ConfigureAwait(false);
            var statement = await GetDeleteAsync(session).ConfigureAwait(false);
            await session.ExecuteAsync(statement.Bind(BuildIdentityValues(organizationId, id))).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
        }

        public async Task<StoragePageResult<TRecord>> QueryAsync(StorageQuery<TRecord> query, CancellationToken cancellationToken = default)
        {
            if (query == null) throw new ArgumentNullException(nameof(query));
            if (query.Sorts.Count > 0) throw new NotSupportedException("Cassandra operational queries do not support caller-defined sorting. Records are returned in Id order within the partition.");
            cancellationToken.ThrowIfCancellationRequested();

            var partitionValues = ResolvePartitionValues(query);
            var indexedFilters = ResolveIndexedFilters(query);
            byte[] pagingState = null;
            if (!String.IsNullOrWhiteSpace(query.Page.ContinuationToken))
            {
                try { pagingState = Convert.FromBase64String(query.Page.ContinuationToken); }
                catch (FormatException ex) { throw new ArgumentException("The operational query continuation token is invalid.", nameof(query), ex); }
            }

            var clauses = new List<string>();
            var values = new List<object>();
            foreach (var partition in _map.PartitionProperties)
            {
                clauses.Add($"{partition.ColumnName} = ?");
                values.Add(partitionValues[partition.Property.Name]);
            }

            foreach (var filter in indexedFilters)
            {
                clauses.Add($"{filter.Property.ColumnName} = ?");
                values.Add(_map.DriverValue(filter.Property, filter.Value));
            }

            var cql = $"SELECT {String.Join(", ", _map.Properties.Select(property => property.ColumnName))} FROM {_map.TableName} WHERE {String.Join(" AND ", clauses)}";
            var session = await GetReadySessionAsync().ConfigureAwait(false);
            var prepared = await session.PrepareAsync(cql).ConfigureAwait(false);
            var statement = prepared.Bind(values.ToArray()).SetPageSize(query.Page.PageSize).SetAutoPage(false);
            if (pagingState != null && pagingState.Length > 0) statement.SetPagingState(pagingState);

            var rows = await session.ExecuteAsync(statement).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            var next = rows.PagingState == null || rows.PagingState.Length == 0 ? null : Convert.ToBase64String(rows.PagingState);
            return new StoragePageResult<TRecord>(rows.Select(_map.Read).ToList(), next);
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
                row => new ExistingColumn(row.GetValue<string>("type"), row.GetValue<string>("kind"), row.GetValue<int>("position")),
                StringComparer.OrdinalIgnoreCase);

            foreach (var expected in ExpectedColumns())
            {
                if (!existing.TryGetValue(expected.Name, out var actual))
                {
                    if (expected.Kind != "regular")
                    {
                        throw new InvalidOperationException($"Cassandra operational table {_map.TableName} is missing required {expected.Kind} column {expected.Name}. Primary-key changes require an explicit migration.");
                    }
                    await session.ExecuteAsync(new SimpleStatement($"ALTER TABLE {_map.TableName} ADD {expected.Name} {expected.Type}")).ConfigureAwait(false);
                    continue;
                }

                if (!String.Equals(NormalizeCqlType(actual.Type), NormalizeCqlType(expected.Type), StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException($"Cassandra operational table {_map.TableName} column {expected.Name} has type {actual.Type}, but {typeof(TRecord).Name} requires {expected.Type}. Type changes require an explicit migration.");
                }

                if (!String.Equals(actual.Kind, expected.Kind, StringComparison.OrdinalIgnoreCase) || (expected.Kind != "regular" && actual.Position != expected.Position))
                {
                    throw new InvalidOperationException($"Cassandra operational table {_map.TableName} column {expected.Name} has key shape {actual.Kind}[{actual.Position}], but {typeof(TRecord).Name} requires {expected.Kind}[{expected.Position}]. Primary-key changes require an explicit migration.");
                }
            }
        }

        private async Task ReconcileIndexesAsync(ISession session)
        {
            foreach (var property in _map.IndexedProperties)
            {
                var indexName = $"{_map.TableName}_{property.ColumnName}_sai_idx";
                var existing = await ReadIndexAsync(session, indexName).ConfigureAwait(false);
                if (existing == null)
                {
                    await session.ExecuteAsync(new SimpleStatement($"CREATE INDEX IF NOT EXISTS {indexName} ON {_map.TableName} ({property.ColumnName}) USING 'sai'")).ConfigureAwait(false);
                    existing = await ReadIndexAsync(session, indexName).ConfigureAwait(false);
                }

                if (existing == null || !String.Equals(existing.TableName, _map.TableName, StringComparison.OrdinalIgnoreCase) || !String.Equals(existing.Target, property.ColumnName, StringComparison.OrdinalIgnoreCase) || !existing.IsSai)
                {
                    throw new InvalidOperationException($"Cassandra operational index {indexName} does not match expected SAI target {_map.TableName}.{property.ColumnName}. Index changes require an explicit migration.");
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
                return new ExistingIndex(row.GetValue<string>("table_name"), target, className);
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
            expected.Add(new ExpectedColumn(_map.Key.ColumnName, _map.Key.CqlType, "clustering", 0));
            foreach (var property in _map.Properties)
            {
                if (partitionNames.Contains(property.ColumnName) || String.Equals(property.ColumnName, _map.Key.ColumnName, StringComparison.OrdinalIgnoreCase)) continue;
                expected.Add(new ExpectedColumn(property.ColumnName, property.CqlType, "regular", -1));
            }
            return expected;
        }

        private async Task<PreparedStatement> GetUpsertAsync(ISession session)
        {
            if (_upsert != null) return _upsert;
            _upsert = await session.PrepareAsync(_map.UpsertCql()).ConfigureAwait(false);
            return _upsert;
        }

        private async Task<PreparedStatement> GetGetAsync(ISession session)
        {
            if (_get != null) return _get;
            var where = String.Join(" AND ", _map.PartitionProperties.Select(property => $"{property.ColumnName} = ?").Concat(new[] { $"{_map.Key.ColumnName} = ?" }));
            _get = await session.PrepareAsync($"SELECT {String.Join(", ", _map.Properties.Select(property => property.ColumnName))} FROM {_map.TableName} WHERE {where}").ConfigureAwait(false);
            return _get;
        }

        private async Task<PreparedStatement> GetDeleteAsync(ISession session)
        {
            if (_delete != null) return _delete;
            var where = String.Join(" AND ", _map.PartitionProperties.Select(property => $"{property.ColumnName} = ?").Concat(new[] { $"{_map.Key.ColumnName} = ?" }));
            _delete = await session.PrepareAsync($"DELETE FROM {_map.TableName} WHERE {where}").ConfigureAwait(false);
            return _delete;
        }

        private object[] BuildIdentityValues(string organizationId, string id)
        {
            if (_map.PartitionProperties.Count != 1 || !String.Equals(_map.PartitionProperties[0].Property.Name, nameof(IOperationalDataRecord.OrganizationId), StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Get/Delete convenience methods require the conventional OrganizationId partition. Use QueryAsync for custom partition definitions.");
            }
            return new[] { _map.DriverValue(_map.PartitionProperties[0], organizationId), _map.DriverValue(_map.Key, id) };
        }

        private Dictionary<string, object> ResolvePartitionValues(StorageQuery<TRecord> query)
        {
            var result = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            foreach (var partition in _map.PartitionProperties)
            {
                var matches = query.Filters.Where(filter => String.Equals(filter.Field, partition.Property.Name, StringComparison.OrdinalIgnoreCase)).ToList();
                if (matches.Count != 1 || matches[0].Operator != StorageFilterOperator.Equal)
                {
                    throw new InvalidOperationException($"Cassandra operational queries require exactly one equality filter for partition field {partition.Property.Name}.");
                }
                result[partition.Property.Name] = _map.DriverValue(partition, matches[0].Value);
            }
            return result;
        }

        private IReadOnlyList<IndexedFilter> ResolveIndexedFilters(StorageQuery<TRecord> query)
        {
            var partitionNames = new HashSet<string>(_map.PartitionProperties.Select(property => property.Property.Name), StringComparer.OrdinalIgnoreCase);
            var indexedByName = _map.IndexedProperties.ToDictionary(property => property.Property.Name, StringComparer.OrdinalIgnoreCase);
            var result = new List<IndexedFilter>();
            foreach (var filter in query.Filters.Where(filter => !partitionNames.Contains(filter.Field)))
            {
                if (!indexedByName.TryGetValue(filter.Field, out var property))
                {
                    throw new NotSupportedException($"Filter {filter.Field} is not declared as an indexed Cassandra operational field. Register it with Index(...) before querying it.");
                }
                if (filter.Operator != StorageFilterOperator.Equal)
                {
                    throw new NotSupportedException($"Indexed Cassandra operational filter {filter.Field} currently supports equality only.");
                }
                result.Add(new IndexedFilter(property, filter.Value));
            }
            return result.AsReadOnly();
        }

        private static void Stamp(TRecord record)
        {
            var now = DateTime.UtcNow;
            if (record.CreationDate == default) record.CreationDate = now;
            else if (record.CreationDate.Kind != DateTimeKind.Utc) record.CreationDate = record.CreationDate.ToUniversalTime();
            record.LastUpdatedDate = now;
        }

        private static void ValidateIdentity(string organizationId, string id)
        {
            if (String.IsNullOrWhiteSpace(organizationId)) throw new ArgumentNullException(nameof(organizationId));
            if (String.IsNullOrWhiteSpace(id)) throw new ArgumentNullException(nameof(id));
        }

        private static string NormalizeCqlType(string type) => String.IsNullOrWhiteSpace(type) ? String.Empty : type.Replace(" ", String.Empty).ToLowerInvariant();

        private sealed class ExistingColumn
        {
            public ExistingColumn(string type, string kind, int position) { Type = type; Kind = kind; Position = position; }
            public string Type { get; }
            public string Kind { get; }
            public int Position { get; }
        }

        private sealed class ExpectedColumn
        {
            public ExpectedColumn(string name, string type, string kind, int position) { Name = name; Type = type; Kind = kind; Position = position; }
            public string Name { get; }
            public string Type { get; }
            public string Kind { get; }
            public int Position { get; }
        }

        private sealed class ExistingIndex
        {
            public ExistingIndex(string tableName, string target, string className) { TableName = tableName; Target = target; ClassName = className; }
            public string TableName { get; }
            public string Target { get; }
            public string ClassName { get; }
            public bool IsSai => !String.IsNullOrWhiteSpace(ClassName) && ClassName.IndexOf("StorageAttachedIndex", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private sealed class IndexedFilter
        {
            public IndexedFilter(CassandraRecordProperty property, object value) { Property = property; Value = value; }
            public CassandraRecordProperty Property { get; }
            public object Value { get; }
        }
    }
}
