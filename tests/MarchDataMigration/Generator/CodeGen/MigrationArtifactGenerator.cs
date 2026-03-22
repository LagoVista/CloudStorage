using MarchDataMigration.Generator.Models;
using MarchDataMigration.Generator.Schema;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MarchDataMigration.Generator.CodeGen
{
    public sealed class MigrationArtifactGenerator
    {
        private readonly SqlSchemaIntrospector _schemaIntrospector = new();

        public async Task GenerateAsync(MigrationGeneratorRequest request, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentException.ThrowIfNullOrWhiteSpace(request.SourceConnectionString);
            ArgumentException.ThrowIfNullOrWhiteSpace(request.TargetConnectionString);
            ArgumentException.ThrowIfNullOrWhiteSpace(request.OutputRootPath);

            var sourceTables = await _schemaIntrospector.LoadAsync(request.SourceConnectionString, ct);
            var targetTables = await _schemaIntrospector.LoadAsync(request.TargetConnectionString, ct);

            var sourceLookup = sourceTables.ToDictionary(t => t.TableName, StringComparer.OrdinalIgnoreCase);
            var targetLookup = targetTables.ToDictionary(t => t.TableName, StringComparer.OrdinalIgnoreCase);

            var tableNames = sourceLookup.Keys.Intersect(targetLookup.Keys, StringComparer.OrdinalIgnoreCase).OrderBy(x => x).ToList();

            if (request.IncludeTables != null && request.IncludeTables.Count > 0)
            {
                var include = new HashSet<string>(request.IncludeTables, StringComparer.OrdinalIgnoreCase);
                tableNames = tableNames.Where(include.Contains).ToList();
            }

            var root = Path.Combine(request.OutputRootPath, request.TestsProjectPath.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(root);
            Directory.CreateDirectory(Path.Combine(root, "Generated"));
            Directory.CreateDirectory(Path.Combine(root, "Mappings"));
            Directory.CreateDirectory(Path.Combine(root, "Tests"));

            foreach (var tableName in request.IncludeTables)
            {
                var sourceName = tableName;
                var targetName = tableName;
                if (tableName.Contains("/"))
                {
                    sourceName = tableName.Split('/')[0];
                    targetName = tableName.Split('/')[1];
                }

                var source = sourceLookup[sourceName];
                var target = targetLookup[targetName];
                var featureDir = Path.Combine(root, "Generated", sourceName);
                Directory.CreateDirectory(featureDir);

                WriteGenerated(Path.Combine(featureDir, $"Source{sourceName}Row.g.cs"), BuildSourceRow(sourceName, source));
                WriteGenerated(Path.Combine(featureDir, $"Target{targetName}Row.g.cs"), BuildTargetRow(targetName, target));
                WriteGenerated(Path.Combine(featureDir, $"{sourceName}TableDefinition.g.cs"), BuildTableDefinition(sourceName, target));
                WriteGenerated(Path.Combine(featureDir, $"{sourceName}SourceReader.g.cs"), BuildSourceReader(sourceName, source));
                WriteGenerated(Path.Combine(featureDir, $"{sourceName}MigrationService.g.cs"), BuildMigrationService(sourceName, target));
                WriteGenerated(Path.Combine(root, "Tests", $"{sourceName}MigrationTests.g.cs"), BuildMigrationTest(sourceName));
                WriteIfMissing(Path.Combine(root, "Mappings", $"{sourceName}Mapper.cs"), BuildMapperStub(sourceName, source, target));
            }
        }

        private static void WriteGenerated(string path, string content)
        {
            File.WriteAllText(path, content, new UTF8Encoding(false));
        }

        private static void WriteIfMissing(string path, string content)
        {
            if (File.Exists(path))
                return;

            File.WriteAllText(path, content, new UTF8Encoding(false));
        }

        private static string BuildSourceRow(string tableName, SchemaTableInfo table)
        {
            var sb = new StringBuilder();
            sb.AppendLine("using System;");
            sb.AppendLine();
            sb.AppendLine($"namespace MarchDataMigration.Generated.{tableName}");
            sb.AppendLine("{");
            sb.AppendLine("    // generated: source-side 1:1 shape");
            sb.AppendLine($"    public sealed class Source{tableName}Row");
            sb.AppendLine("    {");

            foreach (var column in table.Columns.OrderBy(c => c.Ordinal))
            {
                sb.AppendLine($"        public {MapClrType(column.SqlTypeName, column.IsNullable)} {column.Name} {{ get; set; }}");
            }

            sb.AppendLine("    }");
            sb.AppendLine("}");
            return sb.ToString();
        }

        private static string BuildTargetRow(string tableName, SchemaTableInfo table)
        {
            var sb = new StringBuilder();
            sb.AppendLine("using System;");
            sb.AppendLine();
            sb.AppendLine($"namespace MarchDataMigration.Generated.{tableName}");
            sb.AppendLine("{");
            sb.AppendLine("    // generated: target-side 1:1 shape");
            sb.AppendLine($"    public partial class Target{tableName}Row");
            sb.AppendLine("    {");

            foreach (var column in table.Columns.OrderBy(c => c.Ordinal))
            {
                sb.AppendLine($"        public {MapClrType(column.SqlTypeName, column.IsNullable)} {column.Name} {{ get; set; }}");
            }

            sb.AppendLine("    }");
            sb.AppendLine("}");
            return sb.ToString();
        }

        private static string BuildTableDefinition(string tableName, SchemaTableInfo table)
        {
            var sb = new StringBuilder();
            sb.AppendLine("using Microsoft.Data.SqlClient;");
            sb.AppendLine("using System;");
            sb.AppendLine("using System.Data;");
            sb.AppendLine();
            sb.AppendLine($"namespace MarchDataMigration.Generated.{tableName}");
            sb.AppendLine("{");
            sb.AppendLine("    // generated: target table contract");
            sb.AppendLine($"    public static class {tableName}TableDefinition");
            sb.AppendLine("    {");
            sb.AppendLine($"        public const string TableName = \"{table.FullName}\";");
            sb.AppendLine();
            sb.AppendLine("        public static DataTable CreateDataTable()");
            sb.AppendLine("        {");
            sb.AppendLine("            var table = new DataTable();");

            foreach (var column in table.Columns.OrderBy(c => c.Ordinal))
            {
                sb.AppendLine($"            table.Columns.Add(\"{column.Name}\", typeof({GetNonNullableClrType(column.SqlTypeName)}));");
            }

            sb.AppendLine("            return table;");
            sb.AppendLine("        }");
            sb.AppendLine();
            sb.AppendLine($"        public static void AddRow(DataTable table, Target{tableName}Row row)");
            sb.AppendLine("        {");
            sb.AppendLine("            ArgumentNullException.ThrowIfNull(table);");
            sb.AppendLine("            ArgumentNullException.ThrowIfNull(row);");
            sb.AppendLine();
            sb.AppendLine("            table.Rows.Add(");

            var values = table.Columns.OrderBy(c => c.Ordinal).Select(c => ToDataTableValueExpression(c, "row"));
            sb.AppendLine("                " + string.Join(",\n                ", values) + ");");

            sb.AppendLine("        }");
            sb.AppendLine();
            sb.AppendLine("        public static void ConfigureMappings(SqlBulkCopy bulk)");
            sb.AppendLine("        {");
            sb.AppendLine("            ArgumentNullException.ThrowIfNull(bulk);");
            sb.AppendLine();

            foreach (var column in table.Columns.OrderBy(c => c.Ordinal))
            {
                sb.AppendLine($"            bulk.ColumnMappings.Add(\"{column.Name}\", \"{column.Name}\");");
            }

            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine("}");
            return sb.ToString();
        }

        private static string BuildSourceReader(string tableName, SchemaTableInfo table)
        {
            var orderColumn = table.GetOrderColumn();
            var sb = new StringBuilder();
            sb.AppendLine("using Microsoft.Data.SqlClient;");
            sb.AppendLine("using System;");
            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine("using System.Threading;");
            sb.AppendLine("using System.Threading.Tasks;");
            sb.AppendLine();
            sb.AppendLine($"namespace MarchDataMigration.Generated.{tableName}");
            sb.AppendLine("{");
            sb.AppendLine("    // generated: full source query and reader");
            sb.AppendLine($"    public static class {tableName}SourceReader");
            sb.AppendLine("    {");
            sb.AppendLine("        public const string SourceQuery = @\"");
            sb.AppendLine("SELECT");

            var selectLines = table.Columns.OrderBy(c => c.Ordinal).Select(c => $"    {c.Name}");
            sb.AppendLine(string.Join(",\n", selectLines));
            sb.AppendLine($"FROM {table.FullName}");
            sb.AppendLine($"ORDER BY {orderColumn.Name};\";");
            sb.AppendLine();
            sb.AppendLine($"        public static async Task<List<Source{tableName}Row>> LoadAsync(string connectionString, CancellationToken ct, int timeoutSeconds = 120)");
            sb.AppendLine("        {");
            sb.AppendLine("            ArgumentNullException.ThrowIfNull(connectionString);");
            sb.AppendLine();
            sb.AppendLine($"            var rows = new List<Source{tableName}Row>();");
            sb.AppendLine();
            sb.AppendLine("            await using var connection = new SqlConnection(connectionString);");
            sb.AppendLine("            await connection.OpenAsync(ct);");
            sb.AppendLine();
            sb.AppendLine("            await using var command = new SqlCommand(SourceQuery, connection)");
            sb.AppendLine("            {");
            sb.AppendLine("                CommandTimeout = timeoutSeconds");
            sb.AppendLine("            };");
            sb.AppendLine();
            sb.AppendLine("            await using var reader = await command.ExecuteReaderAsync(ct);");
            sb.AppendLine();
            sb.AppendLine("            while (await reader.ReadAsync(ct))");
            sb.AppendLine("            {");
            sb.AppendLine($"                rows.Add(new Source{tableName}Row");
            sb.AppendLine("                {");

            foreach (var column in table.Columns.OrderBy(c => c.Ordinal))
            {
                sb.AppendLine($"                    {column.Name} = {BuildReaderExpression(column)},");
            }

            sb.AppendLine("                });");
            sb.AppendLine("            }");
            sb.AppendLine();
            sb.AppendLine("            return rows;");
            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine("}");
            return sb.ToString();
        }

        private static string BuildMigrationService(string tableName, SchemaTableInfo target)
        {
            var sb = new StringBuilder();
            sb.AppendLine("using MarchDataMigration.Infrastructure;");
            sb.AppendLine("using MarchDataMigration.Mappings;");
            sb.AppendLine("using System;");
            sb.AppendLine("using System.Diagnostics;");
            sb.AppendLine("using System.Linq;");
            sb.AppendLine("using System.Threading;");
            sb.AppendLine("using System.Threading.Tasks;");
            sb.AppendLine();
            sb.AppendLine($"namespace MarchDataMigration.Generated.{tableName}");
            sb.AppendLine("{");
            sb.AppendLine("    // generated: orchestration, preserved mapping seam");
            sb.AppendLine($"    public sealed class {tableName}MigrationService");
            sb.AppendLine("    {");
            sb.AppendLine("        public async Task<TableMigrationResult> MigrateAsync(string sourceConnectionString, string targetConnectionString, CancellationToken ct)");
            sb.AppendLine("        {");
            sb.AppendLine("            var sw = Stopwatch.StartNew();");
            sb.AppendLine();
            sb.AppendLine("            try");
            sb.AppendLine("            {");
            sb.AppendLine($"                var sourceRows = await {tableName}SourceReader.LoadAsync(sourceConnectionString, ct);");
            sb.AppendLine($"                var targetRows = sourceRows.Select(source => {tableName}Mapper.Map(source)).ToList();");
            sb.AppendLine();
            sb.AppendLine($"                var table = {tableName}TableDefinition.CreateDataTable();");
            sb.AppendLine("                foreach (var row in targetRows)");
            sb.AppendLine("                {");
            sb.AppendLine($"                    {tableName}TableDefinition.AddRow(table, row);");
            sb.AppendLine("                }");
            sb.AppendLine();
            sb.AppendLine($"                await SqlBulkInsertService.ExecuteAsync(targetConnectionString, \"DELETE FROM {target.FullName};\", ct);");
            sb.AppendLine();
            sb.AppendLine("                var inserted = await SqlBulkInsertService.BulkInsertAsync(");
            sb.AppendLine("                    targetConnectionString,");
            sb.AppendLine($"                    {tableName}TableDefinition.TableName,");
            sb.AppendLine("                    table,");
            sb.AppendLine($"                    {tableName}TableDefinition.ConfigureMappings,");
            sb.AppendLine("                    ct);");
            sb.AppendLine();
            sb.AppendLine($"                var targetCount = await SqlBulkInsertService.CountAsync(targetConnectionString, {tableName}TableDefinition.TableName, ct);");
            sb.AppendLine();
            sb.AppendLine("                return new TableMigrationResult");
            sb.AppendLine("                {");
            sb.AppendLine($"                    TableName = {tableName}TableDefinition.TableName,");
            sb.AppendLine("                    SourceCount = sourceRows.Count,");
            sb.AppendLine("                    InsertedCount = inserted,");
            sb.AppendLine("                    TargetCountAfterInsert = targetCount,");
            sb.AppendLine("                    Duration = sw.Elapsed,");
            sb.AppendLine("                    Success = true");
            sb.AppendLine("                };");
            sb.AppendLine("            }");
            sb.AppendLine("            catch (Exception ex)");
            sb.AppendLine("            {");
            sb.AppendLine($"                return TableMigrationResult.Failure({tableName}TableDefinition.TableName, sw.Elapsed, ex);");
            sb.AppendLine("            }");
            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine("}");
            return sb.ToString();
        }

        private static string BuildMapperStub(string tableName, SchemaTableInfo source, SchemaTableInfo target)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"using MarchDataMigration.Generated.{tableName};");
            sb.AppendLine();
            sb.AppendLine("namespace MarchDataMigration.Mappings");
            sb.AppendLine("{");
            sb.AppendLine("    // handwritten: safe to edit, never regenerated");
            sb.AppendLine($"    public static class {tableName}Mapper");
            sb.AppendLine("    {");
            sb.AppendLine($"        public static Target{tableName}Row Map(Source{tableName}Row source)");
            sb.AppendLine("        {");
            sb.AppendLine($"            return new Target{tableName}Row");
            sb.AppendLine("            {");

            foreach (var targetColumn in target.Columns.OrderBy(c => c.Ordinal))
            {
                if (source.Columns.Any(c => string.Equals(c.Name, targetColumn.Name, StringComparison.OrdinalIgnoreCase)))
                    sb.AppendLine($"                {targetColumn.Name} = source.{targetColumn.Name},");
                else
                    sb.AppendLine($"                {targetColumn.Name} = default,");
            }

            sb.AppendLine("            };");
            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine("}");
            return sb.ToString();
        }

        private static string BuildMigrationTest(string tableName)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"using MarchDataMigration.Generated.{tableName};");
            sb.AppendLine("using MarchDataMigration.Infrastructure;");
            sb.AppendLine("using NUnit.Framework;");
            sb.AppendLine("using System.Threading;");
            sb.AppendLine("using System.Threading.Tasks;");
            sb.AppendLine();
            sb.AppendLine("namespace MarchDataMigration.Tests");
            sb.AppendLine("{");
            sb.AppendLine("    [TestFixture]");
            sb.AppendLine($"    public class {tableName}MigrationTests : MigrationTestBase");
            sb.AppendLine("    {");
            sb.AppendLine("        [Test]");
            sb.AppendLine($"        public async Task Migrate_{tableName}()");
            sb.AppendLine("        {");
            sb.AppendLine($"            var service = new {tableName}MigrationService();");
            sb.AppendLine("            var result = await service.MigrateAsync(SourceConnectionString, TargetConnectionString, CancellationToken.None);");
            sb.AppendLine();
            sb.AppendLine("            TestContext.WriteLine($\"table={result.TableName} success={result.Success} source={result.SourceCount} inserted={result.InsertedCount} target={result.TargetCountAfterInsert} duration={result.Duration}\");");
            sb.AppendLine();
            sb.AppendLine("            Assert.That(result.Success, Is.True, result.ErrorMessage);");
            sb.AppendLine("            Assert.That(result.SourceCount, Is.EqualTo(result.InsertedCount), \"Source/insert count mismatch.\");");
            sb.AppendLine("            Assert.That(result.TargetCountAfterInsert, Is.EqualTo(result.InsertedCount), \"Target/insert count mismatch.\");");
            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine("}");
            return sb.ToString();
        }

        private static string MapClrType(string sqlTypeName, bool isNullable)
        {
            var type = GetNonNullableClrType(sqlTypeName);
            return type switch
            {
                "string" => "string",
                "byte[]" => "byte[]",
                _ when isNullable => type + "?",
                _ => type
            };
        }

        private static string GetNonNullableClrType(string sqlTypeName)
        {
            return sqlTypeName.ToLowerInvariant() switch
            {
                "uniqueidentifier" => "Guid",
                "bigint" => "long",
                "int" => "int",
                "smallint" => "short",
                "tinyint" => "byte",
                "bit" => "bool",
                "decimal" => "decimal",
                "numeric" => "decimal",
                "money" => "decimal",
                "smallmoney" => "decimal",
                "float" => "double",
                "real" => "float",
                "datetime" => "DateTime",
                "datetime2" => "DateTime",
                "smalldatetime" => "DateTime",
                "date" => "DateTime",
                "time" => "TimeSpan",
                "datetimeoffset" => "DateTimeOffset",
                "binary" => "byte[]",
                "varbinary" => "byte[]",
                "image" => "byte[]",
                _ => "string"
            };
        }

        private static string BuildReaderExpression(SchemaColumnInfo column)
        {
            var ordinal = $"reader.GetOrdinal(\"{column.Name}\")";
            var nullableCheck = $"reader.IsDBNull({ordinal})";
            var type = GetNonNullableClrType(column.SqlTypeName);

            return type switch
            {
                "Guid" when column.IsNullable => $"{nullableCheck} ? (Guid?)null : reader.GetGuid({ordinal})",
                "Guid" => $"reader.GetGuid({ordinal})",
                "long" when column.IsNullable => $"{nullableCheck} ? (long?)null : reader.GetInt64({ordinal})",
                "long" => $"reader.GetInt64({ordinal})",
                "int" when column.IsNullable => $"{nullableCheck} ? (int?)null : reader.GetInt32({ordinal})",
                "int" => $"reader.GetInt32({ordinal})",
                "short" when column.IsNullable => $"{nullableCheck} ? (short?)null : reader.GetInt16({ordinal})",
                "short" => $"reader.GetInt16({ordinal})",
                "byte" when column.IsNullable => $"{nullableCheck} ? (byte?)null : reader.GetByte({ordinal})",
                "byte" => $"reader.GetByte({ordinal})",
                "bool" when column.IsNullable => $"{nullableCheck} ? (bool?)null : reader.GetBoolean({ordinal})",
                "bool" => $"reader.GetBoolean({ordinal})",
                "decimal" when column.IsNullable => $"{nullableCheck} ? (decimal?)null : reader.GetDecimal({ordinal})",
                "decimal" => $"reader.GetDecimal({ordinal})",
                "double" when column.IsNullable => $"{nullableCheck} ? (double?)null : reader.GetDouble({ordinal})",
                "double" => $"reader.GetDouble({ordinal})",
                "float" when column.IsNullable => $"{nullableCheck} ? (float?)null : reader.GetFloat({ordinal})",
                "float" => $"reader.GetFloat({ordinal})",
                "DateTime" when column.IsNullable => $"{nullableCheck} ? (DateTime?)null : reader.GetDateTime({ordinal})",
                "DateTime" => $"reader.GetDateTime({ordinal})",
                "DateTimeOffset" when column.IsNullable => $"{nullableCheck} ? (DateTimeOffset?)null : reader.GetFieldValue<DateTimeOffset>({ordinal})",
                "DateTimeOffset" => $"reader.GetFieldValue<DateTimeOffset>({ordinal})",
                "TimeSpan" when column.IsNullable => $"{nullableCheck} ? (TimeSpan?)null : reader.GetTimeSpan({ordinal})",
                "TimeSpan" => $"reader.GetTimeSpan({ordinal})",
                "byte[]" => $"{nullableCheck} ? null : (byte[])reader[\"{column.Name}\"]",
                _ => $"{nullableCheck} ? null : reader[\"{column.Name}\"].ToString()"
            };
        }

        private static string ToDataTableValueExpression(SchemaColumnInfo column, string rowName)
        {
            var type = GetNonNullableClrType(column.SqlTypeName);
            if (type == "string" || type == "byte[]")
                return $"(object?){rowName}.{column.Name} ?? DBNull.Value";

            return column.IsNullable ? $"{rowName}.{column.Name}.HasValue ? {rowName}.{column.Name}.Value : DBNull.Value" : $"{rowName}.{column.Name}";
        }
    }
}
