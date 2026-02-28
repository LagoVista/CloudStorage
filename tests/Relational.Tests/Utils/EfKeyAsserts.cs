using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

public static class EfKeyAsserts
{
    public static List<string> AssertPrimaryKeyExplicit(DbContext ctx, Type entityClrType, string schema, string table)
    {
        var diffs = new List<string>();

        var model = ctx.GetService<IDesignTimeModel>().Model;
        var et = model.FindEntityType(entityClrType);

        if (et == null)
        {
            diffs.Add($"Entity not in EF model: {entityClrType.FullName}");
            return diffs;
        }

        var pk = et.FindPrimaryKey();
        if (pk == null)
        {
            diffs.Add($"{entityClrType.Name} has no primary key (keyless or not configured). Add .HasKey(...) or map it correctly.");
            return diffs;
        }

        // Ensure PK properties are mapped to the expected store object
        var store = StoreObjectIdentifier.Table(table, schema);

        var unmapped = pk.Properties
            .Where(p => string.IsNullOrWhiteSpace(p.GetColumnName(store)))
            .Select(p => p.Name)
            .ToList();

        if (unmapped.Count > 0)
        {
            diffs.Add(
                $"{entityClrType.Name} primary key properties not mapped to {schema}.{table}: {string.Join(", ", unmapped)} " +
                $"(check [Table], inheritance/table splitting, or property mapping).");
        }

        // Optional: enforce explicit configuration source (maximum determinism)
        if (pk is IConventionKey ck)
        {
            var src = ck.GetConfigurationSource();
            if (src != ConfigurationSource.Explicit)
                diffs.Add($"{entityClrType.Name} primary key is not explicit (ConfigurationSource={src}). Add .HasKey(...) in Configure().");
        }

        return diffs;
    }
}