namespace SchemaVerify.Core;

public enum DiffSeverity
{
    Info,
    Warning,
    Error
}

public sealed class DiffItem
{
    public required DiffSeverity Severity { get; init; }
    public required string Code { get; init; }
    public required string Message { get; init; }

    public string? ContextType { get; init; }
    public string? Provider { get; init; }
    public string? Table { get; init; }
    public string? Column { get; init; }
}

public sealed class DiffReport
{
    public required string ContextType { get; init; }
    public required string Provider { get; init; }
    public required DateTimeOffset GeneratedAtUtc { get; init; }

    public List<DiffItem> Items { get; } = new();

    public int ErrorCount => Items.Count(i => i.Severity == DiffSeverity.Error);
    public int WarningCount => Items.Count(i => i.Severity == DiffSeverity.Warning);
}

public sealed class RunReport
{
    public required DateTimeOffset GeneratedAtUtc { get; init; }
    public required TypeMatchMode TypeMatchMode { get; init; }
    public List<DiffReport> ContextReports { get; } = new();

    public int TotalErrors => ContextReports.Sum(r => r.ErrorCount);
    public int TotalWarnings => ContextReports.Sum(r => r.WarningCount);
}
