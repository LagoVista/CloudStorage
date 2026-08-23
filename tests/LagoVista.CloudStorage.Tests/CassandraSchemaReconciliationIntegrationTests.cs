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
    public class CassandraSchemaReconciliationIntegrationTests
    {
        private ICluster _cluster;
        private ISession _session;
        private ICassandraStorageSettings _settings;

        public sealed class AdditiveSchemaActivityRecord : IActivityRecord
        {
            public string Id { get; set; }
            public string OrganizationId { get; set; }
            public string Organization { get; set; }
            public DateTime CreationDate { get; set; }
            public string Category { get; set; }
            public int Value { get; set; }
        }

        public sealed class IdempotentSchemaActivityRecord : IActivityRecord
        {
            public string Id { get; set; }
            public string OrganizationId { get; set; }
            public string Organization { get; set; }
            public DateTime CreationDate { get; set; }
            public string Category { get; set; }
            public int Value { get; set; }
        }

        public sealed class WrongTypeActivityRecord : IActivityRecord
        {
            public string Id { get; set; }
            public string OrganizationId { get; set; }
            public string Organization { get; set; }
            public DateTime CreationDate { get; set; }
            public int Value { get; set; }
        }

        public sealed class WrongPrimaryKeyActivityRecord : IActivityRecord
        {
            public string Id { get; set; }
            public string OrganizationId { get; set; }
            public string Organization { get; set; }
            public DateTime CreationDate { get; set; }
        }

        [OneTimeSetUp]
        public async Task SetupAsync()
        {
            _settings = CoreStorageTestConnections.Cassandra;
            _cluster = Cluster.Builder()
                .AddContactPoints(_settings.ContactPoints)
                .WithPort(_settings.Port)
                .WithCredentials(_settings.UserName, _settings.Password)
                .Build();

            var bootstrap = await _cluster.ConnectAsync();
            await bootstrap.ExecuteAsync(new SimpleStatement(
                $"CREATE KEYSPACE IF NOT EXISTS {_settings.Keyspace} WITH replication = {{'class':'SimpleStrategy','replication_factor':1}}"));
            bootstrap.Dispose();

            _session = await _cluster.ConnectAsync(_settings.Keyspace);

            await RecreateTableAsync("additive_schema_activity_record", @"
CREATE TABLE additive_schema_activity_record (
    organization_id text,
    creation_date timestamp,
    id text,
    organization text,
    category text,
    legacy_note text,
    PRIMARY KEY ((organization_id), creation_date, id)
) WITH CLUSTERING ORDER BY (creation_date DESC, id ASC)");

            await RecreateTableAsync("idempotent_schema_activity_record", @"
CREATE TABLE idempotent_schema_activity_record (
    organization_id text,
    creation_date timestamp,
    id text,
    organization text,
    category text,
    value int,
    PRIMARY KEY ((organization_id), creation_date, id)
) WITH CLUSTERING ORDER BY (creation_date DESC, id ASC)");

            await RecreateTableAsync("wrong_type_activity_record", @"
CREATE TABLE wrong_type_activity_record (
    organization_id text,
    creation_date timestamp,
    id text,
    organization text,
    value text,
    PRIMARY KEY ((organization_id), creation_date, id)
) WITH CLUSTERING ORDER BY (creation_date DESC, id ASC)");

            await RecreateTableAsync("wrong_primary_key_activity_record", @"
CREATE TABLE wrong_primary_key_activity_record (
    organization text,
    creation_date timestamp,
    id text,
    organization_id text,
    PRIMARY KEY ((organization), creation_date, id)
) WITH CLUSTERING ORDER BY (creation_date DESC, id ASC)");
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            _session?.Dispose();
            _cluster?.Dispose();
        }

        [Test]
        public async Task MissingRegularColumn_IsAddedAndLegacyColumnIsPreserved()
        {
            using (var provider = BuildProvider<AdditiveSchemaActivityRecord>())
            {
                var store = provider.GetRequiredService<IActivityRecordStore<AdditiveSchemaActivityRecord>>();
                await store.InsertAsync(new AdditiveSchemaActivityRecord
                {
                    Id = Guid.NewGuid().ToString("N"),
                    OrganizationId = $"ORG-{Guid.NewGuid():N}",
                    Organization = "Schema Reconciliation",
                    CreationDate = DateTime.UtcNow,
                    Category = "additive",
                    Value = 42
                });
            }

            var columns = (await ReadColumnsAsync("additive_schema_activity_record")).ToList();
            Assert.Multiple(() =>
            {
                Assert.That(columns.Any(row => row.GetValue<string>("column_name") == "value" && row.GetValue<string>("type") == "int"), Is.True);
                Assert.That(columns.Any(row => row.GetValue<string>("column_name") == "legacy_note"), Is.True);
            });
        }

        [Test]
        public async Task Reconciliation_IsIdempotentOnFreshStoreInstance()
        {
            using (var firstProvider = BuildProvider<IdempotentSchemaActivityRecord>())
            {
                var firstStore = firstProvider.GetRequiredService<IActivityRecordStore<IdempotentSchemaActivityRecord>>();
                await firstStore.InsertAsync(CreateIdempotentRecord(1));
            }

            using (var secondProvider = BuildProvider<IdempotentSchemaActivityRecord>())
            {
                var secondStore = secondProvider.GetRequiredService<IActivityRecordStore<IdempotentSchemaActivityRecord>>();
                Assert.DoesNotThrowAsync(() => secondStore.InsertAsync(CreateIdempotentRecord(2)));
            }
        }

        [Test]
        public void ExistingColumnWithWrongType_FailsLoudly()
        {
            using (var provider = BuildProvider<WrongTypeActivityRecord>())
            {
                var store = provider.GetRequiredService<IActivityRecordStore<WrongTypeActivityRecord>>();
                var exception = Assert.ThrowsAsync<InvalidOperationException>(() => store.InsertAsync(new WrongTypeActivityRecord
                {
                    Id = Guid.NewGuid().ToString("N"),
                    OrganizationId = $"ORG-{Guid.NewGuid():N}",
                    Organization = "Wrong Type",
                    CreationDate = DateTime.UtcNow,
                    Value = 42
                }));
                Assert.That(exception.Message, Does.Contain("Type changes require an explicit migration"));
            }
        }

        [Test]
        public void ExistingPrimaryKeyWithWrongShape_FailsLoudly()
        {
            using (var provider = BuildProvider<WrongPrimaryKeyActivityRecord>())
            {
                var store = provider.GetRequiredService<IActivityRecordStore<WrongPrimaryKeyActivityRecord>>();
                var exception = Assert.ThrowsAsync<InvalidOperationException>(() => store.InsertAsync(new WrongPrimaryKeyActivityRecord
                {
                    Id = Guid.NewGuid().ToString("N"),
                    OrganizationId = $"ORG-{Guid.NewGuid():N}",
                    Organization = "Wrong Primary Key",
                    CreationDate = DateTime.UtcNow
                }));
                Assert.That(exception.Message, Does.Contain("Primary-key changes require an explicit migration"));
            }
        }

        private ServiceProvider BuildProvider<TRecord>() where TRecord : IActivityRecord, new()
        {
            var services = new ServiceCollection();
            services.AddSingleton(_settings);
            services.AddSingleton<ICassandraStorageSettings>(_settings);
            services.AddCassandraStorageConnection();
            services.AddActivityRecordStore<TRecord, CassandraActivityRecordStore<TRecord>>(
                definition => definition.PartitionBy(record => record.OrganizationId));
            return services.BuildServiceProvider();
        }

        private async Task RecreateTableAsync(string tableName, string createCql)
        {
            await _session.ExecuteAsync(new SimpleStatement($"DROP TABLE IF EXISTS {tableName}"));
            await _session.ExecuteAsync(new SimpleStatement(createCql));
        }

        private async Task<RowSet> ReadColumnsAsync(string tableName)
        {
            var prepared = await _session.PrepareAsync(@"
SELECT column_name, type, kind, position
FROM system_schema.columns
WHERE keyspace_name = ? AND table_name = ?");
            return await _session.ExecuteAsync(prepared.Bind(_settings.Keyspace, tableName));
        }

        private static IdempotentSchemaActivityRecord CreateIdempotentRecord(int value)
        {
            return new IdempotentSchemaActivityRecord
            {
                Id = Guid.NewGuid().ToString("N"),
                OrganizationId = $"ORG-{Guid.NewGuid():N}",
                Organization = "Schema Reconciliation",
                CreationDate = DateTime.UtcNow,
                Category = "idempotent",
                Value = value
            };
        }
    }
}
