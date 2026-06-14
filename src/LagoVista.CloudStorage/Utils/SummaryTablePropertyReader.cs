using Azure.Data.Tables;
using LagoVista.Core;
using LagoVista.Core.Models;
using System;
using System.Globalization;
using System.Reflection;

namespace LagoVista.CloudStorage.Utils
{
    internal static class SummaryTablePropertyReader
    {
        public static void Set<TSummary>(TSummary summary, PropertyInfo property, object value)
        {
            if (summary == null) throw new ArgumentNullException(nameof(summary));
            if (property == null) throw new ArgumentNullException(nameof(property));

            if (value == null)
                return;

            var targetType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;

            if (targetType == typeof(string))
            {
                property.SetValue(summary, value.ToString());
                return;
            }

            if (targetType == typeof(int))
            {
                property.SetValue(summary, Convert.ToInt32(value, CultureInfo.InvariantCulture));
                return;
            }

            if (targetType == typeof(long))
            {
                property.SetValue(summary, Convert.ToInt64(value, CultureInfo.InvariantCulture));
                return;
            }

            if (targetType == typeof(bool))
            {
                property.SetValue(summary, Convert.ToBoolean(value, CultureInfo.InvariantCulture));
                return;
            }

            if (targetType == typeof(double))
            {
                property.SetValue(summary, Convert.ToDouble(value, CultureInfo.InvariantCulture));
                return;
            }

            if (targetType == typeof(decimal))
            {
                property.SetValue(summary, Decimal.Parse(value.ToString(), CultureInfo.InvariantCulture));
                return;
            }

            if (targetType == typeof(NormalizedId32))
            {
                property.SetValue(summary, NormalizedId32.Parse(value.ToString()));
                return;
            }

            if (targetType == typeof(GuidString36))
            {
                property.SetValue(summary, GuidString36.Parse(value.ToString()));
                return;
            }

            if (targetType == typeof(UtcTimestamp))
            {
                property.SetValue(summary, UtcTimestamp.Parse(value.ToString()));
                return;
            }

            if (targetType == typeof(CalendarDate))
            {
                property.SetValue(summary, CalendarDate.Parse(value.ToString()));
                return;
            }

            throw new NotSupportedException(
                $"Summary table property [{property.Name}] has unsupported read type [{property.PropertyType.FullName}].");
        }

        public static bool TrySetEntityHeader<TSummary>(
            TSummary summary,
            PropertyInfo property,
            TableEntity entity)
        {
            if (summary == null) throw new ArgumentNullException(nameof(summary));
            if (property == null) throw new ArgumentNullException(nameof(property));
            if (entity == null) throw new ArgumentNullException(nameof(entity));

            var targetType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;

            if (!targetType.Name.StartsWith("EntityHeader"))
                return false;

            var idColumnName = $"{property.Name}Id";
            var textColumnName = $"{property.Name}Text";
            var keyColumnName = $"{property.Name}Key";

            var hasId = entity.TryGetValue(idColumnName, out var idValue);
            var hasText = entity.TryGetValue(textColumnName, out var textValue);
            var hasKey = entity.TryGetValue(keyColumnName, out var keyValue);

            if (!hasId && !hasText && !hasKey)
                return true;

            var id = idValue?.ToString();
            var text = textValue?.ToString();
            var key = keyValue?.ToString();

            if (String.IsNullOrWhiteSpace(id) || String.IsNullOrWhiteSpace(text))
            {
                throw new NotSupportedException(
                    $"EntityHeader summary table property [{property.Name}] must have both [{idColumnName}] and [{textColumnName}].");
            }

            if (targetType == typeof(EntityHeader))
            {
                property.SetValue(summary, EntityHeader.Create(id, text));
                return true;
            }

            if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(EntityHeader<>))
            {
                property.SetValue(summary, CreateGenericEntityHeader(targetType, id, text, key));
                return true;
            }

            throw new NotSupportedException(
                $"EntityHeader summary table property [{property.Name}] uses unsupported type [{targetType.FullName}].");
        }

        private static object CreateGenericEntityHeader(Type targetType, string id, string text, string key)
        {
            var enumType = targetType.GetGenericArguments()[0];

            if (!String.IsNullOrWhiteSpace(key))
            {
                var enumValue = Enum.Parse(enumType, key, true);

                var createFromEnumMethod = targetType.GetMethod(
                    "Create",
                    BindingFlags.Public | BindingFlags.Static,
                    null,
                    new[] { enumType },
                    null);

                if (createFromEnumMethod != null)
                    return createFromEnumMethod.Invoke(null, new[] { enumValue });
            }

            var createFromIdTextMethod = targetType.GetMethod(
                "Create",
                BindingFlags.Public | BindingFlags.Static,
                null,
                new[] { typeof(string), typeof(string) },
                null);

            if (createFromIdTextMethod != null)
                return createFromIdTextMethod.Invoke(null, new object[] { id, text });

            throw new NotSupportedException(
                $"Could not create generic EntityHeader [{targetType.FullName}]. Expected Create(enum) or Create(string, string).");
        }
    }
}