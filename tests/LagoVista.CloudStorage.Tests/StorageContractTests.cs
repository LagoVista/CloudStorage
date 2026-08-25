using LagoVista.CloudStorage.Storage;
using LagoVista.Core.Interfaces;
using LagoVista.Core.Models;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LagoVista.Core;

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
            public NormalizedId32 Id { get; set; }
            public EntityHeader Organization { get; set; }
            public string Category { get; set; }
        }

        private class ApplicationDataRecord : IApplicationDataRecord
        {
            public NormalizedId32 Id { get; set; }
            public EntityHeader Organization { get; set; }
            public UtcTimestamp CreationDate { get; set; }
            public UtcTimestamp LastUpdatedDate { get; set; }
            public string Category { get; set; }
        }

        private class FakeActivityStore : IActivityRecordStore<ActivityRecord>
        {
            public Task InsertAsync(ActivityRecord record, CancellationToken cancellationToken = default) => Task.CompletedTask;
            public Task InsertBatchAsync(IEnumerable<ActivityRecord> records, CancellationToken cancellationToken = default) => Task.CompletedTask;
            public Task<StoragePageResult<ActivityRecord>> QueryAsync(HistoryQuery<ActivityRecord> query, CancellationToken cancellationToken = default) => Task.FromResult(new StoragePageResult<ActivityRecord>(Array.Empty<ActivityRecord>()));
        }

        private class FakeScratchStore : IScratchStore
        {
            public Task<TRecord> GetAsync<TRecord>(StorageKey key, CancellationToken cancellationToken = default) where TRecord : class, IScratchDataRecord => Task.FromResult<TRecord>(default);
            public Task UpsertAsync<TRecord>(TRecord record, CancellationToken cancellationToken = default) where TRecord : class, IScratchDataRecord => Task.CompletedTask;
            public Task DeleteAsync<TRecord>(StorageKey key, CancellationToken cancellationToken = default) where TRecord : class, IScratchDataRecord => Task.CompletedTask;
            public Task<StoragePageResult<TRecord>> QueryAsync<TRecord>(StorageQuery<TRecord> query, CancellationToken cancellationToken = default) where TRecord : class, IScratchDataRecord => Task.FromResult(new StoragePageResult<TRecord>(Array.Empty<TRecord>()));
        }

        private class FakeApplicationDataStore : IApplicationDataStore
        {
            public Task<TRecord> GetAsync<TRecord>(StorageKey key, CancellationToken cancellationToken = default) where TRecord : class, IApplicationDataRecord => Task.FromResult<TRecord>(default);
            public Task InsertAsync<TRecord>(TRecord record, CancellationToken cancellationToken = default) where TRecord : class, IApplicationDataRecord => Task.CompletedTask;
            public Task UpdateAsync<TRecord>(TRecord record, CancellationToken cancellationToken = default) where TRecord : class, IApplicationDataRecord => Task.CompletedTask;
            public Task DeleteAsync<TRecord>(StorageKey key, CancellationToken cancellationToken = default) where TRecord : class, IApplicationDataRecord => Task.CompletedTask;
            public Task<StoragePageResult<TRecord>> QueryAsync<TRecord>(StorageQuery<TRecord> query, CancellationToken cancellationToken = default) where TRecord : class, IApplicationDataRecord => Task.FromResult(new StoragePageResult<TRecord>(Array.Empty<TRecord>()));
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
        public void ScratchAndApplicationData_AreRegisteredOnceAsCapabilities()
        {
            var services = new ServiceCollection();
            services.AddScratchStore<FakeScratchStore>();
            services.AddApplicationDataStore<FakeApplicationDataStore>();

            using (var provider = services.BuildServiceProvider())
            using (var scope = provider.CreateScope())
            {
                Assert.That(scope.ServiceProvider.GetRequiredService<IScratchStore>(), Is.TypeOf<FakeScratchStore>());
                Assert.That(scope.ServiceProvider.GetRequiredService<IApplicationDataStore>(), Is.TypeOf<FakeApplicationDataStore>());
            }
        }

        [Test]
        public void MutableRecordDefinitions_UseDeterministicIdentityAndOrganizationPath()
        {
            var services = new ServiceCollection();
            services.ConfigureScratchData<ScratchRecord>(storage => storage.Index(x => x.Category));
            services.ConfigureApplicationData<ApplicationDataRecord>(storage => storage.Index(x => x.Category));

            using (var provider = services.BuildServiceProvider())
            {
                var scratch = provider.GetRequiredService<ScratchStoreOptions<ScratchRecord>>().Definition;
                var application = provider.GetRequiredService<ApplicationDataStoreOptions<ApplicationDataRecord>>().Definition;

                Assert.That(scratch.KeyField, Is.EqualTo(nameof(IScratchDataRecord.Id)));
                Assert.That(scratch.PartitionFields, Does.Contain("Organization.Id"));
                Assert.That(application.KeyField, Is.EqualTo(nameof(IApplicationDataRecord.Id)));
                Assert.That(application.PartitionFields, Does.Contain("Organization.Id"));
                Assert.That(application.IndexedFields, Does.Contain(nameof(ApplicationDataRecord.Category)));
            }
        }

        [Test]
        public void CollectionName_IsDeterministicFromRecordType()
        {
            Assert.That(StorageRecordIdentity.GetCollectionName<ApplicationDataRecord>(), Is.EqualTo(nameof(ApplicationDataRecord)));
            Assert.That(StorageRecordIdentity.GetCollectionName<ScratchRecord>(), Is.EqualTo(nameof(ScratchRecord)));
            Assert.That(StorageRecordIdentity.GetCollectionName<ApplicationDataRecord>(), Is.Not.EqualTo(StorageRecordIdentity.GetCollectionName<ScratchRecord>()));
        }

        [Test]
        public void QueryModels_SupportNestedSelectorsAndOpaquePaging()
        {
            var query = new StorageQuery<ApplicationDataRecord>()
                .Where(x => x.Organization.Id, StorageFilterOperator.Equal, "org-id")
                .OrderBy(x => x.CreationDate, StorageSortDirection.Descending)
                .WithPage(new StoragePageRequest(250, "opaque-token"));

            Assert.That(query.Filters.Single().Field, Is.EqualTo("Organization.Id"));
            Assert.That(query.Sorts.Single().Field, Is.EqualTo(nameof(ApplicationDataRecord.CreationDate)));
            Assert.That(query.Page.PageSize, Is.EqualTo(250));
            Assert.That(query.Page.ContinuationToken, Is.EqualTo("opaque-token"));
        }

        [Test]
        public void ApplicationDataContract_DoesNotRequireName()
        {
            Assert.That(typeof(IApplicationDataRecord).GetProperty("Name"), Is.Null);
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
