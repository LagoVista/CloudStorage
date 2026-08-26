using Cassandra;
using LagoVista;
using LagoVista.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace LagoVista.CloudStorage.Storage
{
    internal sealed class CassandraRecordProperty
    {
        public CassandraRecordProperty(PropertyInfo property, string columnName, string cqlType)
        {
            Property = property;
            ColumnName = columnName;
            CqlType = cqlType;
        }

        public PropertyInfo Property { get; }
        public string ColumnName { get; }
        public string CqlType { get; }
    }

    [CriticalCoverage]
    internal sealed class CassandraRecordMap<TRecord>
        where TRecord : IActivityRecord, new()
    {
        public const string BucketColumnName = "time_bucket";
        private readonly Dictionary<string, CassandraRecordProperty> _byPropertyName;

        public CassandraRecordMap(ActivityRecordStoreOptions<TRecord> options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));
            Definition = options.Definition;

            if (Definition.PartitionFields.Count == 0)
            {
                throw new InvalidOperationException(
                    $"Cassandra activity storage for {typeof(TRecord).Name} requires at least one PartitionBy(...) field.");
            }

            Properties = typeof(TRecord)
                .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(property => property.CanRead && property.CanWrite)
                .Select(CreateProperty)
                .ToList()
                .AsReadOnly();

            _byPropertyName = Properties.ToDictionary(
                property => property.Property.Name,
                StringComparer.OrdinalIgnoreCase);

            Key = GetRequired(Definition.KeyField);
            Time = GetRequired(Definition.TimeField);
            PartitionProperties = Definition.PartitionFields.Select(GetRequired).ToList().AsReadOnly();
            IndexedProperties = Definition.IndexedFields.Select(GetRequired).ToList().AsReadOnly();

            if (PartitionProperties.Any(property => property.Property.Name == Key.Property.Name || property.Property.Name == Time.Property.Name))
            {
                throw new InvalidOperationException("Cassandra partition fields cannot be the activity Id or CreationDate fields.");
            }

            if (IndexedProperties.Any(property => PartitionProperties.Any(partition => partition.Property.Name == property.Property.Name)))
            {
                throw new InvalidOperationException("Cassandra activity indexed fields must be non-partition fields. Partition fields are already queryable by equality.");
            }

            if (Definition.Retention.HasValue && Definition.Retention.Value.TotalSeconds > Int32.MaxValue)
            {
                throw new InvalidOperationException(
                    $"Cassandra activity retention for {typeof(TRecord).Name} cannot exceed {Int32.MaxValue} seconds.");
            }

            TableName = ToSnakeCase(typeof(TRecord).Name);
        }

        public StorageDefinition<TRecord> Definition { get; }
        public string TableName { get; }
        public IReadOnlyList<CassandraRecordProperty> Properties { get; }
        public IReadOnlyList<CassandraRecordProperty> PartitionProperties { get; }
        public IReadOnlyList<CassandraRecordProperty> IndexedProperties { get; }
        public CassandraRecordProperty Key { get; }
        public CassandraRecordProperty Time { get; }
        public bool UsesTimeBuckets => Definition.BucketPeriod != StoragePeriod.All;
        public int RetentionSeconds => Definition.Retention.HasValue
            ? (int)Math.Ceiling(Definition.Retention.Value.TotalSeconds)
            : 0;

        public string CreateTableCql()
        {
            var columns = Properties.Select(property => $"{property.ColumnName} {property.CqlType}").ToList();
            if (UsesTimeBuckets) columns.Add($"{BucketColumnName} text");

            var partitionFields = PartitionProperties.Select(property => property.ColumnName).ToList();
            if (UsesTimeBuckets) partitionFields.Add(BucketColumnName);

            var columnText = String.Join(",\n    ", columns);
            var partition = String.Join(", ", partitionFields);

            return $@"CREATE TABLE IF NOT EXISTS {TableName} (
    {columnText},
    PRIMARY KEY (({partition}), {Time.ColumnName}, {Key.ColumnName})
) WITH CLUSTERING ORDER BY ({Time.ColumnName} DESC, {Key.ColumnName} ASC)
AND default_time_to_live = {RetentionSeconds}";
        }

        public string ReconcileRetentionCql()
        {
            return $"ALTER TABLE {TableName} WITH default_time_to_live = {RetentionSeconds}";
        }

        public string InsertCql()
        {
            var columns = Properties.Select(property => property.ColumnName).ToList();
            if (UsesTimeBuckets) columns.Add(BucketColumnName);
            var markers = String.Join(", ", columns.Select(_ => "?"));
            return $"INSERT INTO {TableName} ({String.Join(", ", columns)}) VALUES ({markers})";
        }

        public object[] Values(TRecord record)
        {
            var values = Properties
                .Select(property => ToDriverValue(property.Property.GetValue(record), property.Property.PropertyType))
                .ToList();

            if (UsesTimeBuckets)
            {
                values.Add(GetBucket(record.CreationDate));
            }

            return values.ToArray();
        }

        public object DriverValue(CassandraRecordProperty property, object value)
        {
            if (property == null) throw new ArgumentNullException(nameof(property));
            return ToDriverValue(value, property.Property.PropertyType);
        }

        public string GetBucket(DateTime value)
        {
            var utc = NormalizeUtc(value);
            switch (Definition.BucketPeriod)
            {
                case StoragePeriod.Month:
                    return utc.ToString("yyyy-MM");
                case StoragePeriod.Quarter:
                    return $"{utc:yyyy}-Q{((utc.Month - 1) / 3) + 1}";
                case StoragePeriod.Year:
                    return utc.ToString("yyyy");
                case StoragePeriod.All:
                    return null;
                default:
                    throw new NotSupportedException($"Storage period {Definition.BucketPeriod} is not supported for Cassandra activity bucketing.");
            }
        }

        public IReadOnlyList<string> GetBuckets(DateTime start, DateTime end)
        {
            if (!UsesTimeBuckets) return Array.Empty<string>();

            var startUtc = NormalizeUtc(start);
            var endUtc = NormalizeUtc(end);
            if (startUtc > endUtc) throw new ArgumentException("Bucket range start cannot be after end.");

            var buckets = new List<string>();
            var cursor = BucketStart(startUtc);
            var final = BucketStart(endUtc);

            while (cursor <= final)
            {
                buckets.Add(GetBucket(cursor));
                cursor = NextBucket(cursor);
            }

            buckets.Reverse();
            return buckets.AsReadOnly();
        }

        public TRecord Read(Row row)
        {
            var record = new TRecord();
            foreach (var property in Properties)
            {
                var value = ReadDriverValue(row, property);
                property.Property.SetValue(record, value);
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

        private DateTime BucketStart(DateTime value)
        {
            switch (Definition.BucketPeriod)
            {
                case StoragePeriod.Month:
                    return new DateTime(value.Year, value.Month, 1, 0, 0, 0, DateTimeKind.Utc);
                case StoragePeriod.Quarter:
                    var quarterMonth = (((value.Month - 1) / 3) * 3) + 1;
                    return new DateTime(value.Year, quarterMonth, 1, 0, 0, 0, DateTimeKind.Utc);
                case StoragePeriod.Year:
                    return new DateTime(value.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                default:
                    throw new NotSupportedException($"Storage period {Definition.BucketPeriod} is not supported for Cassandra activity bucketing.");
            }
        }

        private DateTime NextBucket(DateTime value)
        {
            switch (Definition.BucketPeriod)
            {
                case StoragePeriod.Month:
                    return value.AddMonths(1);
                case StoragePeriod.Quarter:
                    return value.AddMonths(3);
                case StoragePeriod.Year:
                    return value.AddYears(1);
                default:
                    throw new NotSupportedException($"Storage period {Definition.BucketPeriod} is not supported for Cassandra activity bucketing.");
            }
        }

        private static DateTime NormalizeUtc(DateTime value)
        {
            if (value.Kind == DateTimeKind.Utc) return value;
            if (value.Kind == DateTimeKind.Unspecified) return DateTime.SpecifyKind(value, DateTimeKind.Utc);
            return value.ToUniversalTime();
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
            else
            {
                throw new NotSupportedException(
                    $"Cassandra activity record property {property.DeclaringType?.Name}.{property.Name} uses unsupported type {property.PropertyType.Name}.");
            }

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

        internal static string ToSnakeCase(string value)
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
