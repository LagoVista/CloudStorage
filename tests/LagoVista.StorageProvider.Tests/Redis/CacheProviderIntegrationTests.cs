using LagoVista.CloudStorage;
using LagoVista.CloudStorage.Storage;
using LagoVista.Core.Interfaces;
using LagoVista.Core.Models;
using LagoVista.IoT.Logging.Loggers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Threading.Tasks;

namespace LagoVista.StorageProvider.Tests.Redis
{
    [TestClass]
    [DoNotParallelize]
    [TestCategory("Redis")]
    [TestCategory("RedisCacheProvider")]
    public class CacheProviderIntegrationTests
    {
        private static CacheProvider _cache;

        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
            _cache = new CacheProvider(new TestCacheProviderSettings(), new Mock<IAdminLogger>().Object, new Mock<IAppConfig>().Object);
        }

        [TestMethod]
        public async Task AddGetRemoveAndTtlAsync_SatisfiesCacheContract()
        {
            var prefix = NewPrefix();
            var persistentKey = $"{prefix}-persistent";
            var expiringKey = $"{prefix}-expiring";

            await _cache.AddAsync(persistentKey, "alpha", TimeSpan.FromMinutes(1));
            Assert.AreEqual("alpha", await _cache.GetAsync(persistentKey));

            await _cache.RemoveAsync(persistentKey);
            Assert.IsNull(await _cache.GetAsync(persistentKey));

            await _cache.AddAsync(expiringKey, "short-lived", TimeSpan.FromSeconds(1));
            Assert.AreEqual("short-lived", await _cache.GetAsync(expiringKey));
            await Task.Delay(TimeSpan.FromSeconds(2));
            Assert.IsNull(await _cache.GetAsync(expiringKey));
        }

        [TestMethod]
        public async Task GetAndDeleteAndIncrementAsync_AreAtomicAndPersistent()
        {
            var prefix = NewPrefix();
            var objectKey = $"{prefix}-object";
            var counterKey = $"{prefix}-counter";
            var model = new CacheContractModel { Id = "A1", Name = "Redis" };

            await _cache.AddAsync(objectKey, model, TimeSpan.FromMinutes(1));
            var removed = await _cache.GetAndDeleteAsync<CacheContractModel>(objectKey);

            Assert.IsNotNull(removed);
            Assert.AreEqual(model.Id, removed.Id);
            Assert.AreEqual(model.Name, removed.Name);
            Assert.IsNull(await _cache.GetAsync(objectKey));
            Assert.AreEqual(1L, await _cache.IncrementAsync(counterKey));
            Assert.AreEqual(2L, await _cache.IncrementAsync(counterKey));
            Assert.AreEqual(2L, await _cache.GetLongAsync(counterKey));
            await _cache.RemoveAsync(counterKey);
        }

        [TestMethod]
        public async Task DistributedLockAsync_EnforcesOwnershipAndRelease()
        {
            var key = $"{NewPrefix()}-lock";
            var ownerToken = Guid.NewGuid().ToString("N");
            var otherToken = Guid.NewGuid().ToString("N");

            Assert.IsTrue(await _cache.AttemptAcquireLockAsync(key, ownerToken, TimeSpan.FromSeconds(10)));
            Assert.IsFalse(await _cache.AttemptAcquireLockAsync(key, otherToken, TimeSpan.FromSeconds(10)));
            Assert.IsTrue(await _cache.ExtendLockAsync(key, ownerToken, TimeSpan.FromSeconds(10)));
            Assert.IsFalse(await _cache.ReleaseLockAsync(key, otherToken));
            Assert.IsTrue(await _cache.ReleaseLockAsync(key, ownerToken));
            Assert.IsTrue(await _cache.AttemptAcquireLockAsync(key, otherToken, TimeSpan.FromSeconds(10)));
            Assert.IsTrue(await _cache.ReleaseLockAsync(key, otherToken));
        }

        [TestMethod]
        public async Task DependenciesAndCollectionsAsync_InvalidateAndRoundTrip()
        {
            var prefix = NewPrefix();
            var dependencyKey = $"{prefix}-dependency";
            var dependentKey = $"{prefix}-dependent";
            var collectionKey = $"{prefix}-collection";

            await _cache.AddAsync(dependencyKey, "v1", TimeSpan.FromMinutes(1));
            await _cache.AddAsync(dependentKey, "derived", new[] { dependencyKey }, TimeSpan.FromMinutes(1));
            Assert.AreEqual("derived", await _cache.GetAsync(dependentKey));

            await _cache.AddAsync(dependencyKey, "v2", TimeSpan.FromMinutes(1));
            Assert.IsNull(await _cache.GetAsync(dependentKey));
            Assert.AreEqual("v2", await _cache.GetAsync(dependencyKey));

            await _cache.AddToCollectionAsync(collectionKey, "one", "first");
            await _cache.AddToCollectionAsync(collectionKey, "two", "second");
            Assert.AreEqual("first", await _cache.GetFromCollection(collectionKey, "one"));
            Assert.AreEqual("second", await _cache.GetFromCollection(collectionKey, "two"));
            await _cache.RemoveFromCollectionAsync(collectionKey, "one");
            Assert.IsNull(await _cache.GetFromCollection(collectionKey, "one"));
            Assert.AreEqual("second", await _cache.GetFromCollection(collectionKey, "two"));

            await _cache.RemoveAsync(dependencyKey);
            await _cache.RemoveFromCollectionAsync(collectionKey, "two");
        }

        private static string NewPrefix() => $"cloudstorage-redis-contract-{Guid.NewGuid():N}";

        private sealed class TestCacheProviderSettings : ICacheProviderSettings
        {
            public bool UseCache => true;
            public bool UseAuthentication => false;
            public IConnectionSettings CacheSettings { get; } = new ConnectionSettings { Uri = "127.0.0.1:19079,abortConnect=false" };
            public string Password => String.Empty;
        }

        public sealed class CacheContractModel
        {
            public string Id { get; set; }
            public string Name { get; set; }
        }
    }
}
