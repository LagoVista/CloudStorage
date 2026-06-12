using LagoVista.Core;
using LagoVista.Core.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;

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

            if (targetType == typeof(Guid))
            {
                property.SetValue(summary, Guid.Parse(value.ToString()));
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

            if (TrySetEntityHeader(summary, property, targetType, value))
                return;

            throw new NotSupportedException(
                $"Summary table property [{property.Name}] has unsupported read type [{property.PropertyType.FullName}].");
        }

        private static bool TrySetEntityHeader<TSummary>(
            TSummary summary,
            PropertyInfo property,
            Type targetType,
            object value)
        {
            if (!targetType.Name.StartsWith("EntityHeader"))
                return false;

            var text = value.ToString();

            if (targetType == typeof(EntityHeader))
            {
                property.SetValue(summary, EntityHeader.Create(text, text));
                return true;
            }

            if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(EntityHeader<>))
            {
                var createMethod = targetType.GetMethod("Create", new[] { targetType.GetGenericArguments()[0] });
                if (createMethod == null)
                {
                    throw new NotSupportedException(
                        $"Could not find Create method for generic EntityHeader property [{property.Name}].");
                }

                var enumType = targetType.GetGenericArguments()[0];
                var enumValue = Enum.Parse(enumType, text, true);
                var header = createMethod.Invoke(null, new[] { enumValue });

                property.SetValue(summary, header);
                return true;
            }

            throw new NotSupportedException(
                $"EntityHeader summary table property [{property.Name}] uses unsupported type [{targetType.FullName}].");
        }
    }
}
