using LagoVista.CloudStorage.Utils;
using LagoVista.Relational;
using LagoVista.Relational.DataContexts;
using Microsoft.Azure.Cosmos.Serialization.HybridRow.Schemas;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
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


    public BillingDataContext CreateContext()
    {

        var settings = TestConnections.DevSQLServer;
        var cs = SqlServerConnectionStringBuilderEx.Build(settings);

        var opts = new DbContextOptionsBuilder<BillingDataContext>()
               .UseSqlServer(cs)
               .EnableSensitiveDataLogging()
               .Options;

        return new BillingDataContext(opts);
    }

    protected async Task AssertTableMatchesModelAsync(Type entityType,
        bool all = true,
        bool namesOnly = false,
        bool typesOnly = false,
        bool defaultsOnly = false,
        bool fkKeysOnly = false,
        bool pkOnly = false,
        bool showEFSuggestions = false,
        bool showDBSuggestions = false, 
        bool orderOnly = false)
    {
        EntityAttributeGuards.RequireTableAttribute(entityType);

        using var conn = OpenTruthConnection();

        var ctx = CreateContext();

        // Use design-time model so ColumnOrder + relational metadata is available
        var designModel = ctx.GetService<IDesignTimeModel>().Model;

        var et = ctx.Model.FindEntityType(entityType) ?? throw new Exception($"Entity not mapped: {entityType.Name}");
        var table = et.GetTableName();
        var schema = et.GetSchema() ?? "dbo";

        var dbTruth = await SqlServerSchemaReader.ReadTableAsync(conn, schema, table).ConfigureAwait(false);
        var dbTypes = await SqlServerColumnTypeReader.ReadColumnTypesAsync(conn, schema, table).ConfigureAwait(false);


        var efShape = EfModelReader.ReadEntityTableShape(ctx, entityType, schema, table);
        var diffs = SchemaDiff.CompareColumnsStrictOrder(dbTypes, efShape, schema, table);

        var navDiffs = EfNavigationAsserts.AssertAllReferenceAndCollectionPropertiesAreNavigations(ctx, entityType);
        var explicitNavDiffs = ExplicitRelationshipAsserts.AssertDtoNavPropertiesAreExplicit(ctx, entityType);

        var dbFks = await SqlServerForeignKeyReader.ReadOutboundFksAsync(conn, schema, table).ConfigureAwait(false);
        var efFks = EfForeignKeyReaderExplicit.ReadOutboundFks(ctx, entityType, schema, table);

        var fkDiffs = ForeignKeyDiff.Compare(dbFks, efFks);

        var dbDefaults = await SqlServerDefaultConstraintReader.ReadDefaultsAsync(conn, schema, table).ConfigureAwait(false);
        var efDefaults = EfDefaultReader.ReadDefaults(designModel, entityType, schema, table);
        var defaultDiffs = DefaultDiff.Compare(dbDefaults, efDefaults);

        var keyDiffs = EfKeyAsserts.AssertPrimaryKeyExplicit(ctx, entityType, schema, table);

        var efTypes = EfColumnTypeReader.ReadColumnTypes(ctx, entityType, schema, table);
        var typeDiffs = ColumnTypeDiff.CompareStrict(dbTypes, efTypes);

        // Green path: no output
        if (diffs.Count == 0 && navDiffs.Count == 0 && explicitNavDiffs.Count == 0 &&
            fkDiffs.Count == 0 && defaultDiffs.Count == 0 && keyDiffs.Count == 0 &&
            typeDiffs.Count == 0)
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


        if (all || defaultsOnly)
        {
            if (defaultDiffs.Any())
            {
                TestContext.WriteLine("===============================");
                TestContext.WriteLine("Has Default Differences");

                foreach (var d in defaultDiffs)
                {
                    TestContext.WriteLine(d);
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

                TestContext.WriteLine("===============================");
                TestContext.WriteLine();
            }
        }

        if (all || typesOnly)
        {
            if (typeDiffs.Count > 0)
            {
                TestContext.WriteLine("===============================");
                TestContext.WriteLine($"Type differences (strict): {typeDiffs.Count}");
                foreach (var d in typeDiffs) TestContext.WriteLine(d);

                if (showEFSuggestions)
                {
                    TestContext.WriteLine("");
                    TestContext.WriteLine("Suggested HasColumnType mappings (verify property names):");
                    var storeMap = dbTypes.ToDictionary(x => x.ColumnName, x => x.StoreType, StringComparer.OrdinalIgnoreCase);

                    foreach (var col in storeMap.Keys.OrderBy(x => x))
                    {
                        var prop = GuessPropertyName(designModel, entityType, schema, table, col);
                        TestContext.WriteLine($"modelBuilder.Entity<{entityType.Name}>().Property(x => x.{prop}).HasColumnType(\"{storeMap[col]}\");");
                    }
                }

                if(showDBSuggestions)
                {
                    foreach(var t in typeDiffs)
                    {
                         TestContext.WriteLine($"ALTER TABLE [{schema}].[{table}] ALTER COLUMN [{t.ColumnName}] {t.EfType} {(t.IsNullable ? "NULL" : "NOT NULL")};");
                    }
                }

                TestContext.WriteLine("===============================");
                TestContext.WriteLine();
            }
        }


        if (all || pkOnly)
        {
            var pk = designModel.FindEntityType(entityType)?.FindPrimaryKey();
            if (pk != null)
            {
                var propNames = string.Join(", ", pk.Properties.Select(p => $"x.{p.Name}"));
                TestContext.WriteLine($"Suggested:\n modelBuilder.Entity<{entityType.Name}>().HasKey(x => new {{ {propNames} }});");
            }
        }

        if (all || fkKeysOnly)
        {
            if (keyDiffs.Count > 0)
            {
                TestContext.WriteLine("");
                TestContext.WriteLine("Key modeling issues:");
                foreach (var d in keyDiffs) TestContext.WriteLine(d);
            }
        }

        if (diffs.Count > 0)
        {
            TestContext.WriteLine("Column differences (strict order):");
            foreach (var d in diffs)
            {
                TestContext.WriteLine(d);
            }

            foreach (var d in diffs)
            {
                if (!String.IsNullOrEmpty(d.DbSuggestion)) TestContext.WriteLine($"{d.DbSuggestion}");
            }
        }

        if (all || fkKeysOnly)
        {
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
        }

        if (suggestions.Length > 0 && showEFSuggestions)
        {
            TestContext.WriteLine("");
            TestContext.WriteLine("Suggested HasColumnOrder mappings (verify property names):");
            TestContext.WriteLine("===============================");
            foreach (var s in suggestions) TestContext.WriteLine(s);
        }

        var total = diffs.Count + fkDiffs.Count + explicitNavDiffs.Count + navDiffs.Count + keyDiffs.Count + typeDiffs.Count + defaultDiffs.Count;
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