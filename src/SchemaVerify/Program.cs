using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SchemaVerify.Core;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration((ctx, cfg) =>
    {
        cfg.Sources.Clear();
        cfg.AddJsonFile("schema-verify.json", optional: true, reloadOnChange: false);
        cfg.AddEnvironmentVariables(prefix: "SCHEMAVERIFY_");
        cfg.AddCommandLine(args);
    })
    .ConfigureServices((ctx, services) =>
    {
        services.AddSingleton<AppOptions>(sp =>
        {
            var o = new AppOptions();
            ctx.Configuration.Bind(o);
            return o;
        });

        services.AddSingleton<ITypeMatcherFactory, TypeMatcherFactory>();
        services.AddSingleton<IEfSchemaReader, EfSchemaReader>();
        services.AddSingleton<IDbSchemaReaderFactory, DbSchemaReaderFactory>();
        services.AddSingleton<ISchemaDiffer, SchemaDiffer>();
        services.AddSingleton<IReportWriter, ReportWriter>();
        services.AddSingleton<Runner>();
    })
    .ConfigureLogging(logging =>
    {
        logging.ClearProviders();
        logging.AddSimpleConsole(o =>
        {
            o.SingleLine = true;
            o.TimestampFormat = "HH:mm:ss ";
        });
    })
    .Build();

var runner = host.Services.GetRequiredService<Runner>();
var exitCode = await runner.RunAsync();
Environment.ExitCode = exitCode;
