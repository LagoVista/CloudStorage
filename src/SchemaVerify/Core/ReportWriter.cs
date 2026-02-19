using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace SchemaVerify.Core;

public sealed class ReportWriter : IReportWriter
{
    private readonly ILogger<ReportWriter> _logger;

    public ReportWriter(ILogger<ReportWriter> logger)
    {
        _logger = logger;
    }

    public async Task WriteAsync(RunReport report, string? jsonPath, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(jsonPath)) return;

        var json = JsonConvert.SerializeObject(report, Formatting.Indented);
        var dir = Path.GetDirectoryName(jsonPath);
        if (!string.IsNullOrWhiteSpace(dir)) Directory.CreateDirectory(dir);
        await File.WriteAllTextAsync(jsonPath, json, ct);
        _logger.LogInformation("Wrote JSON report to {Path}", jsonPath);
    }

    public void WriteConsoleSummary(RunReport report, bool failOnWarnings)
    {
        _logger.LogInformation("TypeMatchMode: {Mode}", report.TypeMatchMode);
        _logger.LogInformation("Contexts checked: {Count}", report.ContextReports.Count);
        _logger.LogInformation("Total warnings: {Warnings}", report.TotalWarnings);
        _logger.LogInformation("Total errors: {Errors}", report.TotalErrors);

        foreach (var cr in report.ContextReports)
        {
            _logger.LogInformation("[{Provider}] {Context} -> {Errors} errors, {Warnings} warnings",
                cr.Provider,
                cr.ContextType,
                cr.ErrorCount,
                cr.WarningCount);

            foreach (var item in cr.Items.Where(i => i.Severity != DiffSeverity.Info))
            {
                var loc = item.Table is null
                    ? ""
                    : item.Column is null
                        ? $" ({item.Table})"
                        : $" ({item.Table}.{item.Column})";

                if (item.Severity == DiffSeverity.Error)
                {
                    _logger.LogError("{Code}: {Message}{Loc}", item.Code, item.Message, loc);
                }
                else
                {
                    _logger.LogWarning("{Code}: {Message}{Loc}", item.Code, item.Message, loc);
                }
            }
        }

        if (report.TotalErrors == 0 && (!failOnWarnings || report.TotalWarnings == 0))
        {
            _logger.LogInformation("Result: PASS");
        }
        else
        {
            _logger.LogError("Result: FAIL");
        }
    }
}
