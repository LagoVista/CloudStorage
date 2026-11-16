using LagoVista.CloudStorage.Storage;
using LagoVista.CloudStorage.Storage.StorageProviders.Cassandra;
using LagoVista.CloudStorage.Storage.StorageProviders.Mongo;
using LagoVista.Core.Exceptions;
using LagoVista.Core.Interfaces;
using LagoVista.Core.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using LagoVista.CloudStorage.Storage.StorageProviders.Mongo;
using Moq;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.StorageProvider.Tests
{
    [TestClass]
    public sealed class StorageRetentionPolicyTests
    {
        [TestMethod]
        public void Resolve_UsesScopedOverrideBeforeRecordClassDefault()
        {
            var policy = new StorageRetentionPolicy()
                .AddDefault(StorageRecordClass.ActivityRecord, TimeSpan.FromDays(30), summarizeBeforeExpiry: true)
                .AddOverride(StorageRecordClass.ActivityRecord, "ORG-A", TimeSpan.FromDays(7));

            var scoped = policy.Resolve(StorageRecordClass.ActivityRecord, "ORG-A");
            var fallback = policy.Resolve(StorageRecordClass.ActivityRecord, "ORG-B");

            Assert.AreEqual(TimeSpan.FromDays(7), scoped.EffectiveTtl);
            Assert.AreEqual("scoped-override", scoped.Source);
            Assert.AreEqual(TimeSpan.FromDays(30), fallback.EffectiveTtl);
            Assert.IsTrue(fallback.SummarizeBeforeExpiry);
        }

        [TestMethod]
        public void Resolve_ProtectedOverrideIsNonExpiring()
        {
            var policy = new StorageRetentionPolicy()
                .AddDefault(StorageRecordClass.Scratch, TimeSpan.FromHours(24))
                .AddOverride(StorageRecordClass.Scratch, "LEGAL-HOLD", isProtected: true, summarizeBeforeExpiry: true);

            var decision = policy.Resolve(StorageRecordClass.Scratch, "LEGAL-HOLD");

            Assert.IsTrue(decision.IsProtected);
            Assert.IsNull(decision.EffectiveTtl);
            Assert.IsTrue(decision.SummarizeBeforeExpiry);
            Assert.IsTrue(decision.ExplicitlyGoverned);
        }

        [TestMethod]
        public void Resolve_UnconfiguredApplicationDataIsDurable()
        {
            var decision = new StorageRetentionPolicy().Resolve(StorageRecordClass.ApplicationData, "ORG-A");

            Assert.IsNull(decision.EffectiveTtl);
            Assert.IsFalse(decision.ExplicitlyGoverned);
            Assert.IsFalse(decision.IsProtected);
        }

        [TestMethod]
        public void PolicyRejectsProtectedRuleWithTtl()
        {
            Assert.ThrowsExactly<InvalidOperationException>(() =>
                new StorageRetentionPolicy().AddDefault(
                    StorageRecordClass.ActivityRecord,
                    TimeSpan.FromDays(1),
                    isProtected: true));
        }

        [TestMethod]
        public async Task PolicyStore_ForwardsVersionedReadAndConcurrencyToken()
        {
            var applicationData = new Mock<IApplicationDataStore>(MockBehavior.Strict);
            var key = new StorageKey("POLICY", "ORG-A");
            var record = new StorageRetentionPolicyRecord();
            var token = new ApplicationDataConcurrencyToken("version-1");
            var versioned = new VersionedApplicationDataRecord<StorageRetentionPolicyRecord>(record, token);

            applicationData
                .Setup(store => store.GetVersionedAsync<StorageRetentionPolicyRecord>(key, It.IsAny<CancellationToken>()))
                .ReturnsAsync(versioned);

            var store = new StorageRetentionPolicyStore(applicationData.Object);
            var result = await store.GetVersionedAsync(key);

            Assert.AreSame(record, result.Record);
            Assert.AreSame(token, result.ConcurrencyToken);
            applicationData.VerifyAll();
        }

        [TestMethod]
        public async Task PolicyStore_PropagatesConflictForStaleVersion()
        {
            var applicationData = new Mock<IApplicationDataStore>(MockBehavior.Strict);
            var record = new StorageRetentionPolicyRecord();
            var stale = new ApplicationDataConcurrencyToken("stale-version");
            applicationData
                .Setup(store => store.UpdateIfVersionAsync(record, stale, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new RecordConflictException(nameof(StorageRetentionPolicyRecord), record.Id.Value, stale.Value));

            var store = new StorageRetentionPolicyStore(applicationData.Object);
            await Assert.ThrowsExceptionAsync<RecordConflictException>(
                async () => await store.UpdateIfVersionAsync(record, stale));
            applicationData.VerifyAll();
        }

        [TestMethod]
        public void CassandraActivityRetention_MaterializesScopedAndDefaultTtlSeconds()
        {
            var policy = new StorageRetentionPolicy()
                .AddDefault(StorageRecordClass.ActivityRecord, TimeSpan.FromDays(30))
                .AddOverride(StorageRecordClass.ActivityRecord, "ORG-A", TimeSpan.FromDays(7));
            var definition = new StorageDefinition<TestActivityRecord>()
                .KeyBy(record => record.Id)
                .TimeBy(record => record.CreationDate)
                .PartitionBy(record => record.OrganizationId)
                .UseRetentionPolicy(StorageRecordClass.ActivityRecord, policy);
            var map = new CassandraRecordMap<TestActivityRecord>(new ActivityRecordStoreOptions<TestActivityRecord>(definition));

            Assert.AreEqual(604800, map.ResolveRetentionSeconds("ORG-A"));
            Assert.AreEqual(2592000, map.ResolveRetentionSeconds("ORG-B"));
            StringAssert.EndsWith(map.InsertCql(perWriteTtl: true), " USING TTL ?");
        }

        [TestMethod]
        public void CassandraActivityRetention_ProtectedScopeProducesNoPerWriteTtlValue()
        {
            var policy = new StorageRetentionPolicy()
                .AddDefault(StorageRecordClass.ActivityRecord, TimeSpan.FromDays(30))
                .AddOverride(StorageRecordClass.ActivityRecord, "LEGAL-HOLD", isProtected: true);
            var definition = new StorageDefinition<TestActivityRecord>()
                .KeyBy(record => record.Id)
                .TimeBy(record => record.CreationDate)
                .PartitionBy(record => record.OrganizationId)
                .UseRetentionPolicy(StorageRecordClass.ActivityRecord, policy);
            var map = new CassandraRecordMap<TestActivityRecord>(new ActivityRecordStoreOptions<TestActivityRecord>(definition));

            Assert.IsNull(map.ResolveRetentionSeconds("LEGAL-HOLD"));
        }

        [TestMethod]
        public void CassandraActivityEnumFields_AreMappedToTextAndNamedValues()
        {
            var definition = new StorageDefinition<EnumActivityRecord>()
                .KeyBy(record => record.Id)
                .TimeBy(record => record.CreationDate)
                .PartitionBy(record => record.OrganizationId);
            var map = new CassandraRecordMap<EnumActivityRecord>(
                new ActivityRecordStoreOptions<EnumActivityRecord>(definition));
            var record = new EnumActivityRecord
            {
                Id = "ACT-1",
                OrganizationId = "ORG-A",
                Organization = "Organization A",
                CreationDate = new DateTime(2026, 11, 4, 18, 0, 0, DateTimeKind.Utc),
                Action = TestActivityAction.Publish,
                AcceptanceState = TestActivityAcceptance.Uncertain,
                OptionalAction = null
            };

            var actionProperty = map.GetRequired(nameof(EnumActivityRecord.Action));
            var acceptanceProperty = map.GetRequired(nameof(EnumActivityRecord.AcceptanceState));
            var optionalProperty = map.GetRequired(nameof(EnumActivityRecord.OptionalAction));
            Assert.AreEqual("text", actionProperty.CqlType);
            Assert.AreEqual("text", acceptanceProperty.CqlType);
            Assert.AreEqual("text", optionalProperty.CqlType);
            Assert.AreEqual("Publish", map.DriverValue(actionProperty, record.Action));
            Assert.AreEqual("Uncertain", map.DriverValue(acceptanceProperty, record.AcceptanceState));
            Assert.IsNull(map.DriverValue(optionalProperty, record.OptionalAction));
            StringAssert.Contains(map.CreateTableCql(), "\"action\" text");
            StringAssert.Contains(map.CreateTableCql(), "\"acceptance_state\" text");
            StringAssert.Contains(map.CreateTableCql(), "\"optional_action\" text");
        }

        [TestMethod]
        public void MongoExpirationMaterialization_AddsExpirationForGovernedTtl()
        {
            var now = new DateTime(2026, 9, 29, 12, 0, 0, DateTimeKind.Utc);
            var document = new BsonDocument("value", 1);

            MongoMutableRecordStore.MaterializeExpiration(document, TimeSpan.FromHours(24), now);

            Assert.IsTrue(document.Contains("_storageExpiresUtc"));
            Assert.AreEqual(now.AddHours(24), document["_storageExpiresUtc"].ToUniversalTime());
        }

        [TestMethod]
        public void MongoExpirationMaterialization_RemovesExpirationForDurableApplicationData()
        {
            var now = new DateTime(2026, 9, 29, 12, 0, 0, DateTimeKind.Utc);
            var document = new BsonDocument
            {
                { "value", 1 },
                { "_storageExpiresUtc", new BsonDateTime(now.AddHours(1)) }
            };

            MongoMutableRecordStore.MaterializeExpiration(document, null, now);

            Assert.IsFalse(document.Contains("_storageExpiresUtc"));
        }


        [TestMethod]
        public void CassandraReservedIdentifiers_QuoteEveryOperationalSchemaAndWriteColumn()
        {
            var definition = new StorageDefinition<ReservedWordOperationalRecord>()
                .PartitionBy(record => record.OrganizationId)
                .KeyBy(record => record.Id)
                .Index(record => record.Create);
            var map = new CassandraOperationalRecordMap<ReservedWordOperationalRecord>(
                new OperationalDataStoreOptions<ReservedWordOperationalRecord>(storage => storage
                    .PartitionBy(record => record.OrganizationId)
                    .KeyBy(record => record.Id)
                    .Index(record => record.Create)));

            var schema = map.CreateTableCql();
            StringAssert.Contains(schema, "\"create\" int");
            StringAssert.Contains(schema, "\"read\" int");
            StringAssert.Contains(schema, "\"update\" int");
            StringAssert.Contains(schema, "\"delete\" int");
            StringAssert.Contains(schema, "PRIMARY KEY ((\"organization_id\"), \"id\")");
            StringAssert.Contains(map.UpsertCql(), "(\"organization_id\",");
            StringAssert.Contains(map.UpsertCql(), "\"create\"");
            StringAssert.Contains(map.ReconcileRetentionCql(), "ALTER TABLE \"reserved_word_operational_record\"");
            Assert.AreEqual("\"create\"", map.GetRequired(nameof(ReservedWordOperationalRecord.Create)).CqlIdentifier);
        }

        [TestMethod]
        public void CassandraReservedIdentifiers_QuoteActivitySchemaAndInsertColumns()
        {
            var definition = new StorageDefinition<ReservedWordActivityRecord>()
                .PartitionBy(record => record.OrganizationId)
                .KeyBy(record => record.Id)
                .TimeBy(record => record.CreationDate);
            var map = new CassandraRecordMap<ReservedWordActivityRecord>(
                new ActivityRecordStoreOptions<ReservedWordActivityRecord>(definition));

            StringAssert.Contains(map.CreateTableCql(), "\"create\" int");
            StringAssert.Contains(map.CreateTableCql(), "\"read\" int");
            StringAssert.Contains(map.InsertCql(), "\"create\"");
            StringAssert.Contains(map.InsertCql(), "\"read\"");
            Assert.IsTrue(CassandraCqlIdentifier.TargetMatches("\"create\"", "create"));
            Assert.IsTrue(CassandraCqlIdentifier.TargetMatches("create", "create"));
            Assert.IsFalse(CassandraCqlIdentifier.TargetMatches("other", "create"));
            Assert.ThrowsExactly<ArgumentException>(() => CassandraCqlIdentifier.Quote("invalid-column"));
        }

        public sealed class ReservedWordOperationalRecord : IOperationalDataRecord
        {
            public string OrganizationId { get; set; }
            public string Id { get; set; }
            public DateTime CreationDate { get; set; }
            public DateTime LastUpdatedDate { get; set; }
            public int Create { get; set; }
            public int Read { get; set; }
            public int Update { get; set; }
            public int Delete { get; set; }
        }

        public sealed class ReservedWordActivityRecord : IActivityRecord
        {
            public string OrganizationId { get; set; }
            public string Id { get; set; }
            public string Organization { get; set; }
            public DateTime CreationDate { get; set; }
            public int Create { get; set; }
            public int Read { get; set; }
        }


        [TestMethod]
        public void MongoUtcTimestamp_ReadsLegacyMigrationRoundTripDateWithoutChangingWireContract()
        {
            MongoBsonSerialization.Configure();
            var legacy = new BsonDocument
            {
                { "CreationDate", "2024-03-17T12:23:37.0100000Z" }
            };
            var actual = BsonSerializer.Deserialize<LegacyMigrationTimeRecord>(legacy);
            Assert.AreEqual("2024-03-17T12:23:37.010Z", actual.CreationDate.ToString());

            var roundtrip = actual.ToBsonDocument();
            Assert.AreEqual("2024-03-17T12:23:37.010Z", roundtrip["CreationDate"].AsString);
        }

        [TestMethod]
        public void MongoUtcTimestamp_StillRejectsUntrustedNonUtcOrMalformedValues()
        {
            Assert.ThrowsExactly<FormatException>(() =>
                UtcTimestampBsonSerializer.ParsePersistedUtc("2024-03-17T12:23:37.0100000+00:00"));
            Assert.ThrowsExactly<FormatException>(() =>
                UtcTimestampBsonSerializer.ParsePersistedUtc("2024-03-17T12:23:37.0100000Zextra"));
        }

        public sealed class LegacyMigrationTimeRecord
        {
            public UtcTimestamp CreationDate { get; set; }
        }

        public enum TestActivityAction
        {
            Publish,
            Pause
        }

        public enum TestActivityAcceptance
        {
            Accepted,
            Uncertain
        }

        public sealed class EnumActivityRecord : IActivityRecord
        {
            public string Id { get; set; }
            public string OrganizationId { get; set; }
            public string Organization { get; set; }
            public DateTime CreationDate { get; set; }
            public TestActivityAction Action { get; set; }
            public TestActivityAcceptance AcceptanceState { get; set; }
            public TestActivityAction? OptionalAction { get; set; }
        }

        public sealed class TestActivityRecord : IActivityRecord
        {
            public string Id { get; set; }
            public string OrganizationId { get; set; }
            public string Organization { get; set; }
            public DateTime CreationDate { get; set; }
        }
    }
}
