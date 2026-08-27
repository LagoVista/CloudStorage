using LagoVista.CloudStorage.Storage;
using LagoVista.CloudStorage.Storage.ConnectionSettings;
using LagoVista.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LagoVista.StorageProvider.Tests.Cassandra
{
    [TestClass]
    [DoNotParallelize]
    [TestCategory("Cassandra")]
    [TestCategory("CassandraActivityRecord")]
    public class CassandraActivityRecordStoreAdvancedIntegrationTests
    {
        [TestMethod]
        public async Task QueryAsync_SpansMonthlyTimeBucketsInNewestFirstOrder()
        {
            var settings = new TestCassandraStorageSettings();
            using var factory = new CassandraSessionFactory(settings);
            using var services = CreateServices<BucketedActivityRecord>(factory, definition => definition.PartitionBy(record => record.OrganizationId).BucketBy(StoragePeriod.Month));
            var store = services.GetRequiredService<IActivityRecordStore<BucketedActivityRecord>>();
            var organizationId = Guid.NewGuid().ToString("N").ToUpperInvariant();

            var july = CreateBucketedRecord(organizationId, "July", new DateTime(2026, 7, 31, 23, 59, 0, DateTimeKind.Utc));
            var augustEarly = CreateBucketedRecord(organizationId, "August Early", new DateTime(2026, 8, 1, 0, 1, 0, DateTimeKind.Utc));
            var augustLate = CreateBucketedRecord(organizationId, "August Late", new DateTime(2026, 8, 15, 12, 0, 0, DateTimeKind.Utc));

            await store.InsertBatchAsync(new[] { july, augustEarly, augustLate });

            var result = await store.QueryAsync(new HistoryQuery<BucketedActivityRecord>().Between(new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 8, 31, 23, 59, 59, DateTimeKind.Utc)).Where(record => record.OrganizationId, StorageFilterOperator.Equal, organizationId).WithPage(new StoragePageRequest(pageSize: 10)));

            CollectionAssert.AreEqual(new[] { augustLate.Id, augustEarly.Id, july.Id }, result.Items.Select(record => record.Id).ToArray());
            Assert.IsFalse(result.HasMoreRecords);
        }

        [TestMethod]
        public async Task QueryAsync_SpansQuarterlyTimeBucketsAcrossQuarterBoundary()
        {
            var settings = new TestCassandraStorageSettings();
            using var factory = new CassandraSessionFactory(settings);
            using var services = CreateServices<QuarterBucketedActivityRecord>(factory, definition => definition.PartitionBy(record => record.OrganizationId).BucketBy(StoragePeriod.Quarter));
            var store = services.GetRequiredService<IActivityRecordStore<QuarterBucketedActivityRecord>>();
            var organizationId = Guid.NewGuid().ToString("N").ToUpperInvariant();
            var q2 = CreateActivityRecord<QuarterBucketedActivityRecord>(organizationId, "Q2", new DateTime(2026, 6, 30, 23, 59, 0, DateTimeKind.Utc));
            var q3Early = CreateActivityRecord<QuarterBucketedActivityRecord>(organizationId, "Q3 Early", new DateTime(2026, 7, 1, 0, 1, 0, DateTimeKind.Utc));
            var q3Late = CreateActivityRecord<QuarterBucketedActivityRecord>(organizationId, "Q3 Late", new DateTime(2026, 9, 30, 12, 0, 0, DateTimeKind.Utc));

            await store.InsertBatchAsync(new[] { q2, q3Early, q3Late });

            var result = await store.QueryAsync(new HistoryQuery<QuarterBucketedActivityRecord>().Between(new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 9, 30, 23, 59, 59, DateTimeKind.Utc)).Where(record => record.OrganizationId, StorageFilterOperator.Equal, organizationId).WithPage(new StoragePageRequest(pageSize: 10)));

            CollectionAssert.AreEqual(new[] { q3Late.Id, q3Early.Id, q2.Id }, result.Items.Select(record => record.Id).ToArray());
            Assert.IsFalse(result.HasMoreRecords);
        }

        [TestMethod]
        public async Task QueryAsync_SpansYearlyTimeBucketsAcrossYearBoundary()
        {
            var settings = new TestCassandraStorageSettings();
            using var factory = new CassandraSessionFactory(settings);
            using var services = CreateServices<YearBucketedActivityRecord>(factory, definition => definition.PartitionBy(record => record.OrganizationId).BucketBy(StoragePeriod.Year));
            var store = services.GetRequiredService<IActivityRecordStore<YearBucketedActivityRecord>>();
            var organizationId = Guid.NewGuid().ToString("N").ToUpperInvariant();
            var priorYear = CreateActivityRecord<YearBucketedActivityRecord>(organizationId, "2025", new DateTime(2025, 12, 31, 23, 59, 0, DateTimeKind.Utc));
            var currentYearEarly = CreateActivityRecord<YearBucketedActivityRecord>(organizationId, "2026 Early", new DateTime(2026, 1, 1, 0, 1, 0, DateTimeKind.Utc));
            var currentYearLate = CreateActivityRecord<YearBucketedActivityRecord>(organizationId, "2026 Late", new DateTime(2026, 8, 27, 12, 0, 0, DateTimeKind.Utc));

            await store.InsertBatchAsync(new[] { priorYear, currentYearEarly, currentYearLate });

            var result = await store.QueryAsync(new HistoryQuery<YearBucketedActivityRecord>().Between(new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 12, 31, 23, 59, 59, DateTimeKind.Utc)).Where(record => record.OrganizationId, StorageFilterOperator.Equal, organizationId).WithPage(new StoragePageRequest(pageSize: 10)));

            CollectionAssert.AreEqual(new[] { currentYearLate.Id, currentYearEarly.Id, priorYear.Id }, result.Items.Select(record => record.Id).ToArray());
            Assert.IsFalse(result.HasMoreRecords);
        }

        [TestMethod]
        public async Task QueryAsync_AllPeriodDoesNotRequireTimeBucketsAndStillHonorsDateRange()
        {
            var settings = new TestCassandraStorageSettings();
            using var factory = new CassandraSessionFactory(settings);
            using var services = CreateServices<AllPeriodActivityRecord>(factory, definition => definition.PartitionBy(record => record.OrganizationId).BucketBy(StoragePeriod.All));
            var store = services.GetRequiredService<IActivityRecordStore<AllPeriodActivityRecord>>();
            var organizationId = Guid.NewGuid().ToString("N").ToUpperInvariant();
            var older = CreateActivityRecord<AllPeriodActivityRecord>(organizationId, "Older", new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc));
            var includedOld = CreateActivityRecord<AllPeriodActivityRecord>(organizationId, "Included Old", new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc));
            var includedNew = CreateActivityRecord<AllPeriodActivityRecord>(organizationId, "Included New", new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc));

            await store.InsertBatchAsync(new[] { older, includedOld, includedNew });

            var result = await store.QueryAsync(new HistoryQuery<AllPeriodActivityRecord>().Between(new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 12, 31, 23, 59, 59, DateTimeKind.Utc)).Where(record => record.OrganizationId, StorageFilterOperator.Equal, organizationId).WithPage(new StoragePageRequest(pageSize: 10)));

            CollectionAssert.AreEqual(new[] { includedNew.Id, includedOld.Id }, result.Items.Select(record => record.Id).ToArray());
            Assert.IsFalse(result.HasMoreRecords);
        }

        [TestMethod]
        public async Task QueryAsync_FiltersOnDeclaredSaiIndex()
        {
            var settings = new TestCassandraStorageSettings();
            using var factory = new CassandraSessionFactory(settings);
            using var services = CreateServices<IndexedActivityRecord>(factory, definition => definition.PartitionBy(record => record.OrganizationId).Index(record => record.Category));
            var store = services.GetRequiredService<IActivityRecordStore<IndexedActivityRecord>>();
            var organizationId = Guid.NewGuid().ToString("N").ToUpperInvariant();
            var start = DateTime.UtcNow.AddMinutes(-5);

            var keepNewest = CreateIndexedRecord(organizationId, "KEEP", start.AddMinutes(3));
            var discard = CreateIndexedRecord(organizationId, "DISCARD", start.AddMinutes(2));
            var keepOldest = CreateIndexedRecord(organizationId, "KEEP", start.AddMinutes(1));
            await store.InsertBatchAsync(new[] { keepOldest, discard, keepNewest });

            var result = await store.QueryAsync(new HistoryQuery<IndexedActivityRecord>().Between(start, start.AddMinutes(5)).Where(record => record.OrganizationId, StorageFilterOperator.Equal, organizationId).Where(record => record.Category, StorageFilterOperator.Equal, "KEEP").WithPage(new StoragePageRequest(pageSize: 10)));

            CollectionAssert.AreEqual(new[] { keepNewest.Id, keepOldest.Id }, result.Items.Select(record => record.Id).ToArray());
        }

        [TestMethod]
        public async Task Retention_ExpiresRecordsUsingTableDefaultTtl()
        {
            var settings = new TestCassandraStorageSettings();
            using var factory = new CassandraSessionFactory(settings);
            using var services = CreateServices<RetainedActivityRecord>(factory, definition => definition.PartitionBy(record => record.OrganizationId).RetainFor(TimeSpan.FromSeconds(2)));
            var store = services.GetRequiredService<IActivityRecordStore<RetainedActivityRecord>>();
            var organizationId = Guid.NewGuid().ToString("N").ToUpperInvariant();
            var created = DateTime.UtcNow;
            var record = CreateRetainedRecord(organizationId, created);

            await store.InsertAsync(record);

            var immediate = await store.QueryAsync(new HistoryQuery<RetainedActivityRecord>().Between(created.AddMinutes(-1), created.AddMinutes(1)).Where(item => item.OrganizationId, StorageFilterOperator.Equal, organizationId).WithPage(new StoragePageRequest(pageSize: 10)));
            Assert.AreEqual(1, immediate.Items.Count);

            await Task.Delay(TimeSpan.FromSeconds(4));

            var expired = await store.QueryAsync(new HistoryQuery<RetainedActivityRecord>().Between(created.AddMinutes(-1), created.AddMinutes(1)).Where(item => item.OrganizationId, StorageFilterOperator.Equal, organizationId).WithPage(new StoragePageRequest(pageSize: 10)));
            Assert.AreEqual(0, expired.Items.Count);
        }

        private static ServiceProvider CreateServices<TRecord>(ICassandraSessionFactory factory, Action<StorageDefinition<TRecord>> configure) where TRecord : IActivityRecord, new()
        {
            var services = new ServiceCollection();
            services.AddSingleton(factory);
            services.AddActivityRecordStore<TRecord, CassandraActivityRecordStore<TRecord>>(configure);
            return services.BuildServiceProvider();
        }

        private static BucketedActivityRecord CreateBucketedRecord(string organizationId, string message, DateTime creationDate)
        {
            return new BucketedActivityRecord { Id = NewId(), OrganizationId = organizationId, Organization = "Contract Organization", CreationDate = creationDate, Message = message };
        }

        private static TRecord CreateActivityRecord<TRecord>(string organizationId, string message, DateTime creationDate) where TRecord : ActivityRecordBase, new()
        {
            return new TRecord { Id = NewId(), OrganizationId = organizationId, Organization = "Contract Organization", CreationDate = creationDate, Message = message };
        }

        private static IndexedActivityRecord CreateIndexedRecord(string organizationId, string category, DateTime creationDate)
        {
            return new IndexedActivityRecord { Id = NewId(), OrganizationId = organizationId, Organization = "Contract Organization", CreationDate = creationDate, Category = category };
        }

        private static RetainedActivityRecord CreateRetainedRecord(string organizationId, DateTime creationDate)
        {
            return new RetainedActivityRecord { Id = NewId(), OrganizationId = organizationId, Organization = "Contract Organization", CreationDate = creationDate };
        }

        private static string NewId() => Guid.NewGuid().ToString("N").ToUpperInvariant();

        private sealed class TestCassandraStorageSettings : ICassandraStorageSettings
        {
            public IReadOnlyList<string> ContactPoints { get; } = new[] { "127.0.0.1" };
            public string UserName => "cassandra";
            public string Password => "cassandra";
            public string Keyspace => "nuviot_storage_tests";
            public int Port => 19042;
            public string LocalDataCenter => "datacenter1";
        }

        public abstract class ActivityRecordBase : IActivityRecord
        {
            public string Id { get; set; }
            public string OrganizationId { get; set; }
            public string Organization { get; set; }
            public DateTime CreationDate { get; set; }
            public string Message { get; set; }
        }

        public sealed class BucketedActivityRecord : ActivityRecordBase { }
        public sealed class QuarterBucketedActivityRecord : ActivityRecordBase { }
        public sealed class YearBucketedActivityRecord : ActivityRecordBase { }
        public sealed class AllPeriodActivityRecord : ActivityRecordBase { }

        public sealed class IndexedActivityRecord : IActivityRecord
        {
            public string Id { get; set; }
            public string OrganizationId { get; set; }
            public string Organization { get; set; }
            public DateTime CreationDate { get; set; }
            public string Category { get; set; }
        }

        public sealed class RetainedActivityRecord : IActivityRecord
        {
            public string Id { get; set; }
            public string OrganizationId { get; set; }
            public string Organization { get; set; }
            public DateTime CreationDate { get; set; }
        }
    }
}
