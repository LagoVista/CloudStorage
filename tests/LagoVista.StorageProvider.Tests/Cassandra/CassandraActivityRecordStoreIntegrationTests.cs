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
    public class CassandraActivityRecordStoreIntegrationTests
    {
        [TestMethod]
        public async Task InsertAndQueryAsync_SatisfiesCoreActivityRecordContract()
        {
            var settings = new TestCassandraStorageSettings();
            using var factory = new CassandraSessionFactory(settings);
            using var services = CreateServices(factory);

            var store = services.GetRequiredService<IActivityRecordStore<TestActivityRecord>>();
            var organizationId = Guid.NewGuid().ToString("N").ToUpperInvariant();
            var otherOrganizationId = Guid.NewGuid().ToString("N").ToUpperInvariant();
            var start = new DateTime(2026, 8, 26, 12, 0, 0, DateTimeKind.Utc);

            var first = CreateRecord(organizationId, "First", start.AddMinutes(1));
            var second = CreateRecord(organizationId, "Second", start.AddMinutes(2));
            var third = CreateRecord(organizationId, "Third", start.AddMinutes(3));
            var fourth = CreateRecord(organizationId, "Fourth", start.AddMinutes(4));
            var fifth = CreateRecord(organizationId, "Fifth", start.AddMinutes(5));
            var other = CreateRecord(otherOrganizationId, "Other", start.AddMinutes(6));

            await store.InsertAsync(first);
            await store.InsertBatchAsync(new[] { second, third, fourth, fifth, other });

            var firstPage = await store.QueryAsync(
                new HistoryQuery<TestActivityRecord>()
                    .Between(start, start.AddMinutes(10))
                    .Where(record => record.OrganizationId, StorageFilterOperator.Equal, organizationId)
                    .WithPage(new StoragePageRequest(pageSize: 2)));

            Assert.AreEqual(2, firstPage.Items.Count);
            CollectionAssert.AreEqual(new[] { fifth.Id, fourth.Id }, firstPage.Items.Select(record => record.Id).ToArray());
            Assert.IsTrue(firstPage.HasMoreRecords);

            var secondPage = await store.QueryAsync(
                new HistoryQuery<TestActivityRecord>()
                    .Between(start, start.AddMinutes(10))
                    .Where(record => record.OrganizationId, StorageFilterOperator.Equal, organizationId)
                    .WithPage(new StoragePageRequest(pageSize: 2, continuationToken: firstPage.ContinuationToken)));

            Assert.AreEqual(2, secondPage.Items.Count);
            CollectionAssert.AreEqual(new[] { third.Id, second.Id }, secondPage.Items.Select(record => record.Id).ToArray());
            Assert.IsTrue(secondPage.HasMoreRecords);

            var thirdPage = await store.QueryAsync(
                new HistoryQuery<TestActivityRecord>()
                    .Between(start, start.AddMinutes(10))
                    .Where(record => record.OrganizationId, StorageFilterOperator.Equal, organizationId)
                    .WithPage(new StoragePageRequest(pageSize: 2, continuationToken: secondPage.ContinuationToken)));

            Assert.AreEqual(1, thirdPage.Items.Count);
            Assert.AreEqual(first.Id, thirdPage.Items.Single().Id);
            Assert.IsFalse(thirdPage.HasMoreRecords);

            var narrowRange = await store.QueryAsync(
                new HistoryQuery<TestActivityRecord>()
                    .Between(start.AddMinutes(2), start.AddMinutes(3))
                    .Where(record => record.OrganizationId, StorageFilterOperator.Equal, organizationId)
                    .WithPage(new StoragePageRequest(pageSize: 10)));

            CollectionAssert.AreEqual(new[] { third.Id, second.Id }, narrowRange.Items.Select(record => record.Id).ToArray());

            var session = await factory.GetSessionAsync();
            var tableRows = await session.ExecuteAsync(new global::Cassandra.SimpleStatement("SELECT table_name FROM system_schema.tables WHERE keyspace_name = ? AND table_name = ?", settings.Keyspace, "test_activity_record"));

            Assert.IsNotNull(tableRows.FirstOrDefault());
        }

        private static ServiceProvider CreateServices(ICassandraSessionFactory factory)
        {
            var services = new ServiceCollection();
            services.AddSingleton(factory);
            services.AddActivityRecordStore<TestActivityRecord, CassandraActivityRecordStore<TestActivityRecord>>(definition => definition.PartitionBy(record => record.OrganizationId));
            return services.BuildServiceProvider();
        }

        private static TestActivityRecord CreateRecord(string organizationId, string message, DateTime creationDate)
        {
            return new TestActivityRecord
            {
                Id = Guid.NewGuid().ToString("N").ToUpperInvariant(),
                OrganizationId = organizationId,
                Organization = "Contract Organization",
                CreationDate = creationDate,
                Message = message
            };
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

        public sealed class TestActivityRecord : IActivityRecord
        {
            public string Id { get; set; }
            public string OrganizationId { get; set; }
            public string Organization { get; set; }
            public DateTime CreationDate { get; set; }
            public string Message { get; set; }
        }
    }
}
