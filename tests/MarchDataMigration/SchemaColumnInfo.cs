using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LegacyMigrationScaffolding.Schema
{
    public sealed class SchemaColumnInfo
    {
        public string ColumnName { get; set; }
        public string SqlDataType { get; set; }
        public bool IsNullable { get; set; }
        public int OrdinalPosition { get; set; }
        public int? MaxLength { get; set; }
        public int? Precision { get; set; }
        public int? Scale { get; set; }
        public bool IsPrimaryKey { get; set; }
        public bool IsIdentity { get; set; }
    }

    public sealed class SchemaReadOptions
    {
        public string SchemaName { get; set; } = "dbo";
    }

    public sealed class SchemaTableInfo
    {
        public string SchemaName { get; set; }
        public string TableName { get; set; }
        public List<SchemaColumnInfo> Columns { get; set; } = new List<SchemaColumnInfo>();

        public string FullName => $"[{SchemaName}].[{TableName}]";

        public SchemaColumnInfo GetPrimaryKeyColumn()
        {
            return Columns
                .OrderBy(x => x.OrdinalPosition)
                .FirstOrDefault(x => x.IsPrimaryKey);
        }

        public SchemaColumnInfo GetColumn(string columnName)
        {
            return Columns.FirstOrDefault(x => x.ColumnName.Equals(columnName, System.StringComparison.OrdinalIgnoreCase));
        }
    }

    public sealed class ScaffoldingOptions
    {
        public string SourceConnectionString { get; set; }
        public string DestinationConnectionString { get; set; }
        public string OutputFolder { get; set; }
        public string Namespace { get; set; } = "LegacyMigration.Generated";
        public string SchemaName { get; set; } = "dbo";
    }
}
