using Microsoft.EntityFrameworkCore.Diagnostics;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;


namespace Relational.Tests.Core.Database;

public sealed class PrettyCommandInterceptor : DbCommandInterceptor
{
    public bool IsReady { get; set; }

    private static readonly Regex InsertRegex = new(
        @"INSERT\s+INTO\s+(?<table>.+?)\s*\((?<cols>[^)]+)\)\s*VALUES\s*\((?<vals>[^)]+)\)",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

    private static readonly Regex UpdateRegex = new(
        @"UPDATE\s+(?<table>.+?)\s+SET\s+(?<set>.+?)(\s+WHERE\s+(?<where>.+))?$",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

    // Matches: "Column" = @p0   or   Column=@p0
    private static readonly Regex ColumnEqParamRegex = new(
        @"(?<col>(""[^""]+""|\[[^\]]+\]|\w+))\s*=\s*(?<p>@\w+)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public override InterceptionResult<int> NonQueryExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<int> result)
    {
        Dump(command);
        return base.NonQueryExecuting(command, eventData, result);
    }

    public override InterceptionResult<DbDataReader> ReaderExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result)
    {
        Dump(command);
        return base.ReaderExecuting(command, eventData, result);
    }

    public override InterceptionResult<object> ScalarExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<object> result)
    {
        Dump(command);
        return base.ScalarExecuting(command, eventData, result);
    }

    private void Dump(DbCommand command)
    {
        if (!IsReady) return;

        var sql = command.CommandText?.Trim() ?? string.Empty;

        // 1) Try INSERT mapping
        if (TryDumpInsert(command, sql)) return;

        // 2) Try UPDATE mapping
        if (TryDumpUpdate(command, sql)) return;

        // 3) Best-effort mapping for any statement: look for "Col"=@pX patterns
        if (TryDumpBestEffortColumnParamPairs(command, sql)) return;

        // 4) Fallback: just dump parameters cleanly
        DumpParams(command, header: "==== SQL (no safe column map) ====");
    }

    private static bool TryDumpInsert(DbCommand command, string sql)
    {
        var m = InsertRegex.Match(sql);
        if (!m.Success) return false;

        var cols = SplitCsv(m.Groups["cols"].Value).Select(Unquote).ToList();
        var vals = SplitCsv(m.Groups["vals"].Value).ToList();

        Console.WriteLine("==== SQL INSERT ====");
        Console.WriteLine(sql);

        Console.WriteLine("---- Parameters (mapped to INSERT columns) ----");
        for (int i = 0; i < vals.Count; i++)
        {
            var pName = vals[i].Trim();
            var col = i < cols.Count ? cols[i] : "???";
            if (TryGetParam(command, pName, out var p))
                Console.WriteLine($"{pName} ({col}) = {Format(p)}");
            else
                Console.WriteLine($"{pName} ({col}) = <NOT FOUND>");
        }

        return true;
    }

    private static bool TryDumpUpdate(DbCommand command, string sql)
    {
        var m = UpdateRegex.Match(sql);
        if (!m.Success) return false;

        Console.WriteLine("==== SQL UPDATE ====");
        Console.WriteLine(sql);

        // Parse SET clause pairs. Handles: col=@p0, "Col"=@p1, [Col]=@p2
        var setClause = m.Groups["set"].Value;
        var pairs = ColumnEqParamRegex.Matches(setClause)
            .Cast<Match>()
            .Select(x => new { Col = Unquote(x.Groups["col"].Value.Trim()), P = x.Groups["p"].Value.Trim() })
            .ToList();

        if (pairs.Count == 0) return false;

        Console.WriteLine("---- Parameters (mapped to UPDATE SET columns) ----");
        foreach (var pair in pairs)
        {
            if (TryGetParam(command, pair.P, out var p))
                Console.WriteLine($"{pair.P} ({pair.Col}) = {Format(p)}");
            else
                Console.WriteLine($"{pair.P} ({pair.Col}) = <NOT FOUND>");
        }

        // Also dump any remaining params not covered (often WHERE params)
        DumpUnmappedParams(command, pairs.Select(x => x.P).ToHashSet(StringComparer.OrdinalIgnoreCase));

        return true;
    }

    private static bool TryDumpBestEffortColumnParamPairs(DbCommand command, string sql)
    {
        var pairs = ColumnEqParamRegex.Matches(sql)
            .Cast<Match>()
            .Select(x => new { Col = Unquote(x.Groups["col"].Value.Trim()), P = x.Groups["p"].Value.Trim() })
            .DistinctBy(x => (x.Col, x.P))
            .ToList();

        if (pairs.Count == 0) return false;

        Console.WriteLine("==== SQL (best-effort column->param mapping) ====");
        Console.WriteLine(sql);
        Console.WriteLine("---- Parameters (best-effort mapping) ----");

        var mapped = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in pairs)
        {
            mapped.Add(pair.P);
            if (TryGetParam(command, pair.P, out var p))
                Console.WriteLine($"{pair.P} ({pair.Col}) = {Format(p)}");
            else
                Console.WriteLine($"{pair.P} ({pair.Col}) = <NOT FOUND>");
        }

        DumpUnmappedParams(command, mapped);
        return true;
    }

    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
    DbCommand command,
    CommandEventData eventData,
    InterceptionResult<DbDataReader> result,
    CancellationToken cancellationToken = default)
    {
        Dump(command);
        return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
    }

    private static void DumpParams(DbCommand command, string header)
    {
        Console.WriteLine(header);
        Console.WriteLine(command.CommandText);

        Console.WriteLine("---- Parameters ----");
        foreach (DbParameter p in command.Parameters)
            Console.WriteLine($"{p.ParameterName} = {Format(p)}");
    }

    private static void DumpUnmappedParams(DbCommand command, HashSet<string> mapped)
    {
        var any = false;
        foreach (DbParameter p in command.Parameters)
        {
            if (!mapped.Contains(p.ParameterName))
            {
                if (!any)
                {
                    Console.WriteLine("---- Unmapped parameters ----");
                    any = true;
                }

                Console.WriteLine($"{p.ParameterName} = {Format(p)}");
            }
        }
    }

    private static bool TryGetParam(DbCommand command, string name, out DbParameter p)
    {
        // DbParameterCollection indexer uses exact name, so be cautious.
        // Most providers include the '@'. We'll try exact first then a couple variants.
        if (command.Parameters.Contains(name))
        {
            p = (DbParameter)command.Parameters[name]!;
            return true;
        }

        var alt = name.StartsWith("@") ? name.Substring(1) : "@" + name;
        if (command.Parameters.Contains(alt))
        {
            p = (DbParameter)command.Parameters[alt]!;
            return true;
        }

        p = null!;
        return false;
    }

    public override void CommandFailed(DbCommand command, CommandErrorEventData eventData)
    {
        TestContext.Out.WriteLine("=== SQL COMMAND FAILED ===");
        Dump(command);

        TestContext.Out.WriteLine(
            $"Exception: {eventData.Exception.GetType().Name}: {eventData.Exception.Message}");

        if (eventData.Exception.InnerException != null)
        {
            TestContext.Out.WriteLine(
                $"Inner Exception: {eventData.Exception.InnerException.GetType().Name}: {eventData.Exception.InnerException.Message}");
        }

        if (eventData.Exception is Microsoft.Data.Sqlite.SqliteException sqliteEx &&
            sqliteEx.SqliteErrorCode == 19) // SQLITE_CONSTRAINT
        {
            DumpSqliteForeignKeyDiagnostics(command);
        }

        TestContext.Out.WriteLine("==========================");

        base.CommandFailed(command, eventData);
    }

    private static void DumpSqliteForeignKeyDiagnostics(DbCommand failedCommand)
    {
        try
        {
            if (failedCommand.Connection == null)
            {
                TestContext.Out.WriteLine("No connection available for FK diagnostics.");
                return;
            }

            if (failedCommand.Connection.State != ConnectionState.Open)
            {
                TestContext.Out.WriteLine("Connection is not open, cannot run PRAGMA foreign_key_check.");
                return;
            }

            TestContext.Out.WriteLine("=== SQLITE FK DIAGNOSTICS ===");

            using var fkCheck = failedCommand.Connection.CreateCommand();
            fkCheck.Transaction = failedCommand.Transaction;
            fkCheck.CommandText = "PRAGMA foreign_key_check;";

            using var reader = fkCheck.ExecuteReader();

            var foundAny = false;
            while (reader.Read())
            {
                foundAny = true;

                var childTable = reader.IsDBNull(0) ? "<null>" : reader.GetString(0);
                var rowId = reader.IsDBNull(1) ? "<null>" : reader.GetValue(1)?.ToString();
                var parentTable = reader.IsDBNull(2) ? "<null>" : reader.GetString(2);
                var fkId = reader.IsDBNull(3) ? "<null>" : reader.GetValue(3)?.ToString();

                TestContext.Out.WriteLine(
                    $"Violation => ChildTable: {childTable}, RowId: {rowId}, ParentTable: {parentTable}, FkId: {fkId}");

                DumpForeignKeyList(failedCommand, childTable);
            }

            if (!foundAny)
            {
                TestContext.Out.WriteLine(
                    "PRAGMA foreign_key_check returned no rows.");
                TestContext.Out.WriteLine(
                    "This can happen when the failing statement is rejected immediately, so no violating row remains to inspect.");
            }

            TestContext.Out.WriteLine("=== END SQLITE FK DIAGNOSTICS ===");
        }
        catch (Exception ex)
        {
            TestContext.Out.WriteLine(
                $"Failed to gather SQLite FK diagnostics: {ex.GetType().Name}: {ex.Message}");
        }
    }

    private static void DumpForeignKeyList(DbCommand failedCommand, string tableName)
    {
        if (string.IsNullOrWhiteSpace(tableName))
            return;

        try
        {
            using var fkList = failedCommand.Connection!.CreateCommand();
            fkList.Transaction = failedCommand.Transaction;
            fkList.CommandText = $"PRAGMA foreign_key_list(\"{tableName.Replace("\"", "\"\"")}\");";

            using var reader = fkList.ExecuteReader();

            TestContext.Out.WriteLine($"foreign_key_list({tableName}):");

            while (reader.Read())
            {
                // SQLite columns:
                // id, seq, table, from, to, on_update, on_delete, match
                var id = reader.GetValue(0);
                var seq = reader.GetValue(1);
                var parentTable = reader.GetValue(2);
                var fromColumn = reader.GetValue(3);
                var toColumn = reader.GetValue(4);
                var onUpdate = reader.GetValue(5);
                var onDelete = reader.GetValue(6);
                var match = reader.GetValue(7);

                TestContext.Out.WriteLine(
                    $"  Id={id}, Seq={seq}, From={fromColumn}, Parent={parentTable}.{toColumn}, OnUpdate={onUpdate}, OnDelete={onDelete}, Match={match}");
            }
        }
        catch (Exception ex)
        {
            TestContext.Out.WriteLine(
                $"Failed to dump foreign_key_list({tableName}): {ex.GetType().Name}: {ex.Message}");
        }
    }

    public override Task CommandFailedAsync(
        DbCommand command,
        CommandErrorEventData eventData,
        CancellationToken cancellationToken = default)
    {
        CommandFailed(command, eventData);
        return base.CommandFailedAsync(command, eventData, cancellationToken);
    }

    private static string Format(DbParameter p)
    {
        var val = p.Value == null || p.Value == DBNull.Value ? "<NULL>" : p.Value;
        var clr = p.Value == null || p.Value == DBNull.Value ? "null" : p.Value.GetType().Name;
        return $"{val} [{p.DbType}/{clr}]";
    }

    private static IEnumerable<string> SplitCsv(string s)
        => s.Split(',').Select(x => x.Trim()).Where(x => x.Length > 0);

    private static string Unquote(string s)
        => s.Trim().Trim('"').Trim('[', ']');

    // .NET 6 DistinctBy is available; if you’re earlier, replace with GroupBy.
}