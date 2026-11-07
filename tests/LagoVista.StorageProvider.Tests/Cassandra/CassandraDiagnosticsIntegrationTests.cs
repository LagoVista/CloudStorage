using Cassandra;
using LagoVista.CloudStorage.Diagnostics;
using LagoVista.CloudStorage.Storage;
using LagoVista.CloudStorage.Storage.ConnectionSettings;
using LagoVista.CloudStorage.Storage.StorageProviders.Cassandra;
using LagoVista.IoT.Logging.Loggers;
using LagoVista.IoT.Logging.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LagoVista.StorageProvider.Tests.Cassandra
{
    [TestClass]
    [TestCategory("Integration")]
    [TestCategory("CassandraDiagnostics")]
    [DoNotParallelize]
    public class CassandraDiagnosticsIntegrationTests
    {
        [TestMethod]
        public async Task WriterAndReader_RoundTripThroughCassandraWithFortyEightHourTtl()
        {
            var settings = new CassandraDiagnosticsTestSettings();
            using var factory = new CassandraSessionFactory(settings);
            var services = new ServiceCollection();
            services.AddSingleton<ICassandraSessionFactory>(factory);
            services.AddCassandraDiagnostics("aptix-diagnostics-test", "DEV", "1.0.0");
            using var provider = services.BuildServiceProvider();
            using var scope = provider.CreateScope();
            var writer = scope.ServiceProvider.GetRequiredService<ILogWriter>();
            var reader = scope.ServiceProvider.GetRequiredService<IDiagnosticLogReader>();
            var tag = "ROUNDTRIP_" + Guid.NewGuid().ToString("N").ToUpperInvariant();
            var record = new LogRecord { LogLevel = "Error", Tag = tag, Message = "Diagnostics Cassandra roundtrip", ActivityId = "test-run" };

            await writer.WriteError(record);
            var now = DateTime.UtcNow;
            var page = await reader.QueryAsync(new DiagnosticLogQuery
            {
                Application = "aptix-diagnostics-test",
                Environment = "DEV",
                Tag = tag,
                StartUtc = now.AddMinutes(-5),
                EndUtc = now.AddMinutes(5),
                PageSize = 10
            });

            Assert.IsTrue(page.Records.Any(item => item.Id.Equals(record.Id) &&
                item.Message == record.Message && item.ActivityId == "test-run"));

            var session = await factory.GetSessionAsync();
            var rows = await session.ExecuteAsync(new SimpleStatement(
                "SELECT default_time_to_live FROM system_schema.tables WHERE keyspace_name = ? AND table_name = ?",
                settings.Keyspace, "diagnostic_log_activity_record"));
            var table = rows.FirstOrDefault();
            Assert.IsNotNull(table);
            Assert.AreEqual(172800, table.GetValue<int>("default_time_to_live"));
        }

        private sealed class CassandraDiagnosticsTestSettings : ICassandraStorageSettings
        {
            public IReadOnlyList<string> ContactPoints { get; } = new[] { "127.0.0.1" };
            public string UserName => "cassandra";
            public string Password => "cassandra";
            public string Keyspace => "nuviot_storage_tests";
            public int Port => 19042;
            public string LocalDataCenter => "datacenter1";
        }
    }
}
