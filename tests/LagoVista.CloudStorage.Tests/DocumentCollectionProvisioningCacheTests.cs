using LagoVista.CloudStorage.DocumentDB;
using NUnit.Framework;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.CloudStorage.Tests
{
    public class DocumentCollectionProvisioningCacheTests
    {
        [Test]
        public async Task EnsureAsync_RepeatedCalls_RunEnsureOnce()
        {
            var cache = new DocumentCollectionProvisioningCache();
            var calls = 0;

            await cache.EnsureAsync("collection", () =>
            {
                Interlocked.Increment(ref calls);
                return Task.CompletedTask;
            });

            await cache.EnsureAsync("collection", () =>
            {
                Interlocked.Increment(ref calls);
                return Task.CompletedTask;
            });

            Assert.That(calls, Is.EqualTo(1));
            Assert.That(cache.IsVerified("collection"), Is.True);
        }

        [Test]
        public async Task EnsureAsync_ConcurrentCalls_ShareOneEnsure()
        {
            var cache = new DocumentCollectionProvisioningCache();
            var calls = 0;
            var release = new TaskCompletionSource<bool>();

            var tasks = Enumerable.Range(0, 10)
                .Select(_ => cache.EnsureAsync("collection", async () =>
                {
                    Interlocked.Increment(ref calls);
                    await release.Task.ConfigureAwait(false);
                }))
                .ToArray();

            await Task.Delay(25);
            release.SetResult(true);
            await Task.WhenAll(tasks);

            Assert.That(calls, Is.EqualTo(1));
        }

        [Test]
        public void EnsureAsync_Failure_IsNotCached()
        {
            var cache = new DocumentCollectionProvisioningCache();
            var calls = 0;

            Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await cache.EnsureAsync("collection", () =>
                {
                    Interlocked.Increment(ref calls);
                    throw new InvalidOperationException("first attempt failed");
                }));

            Assert.That(cache.IsVerified("collection"), Is.False);

            Assert.DoesNotThrowAsync(async () =>
                await cache.EnsureAsync("collection", () =>
                {
                    Interlocked.Increment(ref calls);
                    return Task.CompletedTask;
                }));

            Assert.That(calls, Is.EqualTo(2));
            Assert.That(cache.IsVerified("collection"), Is.True);
        }
    }
}
