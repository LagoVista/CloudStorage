using LagoVista.CloudStorage.Storage;
using LagoVista.CloudStorage.Storage.ConnectionSettings;
using LagoVista.CloudStorage.Storage.StorageProviders.Cassandra;
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
    [TestCategory("CassandraOperationalData")]
    public class CassandraOperationalDataStoreIntegrationTests
    {
        [TestMethod]
        public async Task CrudAndPagingAsync_SatisfiesCoreOperationalDataContract()
        {
            var settings = new TestCassandraStorageSettings();
            using var factory = new CassandraSessionFactory(settings);
            using var services = CreateServices<TestOperationalRecord>(factory);
            var store = services.GetRequiredService<IOperationalDataStore<TestOperationalRecord>>();
            var organizationId = Guid.NewGuid().ToString("N").ToUpperInvariant();
            var otherOrganizationId = Guid.NewGuid().ToString("N").ToUpperInvariant();
            var creationDate = new DateTime(2026, 8, 26, 12, 0, 0, DateTimeKind.Utc);
            var first = CreateRecord<TestOperationalRecord>(organizationId, "A", "First", creationDate);

            await store.UpsertAsync(first);
            Assert.AreEqual(creationDate, first.CreationDate);
            Assert.AreNotEqual(default, first.LastUpdatedDate);
            Assert.AreEqual(DateTimeKind.Utc, first.LastUpdatedDate.Kind);

            var loaded = await store.GetAsync(organizationId, first.Id);
            Assert.IsNotNull(loaded);
            Assert.AreEqual("First", loaded.Value);
            Assert.AreEqual(creationDate, loaded.CreationDate);

            var firstUpdatedUtc = loaded.LastUpdatedDate;
            await Task.Delay(20);
            first.Value = "Updated";
            await store.UpsertAsync(first);
            loaded = await store.GetAsync(organizationId, first.Id);
            Assert.AreEqual("Updated", loaded.Value);
            Assert.IsTrue(loaded.LastUpdatedDate > firstUpdatedUtc);

            var second = CreateRecord<TestOperationalRecord>(organizationId, "B", "Second", default);
            var third = CreateRecord<TestOperationalRecord>(organizationId, "C", "Third", default);
            var fourth = CreateRecord<TestOperationalRecord>(organizationId, "D", "Fourth", default);
            var fifth = CreateRecord<TestOperationalRecord>(organizationId, "E", "Fifth", default);
            var other = CreateRecord<TestOperationalRecord>(otherOrganizationId, "A", "Other", default);
            await store.UpsertBatchAsync(new[] { second, third, fourth, fifth, other });

            var firstPage = await store.QueryAsync(new StorageQuery<TestOperationalRecord>().Where(record => record.OrganizationId, StorageFilterOperator.Equal, organizationId).WithPage(new StoragePageRequest(pageSize: 2)));
            CollectionAssert.AreEqual(new[] { "A", "B" }, firstPage.Items.Select(record => record.Id).ToArray());
            Assert.IsTrue(firstPage.HasMoreRecords);

            var secondPage = await store.QueryAsync(new StorageQuery<TestOperationalRecord>().Where(record => record.OrganizationId, StorageFilterOperator.Equal, organizationId).WithPage(new StoragePageRequest(pageSize: 2, continuationToken: firstPage.ContinuationToken)));
            CollectionAssert.AreEqual(new[] { "C", "D" }, secondPage.Items.Select(record => record.Id).ToArray());
            Assert.IsTrue(secondPage.HasMoreRecords);

            var thirdPage = await store.QueryAsync(new StorageQuery<TestOperationalRecord>().Where(record => record.OrganizationId, StorageFilterOperator.Equal, organizationId).WithPage(new StoragePageRequest(pageSize: 2, continuationToken: secondPage.ContinuationToken)));
            CollectionAssert.AreEqual(new[] { "E" }, thirdPage.Items.Select(record => record.Id).ToArray());
            Assert.IsFalse(thirdPage.HasMoreRecords);

            await store.DeleteAsync(organizationId, first.Id);
            Assert.IsNull(await store.GetAsync(organizationId, first.Id));
            Assert.IsNotNull(await store.GetAsync(otherOrganizationId, other.Id));
        }

        [TestMethod]
        public async Task IndexedQueryAsync_UsesDeclaredSaiIndex()
        {
            var settings = new TestCassandraStorageSettings();
            using var factory = new CassandraSessionFactory(settings);
            using var services = CreateServices<IndexedOperationalRecord>(factory, definition => definition.Index(record => record.Status));
            var store = services.GetRequiredService<IOperationalDataStore<IndexedOperationalRecord>>();
            var organizationId = Guid.NewGuid().ToString("N").ToUpperInvariant();
            await store.UpsertBatchAsync(new[] { CreateIndexedRecord(organizationId, "A", "ready"), CreateIndexedRecord(organizationId, "B", "waiting"), CreateIndexedRecord(organizationId, "C", "ready") });

            var result = await store.QueryAsync(new StorageQuery<IndexedOperationalRecord>().Where(record => record.OrganizationId, StorageFilterOperator.Equal, organizationId).Where(record => record.Status, StorageFilterOperator.Equal, "ready").WithPage(new StoragePageRequest(pageSize: 10)));
            CollectionAssert.AreEqual(new[] { "A", "C" }, result.Items.Select(record => record.Id).ToArray());
        }

        [TestMethod]
        public async Task RetentionAsync_ExpiresOperationalRecords()
        {
            var settings = new TestCassandraStorageSettings();
            using var factory = new CassandraSessionFactory(settings);
            using var services = CreateServices<ExpiringOperationalRecord>(factory, definition => definition.RetainFor(TimeSpan.FromSeconds(2)));
            var store = services.GetRequiredService<IOperationalDataStore<ExpiringOperationalRecord>>();
            var organizationId = Guid.NewGuid().ToString("N").ToUpperInvariant();
            var record = CreateRecord<ExpiringOperationalRecord>(organizationId, "TTL", "Expires", default);

            await store.UpsertAsync(record);
            Assert.IsNotNull(await store.GetAsync(organizationId, record.Id));
            await Task.Delay(TimeSpan.FromSeconds(4));
            Assert.IsNull(await store.GetAsync(organizationId, record.Id));
        }

        [TestMethod]
        public async Task ScalarTypesAsync_RoundTripEverySupportedCassandraPropertyType()
        {
            var settings = new TestCassandraStorageSettings();
            using var factory = new CassandraSessionFactory(settings);
            using var services = CreateServices<ScalarOperationalRecord>(factory);
            var store = services.GetRequiredService<IOperationalDataStore<ScalarOperationalRecord>>();
            var organizationId = Guid.NewGuid().ToString("N").ToUpperInvariant();
            var guidValue = Guid.NewGuid();
            var dateTimeValue = new DateTime(2026, 8, 27, 15, 16, 17, 123, DateTimeKind.Utc);
            var dateTimeOffsetValue = new DateTimeOffset(2026, 8, 27, 11, 12, 13, 456, TimeSpan.Zero);
            var bytes = new byte[] { 0, 1, 2, 127, 128, 254, 255 };
            var record = new ScalarOperationalRecord
            {
                Id = "SCALAR",
                OrganizationId = organizationId,
                CreationDate = new DateTime(2026, 8, 27, 10, 0, 0, DateTimeKind.Utc),
                Value = "text-value",
                DateTimeValue = dateTimeValue,
                DateTimeOffsetValue = dateTimeOffsetValue,
                GuidValue = guidValue,
                BoolValue = true,
                IntValue = 123456,
                LongValue = 9876543210L,
                ShortValue = 12345,
                FloatValue = 12.5f,
                DoubleValue = 98.125d,
                DecimalValue = 123456.789m,
                BytesValue = bytes,
                NullableIntValue = null
            };

            await store.UpsertAsync(record);
            var loaded = await store.GetAsync(organizationId, record.Id);

            Assert.IsNotNull(loaded);
            Assert.AreEqual(record.Value, loaded.Value);
            Assert.AreEqual(dateTimeValue, loaded.DateTimeValue);
            Assert.AreEqual(dateTimeOffsetValue, loaded.DateTimeOffsetValue);
            Assert.AreEqual(guidValue, loaded.GuidValue);
            Assert.AreEqual(true, loaded.BoolValue);
            Assert.AreEqual(123456, loaded.IntValue);
            Assert.AreEqual(9876543210L, loaded.LongValue);
            Assert.AreEqual((short)12345, loaded.ShortValue);
            Assert.AreEqual(12.5f, loaded.FloatValue);
            Assert.AreEqual(98.125d, loaded.DoubleValue);
            Assert.AreEqual(123456.789m, loaded.DecimalValue);
            CollectionAssert.AreEqual(bytes, loaded.BytesValue);
            Assert.IsNull(loaded.NullableIntValue);
        }

        private static ServiceProvider CreateServices<TRecord>(ICassandraSessionFactory factory, Action<StorageDefinition<TRecord>> configure = null) where TRecord : class, IOperationalDataRecord, new()
        {
            var services = new ServiceCollection();
            services.AddSingleton(factory);
            services.AddOperationalDataStore<TRecord, CassandraOperationalDataStore<TRecord>>(configure);
            return services.BuildServiceProvider();
        }

        private static TRecord CreateRecord<TRecord>(string organizationId, string id, string value, DateTime creationDate) where TRecord : TestOperationalRecordBase, new()
        {
            return new TRecord { Id = id, OrganizationId = organizationId, Value = value, CreationDate = creationDate };
        }

        private static IndexedOperationalRecord CreateIndexedRecord(string organizationId, string id, string status)
        {
            return new IndexedOperationalRecord { Id = id, OrganizationId = organizationId, Value = id, Status = status };
        }

        private sealed class TestCassandraStorageSettings : ICassandraStorageSettings
        {
            public IReadOnlyList<string> ContactPoints { get; } = new[] { "127.0.0.1" };
            public string UserName => "cassandra";
            public string Password => "cassandra";
            public string Keyspace => "nuviot_storage_tests";
            public int Port => 19042;
            public string LocalDataCenter => "datacenter1";
        }

        public abstract class TestOperationalRecordBase : IOperationalDataRecord
        {
            public string Id { get; set; }
            public string OrganizationId { get; set; }
            public DateTime CreationDate { get; set; }
            public DateTime LastUpdatedDate { get; set; }
            public string Value { get; set; }
        }

        public sealed class TestOperationalRecord : TestOperationalRecordBase { }
        public sealed class ExpiringOperationalRecord : TestOperationalRecordBase { }

        public sealed class IndexedOperationalRecord : TestOperationalRecordBase
        {
            public string Status { get; set; }
        }

        public sealed class ScalarOperationalRecord : TestOperationalRecordBase
        {
            public DateTime DateTimeValue { get; set; }
            public DateTimeOffset DateTimeOffsetValue { get; set; }
            public Guid GuidValue { get; set; }
            public bool BoolValue { get; set; }
            public int IntValue { get; set; }
            public long LongValue { get; set; }
            public short ShortValue { get; set; }
            public float FloatValue { get; set; }
            public double DoubleValue { get; set; }
            public decimal DecimalValue { get; set; }
            public byte[] BytesValue { get; set; }
            public int? NullableIntValue { get; set; }
        }
    }
}
