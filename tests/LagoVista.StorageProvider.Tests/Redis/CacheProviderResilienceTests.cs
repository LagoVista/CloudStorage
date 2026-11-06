using LagoVista.CloudStorage;
using LagoVista.CloudStorage.Storage;
using LagoVista.Core.Interfaces;
using LagoVista.Core.Models;
using LagoVista.IoT.Logging.Loggers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using StackExchange.Redis;
using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace LagoVista.StorageProvider.Tests.Redis
{
    [TestClass]
    [DoNotParallelize]
    public sealed class CacheProviderResilienceTests
    {
        [TestMethod]
        public async Task UnavailableRedis_DoesNotPreventConstruction_AndOptionalReadsReturnMisses()
        {
            using var portReservation = new TcpListener(IPAddress.Loopback, 0);
            portReservation.Start();
            var port = ((IPEndPoint)portReservation.LocalEndpoint).Port;
            portReservation.Stop();

            var logger = new Mock<IAdminLogger>();
            using var cache = new CacheProvider(
                new EnabledSettings($"127.0.0.1:{port},connectTimeout=300,connectRetry=0,syncTimeout=400"),
                logger.Object, new Mock<IAppConfig>().Object);

            Assert.IsNull(await cache.GetAsync("missing-key"));
            Assert.IsNull(await cache.GetAsync<string>("missing-key"));
            var multiple = await cache.GetManyAsync(new[] { "missing-one", "missing-two" });
            Assert.AreEqual(2, multiple.Count);
            Assert.IsTrue(multiple.Values.All(value => value == null));
            Assert.IsFalse((await cache.GetCollection("missing-collection")).Any());
            Assert.IsNull(await cache.GetFromCollection("missing-collection", "missing-key"));

            // A failed optional read is reported once, without a log flood per lookup.
            Assert.AreEqual(1, logger.Invocations.Count(call => call.Method.Name == "AddError"));

            // Counters are coordination state, not optional cache hints: do not invent success.
            Exception counterFailure = null;
            try { await cache.IncrementAsync("counter"); }
            catch (Exception ex) { counterFailure = ex; }
            Assert.IsTrue(counterFailure is RedisConnectionException || counterFailure is RedisTimeoutException,
                $"Expected Redis transport failure, got {counterFailure?.GetType().Name ?? "no failure"}.");
        }

        private sealed class EnabledSettings : ICacheProviderSettings
        {
            public EnabledSettings(string uri) => CacheSettings = new ConnectionSettings { Uri = uri };
            public bool UseCache => true;
            public bool UseAuthentication => false;
            public IConnectionSettings CacheSettings { get; }
            public string Password => String.Empty;
        }
    }
}
