using System;
using System.Collections.Generic;
using System.Linq;

namespace LagoVista.CloudStorage.Storage
{
    public enum MetricAggregate
    {
        Count,
        Sum,
        Average,
        Minimum,
        Maximum
    }

    public sealed class MetricDimensionDefinition
    {
        public MetricDimensionDefinition(string key, string name, bool queryImportant = false)
        {
            if (String.IsNullOrWhiteSpace(key)) throw new ArgumentNullException(nameof(key));
            if (String.IsNullOrWhiteSpace(name)) throw new ArgumentNullException(nameof(name));

            Key = key.Trim();
            Name = name.Trim();
            QueryImportant = queryImportant;
        }

        public string Key { get; }
        public string Name { get; }
        public bool QueryImportant { get; }
    }

    public sealed class MetricDefinition
    {
        public MetricDefinition(string id, string key, string name, IEnumerable<MetricDimensionDefinition> dimensions = null)
        {
            if (String.IsNullOrWhiteSpace(id)) throw new ArgumentNullException(nameof(id));
            if (String.IsNullOrWhiteSpace(key)) throw new ArgumentNullException(nameof(key));
            if (String.IsNullOrWhiteSpace(name)) throw new ArgumentNullException(nameof(name));

            Id = id.Trim();
            Key = key.Trim();
            Name = name.Trim();
            Dimensions = (dimensions ?? Array.Empty<MetricDimensionDefinition>())
                .GroupBy(dimension => dimension.Key, StringComparer.OrdinalIgnoreCase)
                .Select(group => group.First())
                .ToList()
                .AsReadOnly();
        }

        public string Id { get; }
        public string Key { get; }
        public string Name { get; }
        public IReadOnlyList<MetricDimensionDefinition> Dimensions { get; }
    }

    public sealed class MetricRecord
    {
        public MetricRecord(
            string id,
            string organizationId,
            string organization,
            string metric,
            DateTime timestamp,
            double value,
            IReadOnlyDictionary<string, string> dimensions = null)
        {
            if (String.IsNullOrWhiteSpace(id)) throw new ArgumentNullException(nameof(id));
            if (String.IsNullOrWhiteSpace(organizationId)) throw new ArgumentNullException(nameof(organizationId));
            if (String.IsNullOrWhiteSpace(organization)) throw new ArgumentNullException(nameof(organization));
            if (String.IsNullOrWhiteSpace(metric)) throw new ArgumentNullException(nameof(metric));

            Id = id.Trim();
            OrganizationId = organizationId.Trim();
            Organization = organization.Trim();
            Metric = metric.Trim();
            Timestamp = NormalizeUtc(timestamp);
            Value = value;
            Dimensions = dimensions ?? new Dictionary<string, string>();
        }

        public string Id { get; }
        public string OrganizationId { get; }
        public string Organization { get; }
        public string Metric { get; }
        public DateTime Timestamp { get; }
        public double Value { get; }
        public IReadOnlyDictionary<string, string> Dimensions { get; }

        private static DateTime NormalizeUtc(DateTime value)
        {
            if (value.Kind == DateTimeKind.Utc) return value;
            if (value.Kind == DateTimeKind.Unspecified) return DateTime.SpecifyKind(value, DateTimeKind.Utc);
            return value.ToUniversalTime();
        }
    }

    public sealed class MetricDimensionFilter
    {
        public MetricDimensionFilter(string key, string value)
        {
            if (String.IsNullOrWhiteSpace(key)) throw new ArgumentNullException(nameof(key));
            if (value == null) throw new ArgumentNullException(nameof(value));

            Key = key.Trim();
            Value = value;
        }

        public string Key { get; }
        public string Value { get; }
    }

    public sealed class MetricQuery
    {
        public MetricQuery(
            string organizationId,
            string metric,
            DateTime start,
            DateTime end,
            MetricAggregate aggregate = MetricAggregate.Sum,
            TimeSpan? bucket = null,
            IEnumerable<MetricDimensionFilter> dimensions = null,
            IEnumerable<string> groupByDimensions = null)
        {
            if (String.IsNullOrWhiteSpace(organizationId)) throw new ArgumentNullException(nameof(organizationId));
            if (String.IsNullOrWhiteSpace(metric)) throw new ArgumentNullException(nameof(metric));

            OrganizationId = organizationId.Trim();
            Metric = metric.Trim();
            Start = NormalizeUtc(start);
            End = NormalizeUtc(end);
            if (Start > End) throw new ArgumentException("Metric query start must be before or equal to end.");
            if (bucket.HasValue && bucket.Value <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(bucket));

            Aggregate = aggregate;
            Bucket = bucket;
            Dimensions = (dimensions ?? Array.Empty<MetricDimensionFilter>()).ToList().AsReadOnly();
            GroupByDimensions = (groupByDimensions ?? Array.Empty<string>())
                .Where(key => !String.IsNullOrWhiteSpace(key))
                .Select(key => key.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList()
                .AsReadOnly();
        }

        public string OrganizationId { get; }
        public string Metric { get; }
        public DateTime Start { get; }
        public DateTime End { get; }
        public MetricAggregate Aggregate { get; }
        public TimeSpan? Bucket { get; }
        public IReadOnlyList<MetricDimensionFilter> Dimensions { get; }
        public IReadOnlyList<string> GroupByDimensions { get; }

        private static DateTime NormalizeUtc(DateTime value)
        {
            if (value.Kind == DateTimeKind.Utc) return value;
            if (value.Kind == DateTimeKind.Unspecified) return DateTime.SpecifyKind(value, DateTimeKind.Utc);
            return value.ToUniversalTime();
        }
    }

    public sealed class MetricValue
    {
        public MetricValue(DateTime timestamp, double value, IReadOnlyDictionary<string, string> dimensions = null)
        {
            Timestamp = timestamp;
            Value = value;
            Dimensions = dimensions ?? new Dictionary<string, string>();
        }

        public DateTime Timestamp { get; }
        public double Value { get; }
        public IReadOnlyDictionary<string, string> Dimensions { get; }
    }

    public sealed class MetricQueryResult
    {
        public MetricQueryResult(IEnumerable<MetricValue> values)
        {
            Values = (values ?? Array.Empty<MetricValue>()).ToList().AsReadOnly();
        }

        public IReadOnlyList<MetricValue> Values { get; }
    }
}
