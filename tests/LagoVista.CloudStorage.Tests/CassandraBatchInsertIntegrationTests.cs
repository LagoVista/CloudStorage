using Cassandra;
using LagoVista.CloudStorage.Storage;
using LagoVista.Core.Interfaces;
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
    public class CassandraBatchInsertIntegrationTests
    {
        public sealed class BatchActivityRecord : IActivityRecord
        {
            public string Id { get; set; }
            public string OrganizationId { get; set; }
            public string Organization { get; set; }
            public DateTime CreationDate { get; set; }
            public int Value { get; set; }
        }

        private IServiceProvider _services;
        private IActivityRecordStore<BatchActivityRecord> _store;
        private ICassandraStorageSettings _settings;

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
            services.AddActivityRecordStore<BatchActivityRecord, CassandraActivityRecordStore<BatchActivityRecord>>(
                definition => definition.PartitionBy(record => record.OrganizationId));

            _services = services.BuildServiceProvider();
            _store = _services.GetRequiredService<IActivityRecordStore<BatchActivityRecord>>();
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            (_services as IDisposable)?.Dispose();
        }

        [Test]
        public async Task InsertBatch_AcrossMultiplePartitionsAndConcurrencyWaves_PersistsAllRecords()
        {
            var organizations = new[]
            {
                $"ORG-{Guid.NewGuid():N}",
                $"ORG-{Guid.NewGuid():N}",
                $"ORG-{Guid.NewGuid():N}"
            };
            var now = DateTime.UtcNow;
            var records = new List<BatchActivityRecord>();

            for (var index = 0; index < 40; index++)
            {
                records.Add(new BatchActivityRecord
                {
                    Id = Guid.NewGuid().ToString("N"),
                    OrganizationId = organizations[index % organizations.Length],
                    Organization = $"Batch Organization {index % organizations.Length}",
                    CreationDate = now.AddMilliseconds(index),
                    Value = index
                });
            }

            await _store.InsertBatchAsync(records);

            foreach (var organizationId in organizations)
            {
                var expected = records
                    .Where(record => record.OrganizationId == organizationId)
                    .OrderByDescending(record => record.CreationDate)
                    .Select(record => record.Id)
                    .ToList();

                var result = await _store.QueryAsync(
                    new HistoryQuery<BatchActivityRecord>()
                        .Where(record => record.OrganizationId, StorageFilterOperator.Equal, organizationId)
                        .WithPage(new StoragePageRequest(100)));

                Assert.That(result.Items.Select(record => record.Id), Is.EqualTo(expected));
            }
        }
    }
}
