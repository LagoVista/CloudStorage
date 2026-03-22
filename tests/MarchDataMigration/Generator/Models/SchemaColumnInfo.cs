namespace MarchDataMigration.Generator.Models
{
    public sealed class SchemaColumnInfo
    {
        public string Name { get; init; }
        public string SqlTypeName { get; init; }
        public bool IsNullable { get; init; }
        public int Ordinal { get; init; }
        public bool IsPrimaryKey { get; init; }
    }
}
