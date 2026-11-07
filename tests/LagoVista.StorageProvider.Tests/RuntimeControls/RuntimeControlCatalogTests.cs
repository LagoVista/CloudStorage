using LagoVista.CloudStorage.RuntimeControls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

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
        public void WebRequestTiming_RejectsInvalidBooleanValues()
        {
            var definition = RuntimeControlCatalog.Find(RuntimeControlCatalog.WebRequestTiming);

            Assert.ThrowsException<ArgumentException>(() => RuntimeControlCatalog.NormalizeValue(definition, "sometimes"));
        }
    }
}
