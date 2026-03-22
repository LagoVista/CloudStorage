using System.Collections.Generic;
using System.Linq;

namespace MarchDataMigration.Generator.Models
{
    public sealed class SchemaTableInfo
    {
        public string SchemaName { get; init; }
        public string TableName { get; init; }
        public List<SchemaColumnInfo> Columns { get; init; } = new();

        public string FullName => $"{SchemaName}.{TableName}";

        public SchemaColumnInfo GetOrderColumn()
        {
            return Columns.OrderBy(c => c.Ordinal).FirstOrDefault(c => c.IsPrimaryKey) ?? Columns.OrderBy(c => c.Ordinal).First();
        }
    }
}
