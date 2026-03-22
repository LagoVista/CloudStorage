// ================================
// File: LegacyMigrationScaffolding/Runtime/TableMigrationBase.cs
// ================================
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace LegacyMigrationScaffolding.Runtime
{
    public abstract class TableMigrationBase<TSource, TDestination>
        where TSource : class
        where TDestination : class
    {
        public abstract string SourceSchemaName { get; }
        public abstract string SourceTableName { get; }
        public abstract string DestinationSchemaName { get; }
        public abstract string DestinationTableName { get; }
        public abstract string KeyColumnName { get; }
        public virtual int BatchSize => 500;

        protected abstract TSource MaterializeSource(IDataRecord record);
        protected abstract TDestination Map(TSource source);
        protected abstract Task PersistAsync(TDestination destination, CancellationToken cancellationToken);

        protected virtual string BuildReadBatchSql()
        {
            return $@"
SELECT TOP (@take) *
FROM [{SourceSchemaName}].[{SourceTableName}]
WHERE [{KeyColumnName}] > @lastKey
ORDER BY [{KeyColumnName}]";
        }

        protected virtual object GetLastKeyValue(TSource source)
        {
            var property = typeof(TSource).GetProperty(KeyColumnName);

            if (property == null)
                throw new InvalidOperationException($"Could not find source key property '{KeyColumnName}' on type {typeof(TSource).Name}.");

            return property.GetValue(source);
        }

        protected virtual object GetInitialKeyValue()
        {
            var property = typeof(TSource).GetProperty(KeyColumnName);

            if (property == null)
                throw new InvalidOperationException($"Could not find source key property '{KeyColumnName}' on type {typeof(TSource).Name}.");

            var propertyType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;

            if (propertyType == typeof(Guid))
                return Guid.Empty;

            if (propertyType == typeof(int))
                return int.MinValue;

            if (propertyType == typeof(long))
                return long.MinValue;

            if (propertyType == typeof(short))
                return short.MinValue;

            if (propertyType == typeof(byte))
                return byte.MinValue;

            if (propertyType == typeof(DateTime))
                return DateTime.MinValue;

            if (propertyType == typeof(string))
                return string.Empty;

            throw new InvalidOperationException($"Unsupported key type '{propertyType.Name}' for paging.");
        }

        public async Task<int> ExecuteAsync(string sourceConnectionString, CancellationToken cancellationToken = default)
        {
            var rowsProcessed = 0;
            var lastKey = GetInitialKeyValue();

            using var conn = new SqlConnection(sourceConnectionString);
            await conn.OpenAsync(cancellationToken);

            while (true)
            {
                var batch = await ReadBatchAsync(conn, lastKey, BatchSize, cancellationToken);

                if (batch.Count == 0)
                    break;

                foreach (var source in batch)
                {
                    var destination = Map(source);
                    await PersistAsync(destination, cancellationToken);
                    rowsProcessed++;
                    lastKey = GetLastKeyValue(source);
                }
            }

            return rowsProcessed;
        }

        protected virtual async Task<List<TSource>> ReadBatchAsync(SqlConnection conn, object lastKey, int take, CancellationToken cancellationToken)
        {
            var list = new List<TSource>();

            using var cmd = new SqlCommand(BuildReadBatchSql(), conn);
            cmd.Parameters.AddWithValue("@take", take);
            cmd.Parameters.AddWithValue("@lastKey", lastKey ?? DBNull.Value);

            using var reader = await cmd.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                list.Add(MaterializeSource(reader));
            }

            return list;
        }
    }
}