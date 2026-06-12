using LagoVista.Core;
using LagoVista.Core.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace LagoVista.CloudStorage.Utils
{
    public sealed class TableStorageQuery
    {
        private readonly List<string> _filters = new List<string>();

        public static TableStorageQuery Create()
        {
            return new TableStorageQuery();
        }

        public TableStorageQuery WhereEquals(string field, object value)
        {
            _filters.Add($"{field} eq {FormatValue(value)}");
            return this;
        }

        public TableStorageQuery WhereGreaterThan(string field, object value)
        {
            _filters.Add($"{field} gt {FormatValue(value)}");
            return this;
        }

        public TableStorageQuery WhereLessThan(string field, object value)
        {
            _filters.Add($"{field} lt {FormatValue(value)}");
            return this;
        }

        public TableStorageQuery WhereGreaterThanOrEqual(string field, object value)
        {
            _filters.Add($"{field} ge {FormatValue(value)}");
            return this;
        }

        public TableStorageQuery WhereLessThanOrEqual(string field, object value)
        {
            _filters.Add($"{field} le {FormatValue(value)}");
            return this;
        }

        public TableStorageQuery And(TableStorageQuery query)
        {
            if (query != null && query._filters.Count > 0)
                _filters.AddRange(query._filters);

            return this;
        }

        public string ToFilterString()
        {
            return String.Join(" and ", _filters.Select(filter => $"({filter})"));
        }

        private static string FormatValue(object value)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));

            switch (value)
            {
                case bool boolValue:
                    return boolValue.ToString().ToLowerInvariant();

                case int _:
                case long _:
                case double _:
                case decimal _:
                    return Convert.ToString(value, CultureInfo.InvariantCulture);

                case NormalizedId32 id:
                    return Quote(id.Value);

                case UtcTimestamp timestamp:
                    return Quote(timestamp.ToDateTimeUtc().ToString("O"));

                case CalendarDate calendarDate:
                    return Quote(calendarDate.ToString());

                case EntityHeader header:
                    return Quote(ResolveEntityHeaderValue(header));

                case DateTime dateTime:
                    return Quote(dateTime.ToUniversalTime().ToString("O"));

                case DateTimeOffset dateTimeOffset:
                    return Quote(dateTimeOffset.UtcDateTime.ToString("O"));

                default:
                    return Quote(value.ToString());
            }
        }

        private static string ResolveEntityHeaderValue(EntityHeader header)
        {
            if (header == null)
                throw new ArgumentNullException(nameof(header));

            if (!String.IsNullOrWhiteSpace(header.Key))
                return header.Key;

            if (!String.IsNullOrWhiteSpace(header.Id))
                return header.Id;

            if (!String.IsNullOrWhiteSpace(header.Text))
                return header.Text;

            throw new InvalidOperationException("EntityHeader query value must have Key, Id, or Text.");
        }

        private static string Quote(string value)
        {
            return $"'{value.Replace("'", "''")}'";
        }
    }
}
