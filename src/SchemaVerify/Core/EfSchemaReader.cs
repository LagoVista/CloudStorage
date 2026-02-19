using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace SchemaVerify.Core;

public sealed class EfSchemaReader : IEfSchemaReader
{
    public SchemaModel Read(DbContext context)
    {
        var model = new SchemaModel();

        foreach (var entityType in context.Model.GetEntityTypes())
        {
            // Skip entity types that don't map to a table (e.g., query types)
            var tableName = entityType.GetTableName();
            if (string.IsNullOrWhiteSpace(tableName)) continue;

            var schema = entityType.GetSchema() ?? "dbo";
            var table = model.FindTable(schema, tableName);
            if (table is null)
            {
                table = new TableModel { Schema = schema, Name = tableName };
                model.Tables.Add(table);
            }

            foreach (var prop in entityType.GetProperties())
            {
                var columnName = prop.GetColumnName(StoreObjectIdentifier.Table(tableName, schema));
                if (string.IsNullOrWhiteSpace(columnName)) continue;

                // Relational store type (provider-specific)
                var storeType = prop.GetColumnType();

                var col = new ColumnModel
                {
                    Name = columnName!,
                    StoreType = storeType ?? string.Empty,
                    IsNullable = prop.IsColumnNullable(),
                    MaxLength = prop.GetMaxLength(),
                    Precision = prop.GetPrecision(),
                    Scale = prop.GetScale()
                };

                if (table.FindColumn(col.Name) is null)
                {
                    table.Columns.Add(col);
                }
            }

            var pk = entityType.FindPrimaryKey();
            if (pk is not null)
            {
                // Ensure we store PK in column name order.
                table.PrimaryKey.Clear();
                foreach (var pkProp in pk.Properties)
                {
                    var pkColName = pkProp.GetColumnName(StoreObjectIdentifier.Table(tableName, schema));
                    if (!string.IsNullOrWhiteSpace(pkColName))
                    {
                        table.PrimaryKey.Add(pkColName!);
                    }
                }
            }
        }

        // Sort for stable diffs
        model.Tables.Sort((a, b) => string.Compare(a.Schema + "." + a.Name, b.Schema + "." + b.Name, StringComparison.OrdinalIgnoreCase));
        foreach (var t in model.Tables)
        {
            t.Columns.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
        }

        return model;
    }
}
