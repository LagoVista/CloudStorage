using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Collections.Generic;
using System.Linq;

public static class EfColumnTypeReader
{
    public static IReadOnlyList<ColumnTypeShape> ReadColumnTypes(DbContext ctx, Type entityClrType, string schema, string table)
    {
        var model = ctx.GetService<IDesignTimeModel>().Model;
        var et = model.FindEntityType(entityClrType)
            ?? throw new InvalidOperationException($"Entity not in EF model: {entityClrType.FullName}");

        var mappedSchema = et.GetSchema() ?? schema;
        var mappedTable = et.GetTableName() ?? table;
        var store = StoreObjectIdentifier.Table(mappedTable, mappedSchema);

        var mappingSource = ctx.GetService<IRelationalTypeMappingSource>();

        var list = new List<ColumnTypeShape>();

        foreach (var p in et.GetProperties())
        {
            // Skip properties not mapped to this store object (prevents the exception you hit earlier)
            var colName = p.GetColumnName(store);
            if (string.IsNullOrWhiteSpace(colName))
                continue;

            // Prefer explicitly configured column type if present
            var configured = p.GetColumnType(store);

            // Otherwise ask EF’s provider mapping what it would generate
            var inferred = mappingSource.FindMapping(p)?.StoreType;

            var storeType = (configured ?? inferred ?? string.Empty).Trim().ToLowerInvariant().Replace(" ", string.Empty);

            if (!string.IsNullOrWhiteSpace(storeType))
                list.Add(new ColumnTypeShape(colName, storeType));
        }

        return list;
    }
}