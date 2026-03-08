using LagoVista.CloudStorage.Utils;
using LagoVista.Core.Models;
using LagoVista.Models;
using LagoVista.Relational;
using LagoVista.Relational.DataContexts;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Relational.Tests
{
    public sealed class SchemaValidator : SchemaContractTestBase {
        private static readonly ImmutableHashSet<string> IgnoredTables = ImmutableHashSet.Create(StringComparer.OrdinalIgnoreCase,
        "__EFMigrationsHistory", "sysdiagrams" // add others if you have them
    );


        [Test]
        public async Task AllEntitiesMatchDatabaseSchema()
        {

            using var ctx = CreateContext();

            var entities = ctx.Model.GetEntityTypes()
                .Where(e =>
                    e.ClrType != null &&
                    !e.IsOwned() &&
                    e.FindPrimaryKey() != null &&
                    e.GetTableName() != null &&
                    e.GetViewName() == null);


            foreach (var entity in entities.OrderBy(e => e.ClrType.Name))
            {
                Console.WriteLine($"Testing {entity.ClrType.Name} against {entity.GetSchema() ?? "dbo"}.{entity.GetTableName()}");
                var dtoType = entity.ClrType;
                await AssertTableMatchesModelAsync(dtoType, true, showEFSuggestions: false, showDBSuggestions: true, typesOnly: true);
            }
        }

        [Test]
        public async Task CheckSingleTable() => await AssertTableMatchesModelAsync(typeof(TimeEntryDTO), true, showEFSuggestions:true, showDBSuggestions:true, typesOnly:true );

        private static HashSet<string> GetExpectedTablesFromDbSets(DbContext ctx)
        {
            var expected = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            var dbSetEntityTypes = ctx.GetType()
                .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(p => p.PropertyType.IsGenericType &&
                            p.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>))
                .Select(p => p.PropertyType.GetGenericArguments()[0]);

            foreach (var clrType in dbSetEntityTypes)
            {
                var et = ctx.Model.FindEntityType(clrType);
                if (et == null) continue;

                // Skip views / keyless / owned / anything not mapped to a table
                if (et.GetTableName() == null) continue;
                if (et.GetViewName() != null) continue;
                if (et.IsOwned()) continue;
                if (et.FindPrimaryKey() == null) continue;

                var schema = et.GetSchema() ?? "dbo";
                expected.Add($"{schema}.{et.GetTableName()}");
            }

            return expected;
        }

        [Test]
        public async Task Should_Have_A_Contract_Test_For_Every_Dbo_Table()
        {
            using var ctx = CreateContext();

            var dbTables = await ctx.Database
                .SqlQueryRaw<string>(@"
            SELECT CONCAT(s.name, '.', t.name) AS [Value]
            FROM sys.tables t
            JOIN sys.schemas s ON s.schema_id = t.schema_id
            WHERE t.is_ms_shipped = 0
            ORDER BY s.name, t.name;
        ")
                .ToListAsync();

            var expected = GetExpectedTablesFromDbSets(ctx);

            var actual = dbTables
            .Where(x =>
            {
                var name = x.Split('.').Last();
                if (IgnoredTables.Contains(name))
                {
                    Console.WriteLine($"Ignoring table {x}");
                    return false;
                }
                return true;
            })
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var missingInContracts = actual.Except(expected).OrderBy(x => x).ToList();
            var mappedButMissingInDb = expected.Except(actual).OrderBy(x => x).ToList();

            if (missingInContracts.Count > 0 || mappedButMissingInDb.Count > 0)
            {
                var msg =
                    "Schema contract coverage mismatch:\n" +
                    (missingInContracts.Count > 0 ? "\nTables in DB with NO contract mapping:\n  - " + string.Join("\n  - ", missingInContracts) + "\n" : "") +
                    (mappedButMissingInDb.Count > 0 ? "\nMapped tables missing in DB:\n  - " + string.Join("\n  - ", mappedButMissingInDb) + "\n" : "");
                Assert.Fail(msg);
            }
        }
    }
}
