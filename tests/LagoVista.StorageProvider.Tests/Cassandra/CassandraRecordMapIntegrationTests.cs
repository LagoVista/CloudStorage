using LagoVista.CloudStorage.Storage;
using LagoVista.CloudStorage.Storage.ConnectionSettings;
using LagoVista.CloudStorage.Storage.StorageProviders.Cassandra;
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
    public class CassandraRecordMapIntegrationTests
    {
        [TestMethod]
        public async Task SupportedScalarTypes_RoundTripThroughCassandra()
        {
            var settings = new TestCassandraStorageSettings();
            using var factory = new CassandraSessionFactory(settings);
            using var services = CreateServices<CassandraTypeMatrixRecord>(factory, definition => definition.PartitionBy(record => record.OrganizationId));
            var store = services.GetRequiredService<IActivityRecordStore<CassandraTypeMatrixRecord>>();
            var organizationId = Guid.NewGuid().ToString("N").ToUpperInvariant();
            var creationDate = new DateTime(2026, 8, 26, 18, 30, 0, DateTimeKind.Utc);
            var timestampOffset = new DateTimeOffset(2026, 8, 26, 18, 31, 0, TimeSpan.Zero);
            var guidValue = Guid.NewGuid();
            var blobValue = new byte[] { 1, 2, 3, 4, 5 };
            var record = new CassandraTypeMatrixRecord
            {
                Id = Guid.NewGuid().ToString("N").ToUpperInvariant(),
                OrganizationId = organizationId,
                Organization = "Type Matrix Organization",
                CreationDate = creationDate,
                TextValue = "round-trip",
                DateTimeOffsetValue = timestampOffset,
                GuidValue = guidValue,
                BoolValue = true,
                IntValue = 42,
                LongValue = 9876543210L,
                ShortValue = 123,
                FloatValue = 12.5f,
                DoubleValue = 1234.5678,
                DecimalValue = 9876.5432m,
                BlobValue = blobValue,
                NullableIntValue = null
            };

            await store.InsertAsync(record);

            var result = await store.QueryAsync(new HistoryQuery<CassandraTypeMatrixRecord>().Between(creationDate.AddMinutes(-1), creationDate.AddMinutes(1)).Where(item => item.OrganizationId, StorageFilterOperator.Equal, organizationId).WithPage(new StoragePageRequest(pageSize: 10)));
            var loaded = result.Items.Single();

            Assert.AreEqual(record.Id, loaded.Id);
            Assert.AreEqual(record.OrganizationId, loaded.OrganizationId);
            Assert.AreEqual(record.Organization, loaded.Organization);
            Assert.AreEqual(creationDate, loaded.CreationDate);
            Assert.AreEqual("round-trip", loaded.TextValue);
            Assert.AreEqual(timestampOffset, loaded.DateTimeOffsetValue);
            Assert.AreEqual(guidValue, loaded.GuidValue);
            Assert.IsTrue(loaded.BoolValue);
            Assert.AreEqual(42, loaded.IntValue);
            Assert.AreEqual(9876543210L, loaded.LongValue);
            Assert.AreEqual((short)123, loaded.ShortValue);
            Assert.AreEqual(12.5f, loaded.FloatValue);
            Assert.AreEqual(1234.5678, loaded.DoubleValue);
            Assert.AreEqual(9876.5432m, loaded.DecimalValue);
            CollectionAssert.AreEqual(blobValue, loaded.BlobValue);
            Assert.IsNull(loaded.NullableIntValue);
        }

        [TestMethod]
        public void UnsupportedPropertyType_FailsDuringStoreConstruction()
        {
            var settings = new TestCassandraStorageSettings();
            using var factory = new CassandraSessionFactory(settings);
            var services = new ServiceCollection();
            services.AddSingleton<ICassandraSessionFactory>(factory);
            services.AddActivityRecordStore<UnsupportedTypeActivityRecord, CassandraActivityRecordStore<UnsupportedTypeActivityRecord>>(definition => definition.PartitionBy(record => record.OrganizationId));
            using var provider = services.BuildServiceProvider();

            Assert.ThrowsExactly<NotSupportedException>(() => provider.GetRequiredService<IActivityRecordStore<UnsupportedTypeActivityRecord>>());
        }

        private static ServiceProvider CreateServices<TRecord>(ICassandraSessionFactory factory, Action<StorageDefinition<TRecord>> configure) where TRecord : IActivityRecord, new()
        {
            var services = new ServiceCollection();
            services.AddSingleton<ICassandraSessionFactory>(factory);
            services.AddActivityRecordStore<TRecord, CassandraActivityRecordStore<TRecord>>(configure);
            return services.BuildServiceProvider();
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

        public sealed class CassandraTypeMatrixRecord : IActivityRecord
        {
            public string Id { get; set; }
            public string OrganizationId { get; set; }
            public string Organization { get; set; }
            public DateTime CreationDate { get; set; }
            public string TextValue { get; set; }
            public DateTimeOffset DateTimeOffsetValue { get; set; }
            public Guid GuidValue { get; set; }
            public bool BoolValue { get; set; }
            public int IntValue { get; set; }
            public long LongValue { get; set; }
            public short ShortValue { get; set; }
            public float FloatValue { get; set; }
            public double DoubleValue { get; set; }
            public decimal DecimalValue { get; set; }
            public byte[] BlobValue { get; set; }
            public int? NullableIntValue { get; set; }
        }

        public sealed class UnsupportedTypeActivityRecord : IActivityRecord
        {
            public string Id { get; set; }
            public string OrganizationId { get; set; }
            public string Organization { get; set; }
            public DateTime CreationDate { get; set; }
            public TimeSpan UnsupportedValue { get; set; }
        }
    }
}
