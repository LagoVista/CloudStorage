using LagoVista.CloudStorage.RuntimeControls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;

namespace LagoVista.StorageProvider.Tests.RuntimeControls
{
    [TestClass]
    public sealed class RuntimeControlCatalogTests
    {
        [TestMethod]
        public void WebRequestTiming_IsRegisteredAsDefaultOffBoolean()
        {
            var definition = RuntimeControlCatalog.Find(RuntimeControlCatalog.WebRequestTiming);

            Assert.AreEqual("Web.Diagnostics.RequestTiming", definition.Key);
            Assert.AreEqual("false", definition.DefaultValue);
            Assert.IsTrue(definition.IsBoolean);
            Assert.AreEqual("true", RuntimeControlCatalog.NormalizeValue(definition, "TRUE"));
            Assert.AreEqual("false", RuntimeControlCatalog.NormalizeValue(definition, "FALSE"));
            Assert.AreEqual(
                "nuviot:runtime-controls:v1:web.diagnostics.requesttiming",
                RuntimeControlCatalog.StorageKey(RuntimeControlCatalog.WebRequestTiming));
        }

        [TestMethod]
        public void McpDiagnostics_IsRegisteredAsDefaultOffBoolean()
        {
            var definition = RuntimeControlCatalog.Find(RuntimeControlCatalog.McpDiagnostics);

            Assert.AreEqual("Mcp.Diagnostics", definition.Key);
            Assert.AreEqual("MCP Diagnostics", definition.Group);
            Assert.AreEqual("false", definition.DefaultValue);
            Assert.IsTrue(definition.IsBoolean);
            Assert.AreEqual(
                "nuviot:runtime-controls:v1:mcp.diagnostics",
                RuntimeControlCatalog.StorageKey(RuntimeControlCatalog.McpDiagnostics));
        }

        [TestMethod]
        public void JobManagerDiagnostics_IsRegisteredAsDefaultOffBoolean()
        {
            var definition = RuntimeControlCatalog.Find(RuntimeControlCatalog.JobManagerDiagnostics);

            Assert.AreEqual("JobManager.Diagnostics", definition.Key);
            Assert.AreEqual("Job Manager", definition.Group);
            Assert.AreEqual("false", definition.DefaultValue);
            Assert.IsTrue(definition.IsBoolean);
            Assert.AreEqual(
                "nuviot:runtime-controls:v1:jobmanager.diagnostics",
                RuntimeControlCatalog.StorageKey(RuntimeControlCatalog.JobManagerDiagnostics));
        }

        [TestMethod]
        public void NotificationSenderDiagnostics_IsRegisteredAsDefaultOffBoolean()
        {
            var definition = RuntimeControlCatalog.Find(RuntimeControlCatalog.NotificationSenderDiagnostics);

            Assert.AreEqual("Deployments.Diagnostics.NotificationSender", definition.Key);
            Assert.AreEqual("Deployments", definition.Group);
            Assert.AreEqual("false", definition.DefaultValue);
            Assert.IsTrue(definition.IsBoolean);
            Assert.IsTrue(definition.VisibleInAdmin);
            Assert.AreEqual(
                "nuviot:runtime-controls:v1:deployments.diagnostics.notificationsender",
                RuntimeControlCatalog.StorageKey(RuntimeControlCatalog.NotificationSenderDiagnostics));
        }

        [TestMethod]
        public void S3FileStorageDiagnostics_IsRegisteredAsDefaultOffBoolean()
        {
            var definition = RuntimeControlCatalog.Find(RuntimeControlCatalog.S3FileStorageDiagnostics);

            Assert.AreEqual("CloudStorage.Diagnostics.S3FileStorage", definition.Key);
            Assert.AreEqual("Cloud Storage", definition.Group);
            Assert.AreEqual("false", definition.DefaultValue);
            Assert.IsTrue(definition.IsBoolean);
            Assert.AreEqual("true", RuntimeControlCatalog.NormalizeValue(definition, "TRUE"));
            Assert.AreEqual(
                "nuviot:runtime-controls:v1:cloudstorage.diagnostics.s3filestorage",
                RuntimeControlCatalog.StorageKey(RuntimeControlCatalog.S3FileStorageDiagnostics));
        }

        [TestMethod]
        public void Catalog_ProvidesDisplayMetadataAndHidesUnwiredControls()
        {
            var visible = RuntimeControlCatalog.Definitions.Where(definition => definition.VisibleInAdmin).ToList();

            Assert.IsTrue(visible.Count > 0);
            Assert.IsTrue(visible.All(definition =>
                !String.IsNullOrWhiteSpace(definition.Key) &&
                !String.IsNullOrWhiteSpace(definition.Title) &&
                !String.IsNullOrWhiteSpace(definition.Group) &&
                !String.IsNullOrWhiteSpace(definition.Description)));
            Assert.IsTrue(visible.Any(definition => definition.Key == RuntimeControlCatalog.S3FileStorageDiagnostics));
            Assert.IsTrue(visible.Any(definition => definition.Key == RuntimeControlCatalog.McpDiagnostics));
            Assert.IsFalse(visible.Any(definition => definition.Key == RuntimeControlCatalog.SlowQueryThreshold));
            Assert.IsFalse(RuntimeControlCatalog.Find(RuntimeControlCatalog.SlowQueryThreshold).VisibleInAdmin);
        }

        [TestMethod]
        public void WebRequestTiming_RejectsInvalidBooleanValues()
        {
            var definition = RuntimeControlCatalog.Find(RuntimeControlCatalog.WebRequestTiming);

            Assert.ThrowsException<ArgumentException>(() => RuntimeControlCatalog.NormalizeValue(definition, "sometimes"));
        }
    }
}
