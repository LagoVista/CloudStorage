using System;
using System.Data;

namespace LegacyMigrationScaffolding.Runtime
{
    public static class DataRecordExtensions
    {
        public static T GetValueOrDefault<T>(this IDataRecord record, string columnName)
        {
            var ordinal = record.GetOrdinal(columnName);

            if (record.IsDBNull(ordinal))
                return default;

            var targetType = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);
            var value = record.GetValue(ordinal);

            if (targetType == typeof(Guid))
                return (T)(object)(value is Guid guid ? guid : Guid.Parse(value.ToString()));

            if (targetType == typeof(DateTime))
                return (T)(object)(value is DateTime dateTime ? dateTime : Convert.ToDateTime(value));

            if (targetType == typeof(DateTimeOffset))
            {
                if (value is DateTimeOffset dto)
                    return (T)(object)dto;

                if (value is DateTime dt)
                    return (T)(object)new DateTimeOffset(dt);

                return (T)(object)DateTimeOffset.Parse(value.ToString());
            }

            if (targetType == typeof(TimeSpan))
            {
                if (value is TimeSpan ts)
                    return (T)(object)ts;

                return (T)(object)TimeSpan.Parse(value.ToString());
            }

            if (targetType == typeof(string))
                return (T)(object)Convert.ToString(value);

            if (targetType == typeof(bool))
                return (T)(object)Convert.ToBoolean(value);

            if (targetType == typeof(int))
                return (T)(object)Convert.ToInt32(value);

            if (targetType == typeof(long))
                return (T)(object)Convert.ToInt64(value);

            if (targetType == typeof(short))
                return (T)(object)Convert.ToInt16(value);

            if (targetType == typeof(byte))
                return (T)(object)Convert.ToByte(value);

            if (targetType == typeof(decimal))
                return (T)(object)Convert.ToDecimal(value);

            if (targetType == typeof(double))
                return (T)(object)Convert.ToDouble(value);

            if (targetType == typeof(float))
                return (T)(object)Convert.ToSingle(value);

            if (targetType == typeof(byte[]))
                return (T)value;

            return (T)Convert.ChangeType(value, targetType);
        }
    }
}