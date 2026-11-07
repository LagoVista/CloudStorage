using LagoVista.CloudStorage.RuntimeControls;
using LagoVista.CloudStorage.Storage;
using LagoVista.IoT.Logging.Loggers;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace LagoVista.CloudStorage.Diagnostics
{
    /// <summary>
    /// Explicit opt-in registration. Existing CreateLogZWriter bootstrap callers are
    /// unchanged until an application elects to use this sink.
    /// </summary>
    public static class CassandraDiagnosticsRegistration
    {
        public static IServiceCollection AddCassandraDiagnostics(
            this IServiceCollection services, string application, string environment, string version = null, bool writeConsole = true)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));
            if (String.IsNullOrWhiteSpace(application)) throw new ArgumentNullException(nameof(application));
            if (String.IsNullOrWhiteSpace(environment)) throw new ArgumentNullException(nameof(environment));

            services.AddActivityRecordStore<DiagnosticLogActivityRecord>(definition => definition
                .PartitionBy(record => record.Application)
                .PartitionBy(record => record.Environment)
                .BucketBy(StoragePeriod.Month)
                .Index(record => record.LogLevel)
                .Index(record => record.Tag)
                .RetainFor(TimeSpan.FromHours(48)));

            services.AddScoped<IDiagnosticLogReader, CassandraDiagnosticLogReader>();
            services.AddScoped<CassandraLogWriter>(provider => new CassandraLogWriter(
                provider.GetRequiredService<IActivityRecordStore<DiagnosticLogActivityRecord>>(),
                provider.GetService<IRuntimeControlService>(),
                application, environment, version, writeConsole));
            services.AddScoped<ILogWriter>(provider => provider.GetRequiredService<CassandraLogWriter>());

            return services;
        }
    }
}
