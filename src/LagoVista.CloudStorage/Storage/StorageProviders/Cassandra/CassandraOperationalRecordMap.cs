using Cassandra;
using LagoVista;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace LagoVista.CloudStorage.Storage
{
    [CriticalCoverage]
    internal sealed class CassandraOperationalRecordMap<TRecord>
        where TRecord : class, IOperationalDataRecord, new()
    {
        private readonly Dictionary<string, CassandraRecordProperty> _byPropertyName;

        public CassandraOperationalRecordMap(OperationalDataStoreOptions<TRecord> options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));
            Definition = options.Definition;

            Properties = typeof(TRecord)
                .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(property => property.CanRead && property.CanWrite)
                .Select(CreateProperty)
                .ToList()
                .AsReadOnly();

            _byPropertyName = Properties.ToDictionary(property => property.Property.Name, StringComparer.OrdinalIgnoreCase);
            Key = GetRequired(Definition.KeyField);
            PartitionProperties = Definition.PartitionFields.Select(GetRequired).ToList().AsReadOnly();
            IndexedProperties = Definition.IndexedFields.Select(GetRequired).ToList().AsReadOnly();

            if (!String.Equals(Key.Property.Name, nameof(IOperationalDataRecord.Id), StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Cassandra operational records are conventionally keyed by Id.");
            }

            if (PartitionProperties.Count != 1 || !String.Equals(PartitionProperties[0].Property.Name, nameof(IOperationalDataRecord.OrganizationId), StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Cassandra operational records are conventionally partitioned by OrganizationId only.");
            }

            if (IndexedProperties.Any(property => PartitionProperties.Any(partition => partition.Property.Name == property.Property.Name)))
            {
                throw new InvalidOperationException("Cassandra operational indexed fields must be non-partition fields. Partition fields are already queryable by equality.");
            }

            if (Definition.BucketPeriod != StoragePeriod.All)
            {
                throw new InvalidOperationException("Cassandra operational storage does not support time buckets.");
            }

            if (Definition.Retention.HasValue && Definition.Retention.Value.TotalSeconds > Int32.MaxValue)
            {
                throw new InvalidOperationException($"Cassandra operational retention for {typeof(TRecord).Name} cannot exceed {Int32.MaxValue} seconds.");
            }

            TableName = ToSnakeCase(typeof(TRecord).Name);
        }

        public StorageDefinition<TRecord> Definition { get; }
        public string TableName { get; }
        public IReadOnlyList<CassandraRecordProperty> Properties { get; }
        public IReadOnlyList<CassandraRecordProperty> PartitionProperties { get; }
        public IReadOnlyList<CassandraRecordProperty> IndexedProperties { get; }
        public CassandraRecordProperty Key { get; }
        public int RetentionSeconds => Definition.Retention.HasValue ? (int)Math.Ceiling(Definition.Retention.Value.TotalSeconds) : 0;

        public string CreateTableCql()
        {
            var columns = String.Join(",\n    ", Properties.Select(property => $"{property.ColumnName} {property.CqlType}"));
            var partition = String.Join(", ", PartitionProperties.Select(property => property.ColumnName));
            return $@"CREATE TABLE IF NOT EXISTS {TableName} (
    {columns},
    PRIMARY KEY (({partition}), {Key.ColumnName})
) WITH CLUSTERING ORDER BY ({Key.ColumnName} ASC)
AND default_time_to_live = {RetentionSeconds}";
        }

        public string ReconcileRetentionCql() => $"ALTER TABLE {TableName} WITH default_time_to_live = {RetentionSeconds}";

        public string UpsertCql()
        {
            var columns = Properties.Select(property => property.ColumnName).ToList();
            var markers = String.Join(", ", columns.Select(_ => "?"));
            return $"INSERT INTO {TableName} ({String.Join(", ", columns)}) VALUES ({markers})";
        }

        public object[] Values(TRecord record) => Properties
            .Select(property => ToDriverValue(property.Property.GetValue(record), property.Property.PropertyType))
            .ToArray();

        public object DriverValue(CassandraRecordProperty property, object value)
        {
            if (property == null) throw new ArgumentNullException(nameof(property));
            return ToDriverValue(value, property.Property.PropertyType);
        }

        public TRecord Read(Row row)
        {
            var record = new TRecord();
            foreach (var property in Properties)
            {
                property.Property.SetValue(record, ReadDriverValue(row, property));
            }
            return record;
        }

        public CassandraRecordProperty GetRequired(string propertyName)
        {
            if (String.IsNullOrWhiteSpace(propertyName) || !_byPropertyName.TryGetValue(propertyName, out var property))
            {
                throw new InvalidOperationException($"Property '{propertyName}' is not available on {typeof(TRecord).Name}.");
            }
            return property;
        }

        private static CassandraRecordProperty CreateProperty(PropertyInfo property)
        {
            var type = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
            string cqlType;

            if (type == typeof(string)) cqlType = "text";
            else if (type == typeof(DateTime) || type == typeof(DateTimeOffset)) cqlType = "timestamp";
            else if (type == typeof(Guid)) cqlType = "uuid";
            else if (type == typeof(bool)) cqlType = "boolean";
            else if (type == typeof(int)) cqlType = "int";
            else if (type == typeof(long)) cqlType = "bigint";
            else if (type == typeof(short)) cqlType = "smallint";
            else if (type == typeof(float)) cqlType = "float";
            else if (type == typeof(double)) cqlType = "double";
            else if (type == typeof(decimal)) cqlType = "decimal";
            else if (type == typeof(byte[])) cqlType = "blob";
            else throw new NotSupportedException($"Cassandra operational record property {property.DeclaringType?.Name}.{property.Name} uses unsupported type {property.PropertyType.Name}.");

            return new CassandraRecordProperty(property, ToSnakeCase(property.Name), cqlType);
        }

        private static object ToDriverValue(object value, Type propertyType)
        {
            if (value == null) return null;
            var type = Nullable.GetUnderlyingType(propertyType) ?? propertyType;
            if (type == typeof(DateTime))
            {
                var date = (DateTime)value;
                if (date.Kind == DateTimeKind.Local) date = date.ToUniversalTime();
                else if (date.Kind == DateTimeKind.Unspecified) date = DateTime.SpecifyKind(date, DateTimeKind.Utc);
                return new DateTimeOffset(date);
            }
            return value;
        }

        private static object ReadDriverValue(Row row, CassandraRecordProperty property)
        {
            var propertyType = property.Property.PropertyType;
            var targetType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;
            if (row.IsNull(property.ColumnName)) return null;
            if (targetType == typeof(DateTime)) return row.GetValue<DateTimeOffset>(property.ColumnName).UtcDateTime;
            if (targetType == typeof(DateTimeOffset)) return row.GetValue<DateTimeOffset>(property.ColumnName);
            if (targetType == typeof(string)) return row.GetValue<string>(property.ColumnName);
            if (targetType == typeof(Guid)) return row.GetValue<Guid>(property.ColumnName);
            if (targetType == typeof(bool)) return row.GetValue<bool>(property.ColumnName);
            if (targetType == typeof(int)) return row.GetValue<int>(property.ColumnName);
            if (targetType == typeof(long)) return row.GetValue<long>(property.ColumnName);
            if (targetType == typeof(short)) return row.GetValue<short>(property.ColumnName);
            if (targetType == typeof(float)) return row.GetValue<float>(property.ColumnName);
            if (targetType == typeof(double)) return row.GetValue<double>(property.ColumnName);
            if (targetType == typeof(decimal)) return row.GetValue<decimal>(property.ColumnName);
            if (targetType == typeof(byte[])) return row.GetValue<byte[]>(property.ColumnName);
            throw new NotSupportedException($"Unsupported Cassandra property type {propertyType.Name}.");
        }

        private static string ToSnakeCase(string value)
        {
            if (String.IsNullOrWhiteSpace(value)) throw new ArgumentNullException(nameof(value));
            var builder = new StringBuilder(value.Length + 8);
            for (var index = 0; index < value.Length; index++)
            {
                var character = value[index];
                if (Char.IsUpper(character) && index > 0) builder.Append('_');
                builder.Append(Char.ToLowerInvariant(character));
            }
            return builder.ToString();
        }
    }
}
