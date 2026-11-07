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

        [DataTestMethod]
        [DataRow("Verbose")]
        [DataRow("Trace")]
        public async Task Writer_PersistsJobManagerDiagnosticsWithoutGlobalVerbose(string level)
        {
            var store = new Mock<IActivityRecordStore<DiagnosticLogActivityRecord>>();
            store.Setup(x => x.InsertAsync(It.IsAny<DiagnosticLogActivityRecord>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            var controls = new Mock<IRuntimeControlService>();
            controls.Setup(x => x.IsEnabled(RuntimeControlCatalog.JobManagerDiagnostics)).Returns(true);
            controls.Setup(x => x.IsEnabled(RuntimeControlCatalog.LoggingVerbose)).Returns(false);
            var writer = new CassandraLogWriter(store.Object, controls.Object, "api-host", "DEV", writeConsole: false);

            await writer.WriteEvent(new LogRecord
            {
                LogLevel = level,
                Tag = RuntimeControlCatalog.JobManagerDiagnostics,
                Message = "Job invocation claimed"
            });
            await writer.WriteEvent(new LogRecord { LogLevel = level, Tag = "Other.Diagnostics", Message = "Other trace" });

            store.Verify(x => x.InsertAsync(It.Is<DiagnosticLogActivityRecord>(r =>
                r.Tag == RuntimeControlCatalog.JobManagerDiagnostics && r.LogLevel == level),
                It.IsAny<CancellationToken>()), Times.Once);
            store.Verify(x => x.InsertAsync(It.IsAny<DiagnosticLogActivityRecord>(), It.IsAny<CancellationToken>()), Times.Once);
            controls.Verify(x => x.IsEnabled(RuntimeControlCatalog.LoggingVerbose), Times.Once);
        }

        [TestMethod]
        public async Task Writer_JobManagerDiagnosticsStayOffWhenGlobalVerboseIsOn()
        {
            var store = new Mock<IActivityRecordStore<DiagnosticLogActivityRecord>>();
            store.Setup(x => x.InsertAsync(It.IsAny<DiagnosticLogActivityRecord>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            var controls = new Mock<IRuntimeControlService>();
            controls.Setup(x => x.IsEnabled(RuntimeControlCatalog.JobManagerDiagnostics)).Returns(false);
            controls.Setup(x => x.IsEnabled(RuntimeControlCatalog.LoggingVerbose)).Returns(true);
            var writer = new CassandraLogWriter(store.Object, controls.Object, "api-host", "DEV", writeConsole: false);

            await writer.WriteEvent(new LogRecord
            {
                LogLevel = "Verbose",
                Tag = RuntimeControlCatalog.JobManagerDiagnostics,
                Message = "Job invocation claimed"
            });
            await writer.WriteEvent(new LogRecord { LogLevel = "Trace", Tag = "Other.Diagnostics", Message = "Other trace" });
            await writer.WriteError(new LogRecord { LogLevel = "Error", Tag = "JobManager", Message = "Job failed" });

            store.Verify(x => x.InsertAsync(It.Is<DiagnosticLogActivityRecord>(r =>
                r.Tag == RuntimeControlCatalog.JobManagerDiagnostics), It.IsAny<CancellationToken>()), Times.Never);
            store.Verify(x => x.InsertAsync(It.Is<DiagnosticLogActivityRecord>(r =>
                r.Tag == "Other.Diagnostics" && r.LogLevel == "Trace"), It.IsAny<CancellationToken>()), Times.Once);
            store.Verify(x => x.InsertAsync(It.Is<DiagnosticLogActivityRecord>(r =>
                r.Tag == "JobManager" && r.LogLevel == "Error"), It.IsAny<CancellationToken>()), Times.Once);
        }

        [TestMethod]
        public async Task Writer_S3DiagnosticsUseOnlyTheirScopedFlag_AndPreserveWarningsAndErrors()
        {
            var store = new Mock<IActivityRecordStore<DiagnosticLogActivityRecord>>();
            store.Setup(x => x.InsertAsync(It.IsAny<DiagnosticLogActivityRecord>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            var enabled = false;
            var controls = new Mock<IRuntimeControlService>();
            controls.Setup(x => x.IsEnabled(RuntimeControlCatalog.S3FileStorageDiagnostics)).Returns(() => enabled);
            controls.Setup(x => x.IsEnabled(RuntimeControlCatalog.LoggingVerbose)).Returns(true);
            var writer = new CassandraLogWriter(store.Object, controls.Object, "api-host", "DEV", writeConsole: false);

            await writer.WriteEvent(new LogRecord
            {
                LogLevel = "Verbose", Tag = RuntimeControlCatalog.S3FileStorageDiagnostics, Message = "S3 object read"
            });
            store.Verify(x => x.InsertAsync(It.IsAny<DiagnosticLogActivityRecord>(), It.IsAny<CancellationToken>()), Times.Never);

            enabled = true;
            await writer.WriteEvent(new LogRecord
            {
                LogLevel = "Verbose", Tag = RuntimeControlCatalog.S3FileStorageDiagnostics, Message = "S3 object read"
            });
            enabled = false;
            await writer.WriteEvent(new LogRecord
            {
                LogLevel = "Verbose", Tag = RuntimeControlCatalog.S3FileStorageDiagnostics, Message = "S3 object read"
            });

            store.Verify(x => x.InsertAsync(It.Is<DiagnosticLogActivityRecord>(record =>
                record.Tag == RuntimeControlCatalog.S3FileStorageDiagnostics && record.LogLevel == "Verbose"),
                It.IsAny<CancellationToken>()), Times.Once);
            controls.Verify(x => x.IsEnabled(RuntimeControlCatalog.LoggingVerbose), Times.Never);

            await writer.WriteEvent(new LogRecord
            {
                LogLevel = "Warning", Tag = RuntimeControlCatalog.S3FileStorageDiagnostics, Message = "S3 retry"
            });
            await writer.WriteError(new LogRecord
            {
                LogLevel = "Error", Tag = RuntimeControlCatalog.S3FileStorageDiagnostics, Message = "S3 failure"
            });
            store.Verify(x => x.InsertAsync(It.Is<DiagnosticLogActivityRecord>(record => record.LogLevel == "Warning"),
                It.IsAny<CancellationToken>()), Times.Once);
            store.Verify(x => x.InsertAsync(It.Is<DiagnosticLogActivityRecord>(record => record.LogLevel == "Error"),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [TestMethod]
        public async Task Writer_NotificationSenderDiagnosticsUseOnlyTheirScopedFlag_AndPreserveWarningsAndErrors()
        {
            var store = new Mock<IActivityRecordStore<DiagnosticLogActivityRecord>>();
            store.Setup(x => x.InsertAsync(It.IsAny<DiagnosticLogActivityRecord>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            var enabled = false;
            var controls = new Mock<IRuntimeControlService>();
            controls.Setup(x => x.IsEnabled(RuntimeControlCatalog.NotificationSenderDiagnostics)).Returns(() => enabled);
            controls.Setup(x => x.IsEnabled(RuntimeControlCatalog.LoggingVerbose)).Returns(true);
            var writer = new CassandraLogWriter(store.Object, controls.Object, "api-host", "DEV", writeConsole: false);

            await writer.WriteEvent(new LogRecord
            {
                LogLevel = "Verbose", Tag = RuntimeControlCatalog.NotificationSenderDiagnostics, Message = "Recipient routing"
            });
            store.Verify(x => x.InsertAsync(It.IsAny<DiagnosticLogActivityRecord>(), It.IsAny<CancellationToken>()), Times.Never);

            enabled = true;
            await writer.WriteEvent(new LogRecord
            {
                LogLevel = "Verbose", Tag = RuntimeControlCatalog.NotificationSenderDiagnostics, Message = "Recipient routing"
            });
            enabled = false;
            await writer.WriteEvent(new LogRecord
            {
                LogLevel = "Verbose", Tag = RuntimeControlCatalog.NotificationSenderDiagnostics, Message = "Recipient routing"
            });

            store.Verify(x => x.InsertAsync(It.Is<DiagnosticLogActivityRecord>(record =>
                record.Tag == RuntimeControlCatalog.NotificationSenderDiagnostics && record.LogLevel == "Verbose"),
                It.IsAny<CancellationToken>()), Times.Once);
            controls.Verify(x => x.IsEnabled(RuntimeControlCatalog.LoggingVerbose), Times.Never);

            await writer.WriteEvent(new LogRecord
            {
                LogLevel = "Warning", Tag = RuntimeControlCatalog.NotificationSenderDiagnostics, Message = "Missing recipient"
            });
            await writer.WriteError(new LogRecord
            {
                LogLevel = "Error", Tag = RuntimeControlCatalog.NotificationSenderDiagnostics, Message = "Delivery failed"
            });
            store.Verify(x => x.InsertAsync(It.Is<DiagnosticLogActivityRecord>(record => record.LogLevel == "Warning"),
                It.IsAny<CancellationToken>()), Times.Once);
            store.Verify(x => x.InsertAsync(It.Is<DiagnosticLogActivityRecord>(record => record.LogLevel == "Error"),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [TestMethod]
        public async Task Writer_AgentPipelineDiagnosticsUseOnlyTheirScopedFlag_AndPreserveWarningsAndErrors()
        {
            var store = new Mock<IActivityRecordStore<DiagnosticLogActivityRecord>>();
            store.Setup(x => x.InsertAsync(It.IsAny<DiagnosticLogActivityRecord>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            var enabled = false;
            var controls = new Mock<IRuntimeControlService>();
            controls.Setup(x => x.IsEnabled(RuntimeControlCatalog.AgentPipelineDiagnostics)).Returns(() => enabled);
            controls.Setup(x => x.IsEnabled(RuntimeControlCatalog.LoggingVerbose)).Returns(true);
            var writer = new CassandraLogWriter(store.Object, controls.Object, "api-host", "DEV", writeConsole: false);

            await writer.WriteEvent(new LogRecord { LogLevel = "Verbose", Tag = RuntimeControlCatalog.AgentPipelineDiagnostics, Message = "Started" });
            store.Verify(x => x.InsertAsync(It.IsAny<DiagnosticLogActivityRecord>(), It.IsAny<CancellationToken>()), Times.Never);

            enabled = true;
            await writer.WriteEvent(new LogRecord { LogLevel = "Verbose", Tag = RuntimeControlCatalog.AgentPipelineDiagnostics, Message = "Started" });
            enabled = false;
            await writer.WriteEvent(new LogRecord { LogLevel = "Verbose", Tag = RuntimeControlCatalog.AgentPipelineDiagnostics, Message = "Started" });

            store.Verify(x => x.InsertAsync(It.Is<DiagnosticLogActivityRecord>(r =>
                r.Tag == RuntimeControlCatalog.AgentPipelineDiagnostics && r.LogLevel == "Verbose"),
                It.IsAny<CancellationToken>()), Times.Once);
            controls.Verify(x => x.IsEnabled(RuntimeControlCatalog.LoggingVerbose), Times.Never);

            await writer.WriteEvent(new LogRecord { LogLevel = "Warning", Tag = RuntimeControlCatalog.AgentPipelineDiagnostics, Message = "Retry" });
            await writer.WriteError(new LogRecord { LogLevel = "Error", Tag = RuntimeControlCatalog.AgentPipelineDiagnostics, Message = "Failure" });
            store.Verify(x => x.InsertAsync(It.Is<DiagnosticLogActivityRecord>(r => r.LogLevel == "Warning"), It.IsAny<CancellationToken>()), Times.Once);
            store.Verify(x => x.InsertAsync(It.Is<DiagnosticLogActivityRecord>(r => r.LogLevel == "Error"), It.IsAny<CancellationToken>()), Times.Once);
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
