namespace SchemaVerify.Core;

public sealed class SchemaModel
{
    public List<TableModel> Tables { get; } = new();

    public TableModel? FindTable(string schema, string name)
        => Tables.FirstOrDefault(t =>
            string.Equals(t.Schema, schema, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(t.Name, name, StringComparison.OrdinalIgnoreCase));
}

public sealed class TableModel
{
    public required string Schema { get; init; }
    public required string Name { get; init; }

    public List<ColumnModel> Columns { get; } = new();

    /// <summary>
    /// Primary key column names in ordinal order.
    /// </summary>
    public List<string> PrimaryKey { get; } = new();

    public ColumnModel? FindColumn(string name)
        => Columns.FirstOrDefault(c => string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase));
}

public sealed class ColumnModel
{
    public required string Name { get; init; }
    public required string StoreType { get; init; }
    public required bool IsNullable { get; init; }

    public int? MaxLength { get; init; }
    public int? Precision { get; init; }
    public int? Scale { get; init; }
}
