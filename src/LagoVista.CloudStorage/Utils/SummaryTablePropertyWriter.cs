using Azure.Data.Tables;
using LagoVista.Core;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using static LagoVista.Core.Models.AdaptiveCard.MSTeams;

namespace LagoVista.CloudStorage.Utils
{

    internal static class SummaryTablePropertyWriter
    {
        public static void Add(TableEntity entity, string columnName, object value)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));
            if (String.IsNullOrWhiteSpace(columnName)) throw new ArgumentNullException(nameof(columnName));

            if (value == null)
                return;

            switch (value)
            {
                case string text:
                    entity[columnName] = text;
                    return;

                case int intValue:
                    entity[columnName] = intValue;
                    return;

                case long longValue:
                    entity[columnName] = longValue;
                    return;

                case bool boolValue:
                    entity[columnName] = boolValue;
                    return;

                case double doubleValue:
                    entity[columnName] = doubleValue;
                    return;

                case decimal decimalValue:
                    entity[columnName] = decimalValue.ToString(CultureInfo.InvariantCulture);
                    return;

                case GuidString36 guidString36:
                    entity[columnName] = guidString36.Value;
                    return;

                case NormalizedId32 normalizedId:
                    entity[columnName] = normalizedId.Value;
                    return;

                case UtcTimestamp timestamp:
                    entity[columnName] = timestamp.ToString();
                    return;

                case CalendarDate calendarDate:
                    entity[columnName] = calendarDate.ToString();
                    return;
            }

            if (TryAddEntityHeader(entity, columnName, value))
                return;

            throw new NotSupportedException(
                $"Summary table property [{columnName}] has unsupported type [{value.GetType().FullName}]. " +
                "Supported types are string, int, long, bool, double, decimal, GuidString36," +
                "NormalizedId32, UtcTimestamp, CalendarDate, EntityHeader, and EntityHeader<T>.");
        }

        private static bool TryAddEntityHeader(TableEntity entity, string columnName, object value)
        {
            var type = value.GetType();

            if (!type.Name.StartsWith("EntityHeader"))
                return false;

            var key = type.GetProperty("Key")?.GetValue(value)?.ToString();
            if (!String.IsNullOrWhiteSpace(key))
            {
                entity[columnName] = key;
                return true;
            }

            var id = type.GetProperty("Id")?.GetValue(value)?.ToString();
            if (!String.IsNullOrWhiteSpace(id))
            {
                entity[columnName] = id;
                return true;
            }

            var text = type.GetProperty("Text")?.GetValue(value)?.ToString();
            if (!String.IsNullOrWhiteSpace(text))
            {
                entity[columnName] = text;
                return true;
            }

            throw new NotSupportedException(
                $"EntityHeader summary table property [{columnName}] does not have Key, Id, or Text.");
        }
    }
}
