using LagoVista.CloudStorage.Interfaces;
using LagoVista.CloudStorage.Models;
using LagoVista.CloudStorage.RuntimeControls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.StorageProvider.Tests.RuntimeControls
{
    [TestClass]
    public class SignedRequestBodyHashControlTests
    {
        [TestMethod]
        public async Task BodyHashControlIsDefaultOnAndSupportsTemporaryRollback()
        {
            var key = RuntimeControlCatalog.VerifySignedRequestBodyHash;
            var definition = RuntimeControlCatalog.Find(key);
            Assert.IsTrue(definition.VisibleInAdmin);
            Assert.IsTrue(definition.IsBoolean);
            Assert.AreEqual("true", definition.DefaultValue);

            ValkeyKeyInfo value = null;
            var repo = new Mock<IValkeyAdminRepo>();
            repo.Setup(x => x.GetAsync(0, RuntimeControlCatalog.StorageKey(key), It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => value);
            repo.Setup(x => x.SetAsync(It.IsAny<ValkeySetRequest>(), It.IsAny<CancellationToken>()))
                .Callback<ValkeySetRequest, CancellationToken>((request, _) =>
                {
                    Assert.AreEqual(RuntimeControlCatalog.StorageKey(key), request.Key);
                    value = new ValkeyKeyInfo { Key = request.Key, Value = request.Value, TtlSeconds = request.TtlSeconds };
                }).Returns(Task.CompletedTask);
            repo.Setup(x => x.DeleteAsync(0, RuntimeControlCatalog.StorageKey(key), It.IsAny<CancellationToken>()))
                .Callback(() => value = null).ReturnsAsync(true);
            var controls = new RuntimeControlService(repo.Object);

            Assert.AreEqual("true", (await controls.GetAsync(key)).EffectiveValue);
            Assert.IsTrue(controls.IsEnabled(key));
            var disabled = await controls.SetAsync(key, "false", 900);
            Assert.IsTrue(disabled.HasOverride);
            Assert.AreEqual(900L, disabled.TtlSeconds.Value);
            Assert.IsFalse(controls.IsEnabled(key));
            // Another process reads the same authoritative override.
            var replica = new RuntimeControlService(repo.Object);
            Assert.AreEqual("false", (await replica.GetAsync(key)).EffectiveValue);
            Assert.IsFalse(replica.IsEnabled(key));
            // An expired local snapshot reverts even before the next refresh.
            value.TtlSeconds = 0;
            await controls.GetAsync(key);
            Assert.IsTrue(controls.IsEnabled(key));
            await controls.ClearAsync(key);
            Assert.AreEqual("true", (await controls.GetAsync(key)).EffectiveValue);
            Assert.IsTrue(controls.IsEnabled(key));
        }

        [TestMethod]
        public void MissingOrUnavailableValkeyDoesNotDisableVerification()
        {
            var repo = new Mock<IValkeyAdminRepo>();
            repo.Setup(x => x.GetAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("unavailable"));
            var controls = new RuntimeControlService(repo.Object);
            Assert.IsTrue(controls.IsEnabled(RuntimeControlCatalog.VerifySignedRequestBodyHash));
        }
    }
}
