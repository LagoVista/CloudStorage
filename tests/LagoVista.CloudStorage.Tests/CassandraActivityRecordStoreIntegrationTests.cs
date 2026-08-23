using Cassandra;
using LagoVista.CloudStorage.Storage;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using System;
using System.Collections.Generic;
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
        private IActivityRecordStore<RetainedActivityRecord> _retainedStore;
        private IActivityRecordStore<IndexedActivityRecord> _indexedStore;
        private IActivityRecordStore<BucketedIndexedActivityRecord> _bucketedIndexedStore;
        private ICassandraSessionFactory _sessionFactory;
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

        public sealed class RetainedActivityRecord : IActivityRecord
        {
            public string Id { get; set; }
            public string OrganizationId { get; set; }
            public string Organization { get; set; }
            public DateTime CreationDate { get; set; }
            public string Category { get; set; }
        }

        public sealed class IndexedActivityRecord : IActivityRecord
        {
            public string Id { get; set; }
            public string OrganizationId { get; set; }
            public string Organization { get; set; }
            public DateTime CreationDate { get; set; }
            public string Category { get; set; }
            public int Value { get; set; }
        }

        public sealed class BucketedIndexedActivityRecord : IActivityRecord
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
            services.AddActivityRecordStore<RetainedActivityRecord, CassandraActivityRecordStore<RetainedActivityRecord>>(
                definition => definition
                    .PartitionBy(record => record.OrganizationId)
                    .RetainFor(TimeSpan.FromSeconds(120)));
            services.AddActivityRecordStore<IndexedActivityRecord, CassandraActivityRecordStore<IndexedActivityRecord>>(
                definition => definition
                    .PartitionBy(record => record.OrganizationId)
                    .Index(record => record.Category));
            services.AddActivityRecordStore<BucketedIndexedActivityRecord, CassandraActivityRecordStore<BucketedIndexedActivityRecord>>(
                definition => definition
                    .PartitionBy(record => record.OrganizationId)
                    .Index(record => record.Category)
                    .BucketBy(StoragePeriod.Month));

            _services = services.BuildServiceProvider();
            _store = _services.GetRequiredService<IActivityRecordStore<TestActivityRecord>>();
            _bucketedStore = _services.GetRequiredService<IActivityRecordStore<BucketedActivityRecord>>();
            _retainedStore = _services.GetRequiredService<IActivityRecordStore<RetainedActivityRecord>>();
            _indexedStore = _services.GetRequiredService<IActivityRecordStore<IndexedActivityRecord>>();
            _bucketedIndexedStore = _services.GetRequiredService<IActivityRecordStore<BucketedIndexedActivityRecord>>();
            _sessionFactory = _services.GetRequiredService<ICassandraSessionFactory>();
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

        [Test]
        public async Task RetainedStore_AppliesTableDefaultTtlToInsertedRows()
        {
            var record = new RetainedActivityRecord
            {
                Id = Guid.NewGuid().ToString("N"),
                OrganizationId = $"ORG-{Guid.NewGuid():N}",
                Organization = "Retention Test Organization",
                CreationDate = DateTime.UtcNow,
                Category = "retention"
            };

            await _retainedStore.InsertAsync(record);

            var session = await _sessionFactory.GetSessionAsync();
            var tableStatement = await session.PrepareAsync(
                "SELECT default_time_to_live FROM system_schema.tables WHERE keyspace_name = ? AND table_name = ?");
            var tableRows = await session.ExecuteAsync(tableStatement.Bind(_settings.Keyspace, "retained_activity_record"));
            var tableTtl = tableRows.Single().GetValue<int>("default_time_to_live");

            var ttlStatement = await session.PrepareAsync(@"
SELECT TTL(organization) AS remaining_ttl
FROM retained_activity_record
WHERE organization_id = ? AND creation_date = ? AND id = ?");
            var ttlRows = await session.ExecuteAsync(ttlStatement.Bind(
                record.OrganizationId,
                new DateTimeOffset(record.CreationDate),
                record.Id));
            var remainingTtl = ttlRows.Single().GetValue<int>("remaining_ttl");

            Assert.Multiple(() =>
            {
                Assert.That(tableTtl, Is.EqualTo(120));
                Assert.That(remainingTtl, Is.GreaterThan(0));
                Assert.That(remainingTtl, Is.LessThanOrEqualTo(120));
            });
        }

        [Test]
        public async Task IndexedQuery_DeclaredField_ReturnsOnlyMatches()
        {
            var organizationId = $"ORG-{Guid.NewGuid():N}";
            var now = DateTime.UtcNow;
            var matchingOlder = CreateIndexedRecord(organizationId, "match", now.AddSeconds(-3), 1);
            var ignored = CreateIndexedRecord(organizationId, "ignore", now.AddSeconds(-2), 2);
            var matchingNewer = CreateIndexedRecord(organizationId, "match", now.AddSeconds(-1), 3);

            await _indexedStore.InsertBatchAsync(new[] { matchingOlder, ignored, matchingNewer });

            var query = new HistoryQuery<IndexedActivityRecord>()
                .Where(record => record.OrganizationId, StorageFilterOperator.Equal, organizationId)
                .Where(record => record.Category, StorageFilterOperator.Equal, "match")
                .Between(now.AddMinutes(-1), now);

            var result = await _indexedStore.QueryAsync(query);

            Assert.That(result.Items.Select(record => record.Id), Is.EqualTo(new[] { matchingNewer.Id, matchingOlder.Id }));
        }

        [Test]
        public void IndexedQuery_UndeclaredField_FailsFast()
        {
            var query = new HistoryQuery<IndexedActivityRecord>()
                .Where(record => record.OrganizationId, StorageFilterOperator.Equal, "ORG1")
                .Where(record => record.Value, StorageFilterOperator.Equal, 42);

            var exception = Assert.ThrowsAsync<NotSupportedException>(() => _indexedStore.QueryAsync(query));
            Assert.That(exception.Message, Does.Contain("Register it with Index(...)"));
        }

        [Test]
        public async Task IndexedStore_CreatesSaiIndexMetadata()
        {
            await _indexedStore.InsertAsync(CreateIndexedRecord(
                $"ORG-{Guid.NewGuid():N}", "metadata", DateTime.UtcNow, 1));

            var session = await _sessionFactory.GetSessionAsync();
            var prepared = await session.PrepareAsync(@"
SELECT index_name, kind, options
FROM system_schema.indexes
WHERE keyspace_name = ? AND table_name = ?");
            var rows = (await session.ExecuteAsync(prepared.Bind(_settings.Keyspace, "indexed_activity_record"))).ToList();
            var index = rows.Single(row => row.GetValue<string>("index_name") == "indexed_activity_record_category_sai_idx");
            var options = index.GetValue<IDictionary<string, string>>("options");

            Assert.Multiple(() =>
            {
                Assert.That(index.GetValue<string>("kind"), Is.EqualTo("CUSTOM"));
                Assert.That(options["target"], Is.EqualTo("category"));
                Assert.That(options["class_name"].ToLowerInvariant(), Does.Contain("sai").Or.Contain("storageattachedindex"));
            });
        }

        [Test]
        public async Task BucketedIndexedQuery_AcrossBuckets_FiltersDeclaredIndex()
        {
            var organizationId = $"ORG-{Guid.NewGuid():N}";
            var julyMatch = CreateBucketedIndexedRecord(organizationId, "match", new DateTime(2026, 7, 31, 23, 59, 0, DateTimeKind.Utc), 1);
            var augustIgnored = CreateBucketedIndexedRecord(organizationId, "ignore", new DateTime(2026, 8, 1, 0, 1, 0, DateTimeKind.Utc), 2);
            var augustMatch = CreateBucketedIndexedRecord(organizationId, "match", new DateTime(2026, 8, 1, 0, 2, 0, DateTimeKind.Utc), 3);

            await _bucketedIndexedStore.InsertBatchAsync(new[] { julyMatch, augustIgnored, augustMatch });

            var query = new HistoryQuery<BucketedIndexedActivityRecord>()
                .Where(record => record.OrganizationId, StorageFilterOperator.Equal, organizationId)
                .Where(record => record.Category, StorageFilterOperator.Equal, "match")
                .Between(
                    new DateTime(2026, 7, 31, 23, 58, 0, DateTimeKind.Utc),
                    new DateTime(2026, 8, 1, 0, 3, 0, DateTimeKind.Utc));

            var result = await _bucketedIndexedStore.QueryAsync(query);
            Assert.That(result.Items.Select(record => record.Id), Is.EqualTo(new[] { augustMatch.Id, julyMatch.Id }));
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

        private static IndexedActivityRecord CreateIndexedRecord(string organizationId, string category, DateTime creationDate, int value)
        {
            return new IndexedActivityRecord
            {
                Id = Guid.NewGuid().ToString("N"),
                OrganizationId = organizationId,
                Organization = "Indexed Integration Test Organization",
                CreationDate = creationDate,
                Category = category,
                Value = value
            };
        }

        private static BucketedIndexedActivityRecord CreateBucketedIndexedRecord(string organizationId, string category, DateTime creationDate, int value)
        {
            return new BucketedIndexedActivityRecord
            {
                Id = Guid.NewGuid().ToString("N"),
                OrganizationId = organizationId,
                Organization = "Bucketed Indexed Integration Test Organization",
                CreationDate = creationDate,
                Category = category,
                Value = value
            };
        }
    }
}
