using LagoVista.CloudStorage.Storage;
using LagoVista.Core.Models;
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
        private class ActivityRecord : IActivityRecord
        {
            public string Id { get; set; }
            public string OrganizationId { get; set; }
            public string Organization { get; set; }
            public DateTime CreationDate { get; set; }
            public string Category { get; set; }
        }

        private class ScratchRecord : IScratchDataRecord
        {
            public string Id { get; set; }
            public EntityHeader Organization { get; set; }
            public string Category { get; set; }
        }

        private class ApplicationDataRecord : IApplicationDataRecord
        {
            public string Id { get; set; }
            public EntityHeader Organization { get; set; }
            public DateTime CreationDate { get; set; }
            public DateTime LastUpdatedDate { get; set; }
            public string Category { get; set; }
        }

        private class FakeActivityStore : IActivityRecordStore<ActivityRecord>
        {
            public Task InsertAsync(ActivityRecord record, CancellationToken cancellationToken = default) => Task.CompletedTask;
            public Task InsertBatchAsync(IEnumerable<ActivityRecord> records, CancellationToken cancellationToken = default) => Task.CompletedTask;
            public Task<StoragePageResult<ActivityRecord>> QueryAsync(HistoryQuery<ActivityRecord> query, CancellationToken cancellationToken = default) => Task.FromResult(new StoragePageResult<ActivityRecord>(Array.Empty<ActivityRecord>()));
        }

        private class FakeScratchStore : IScratchStore<ScratchRecord>
        {
            public Task<ScratchRecord> GetAsync(StorageKey key, CancellationToken cancellationToken = default) => Task.FromResult<ScratchRecord>(null);
            public Task UpsertAsync(ScratchRecord record, CancellationToken cancellationToken = default) => Task.CompletedTask;
            public Task DeleteAsync(StorageKey key, CancellationToken cancellationToken = default) => Task.CompletedTask;
            public Task<StoragePageResult<ScratchRecord>> QueryAsync(StorageQuery<ScratchRecord> query, CancellationToken cancellationToken = default) => Task.FromResult(new StoragePageResult<ScratchRecord>(Array.Empty<ScratchRecord>()));
        }

        private class FakeApplicationDataStore : IApplicationDataStore<ApplicationDataRecord>
        {
            public Task<ApplicationDataRecord> GetAsync(StorageKey key, CancellationToken cancellationToken = default) => Task.FromResult<ApplicationDataRecord>(null);
            public Task InsertAsync(ApplicationDataRecord record, CancellationToken cancellationToken = default) => Task.CompletedTask;
            public Task UpdateAsync(ApplicationDataRecord record, CancellationToken cancellationToken = default) => Task.CompletedTask;
            public Task DeleteAsync(StorageKey key, CancellationToken cancellationToken = default) => Task.CompletedTask;
            public Task<StoragePageResult<ApplicationDataRecord>> QueryAsync(StorageQuery<ApplicationDataRecord> query, CancellationToken cancellationToken = default) => Task.FromResult(new StoragePageResult<ApplicationDataRecord>(Array.Empty<ApplicationDataRecord>()));
        }

        [Test]
        public void AddActivityRecordStore_UsesHouseIdentityAndCreationDate()
        {
            var services = new ServiceCollection();
            services.AddActivityRecordStore<ActivityRecord, FakeActivityStore>(storage => storage.PartitionBy(x => x.OrganizationId).BucketBy(StoragePeriod.Month).Index(x => x.Category));

            using (var provider = services.BuildServiceProvider())
            using (var scope = provider.CreateScope())
            {
                Assert.That(scope.ServiceProvider.GetRequiredService<IActivityRecordStore<ActivityRecord>>(), Is.TypeOf<FakeActivityStore>());
                var options = scope.ServiceProvider.GetRequiredService<ActivityRecordStoreOptions<ActivityRecord>>();
                Assert.That(options.Definition.KeyField, Is.EqualTo(nameof(ActivityRecord.Id)));
                Assert.That(options.Definition.TimeField, Is.EqualTo(nameof(ActivityRecord.CreationDate)));
                Assert.That(options.Definition.PartitionFields, Does.Contain(nameof(ActivityRecord.OrganizationId)));
                Assert.That(options.Definition.IndexedFields, Does.Contain(nameof(ActivityRecord.Category)));
                Assert.That(options.Definition.BucketPeriod, Is.EqualTo(StoragePeriod.Month));
            }
        }

        [Test]
        public void InterfaceConstrainedActivitySelectors_AreAccepted()
        {
            var definition = BuildActivityDefinition<ActivityRecord>();

            Assert.That(definition.KeyField, Is.EqualTo(nameof(IActivityRecord.Id)));
            Assert.That(definition.TimeField, Is.EqualTo(nameof(IActivityRecord.CreationDate)));
            Assert.That(definition.PartitionFields, Does.Contain(nameof(IActivityRecord.OrganizationId)));
        }

        [Test]
        public void ScratchAndApplicationData_RemainSeparateCapabilities()
        {
            var services = new ServiceCollection();
            services.AddScratchStore<ScratchRecord, FakeScratchStore>(storage => storage.KeyBy(x => x.Id));
            services.AddApplicationDataStore<ApplicationDataRecord, FakeApplicationDataStore>(storage => storage.KeyBy(x => x.Id).Index(x => x.Category));

            using (var provider = services.BuildServiceProvider())
            using (var scope = provider.CreateScope())
            {
                Assert.That(scope.ServiceProvider.GetRequiredService<IScratchStore<ScratchRecord>>(), Is.TypeOf<FakeScratchStore>());
                Assert.That(scope.ServiceProvider.GetRequiredService<IApplicationDataStore<ApplicationDataRecord>>(), Is.TypeOf<FakeApplicationDataStore>());
            }
        }

        [Test]
        public void MutableStores_WithoutKeyField_FailFast()
        {
            var scratchServices = new ServiceCollection();
            var applicationServices = new ServiceCollection();

            Assert.Throws<InvalidOperationException>(() => scratchServices.AddScratchStore<ScratchRecord, FakeScratchStore>(storage => storage.Index(x => x.Category)));
            Assert.Throws<InvalidOperationException>(() => applicationServices.AddApplicationDataStore<ApplicationDataRecord, FakeApplicationDataStore>(storage => storage.Index(x => x.Category)));
        }

        [Test]
        public void QueryModels_UseTypedSelectorsAndOpaquePaging()
        {
            var query = new StorageQuery<ApplicationDataRecord>().Where(x => x.Category, StorageFilterOperator.Equal, "telemetry").OrderBy(x => x.CreationDate, StorageSortDirection.Descending).WithPage(new StoragePageRequest(250, "opaque-token"));

            Assert.That(query.Filters.Single().Field, Is.EqualTo(nameof(ApplicationDataRecord.Category)));
            Assert.That(query.Sorts.Single().Field, Is.EqualTo(nameof(ApplicationDataRecord.CreationDate)));
            Assert.That(query.Page.PageSize, Is.EqualTo(250));
            Assert.That(query.Page.ContinuationToken, Is.EqualTo("opaque-token"));
        }

        [Test]
        public void ActivityRecordContract_DoesNotExposeMutationOperations()
        {
            var methodNames = typeof(IActivityRecordStore<ActivityRecord>).GetMethods().Select(method => method.Name).ToList();
            Assert.That(methodNames, Does.Not.Contain("UpdateAsync"));
            Assert.That(methodNames, Does.Not.Contain("DeleteAsync"));
            Assert.That(methodNames, Does.Contain("InsertAsync"));
            Assert.That(methodNames, Does.Contain("InsertBatchAsync"));
        }

        private static FlatStorageDefinition<TRecord> BuildActivityDefinition<TRecord>()
            where TRecord : IActivityRecord
        {
            return new FlatStorageDefinition<TRecord>()
                .KeyBy(record => record.Id)
                .TimeBy(record => record.CreationDate)
                .PartitionBy(record => record.OrganizationId);
        }
    }
}
