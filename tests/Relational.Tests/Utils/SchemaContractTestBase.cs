using LagoVista.CloudStorage.Utils;
using LagoVista.Relational;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using System;
using System.Linq;
using System.Threading.Tasks;

public abstract class SchemaContractTestBase
{
    protected SqlConnection OpenTruthConnection()
    {
        var settings = TestConnections.DevSQLServer;
        var cs = SqlServerConnectionStringBuilderEx.Build(settings);

        var conn = new SqlConnection(cs);
        conn.Open();
        return conn;
    }

    protected abstract DbContext CreateContextForTruthDb(string sqlServerConnectionString);


    protected async Task AssertTableMatchesModelAsync(
    Type entityType,
    string schema,
    string table)
    {
        EntityAttributeGuards.RequireTableAttribute(entityType);

        using var conn = OpenTruthConnection();

        var dbTruth = await SqlServerSchemaReader.ReadTableAsync(conn, schema, table).ConfigureAwait(false);

        await using var ctx = CreateContextForTruthDb(conn.ConnectionString);

        // Use design-time model so ColumnOrder + relational metadata is available
        var designModel = ctx.GetService<IDesignTimeModel>().Model;

        var efShape = EfModelReader.ReadEntityTableShape(ctx, entityType, schema, table);
        var diffs = SchemaDiff.CompareColumnsStrictOrder(dbTruth, efShape);

        var navDiffs = EfNavigationAsserts.AssertAllReferenceAndCollectionPropertiesAreNavigations(ctx, entityType);
        var explicitNavDiffs = ExplicitRelationshipAsserts.AssertDtoNavPropertiesAreExplicit(ctx, entityType);

        var dbFks = await SqlServerForeignKeyReader.ReadOutboundFksAsync(conn, schema, table).ConfigureAwait(false);
        var efFks = EfForeignKeyReaderExplicit.ReadOutboundFks(ctx, entityType, schema, table);

        var fkDiffs = ForeignKeyDiff.Compare(dbFks, efFks);

        var dbDefaults = await SqlServerDefaultConstraintReader.ReadDefaultsAsync(conn, schema, table).ConfigureAwait(false);
        var efDefaults = EfDefaultReader.ReadDefaults(designModel, entityType, schema, table);
        var defaultDiffs = DefaultDiff.Compare(dbDefaults, efDefaults);

        // Green path: no output
        if (diffs.Count == 0 && navDiffs.Count == 0 && explicitNavDiffs.Count == 0 && fkDiffs.Count == 0 && defaultDiffs.Count == 0)
            return;

        // Only generate suggestions if we have column/order diffs.
        var suggestions = diffs.Count == 0
            ? Array.Empty<string>()
            : dbTruth.Columns
                .OrderBy(c => c.Ordinal)
                .Select(c =>
                    $"modelBuilder.Entity<{entityType.Name}>()" +
                    $".Property(x => x.{GuessPropertyName(designModel, entityType, schema, table, c.Name)})" +
                    $".HasColumnOrder({c.Ordinal});")
                .ToArray();

        TestContext.WriteLine($"=== {schema}.{table} ({entityType.Name}) out of sync ===");

        foreach (var d in defaultDiffs)
        {
            TestContext.WriteLine(d);
        }

        if (defaultDiffs.Any())
        {
            foreach (var d in dbDefaults.Defaults.OrderBy(x => x.ColumnName))
            {
                TestContext.WriteLine($"// {d.ColumnName} DB default: {d.DefaultSqlNormalized}");
            }

            if (dbDefaults.Defaults.Any())
            {
                TestContext.WriteLine("");
                TestContext.WriteLine("Suggested table default values");
                TestContext.WriteLine("===============================");
                foreach (var d in dbDefaults.Defaults.OrderBy(x => x.ColumnName))
                {
                    TestContext.WriteLine($" modelBuilder.Entity<{entityType.Name}>().Property(x => x.{d.ColumnName}).HasDefaultValueSql(\"{d.DefaultSqlNormalized}\");");
                }
            }
        }

        if (diffs.Count > 0)
        {
            TestContext.WriteLine("Column differences (strict order):");
            foreach (var d in diffs) TestContext.WriteLine(d);
        }

        if (fkDiffs.Count > 0)
        {
            TestContext.WriteLine("");
            TestContext.WriteLine("Foreign key differences (includes ON DELETE mismatches):");
            foreach (var d in fkDiffs) TestContext.WriteLine(d);
        }

        if (explicitNavDiffs.Count > 0)
        {
            TestContext.WriteLine("");
            TestContext.WriteLine("Explicit navigation configuration issues:");
            foreach (var d in explicitNavDiffs) TestContext.WriteLine(d);
        }

        if (navDiffs.Count > 0)
        {
            TestContext.WriteLine("");
            TestContext.WriteLine("Navigation modeling issues:");
            foreach (var d in navDiffs) TestContext.WriteLine(d);
        }

        if (suggestions.Length > 0)
        {
            TestContext.WriteLine("");
            TestContext.WriteLine("Suggested HasColumnOrder mappings (verify property names):");
            TestContext.WriteLine("===============================");
            foreach (var s in suggestions) TestContext.WriteLine(s);
        }

        var total = diffs.Count + fkDiffs.Count + explicitNavDiffs.Count + navDiffs.Count;
        Assert.Fail($"{schema}.{table}: {total} schema differences");
    }


    private static string GuessPropertyName(IModel designModel, Type entityType, string schema, string table, string columnName)
    {
        // Map DB column name back to CLR property name using EF metadata.
        var et = designModel.FindEntityType(entityType);
        if (et == null) return columnName;

        // Since you're standardizing ToTable("X","dbo"), schema/table should match directly here.
        var storeObject = StoreObjectIdentifier.Table(table, schema);

        foreach (var p in et.GetProperties())
        {
            var cn = p.GetColumnName(storeObject);
            if (!string.IsNullOrWhiteSpace(cn) && cn.Equals(columnName, StringComparison.OrdinalIgnoreCase))
                return p.Name;
        }

        return columnName;
    }
}