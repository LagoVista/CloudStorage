using System.Reflection;
using Microsoft.Extensions.Logging;

namespace SchemaVerify.Core;

public sealed class Runner
{
    private readonly AppOptions _options;
    private readonly ILogger<Runner> _logger;
    private readonly IEfSchemaReader _efReader;
    private readonly IDbSchemaReaderFactory _dbFactory;
    private readonly ITypeMatcherFactory _typeMatcherFactory;
    private readonly ISchemaDiffer _differ;
    private readonly IReportWriter _reporter;

    public Runner(
        AppOptions options,
        ILogger<Runner> logger,
        IEfSchemaReader efReader,
        IDbSchemaReaderFactory dbFactory,
        ITypeMatcherFactory typeMatcherFactory,
        ISchemaDiffer differ,
        IReportWriter reporter)
    {
        _options = options;
        _logger = logger;
        _efReader = efReader;
        _dbFactory = dbFactory;
        _typeMatcherFactory = typeMatcherFactory;
        _differ = differ;
        _reporter = reporter;
    }

    public async Task<int> RunAsync(CancellationToken ct = default)
    {
        var mode = ParseMode(_options.TypeMatchMode);
        var assemblies = LoadAssemblies(_options.AssemblyPaths);

        if (_options.Contexts.Count == 0)
        {
            _logger.LogError("No contexts configured. Create schema-verify.json (or start from schema-verify.sample.json)."
                + " Set Contexts[] with ContextType and Targets[].");
            return 2;
        }

        var runReport = new RunReport
        {
            GeneratedAtUtc = DateTimeOffset.UtcNow,
            TypeMatchMode = mode
        };

        foreach (var ctxSpec in _options.Contexts)
        {
            if (ctxSpec.Targets.Count == 0)
            {
                _logger.LogWarning("Context {ContextType} has no targets; skipping.", ctxSpec.ContextType);
                continue;
            }

            foreach (var target in ctxSpec.Targets)
            {
                var provider = (target.Provider ?? string.Empty).Trim().ToLowerInvariant();
                _logger.LogInformation("Checking {Context} against {Provider}...", ctxSpec.ContextType, provider);

                using var dbContext = DbContextFactoryResolver.CreateDbContext(
                    ctxSpec.ContextType,
                    provider,
                    target.ConnectionString,
                    assemblies);

                var efSchema = _efReader.Read(dbContext);

                await using var conn = _dbFactory.CreateConnection(provider, target.ConnectionString);
                await conn.OpenAsync(ct);
                var dbReader = _dbFactory.Create(provider);
                var dbSchema = await dbReader.ReadAsync(conn, ct);

                var matcher = _typeMatcherFactory.Create(mode, provider);
                var diff = _differ.Diff(ctxSpec.ContextType, provider, efSchema, dbSchema, matcher);
                runReport.ContextReports.Add(diff);
            }
        }

        _reporter.WriteConsoleSummary(runReport, _options.FailOnWarnings);
        await _reporter.WriteAsync(runReport, _options.Output.JsonPath, ct);

        var shouldFail = runReport.TotalErrors > 0 || (_options.FailOnWarnings && runReport.TotalWarnings > 0);
        return shouldFail ? 1 : 0;
    }

    private static TypeMatchMode ParseMode(string? s)
        => Enum.TryParse<TypeMatchMode>((s ?? string.Empty).Trim(), ignoreCase: true, out var mode)
            ? mode
            : TypeMatchMode.Family;

    private static List<Assembly> LoadAssemblies(List<string> assemblyPaths)
    {
        var assemblies = new List<Assembly>();

        // Always include already-loaded assemblies (SchemaVerify itself, plus whatever the app already loaded)
        assemblies.AddRange(AppDomain.CurrentDomain.GetAssemblies().Where(a => !a.IsDynamic));

        foreach (var path in assemblyPaths)
        {
            var p = path?.Trim();
            if (string.IsNullOrWhiteSpace(p)) continue;
            if (!File.Exists(p)) continue;

            var asm = Assembly.LoadFrom(p);
            assemblies.Add(asm);
        }

        // De-dupe
        return assemblies
            .GroupBy(a => a.FullName)
            .Select(g => g.First())
            .ToList();
    }
}
