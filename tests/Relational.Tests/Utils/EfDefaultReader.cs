using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public sealed record EfColumnDefaultShape(string ColumnName, string DefaultSqlNormalized);

public sealed record EfTableDefaultsShape(string Schema, string Table, IReadOnlyList<EfColumnDefaultShape> Defaults);

public static class EfDefaultReader
{
    public static EfTableDefaultsShape ReadDefaults(IModel designModel, Type entityClrType, string schema, string table)
    {
        var et = designModel.FindEntityType(entityClrType)
                 ?? throw new InvalidOperationException($"Entity not in EF model: {entityClrType.FullName}");

        var mappedSchema = et.GetSchema() ?? schema;
        var mappedTable = et.GetTableName() ?? table;
        var store = StoreObjectIdentifier.Table(mappedTable, mappedSchema);

        var list = new List<EfColumnDefaultShape>();

        foreach (var p in et.GetProperties())
        {
            var col = p.GetColumnName(store) ?? p.Name;

            // 1) DefaultValueSql is always intentional for DDL.
            var dvSql = p.GetDefaultValueSql(store);
            if (!string.IsNullOrWhiteSpace(dvSql))
            {
                list.Add(new EfColumnDefaultShape(col, DefaultSqlNormalizer.NormalizeEf(dvSql, defaultValue: null)));
                continue;
            }

            // 2) DefaultValue (constant) only if explicitly configured.
            if (p is IConventionProperty cp)
            {
                var src = cp.GetDefaultValueConfigurationSource();
                if (src == ConfigurationSource.Explicit)
                {
                    var dv = p.GetDefaultValue(store);
                    if (dv != null)
                        list.Add(new EfColumnDefaultShape(col, DefaultSqlNormalizer.NormalizeEf(defaultValueSql: null, defaultValue: dv)));
                }
            }
        }

        return new EfTableDefaultsShape(mappedSchema, mappedTable, list.OrderBy(x => x.ColumnName).ToList());
    }
}