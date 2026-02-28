using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System;
using System.Collections.Generic;
using System.Linq;

public sealed record EfFkShape(
    string FromSchema,
    string FromTable,
    string[] FromColumns,
    string ToSchema,
    string ToTable,
    string[] ToColumns,
    string OnDelete,
    ConfigurationSource Source);

public static class EfForeignKeyReaderExplicit
{
    public static IReadOnlyList<EfFkShape> ReadOutboundFks(DbContext ctx, Type entityClrType, string schema, string table)
    {
        var model = ctx.GetService<IDesignTimeModel>().Model;
        var et = model.FindEntityType(entityClrType)
                 ?? throw new InvalidOperationException($"Entity not in EF model: {entityClrType.FullName}");

        if (et is not IConventionEntityType cet)
            throw new InvalidOperationException($"Cannot inspect configuration source for {entityClrType.Name}.");

        var storeObject = StoreObjectIdentifier.Table(table, schema);

        var list = new List<EfFkShape>();

        foreach (var fk in cet.GetForeignKeys())
        {
            var fromCols = fk.Properties
                .Select(p => p.GetColumnName(storeObject) ?? p.Name)
                .ToArray();

            var pet = fk.PrincipalEntityType;
            var toTable = pet.GetTableName() ?? pet.ClrType.Name;
            var toSchema = pet.GetSchema() ?? "dbo";
            var toStore = StoreObjectIdentifier.Table(toTable, toSchema);

            var toCols = fk.PrincipalKey.Properties
                .Select(p => p.GetColumnName(toStore) ?? p.Name)
                .ToArray();

            TestContext.WriteLine(
                $"EF FK: ({string.Join(",", fromCols)}) -> {toSchema}.{toTable}({string.Join(",", toCols)}) Delete={fk.DeleteBehavior} Nav={fk.DependentToPrincipal?.Name ?? "(none)"}");

            var onDelete = fk.DeleteBehavior switch
            {
                DeleteBehavior.Cascade => "CASCADE",
                DeleteBehavior.SetNull => "SET_NULL",
                _ => "NO_ACTION"
            };

            list.Add(new EfFkShape(
                FromSchema: schema,
                FromTable: table,
                FromColumns: fromCols,
                ToSchema: toSchema,
                ToTable: toTable,
                ToColumns: toCols,
                OnDelete: onDelete,
                Source: fk.GetConfigurationSource()));
        }

        var shadowFkProps = et.GetForeignKeys()
            .SelectMany(fk => fk.Properties)
            .Where(p => p.IsShadowProperty())
            .ToList();

                if (shadowFkProps.Count > 0)
                {
                    foreach (var p in shadowFkProps)
                        TestContext.WriteLine($"Shadow FK property detected: {entityClrType.Name}.{p.Name}");

                    Assert.Fail($"{entityClrType.Name} contains shadow foreign keys.");
                }

        return list;
    }
}