using LagoVista;
using LagoVista.CloudStorage.Storage;
using LagoVista.CloudStorage.Storage.ConnectionSettings;
using Npgsql;
using NpgsqlTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.Relational.Storage
{
    [CriticalCoverage]
    public sealed class PostgresMetricsStore : IMetricsStore
    {
        private readonly IMetricsStorageSettings _settings;
        private readonly string _schema;
        private readonly string _definitionsTable;
        private readonly string _recordsTable;
        private readonly SemaphoreSlim _schemaLock = new SemaphoreSlim(1, 1);
        private volatile bool _schemaReady;

        public PostgresMetricsStore(IMetricsStorageSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            if (String.IsNullOrWhiteSpace(settings.SchemaName)) throw new ArgumentException("Metrics schema name is required.", nameof(settings));

            _schema = QuoteIdentifier(settings.SchemaName);
            _definitionsTable = $"{_schema}.{QuoteIdentifier("metric_definitions")}";
            _recordsTable = $"{_schema}.{QuoteIdentifier("metric_records")}";
        }

        public async Task RegisterDefinitionAsync(MetricDefinition definition, CancellationToken cancellationToken = default)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            cancellationToken.ThrowIfCancellationRequested();

            await using var connection = await OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
            var dimensionsJson = JsonSerializer.Serialize(definition.Dimensions.Select(dimension => new StoredMetricDimension
            {
                Key = dimension.Key,
                Name = dimension.Name,
                QueryImportant = dimension.QueryImportant
            }));

            var sql = $@"
INSERT INTO {_definitionsTable} (id, key, name, dimensions)
VALUES (@id, @key, @name, CAST(@dimensions AS jsonb))
ON CONFLICT (id) DO UPDATE SET
    key = EXCLUDED.key,
    name = EXCLUDED.name,
    dimensions = EXCLUDED.dimensions;";

            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("id", definition.Id);
            command.Parameters.AddWithValue("key", definition.Key);
            command.Parameters.AddWithValue("name", definition.Name);
            command.Parameters.AddWithValue("dimensions", dimensionsJson);
            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        public async Task<MetricDefinition> GetDefinitionAsync(string metric, CancellationToken cancellationToken = default)
        {
            if (String.IsNullOrWhiteSpace(metric)) throw new ArgumentNullException(nameof(metric));
            cancellationToken.ThrowIfCancellationRequested();

            await using var connection = await OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
            return await GetDefinitionAsync(connection, metric, cancellationToken).ConfigureAwait(false);
        }

        public async Task RecordAsync(MetricRecord record, CancellationToken cancellationToken = default)
        {
            if (record == null) throw new ArgumentNullException(nameof(record));
            cancellationToken.ThrowIfCancellationRequested();

            await using var connection = await OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
            var definition = await RequireDefinitionAsync(connection, record.Metric, cancellationToken).ConfigureAwait(false);
            ValidateDimensions(definition, record.Dimensions.Keys, "record");
            await InsertRecordAsync(connection, record, definition.Key, cancellationToken).ConfigureAwait(false);
        }

        public async Task RecordBatchAsync(IEnumerable<MetricRecord> records, CancellationToken cancellationToken = default)
        {
            if (records == null) throw new ArgumentNullException(nameof(records));
            cancellationToken.ThrowIfCancellationRequested();

            var materialized = records.ToList();
            if (materialized.Count == 0) return;
            if (materialized.Any(record => record == null)) throw new ArgumentException("Metric batches cannot contain null records.", nameof(records));

            await using var connection = await OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
            await using var transaction = await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
            var definitions = new Dictionary<string, MetricDefinition>(StringComparer.OrdinalIgnoreCase);

            foreach (var record in materialized)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!definitions.TryGetValue(record.Metric, out var definition))
                {
                    definition = await RequireDefinitionAsync(connection, record.Metric, cancellationToken).ConfigureAwait(false);
                    definitions[record.Metric] = definition;
                }

                ValidateDimensions(definition, record.Dimensions.Keys, "record");
                await InsertRecordAsync(connection, record, definition.Key, cancellationToken, transaction).ConfigureAwait(false);
            }

            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        }

        public async Task<MetricQueryResult> QueryAsync(MetricQuery query, CancellationToken cancellationToken = default)
        {
            if (query == null) throw new ArgumentNullException(nameof(query));
            cancellationToken.ThrowIfCancellationRequested();

            await using var connection = await OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
            var definition = await RequireDefinitionAsync(connection, query.Metric, cancellationToken).ConfigureAwait(false);
            ValidateDimensions(definition, query.Dimensions.Select(filter => filter.Key), "query filter");
            ValidateDimensions(definition, query.GroupByDimensions, "group-by");

            var parameters = new List<NpgsqlParameter>
            {
                new NpgsqlParameter("organization_id", query.OrganizationId),
                new NpgsqlParameter("metric", definition.Key),
                new NpgsqlParameter("start", query.Start),
                new NpgsqlParameter("end", query.End)
            };

            var where = new StringBuilder("organization_id = @organization_id AND metric = @metric AND timestamp >= @start AND timestamp <= @end");
            for (var index = 0; index < query.Dimensions.Count; index++)
            {
                where.Append($" AND dimensions ->> @filter_key_{index} = @filter_value_{index}");
                parameters.Add(new NpgsqlParameter($"filter_key_{index}", query.Dimensions[index].Key));
                parameters.Add(new NpgsqlParameter($"filter_value_{index}", query.Dimensions[index].Value));
            }

            var select = new List<string>();
            var groupBy = new List<string>();
            var hasBucket = query.Bucket.HasValue;
            if (hasBucket)
            {
                select.Add("time_bucket(@bucket, timestamp) AS bucket_timestamp");
                groupBy.Add("bucket_timestamp");
                parameters.Add(new NpgsqlParameter("bucket", NpgsqlDbType.Interval) { Value = query.Bucket.Value });
            }

            for (var index = 0; index < query.GroupByDimensions.Count; index++)
            {
                select.Add($"dimensions ->> @group_key_{index} AS group_value_{index}");
                groupBy.Add($"group_value_{index}");
                parameters.Add(new NpgsqlParameter($"group_key_{index}", query.GroupByDimensions[index]));
            }

            select.Add($"{AggregateExpression(query.Aggregate)} AS aggregate_value");
            var sql = new StringBuilder($"SELECT {String.Join(", ", select)} FROM {_recordsTable} WHERE {where}");
            if (groupBy.Count > 0) sql.Append($" GROUP BY {String.Join(", ", groupBy)}");
            if (hasBucket) sql.Append(" ORDER BY bucket_timestamp");

            await using var command = new NpgsqlCommand(sql.ToString(), connection);
            command.Parameters.AddRange(parameters.ToArray());
            await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            var values = new List<MetricValue>();

            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                var ordinal = 0;
                var timestamp = hasBucket ? reader.GetFieldValue<DateTime>(ordinal++) : query.Start;
                var dimensions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var groupKey in query.GroupByDimensions)
                {
                    dimensions[groupKey] = reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
                    ordinal++;
                }

                var value = reader.IsDBNull(ordinal) ? 0 : Convert.ToDouble(reader.GetValue(ordinal));
                values.Add(new MetricValue(NormalizeUtc(timestamp), value, dimensions));
            }

            return new MetricQueryResult(values);
        }

        private async Task<NpgsqlConnection> OpenConnectionAsync(CancellationToken cancellationToken)
        {
            await EnsureSchemaAsync(cancellationToken).ConfigureAwait(false);
            var connection = new NpgsqlConnection(BuildConnectionString());
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
            return connection;
        }

        private async Task EnsureSchemaAsync(CancellationToken cancellationToken)
        {
            if (_schemaReady) return;

            await _schemaLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                if (_schemaReady) return;

                await using var connection = new NpgsqlConnection(BuildConnectionString());
                await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
                var sql = $@"
CREATE EXTENSION IF NOT EXISTS timescaledb;
CREATE SCHEMA IF NOT EXISTS {_schema};
CREATE TABLE IF NOT EXISTS {_definitionsTable} (
    id text PRIMARY KEY,
    key text NOT NULL UNIQUE,
    name text NOT NULL,
    dimensions jsonb NOT NULL DEFAULT '[]'::jsonb
);
CREATE TABLE IF NOT EXISTS {_recordsTable} (
    id text NOT NULL,
    organization_id text NOT NULL,
    organization text NOT NULL,
    metric text NOT NULL,
    timestamp timestamptz NOT NULL,
    value double precision NOT NULL,
    dimensions jsonb NOT NULL DEFAULT '{{}}'::jsonb,
    PRIMARY KEY (id, timestamp)
);
CREATE INDEX IF NOT EXISTS ix_metric_records_org_metric_timestamp ON {_recordsTable} (organization_id, metric, timestamp DESC);
CREATE INDEX IF NOT EXISTS ix_metric_records_dimensions ON {_recordsTable} USING GIN (dimensions);
SELECT create_hypertable(format('%I.%I', @schema_name, 'metric_records')::regclass, 'timestamp', if_not_exists => TRUE, migrate_data => TRUE);";

                await using var command = new NpgsqlCommand(sql, connection);
                command.Parameters.AddWithValue("schema_name", _settings.SchemaName);
                await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                _schemaReady = true;
            }
            finally
            {
                _schemaLock.Release();
            }
        }

        private async Task<MetricDefinition> RequireDefinitionAsync(NpgsqlConnection connection, string metric, CancellationToken cancellationToken)
        {
            var definition = await GetDefinitionAsync(connection, metric, cancellationToken).ConfigureAwait(false);
            if (definition == null) throw new InvalidOperationException($"Metric definition '{metric}' has not been registered.");
            return definition;
        }

        private async Task<MetricDefinition> GetDefinitionAsync(NpgsqlConnection connection, string metric, CancellationToken cancellationToken)
        {
            var sql = $"SELECT id, key, name, dimensions::text FROM {_definitionsTable} WHERE id = @metric OR key = @metric LIMIT 1";
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("metric", metric.Trim());
            await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false)) return null;

            var storedDimensions = JsonSerializer.Deserialize<List<StoredMetricDimension>>(reader.GetString(3)) ?? new List<StoredMetricDimension>();
            return new MetricDefinition(reader.GetString(0), reader.GetString(1), reader.GetString(2), storedDimensions.Select(dimension => new MetricDimensionDefinition(dimension.Key, dimension.Name, dimension.QueryImportant)));
        }

        private async Task InsertRecordAsync(NpgsqlConnection connection, MetricRecord record, string metricKey, CancellationToken cancellationToken, NpgsqlTransaction transaction = null)
        {
            var sql = $@"
INSERT INTO {_recordsTable} (id, organization_id, organization, metric, timestamp, value, dimensions)
VALUES (@id, @organization_id, @organization, @metric, @timestamp, @value, CAST(@dimensions AS jsonb));";
            await using var command = new NpgsqlCommand(sql, connection, transaction);
            command.Parameters.AddWithValue("id", record.Id);
            command.Parameters.AddWithValue("organization_id", record.OrganizationId);
            command.Parameters.AddWithValue("organization", record.Organization);
            command.Parameters.AddWithValue("metric", metricKey);
            command.Parameters.AddWithValue("timestamp", record.Timestamp);
            command.Parameters.AddWithValue("value", record.Value);
            command.Parameters.AddWithValue("dimensions", JsonSerializer.Serialize(record.Dimensions));
            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        private string BuildConnectionString()
        {
            return new NpgsqlConnectionStringBuilder
            {
                Host = _settings.HostName,
                Port = _settings.Port,
                Username = _settings.UserName,
                Password = _settings.Password,
                Timeout = 10,
                CommandTimeout = 30,
                Pooling = true
            }.ConnectionString;
        }

        private static void ValidateDimensions(MetricDefinition definition, IEnumerable<string> keys, string usage)
        {
            var legal = new HashSet<string>(definition.Dimensions.Select(dimension => dimension.Key), StringComparer.OrdinalIgnoreCase);
            var invalid = keys.Where(key => !legal.Contains(key)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            if (invalid.Count > 0) throw new InvalidOperationException($"Metric '{definition.Key}' {usage} contains undeclared dimension(s): {String.Join(", ", invalid)}.");
        }

        private static string AggregateExpression(MetricAggregate aggregate)
        {
            switch (aggregate)
            {
                case MetricAggregate.Count: return "COUNT(*)::double precision";
                case MetricAggregate.Sum: return "SUM(value)";
                case MetricAggregate.Average: return "AVG(value)";
                case MetricAggregate.Minimum: return "MIN(value)";
                case MetricAggregate.Maximum: return "MAX(value)";
                default: throw new ArgumentOutOfRangeException(nameof(aggregate), aggregate, "Unsupported metric aggregate.");
            }
        }

        private static DateTime NormalizeUtc(DateTime value)
        {
            if (value.Kind == DateTimeKind.Utc) return value;
            if (value.Kind == DateTimeKind.Local) return value.ToUniversalTime();
            return DateTime.SpecifyKind(value, DateTimeKind.Utc);
        }

        private static string QuoteIdentifier(string identifier)
        {
            if (String.IsNullOrWhiteSpace(identifier)) throw new ArgumentNullException(nameof(identifier));
            return $"\"{identifier.Replace("\"", "\"\"")}\"";
        }

        private sealed class StoredMetricDimension
        {
            public string Key { get; set; }
            public string Name { get; set; }
            public bool QueryImportant { get; set; }
        }
    }
}
