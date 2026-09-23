using Cassandra;
using LagoVista.CloudStorage.Interfaces;
using LagoVista.CloudStorage.Models;
using LagoVista.CloudStorage.Storage.ConnectionSettings;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.CloudStorage.Storage.StorageProviders.Cassandra
{
    public sealed class CassandraAdminRepo : ICassandraAdminRepo
    {
        private readonly ICassandraSessionFactory _sessionFactory;
        private readonly ICassandraStorageSettings _settings;

        public CassandraAdminRepo(ICassandraSessionFactory sessionFactory, ICassandraStorageSettings settings)
        {
            _sessionFactory = sessionFactory ?? throw new ArgumentNullException(nameof(sessionFactory));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        public string Keyspace => _settings.Keyspace;

        public async Task<IReadOnlyList<CassandraTableInfo>> GetTablesAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var session = await _sessionFactory.GetSessionAsync().ConfigureAwait(false);
            var statement = new SimpleStatement(
                "SELECT table_name FROM system_schema.tables WHERE keyspace_name = ?", _settings.Keyspace);
            var rows = await session.ExecuteAsync(statement).ConfigureAwait(false);

            return rows.Select(row => new CassandraTableInfo { Name = row.GetValue<string>("table_name") })
                .OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public async Task<CassandraQueryResult> QueryAsync(CassandraQueryRequest request, CancellationToken cancellationToken = default)
        {
            if (request == null || String.IsNullOrWhiteSpace(request.Cql))
                throw new ArgumentException("CQL is required.", nameof(request));

            var cql = request.Cql.Trim();
            if (!cql.StartsWith("SELECT ", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Query endpoint only accepts SELECT statements.");

            cancellationToken.ThrowIfCancellationRequested();
            var session = await _sessionFactory.GetSessionAsync().ConfigureAwait(false);
            var statement = new SimpleStatement(cql)
                .SetPageSize(Math.Max(1, Math.Min(request.PageSize, 500)));
            var rows = await session.ExecuteAsync(statement).ConfigureAwait(false);
            var result = new CassandraQueryResult();

            var columns = rows.Columns.Select(column => column.Name).ToList();
            result.Columns.AddRange(columns);

            foreach (var row in rows.Take(Math.Max(1, Math.Min(request.PageSize, 500))))
            {
                var values = new Dictionary<string, object>(StringComparer.Ordinal);
                for (var index = 0; index < columns.Count; index++)
                {
                    var value = row.GetValue<object>(index);
                    values[columns[index]] = NormalizeValue(value);
                }

                result.Rows.Add(JsonConvert.SerializeObject(values));
            }

            return result;
        }

        public async Task ExecuteAsync(CassandraExecuteRequest request, CancellationToken cancellationToken = default)
        {
            if (request == null || String.IsNullOrWhiteSpace(request.Cql))
                throw new ArgumentException("CQL is required.", nameof(request));

            var cql = request.Cql.Trim();
            var allowed = new[] { "INSERT ", "UPDATE ", "DELETE " };
            if (!allowed.Any(prefix => cql.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException("Mutation endpoint only accepts INSERT, UPDATE, or DELETE statements.");

            cancellationToken.ThrowIfCancellationRequested();
            var session = await _sessionFactory.GetSessionAsync().ConfigureAwait(false);
            await session.ExecuteAsync(new SimpleStatement(cql)).ConfigureAwait(false);
        }

        private static object NormalizeValue(object value)
        {
            if (value == null) return null;
            if (value is byte[] bytes) return Convert.ToBase64String(bytes);
            if (value is DateTime dateTime) return dateTime.ToUniversalTime().ToString("O");
            if (value is DateTimeOffset dateTimeOffset) return dateTimeOffset.ToUniversalTime().ToString("O");
            if (value is System.Collections.IDictionary dictionary)
            {
                var normalized = new Dictionary<string, object>();
                foreach (System.Collections.DictionaryEntry item in dictionary)
                    normalized[Convert.ToString(item.Key)] = NormalizeValue(item.Value);
                return normalized;
            }

            if (value is System.Collections.IEnumerable enumerable && !(value is string))
            {
                var items = new List<object>();
                foreach (var item in enumerable) items.Add(NormalizeValue(item));
                return items;
            }

            return value;
        }
    }
}
