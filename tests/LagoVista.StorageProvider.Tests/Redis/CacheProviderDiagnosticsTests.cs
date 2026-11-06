using LagoVista.CloudStorage;
using LagoVista.CloudStorage.RuntimeControls;
using LagoVista.CloudStorage.Storage;
using LagoVista.Core.Interfaces;
using LagoVista.IoT.Logging.Loggers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace LagoVista.StorageProvider.Tests.Redis
{
    [TestClass]
    [DoNotParallelize]
    public sealed class CacheProviderDiagnosticsTests
    {
        [TestMethod]
        public async Task DefaultMode_DoesNotLogRoutineCacheMiss()
        {
            var logger = new Mock<IAdminLogger>();
            var provider = new CacheProvider(new DisabledSettings(), logger.Object, null);

            Assert.IsNull(await provider.GetAndDeleteAsync<string>(Guid.NewGuid().ToString("N")));
            Assert.AreEqual(0, logger.Invocations.Count(call => call.Method.Name == "Trace"));
        }

        [TestMethod]
        public async Task EnabledMode_LogsRoutineCacheMiss()
        {
            var logger = new Mock<IAdminLogger>();
            var controls = new Mock<IRuntimeControlService>();
            controls.Setup(control => control.IsEnabled(RuntimeControlCatalog.CacheProviderDiagnostics))
                .Returns(true);
            var provider = new CacheProvider(new DisabledSettings(), logger.Object, null, controls.Object);

            Assert.IsNull(await provider.GetAndDeleteAsync<string>(Guid.NewGuid().ToString("N")));
            Assert.AreEqual(1, logger.Invocations.Count(call => call.Method.Name == "Trace"));
        }

        private sealed class DisabledSettings : ICacheProviderSettings
        {
            public bool UseCache => false;
            public bool UseAuthentication => false;
            public IConnectionSettings CacheSettings => null;
            public string Password => String.Empty;
        }
    }
}
