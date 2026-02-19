namespace SchemaVerify.Core;

public interface IReportWriter
{
    Task WriteAsync(RunReport report, string? jsonPath, CancellationToken ct = default);
    void WriteConsoleSummary(RunReport report, bool failOnWarnings);
}
