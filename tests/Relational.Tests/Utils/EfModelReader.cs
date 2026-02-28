using LagoVista.Relational;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;


public static class EfModelReader
{
    public static TableShape ReadEntityTableShape(DbContext ctx, Type entityClrType, string expectedSchema, string expectedTable)
    {
        if (ctx == null) throw new ArgumentNullException(nameof(ctx));
        if (entityClrType == null) throw new ArgumentNullException(nameof(entityClrType));

        // IMPORTANT: use the design-time model so relational config like ColumnOrder is available
        var designModel = ctx.GetService<IDesignTimeModel>().Model;

        var et = designModel.FindEntityType(entityClrType)
                 ?? throw new InvalidOperationException($"Entity type not in EF model: {entityClrType.FullName}");

        var efTable = et.GetTableName()
                     ?? throw new InvalidOperationException($"Entity {entityClrType.Name} has no table mapping.");

        var efSchemaRaw = et.GetSchema(); // can be null if default
        var efSchemaNorm = NormalizeSchema(efSchemaRaw);

        if (!string.Equals(efTable, expectedTable, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(efSchemaNorm, NormalizeSchema(expectedSchema), StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"EF maps {entityClrType.Name} to {efSchemaNorm}.{efTable}, but test expects {expectedSchema}.{expectedTable}.");
        }

        // Use the raw schema EF stores (null is valid)
        var storeObject = StoreObjectIdentifier.Table(efTable, efSchemaRaw);

        var cols = new List<ColumnShape>();

        foreach (var p in et.GetProperties())
        {
            var colName = p.GetColumnName(storeObject);
            if (string.IsNullOrWhiteSpace(colName))
                continue;

            var nullable = p.IsNullable;

            // Now safe: ColumnOrder is available from design-time model metadata
            var order = p.GetColumnOrder(storeObject) ?? -1;

            cols.Add(new ColumnShape(colName, nullable, order));
        }

        return new TableShape(expectedSchema, expectedTable, cols);
    }

    private static string NormalizeSchema(string? schema)
        => string.IsNullOrWhiteSpace(schema) ? "dbo" : schema;
}
