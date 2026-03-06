using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

public class TypeDiff
{
    public string ColumnName { get; set; }
    public string DbType { get; set; }
    public string EfType { get; set; }

    public bool IsNullable { get; set; }
    public string Message { get; set; }

    public override string ToString() => Message;
}

public static class ColumnTypeDiff
{
    public static List<TypeDiff> CompareStrict(IReadOnlyList<ColumnTypeShape> db, IReadOnlyList<ColumnTypeShape> ef)
    {
        var diffs = new List<TypeDiff>();

        var dbMap = db.ToDictionary(x => x.ColumnName, x => Normalize(x.StoreType), StringComparer.OrdinalIgnoreCase);
        var efMap = ef.ToDictionary(x => x.ColumnName, x => Normalize(x.StoreType), StringComparer.OrdinalIgnoreCase);

        foreach (var (col, dbType) in dbMap)
        {
            if (!efMap.TryGetValue(col, out var efType))
            {
                diffs.Add(new TypeDiff() { Message = $"Missing in EF: store type for {col} = {dbType}", ColumnName = col, DbType = dbType, EfType = efType });
                continue;
            }

            if (!string.Equals(dbType, efType, StringComparison.OrdinalIgnoreCase))
            {
                var dbColumn = ef.First(x => string.Equals(x.ColumnName, col, StringComparison.OrdinalIgnoreCase));
                diffs.Add(new TypeDiff() { Message = $"Type mismatch on {col}: DB={dbType} EF={efType}", ColumnName = col, DbType = dbType, EfType = efType, IsNullable = dbColumn.IsNullable });
            }
        }

        foreach (var (col, efType) in efMap)
        {
            if (!dbMap.ContainsKey(col))
                diffs.Add(new TypeDiff() { Message = $"Extra in EF: store type for {col} = {efType}", ColumnName = col, DbType = "n/a", EfType = efType });
        }

        return diffs;

        static string Normalize(string s)
            => (s ?? string.Empty).Trim().ToLowerInvariant().Replace(" ", string.Empty);
    }
}