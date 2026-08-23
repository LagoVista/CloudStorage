using Cassandra;
using LagoVista.CloudStorage.Storage;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace LagoVista.CloudStorage.Tests
{
    [NonParallelizable]
    [Category("Integration")]
    [Category("Cassandra")]
    public class CassandraActivityRecordStoreIntegrationTests
    {
        private IServiceProvider _services;
        private IActivityRecordStore<TestActivityRecord> _store;
        private IActivityRecordStore<BucketedActivityRecord> _bucketedStore;
        private ICassandraStorageSettings _settings;

        public sealed class TestActivityRecord : IActivityRecord
        {
            public string Id { get; set; }
            public string OrganizationId { get; set; }
            public string Organization { get; set; }
            public DateTime CreationDate { get; set; }
            public string Category { get; set; }
            public int Value { get; set; }
        }

        public sealed class BucketedActivityRecord : IActivityRecord
        {
            public string Id { get; set; }
            public string OrganizationId { get; set; }
            public string Organization { get; set; }
            public DateTime CreationDate { get; set; }
            public string Category { get; set; }
            public int Value { get; set; }
        }

        [OneTimeSetUp]
        public async Task SetupAsync()
        {
            _settings = CoreStorageTestConnections.Cassandra;

            using (var cluster = Cluster.Builder()
                .AddContactPoints(_settings.ContactPoints)
                .WithPort(_settings.Port)
                .WithCredentials(_settings.UserName, _settings.Password)
                .Build())
            using (var session = await cluster.ConnectAsync())
            {
                await session.ExecuteAsync(new SimpleStatement(
                    $"CREATE KEYSPACE IF NOT EXISTS {_settings.Keyspace} WITH replication = {{'class':'SimpleStrategy','replication_factor':1}}"));
            }

            var services = new ServiceCollection();
            services.AddSingleton(_settings);
            services.AddSingleton<ICassandraStorageSettings>(_settings);
            services.AddCassandraStorageConnection();
            services.AddActivityRecordStore<TestActivityRecord, CassandraActivityRecordStore<TestActivityRecord>>(
                definition => definition.PartitionBy(record => record.OrganizationId));
            services.AddActivityRecordStore<BucketedActivityRecord, CassandraActivityRecordStore<BucketedActivityRecord>>(
                definition => definition
                    .PartitionBy(record => record.OrganizationId)
                    .BucketBy(StoragePeriod.Month));

            _services = services.BuildServiceProvider();
            _store = _services.GetRequiredService<IActivityRecordStore<TestActivityRecord>>();
            _bucketedStore = _services.GetRequiredService<IActivityRecordStore<BucketedActivityRecord>>();
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            (_services as IDisposable)?.Dispose();
        }

        [Test]
        public async Task InsertAndQueryByPartitionAndTime_ReturnsExpectedRecords()
        {
            var organizationId = $"ORG-{Guid.NewGuid():N}";
            var now = DateTime.UtcNow;

            var older = CreateRecord(organizationId, "older", now.AddMinutes(-2), 10);
            var newer = CreateRecord(organizationId, "newer", now.AddMinutes(-1), 20);
            var outside = CreateRecord(organizationId, "outside", now.AddHours(-2), 30);

            await _store.InsertBatchAsync(new[] { older, newer, outside });

            var query = new HistoryQuery<TestActivityRecord>()
                .Where(record => record.OrganizationId, StorageFilterOperator.Equal, organizationId)
                .Between(now.AddMinutes(-5), now);

            var result = await _store.QueryAsync(query);

            Assert.That(result.Items.Select(record => record.Id), Is.EqualTo(new[] { newer.Id, older.Id }));
            Assert.That(result.Items.Select(record => record.Value), Is.EqualTo(new[] { 20, 10 }));
            Assert.That(result.HasMoreRecords, Is.False);
        }

        [Test]
        public async Task QueryWithPageSize_ReturnsOpaqueContinuationToken()
        {
            var organizationId = $"ORG-{Guid.NewGuid():N}";
            var now = DateTime.UtcNow;

            await _store.InsertBatchAsync(new[]
            {
                CreateRecord(organizationId, "one", now.AddSeconds(-3), 1),
                CreateRecord(organizationId, "two", now.AddSeconds(-2), 2),
                CreateRecord(organizationId, "three", now.AddSeconds(-1), 3)
            });

            var firstQuery = new HistoryQuery<TestActivityRecord>()
                .Where(record => record.OrganizationId, StorageFilterOperator.Equal, organizationId)
                .WithPage(new StoragePageRequest(2));

            var first = await _store.QueryAsync(firstQuery);
            Assert.That(first.Items.Count, Is.EqualTo(2));
            Assert.That(first.HasMoreRecords, Is.True);

            var secondQuery = new HistoryQuery<TestActivityRecord>()
                .Where(record => record.OrganizationId, StorageFilterOperator.Equal, organizationId)
                .WithPage(new StoragePageRequest(2, first.ContinuationToken));

            var second = await _store.QueryAsync(secondQuery);
            Assert.That(second.Items.Count, Is.EqualTo(1));
            Assert.That(second.HasMoreRecords, Is.False);
        }

        [Test]
        public async Task BucketedQuery_AcrossMonthBoundary_ReturnsNewestFirst()
        {
            var organizationId = $"ORG-{Guid.NewGuid():N}";
            var older = CreateBucketedRecord(organizationId, "older", new DateTime(2026, 7, 31, 23, 59, 0, DateTimeKind.Utc), 10);
            var newer = CreateBucketedRecord(organizationId, "newer", new DateTime(2026, 8, 1, 0, 1, 0, DateTimeKind.Utc), 20);

            await _bucketedStore.InsertBatchAsync(new[] { older, newer });

            var query = new HistoryQuery<BucketedActivityRecord>()
                .Where(record => record.OrganizationId, StorageFilterOperator.Equal, organizationId)
                .Between(
                    new DateTime(2026, 7, 31, 23, 58, 0, DateTimeKind.Utc),
                    new DateTime(2026, 8, 1, 0, 2, 0, DateTimeKind.Utc));

            var result = await _bucketedStore.QueryAsync(query);

            Assert.That(result.Items.Select(record => record.Id), Is.EqualTo(new[] { newer.Id, older.Id }));
            Assert.That(result.HasMoreRecords, Is.False);
        }

        [Test]
        public async Task BucketedQuery_WithPaging_ContinuesAcrossBuckets()
        {
            var organizationId = $"ORG-{Guid.NewGuid():N}";
            var july = CreateBucketedRecord(organizationId, "july", new DateTime(2026, 7, 31, 23, 59, 0, DateTimeKind.Utc), 1);
            var augustOlder = CreateBucketedRecord(organizationId, "august-older", new DateTime(2026, 8, 1, 0, 1, 0, DateTimeKind.Utc), 2);
            var augustNewer = CreateBucketedRecord(organizationId, "august-newer", new DateTime(2026, 8, 1, 0, 2, 0, DateTimeKind.Utc), 3);

            await _bucketedStore.InsertBatchAsync(new[] { july, augustOlder, augustNewer });

            var firstQuery = new HistoryQuery<BucketedActivityRecord>()
                .Where(record => record.OrganizationId, StorageFilterOperator.Equal, organizationId)
                .Between(
                    new DateTime(2026, 7, 31, 23, 58, 0, DateTimeKind.Utc),
                    new DateTime(2026, 8, 1, 0, 3, 0, DateTimeKind.Utc))
                .WithPage(new StoragePageRequest(2));

            var first = await _bucketedStore.QueryAsync(firstQuery);
            Assert.That(first.Items.Select(record => record.Id), Is.EqualTo(new[] { augustNewer.Id, augustOlder.Id }));
            Assert.That(first.HasMoreRecords, Is.True);

            var secondQuery = new HistoryQuery<BucketedActivityRecord>()
                .Where(record => record.OrganizationId, StorageFilterOperator.Equal, organizationId)
                .Between(
                    new DateTime(2026, 7, 31, 23, 58, 0, DateTimeKind.Utc),
                    new DateTime(2026, 8, 1, 0, 3, 0, DateTimeKind.Utc))
                .WithPage(new StoragePageRequest(2, first.ContinuationToken));

            var second = await _bucketedStore.QueryAsync(secondQuery);
            Assert.That(second.Items.Select(record => record.Id), Is.EqualTo(new[] { july.Id }));
            Assert.That(second.HasMoreRecords, Is.False);
        }

        [Test]
        public void BucketedQuery_WithoutBoundedTimeRange_FailsFast()
        {
            var query = new HistoryQuery<BucketedActivityRecord>()
                .Where(record => record.OrganizationId, StorageFilterOperator.Equal, "ORG1");

            Assert.ThrowsAsync<InvalidOperationException>(() => _bucketedStore.QueryAsync(query));
        }

        private static TestActivityRecord CreateRecord(string organizationId, string category, DateTime creationDate, int value)
        {
            return new TestActivityRecord
            {
                Id = Guid.NewGuid().ToString("N"),
                OrganizationId = organizationId,
                Organization = "Integration Test Organization",
                CreationDate = creationDate,
                Category = category,
                Value = value
            };
        }

        private static BucketedActivityRecord CreateBucketedRecord(string organizationId, string category, DateTime creationDate, int value)
        {
            return new BucketedActivityRecord
            {
                Id = Guid.NewGuid().ToString("N"),
                OrganizationId = organizationId,
                Organization = "Integration Test Organization",
                CreationDate = creationDate,
                Category = category,
                Value = value
            };
        }
    }
}
