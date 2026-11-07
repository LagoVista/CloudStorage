using LagoVista.CloudStorage.Diagnostics;
using LagoVista.CloudStorage.RuntimeControls;
using LagoVista.CloudStorage.Storage;
using LagoVista.IoT.Logging.Loggers;
using LagoVista.IoT.Logging.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.StorageProvider.Tests.Diagnostics
{
    [TestClass]
    public class CassandraDiagnosticsTests
    {
        [TestMethod]
        public void Registration_DeclaresFortyEightHourRetentionAndApplicationEnvironmentPartitions()
        {
            var services = new ServiceCollection();
            services.AddCassandraDiagnostics("api-host", "DEV");
            using var provider = services.BuildServiceProvider();
            var options = provider.GetRequiredService<ActivityRecordStoreOptions<DiagnosticLogActivityRecord>>();
            Assert.AreEqual(TimeSpan.FromHours(48), options.Definition.Retention);
            CollectionAssert.AreEquivalent(
                new[] { nameof(DiagnosticLogActivityRecord.Application), nameof(DiagnosticLogActivityRecord.Environment) },
                options.Definition.PartitionFields.ToArray());
            Assert.AreEqual("false", RuntimeControlCatalog.Find(RuntimeControlCatalog.LoggingVerbose).DefaultValue);
        }

        [TestMethod]
        public void RecordMapping_RetainsIdentifiersAndStructuredParameters()
        {
            var source = new LogRecord
            {
                Application = "api-host",
                Environment = "DEV",
                LogLevel = "Error",
                Tag = "ARTIFACT_CREATE",
                Message = "Create failed",
                ActivityId = "123",
                ExceptionType = "InvalidOperationException",
                HostId = "host-a",
                StackTrace = "sample stack",
                Parameters = new Dictionary<string, string> { ["correlationId"] = "abc-123" }
            };
            var stored = DiagnosticLogActivityRecord.FromLogRecord(source, "fallback", "fallback", "1.2.3");
            var roundTrip = stored.ToLogRecord();

            Assert.AreEqual(source.Id.ToString(), roundTrip.Id.ToString());
            Assert.AreEqual(source.TimeStamp, roundTrip.TimeStamp);
            Assert.AreEqual("api-host", roundTrip.Application);
            Assert.AreEqual("DEV", roundTrip.Environment);
            Assert.AreEqual("ARTIFACT_CREATE", roundTrip.Tag);
            Assert.AreEqual("abc-123", roundTrip.Parameters["correlationId"]);
            Assert.AreEqual("123", roundTrip.ActivityId);
            Assert.AreEqual("InvalidOperationException", roundTrip.ExceptionType);
            Assert.AreEqual("sample stack", roundTrip.StackTrace);
            Assert.AreEqual("1.2.3", roundTrip.Version);
        }

        [TestMethod]
        public async Task Writer_PersistsErrorsButGatesVerboseEvents()
        {
            var store = new Mock<IActivityRecordStore<DiagnosticLogActivityRecord>>();
            store.Setup(x => x.InsertAsync(It.IsAny<DiagnosticLogActivityRecord>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            var controls = new Mock<IRuntimeControlService>();
            controls.Setup(x => x.IsEnabled(RuntimeControlCatalog.LoggingVerbose)).Returns(false);
            var writer = new CassandraLogWriter(store.Object, controls.Object, "api-host", "DEV", "9.1.0");

            await writer.WriteEvent(new LogRecord { LogLevel = "Trace", Message = "temporary trace" });
            store.Verify(x => x.InsertAsync(It.IsAny<DiagnosticLogActivityRecord>(), It.IsAny<CancellationToken>()), Times.Never);

            await writer.WriteError(new LogRecord { LogLevel = "Error", Tag = "CRASH", Message = "failure" });
            store.Verify(x => x.InsertAsync(It.Is<DiagnosticLogActivityRecord>(r =>
                r.LogLevel == "Error" && r.Tag == "CRASH" && r.Application == "api-host"),
                It.IsAny<CancellationToken>()), Times.Once);

            controls.Setup(x => x.IsEnabled(RuntimeControlCatalog.LoggingVerbose)).Returns(true);
            await writer.WriteEvent(new LogRecord { LogLevel = "Trace", Message = "temporary trace" });
            store.Verify(x => x.InsertAsync(It.Is<DiagnosticLogActivityRecord>(r => r.LogLevel == "Trace"),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [TestMethod]
        public async Task Reader_RejectsMissingScopeAndUnboundedQueries()
        {
            var store = new Mock<IActivityRecordStore<DiagnosticLogActivityRecord>>();
            var reader = new CassandraDiagnosticLogReader(store.Object);
            var now = DateTime.UtcNow;

            await Assert.ThrowsExceptionAsync<ArgumentException>(async () =>
                await reader.QueryAsync(new DiagnosticLogQuery
                {
                    Application = "api-host",
                    StartUtc = now.AddMinutes(-5),
                    EndUtc = now
                }));

            await Assert.ThrowsExceptionAsync<ArgumentException>(async () =>
                await reader.QueryAsync(new DiagnosticLogQuery
                {
                    Application = "api-host",
                    Environment = "DEV",
                    StartUtc = now.AddHours(-72),
                    EndUtc = now
                }));

            store.Verify(x => x.QueryAsync(It.IsAny<HistoryQuery<DiagnosticLogActivityRecord>>(),
                It.IsAny<CancellationToken>()), Times.Never);
        }
    }
}
