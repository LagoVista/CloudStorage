// ================================
// File: LegacyMigrationScaffolding/Generation/MigrationScaffolder.cs
// ================================
using LegacyMigrationScaffolding.Schema;
using MarchDataMigration.LegacyMigrationScaffolding.Schema;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LegacyMigrationScaffolding.Generation
{
    public sealed class MigrationScaffolder
    {
        private readonly SchemaReader _schemaReader = new SchemaReader();

        public async Task GenerateAsync(ScaffoldingOptions options, CancellationToken cancellationToken = default)
        {
            Directory.CreateDirectory(options.OutputFolder);

            var sourceTables = await _schemaReader.ReadTablesAsync(options.SourceConnectionString, new SchemaReadOptions { SchemaName = options.SchemaName }, cancellationToken);
            var destinationTables = await _schemaReader.ReadTablesAsync(options.DestinationConnectionString, new SchemaReadOptions { SchemaName = options.SchemaName }, cancellationToken);

            var destinationLookup = destinationTables.ToDictionary(x => x.TableName, x => x, System.StringComparer.OrdinalIgnoreCase);

            foreach (var sourceTable in sourceTables)
            {
                if (!destinationLookup.TryGetValue(sourceTable.TableName, out var destinationTable))
                    continue;

                var code = GenerateTableMigrationCode(options.Namespace, sourceTable, destinationTable);
                var className = $"{CSharpNameHelper.ToPascalCase(sourceTable.TableName)}TableMigration";
                var filePath = Path.Combine(options.OutputFolder, $"{className}.generated.cs");
                await File.WriteAllTextAsync(filePath, code, cancellationToken);
            }
        }

        private string GenerateTableMigrationCode(string ns, SchemaTableInfo sourceTable, SchemaTableInfo destinationTable)
        {
            var tableName = CSharpNameHelper.ToPascalCase(sourceTable.TableName);
            var migrationClassName = $"{tableName}TableMigration";
            var sourceRowClassName = $"{tableName}SourceRow";
            var destinationRowClassName = $"{tableName}DestinationRow";

            var keyColumn = sourceTable.GetPrimaryKeyColumn() ?? sourceTable.Columns.First();

            var sourceProperties = GenerateProperties(sourceTable.Columns);
            var destinationProperties = GenerateProperties(destinationTable.Columns);
            var materializerAssignments = GenerateMaterializerAssignments(sourceTable.Columns);
            var mapAssignments = GenerateMapAssignments(sourceTable, destinationTable);
            var todoComments = GenerateTodoComments(sourceTable, destinationTable);

            var builder = new StringBuilder();

            builder.AppendLine("using LegacyMigrationScaffolding.Runtime;");
            builder.AppendLine("using System.Data;");
            builder.AppendLine("using System.Threading;");
            builder.AppendLine("using System.Threading.Tasks;");
            builder.AppendLine();
            builder.AppendLine($"namespace {ns}");
            builder.AppendLine("{");
            builder.AppendLine($"    public sealed partial class {migrationClassName} : TableMigrationBase<{sourceRowClassName}, {destinationRowClassName}>");
            builder.AppendLine("    {");
            builder.AppendLine($"        public override string SourceSchemaName => \"{CSharpNameHelper.EscapeStringLiteral(sourceTable.SchemaName)}\";");
            builder.AppendLine($"        public override string SourceTableName => \"{CSharpNameHelper.EscapeStringLiteral(sourceTable.TableName)}\";");
            builder.AppendLine($"        public override string DestinationSchemaName => \"{CSharpNameHelper.EscapeStringLiteral(destinationTable.SchemaName)}\";");
            builder.AppendLine($"        public override string DestinationTableName => \"{CSharpNameHelper.EscapeStringLiteral(destinationTable.TableName)}\";");
            builder.AppendLine($"        public override string KeyColumnName => \"{CSharpNameHelper.EscapeStringLiteral(keyColumn.ColumnName)}\";");
            builder.AppendLine();
            builder.AppendLine($"        protected override {sourceRowClassName} MaterializeSource(IDataRecord record)");
            builder.AppendLine("        {");
            builder.AppendLine($"            return new {sourceRowClassName}");
            builder.AppendLine("            {");
            builder.Append(materializerAssignments);
            builder.AppendLine("            };");
            builder.AppendLine("        }");
            builder.AppendLine();
            builder.AppendLine($"        protected override {destinationRowClassName} Map({sourceRowClassName} source)");
            builder.AppendLine("        {");
            if (!string.IsNullOrWhiteSpace(todoComments))
            {
                builder.Append(todoComments);
            }

            builder.AppendLine($"            return new {destinationRowClassName}");
            builder.AppendLine("            {");
            builder.Append(mapAssignments);
            builder.AppendLine("            };");
            builder.AppendLine("        }");
            builder.AppendLine();
            builder.AppendLine($"        protected override partial Task PersistAsync({destinationRowClassName} destination, CancellationToken cancellationToken);");
            builder.AppendLine("    }");
            builder.AppendLine();
            builder.AppendLine($"    public sealed class {sourceRowClassName}");
            builder.AppendLine("    {");
            builder.Append(sourceProperties);
            builder.AppendLine("    }");
            builder.AppendLine();
            builder.AppendLine($"    public sealed class {destinationRowClassName}");
            builder.AppendLine("    {");
            builder.Append(destinationProperties);
            builder.AppendLine("    }");
            builder.AppendLine("}");

            return builder.ToString();
        }

        private string GenerateProperties(IEnumerable<SchemaColumnInfo> columns)
        {
            var builder = new StringBuilder();

            foreach (var column in columns.OrderBy(x => x.OrdinalPosition))
            {
                var propertyName = CSharpNameHelper.SafeIdentifier(column.ColumnName);
                var propertyType = SqlClrTypeMapper.MapToClrType(column);
                builder.AppendLine($"        public {propertyType} {propertyName} {{ get; set; }}");
            }

            return builder.ToString();
        }

        private string GenerateMaterializerAssignments(IEnumerable<SchemaColumnInfo> columns)
        {
            var ordered = columns.OrderBy(x => x.OrdinalPosition).ToList();
            var builder = new StringBuilder();

            for (var idx = 0; idx < ordered.Count; idx++)
            {
                var column = ordered[idx];
                var propertyName = CSharpNameHelper.SafeIdentifier(column.ColumnName);
                var propertyType = SqlClrTypeMapper.MapToClrType(column);
                var comma = idx < ordered.Count - 1 ? "," : string.Empty;
                builder.AppendLine($"                {propertyName} = record.GetValueOrDefault<{propertyType}>(\"{CSharpNameHelper.EscapeStringLiteral(column.ColumnName)}\"){comma}");
            }

            return builder.ToString();
        }

        private string GenerateMapAssignments(SchemaTableInfo sourceTable, SchemaTableInfo destinationTable)
        {
            var builder = new StringBuilder();
            var destinationColumns = destinationTable.Columns.OrderBy(x => x.OrdinalPosition).ToList();

            for (var idx = 0; idx < destinationColumns.Count; idx++)
            {
                var destinationColumn = destinationColumns[idx];
                var sourceColumn = sourceTable.GetColumn(destinationColumn.ColumnName);
                var destinationPropertyName = CSharpNameHelper.SafeIdentifier(destinationColumn.ColumnName);
                var comma = idx < destinationColumns.Count - 1 ? "," : string.Empty;

                if (sourceColumn != null)
                {
                    var sourcePropertyName = CSharpNameHelper.SafeIdentifier(sourceColumn.ColumnName);
                    builder.AppendLine($"                {destinationPropertyName} = source.{sourcePropertyName}{comma}");
                }
                else
                {
                    builder.AppendLine($"                {destinationPropertyName} = default{comma}");
                }
            }

            return builder.ToString();
        }

        private string GenerateTodoComments(SchemaTableInfo sourceTable, SchemaTableInfo destinationTable)
        {
            var builder = new StringBuilder();

            var sourceOnly = sourceTable.Columns
                .Where(x => destinationTable.GetColumn(x.ColumnName) == null)
                .OrderBy(x => x.OrdinalPosition)
                .ToList();

            var destinationOnly = destinationTable.Columns
                .Where(x => sourceTable.GetColumn(x.ColumnName) == null)
                .OrderBy(x => x.OrdinalPosition)
                .ToList();

            foreach (var source in sourceOnly)
            {
                builder.AppendLine($"            // TODO: Source column '{source.ColumnName}' has no matching destination column.");
            }

            foreach (var destination in destinationOnly)
            {
                builder.AppendLine($"            // TODO: Destination column '{destination.ColumnName}' has no matching source column.");
            }

            if (builder.Length > 0)
                builder.AppendLine();

            return builder.ToString();
        }
    }
}