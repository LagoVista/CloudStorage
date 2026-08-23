using LagoVista.CloudStorage.Storage;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.CloudStorage.Tests
{
    public class StorageContractTests
    {
        private class TestEntity
        {
            public string Id { get; set; }
            public string OrganizationId { get; set; }
            public string Category { get; set; }
            public DateTime Timestamp { get; set; }
        }

        private class FakeAppendStore : IAppendHistoryStore<TestEntity>
        {
            public Task InsertAsync(TestEntity entity, CancellationToken cancellationToken = default) => Task.CompletedTask;
            public Task InsertBatchAsync(IEnumerable<TestEntity> entities, CancellationToken cancellationToken = default) => Task.CompletedTask;
            public Task<StoragePageResult<TestEntity>> QueryAsync(HistoryQuery<TestEntity> query, CancellationToken cancellationToken = default) =>
                Task.FromResult(new StoragePageResult<TestEntity>(Array.Empty<TestEntity>()));
        }

        private class FakeScratchStore : IScratchStore<TestEntity>
        {
            public Task<TestEntity> GetAsync(StorageKey key, CancellationToken cancellationToken = default) => Task.FromResult<TestEntity>(null);
            public Task UpsertAsync(TestEntity entity, CancellationToken cancellationToken = default) => Task.CompletedTask;
            public Task DeleteAsync(StorageKey key, CancellationToken cancellationToken = default) => Task.CompletedTask;
            public Task<StoragePageResult<TestEntity>> QueryAsync(StorageQuery<TestEntity> query, CancellationToken cancellationToken = default) =>
                Task.FromResult(new StoragePageResult<TestEntity>(Array.Empty<TestEntity>()));
        }

        private class FakeFlatDocumentStore : IFlatDocumentStore<TestEntity>
        {
            public Task<TestEntity> GetAsync(StorageKey key, CancellationToken cancellationToken = default) => Task.FromResult<TestEntity>(null);
            public Task InsertAsync(TestEntity entity, CancellationToken cancellationToken = default) => Task.CompletedTask;
            public Task UpdateAsync(TestEntity entity, CancellationToken cancellationToken = default) => Task.CompletedTask;
            public Task DeleteAsync(StorageKey key, CancellationToken cancellationToken = default) => Task.CompletedTask;
            public Task<StoragePageResult<TestEntity>> QueryAsync(StorageQuery<TestEntity> query, CancellationToken cancellationToken = default) =>
                Task.FromResult(new StoragePageResult<TestEntity>(Array.Empty<TestEntity>()));
        }

        [Test]
        public void AddAppendHistoryStore_ResolvesCapabilityAndTypedOptions()
        {
            var services = new ServiceCollection();
            services.AddAppendHistoryStore<TestEntity, FakeAppendStore>(storage => storage
                .PartitionBy(x => x.OrganizationId)
                .TimeBy(x => x.Timestamp)
                .BucketBy(StoragePeriod.Month)
                .Index(x => x.Category));

            using (var provider = services.BuildServiceProvider())
            using (var scope = provider.CreateScope())
            {
                Assert.That(scope.ServiceProvider.GetRequiredService<IAppendHistoryStore<TestEntity>>(), Is.TypeOf<FakeAppendStore>());

                var options = scope.ServiceProvider.GetRequiredService<AppendHistoryStoreOptions<TestEntity>>();
                Assert.That(options.Definition.TimeField, Is.EqualTo(nameof(TestEntity.Timestamp)));
                Assert.That(options.Definition.PartitionFields, Does.Contain(nameof(TestEntity.OrganizationId)));
                Assert.That(options.Definition.IndexedFields, Does.Contain(nameof(TestEntity.Category)));
                Assert.That(options.Definition.BucketPeriod, Is.EqualTo(StoragePeriod.Month));
            }
        }

        [Test]
        public void AddAppendHistoryStore_WithoutTimeField_FailsFast()
        {
            var services = new ServiceCollection();

            var ex = Assert.Throws<InvalidOperationException>(() =>
                services.AddAppendHistoryStore<TestEntity, FakeAppendStore>(storage =>
                    storage.PartitionBy(x => x.OrganizationId)));

            Assert.That(ex.Message, Does.Contain("TimeBy"));
        }

        [Test]
        public void ScratchAndFlatDocument_RemainSeparateCapabilities()
        {
            var services = new ServiceCollection();
            services.AddScratchStore<TestEntity, FakeScratchStore>(storage => storage.KeyBy(x => x.Id));
            services.AddFlatDocumentStore<TestEntity, FakeFlatDocumentStore>(storage => storage
                .KeyBy(x => x.Id)
                .Index(x => x.Category));

            using (var provider = services.BuildServiceProvider())
            using (var scope = provider.CreateScope())
            {
                Assert.That(scope.ServiceProvider.GetRequiredService<IScratchStore<TestEntity>>(), Is.TypeOf<FakeScratchStore>());
                Assert.That(scope.ServiceProvider.GetRequiredService<IFlatDocumentStore<TestEntity>>(), Is.TypeOf<FakeFlatDocumentStore>());
            }
        }

        [Test]
        public void MutableStores_WithoutKeyField_FailFast()
        {
            var scratchServices = new ServiceCollection();
            var flatServices = new ServiceCollection();

            Assert.Throws<InvalidOperationException>(() =>
                scratchServices.AddScratchStore<TestEntity, FakeScratchStore>(storage => storage.Index(x => x.Category)));

            Assert.Throws<InvalidOperationException>(() =>
                flatServices.AddFlatDocumentStore<TestEntity, FakeFlatDocumentStore>(storage => storage.Index(x => x.Category)));
        }

        [Test]
        public void QueryModels_UseTypedSelectorsAndOpaquePaging()
        {
            var query = new StorageQuery<TestEntity>()
                .Where(x => x.Category, StorageFilterOperator.Equal, "telemetry")
                .OrderBy(x => x.Timestamp, StorageSortDirection.Descending)
                .WithPage(new StoragePageRequest(250, "opaque-token"));

            Assert.That(query.Filters.Single().Field, Is.EqualTo(nameof(TestEntity.Category)));
            Assert.That(query.Sorts.Single().Field, Is.EqualTo(nameof(TestEntity.Timestamp)));
            Assert.That(query.Page.PageSize, Is.EqualTo(250));
            Assert.That(query.Page.ContinuationToken, Is.EqualTo("opaque-token"));
        }

        [Test]
        public void AppendHistoryContract_DoesNotExposeMutationOperations()
        {
            var methodNames = typeof(IAppendHistoryStore<TestEntity>).GetMethods().Select(method => method.Name).ToList();

            Assert.That(methodNames, Does.Not.Contain("UpdateAsync"));
            Assert.That(methodNames, Does.Not.Contain("DeleteAsync"));
            Assert.That(methodNames, Does.Contain("InsertAsync"));
            Assert.That(methodNames, Does.Contain("InsertBatchAsync"));
        }
    }
}
