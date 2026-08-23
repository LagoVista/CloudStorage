using Cassandra;
using LagoVista.CloudStorage.Storage;
using NUnit.Framework;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace LagoVista.CloudStorage.Tests
{
    [NonParallelizable]
    [Category("Integration")]
    [Category("Cassandra")]
    public class CassandraConnectionIntegrationTests
    {
        private const string TableName = "activity_record_smoke";
        private ICluster _cluster;
        private ISession _session;
        private ICassandraStorageSettings _settings;

        private sealed class TestActivityRecord : IActivityRecord
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

            _session = await _cluster.ConnectAsync();
            await _session.ExecuteAsync(new SimpleStatement(
                $"CREATE KEYSPACE IF NOT EXISTS {_settings.Keyspace} WITH replication = {{'class':'SimpleStrategy','replication_factor':1}}"));

            _session.ChangeKeyspace(_settings.Keyspace);

            await _session.ExecuteAsync(new SimpleStatement($@"
CREATE TABLE IF NOT EXISTS {TableName} (
    organization_id text,
    id text,
    organization text,
    creation_date timestamp,
    PRIMARY KEY (organization_id, id)
)"));
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            _session?.Dispose();
            _cluster?.Dispose();
        }

        [Test]
        public async Task DockerCassandra_CanInsertAndReadActivityRecord()
        {
            var record = new TestActivityRecord
            {
                Id = Guid.NewGuid().ToString("N"),
                OrganizationId = "ORG1",
                Organization = "Test Organization",
                CreationDate = DateTime.UtcNow
            };

            var insert = _session.Prepare($@"
INSERT INTO {TableName} (organization_id, id, organization, creation_date)
VALUES (?, ?, ?, ?)");

            await _session.ExecuteAsync(insert.Bind(
                record.OrganizationId,
                record.Id,
                record.Organization,
                new DateTimeOffset(record.CreationDate)));

            var select = _session.Prepare($@"
SELECT organization_id, id, organization, creation_date
FROM {TableName}
WHERE organization_id = ? AND id = ?");

            var result = await _session.ExecuteAsync(select.Bind(record.OrganizationId, record.Id));
            var row = result.Single();

            Assert.That(row.GetValue<string>("organization_id"), Is.EqualTo(record.OrganizationId));
            Assert.That(row.GetValue<string>("id"), Is.EqualTo(record.Id));
            Assert.That(row.GetValue<string>("organization"), Is.EqualTo(record.Organization));

            var creationDate = row.GetValue<DateTimeOffset>("creation_date").UtcDateTime;
            Assert.That(Math.Abs((creationDate - record.CreationDate).TotalMilliseconds), Is.LessThan(5));
        }
    }
}
