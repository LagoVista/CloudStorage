using LagoVista.CloudStorage.Storage;
using LagoVista.CloudStorage.Storage.StorageProviders.Cassandra;
using LagoVista.CloudStorage.Storage.StorageProviders.Mongo;
using LagoVista.Core.Interfaces;
using LagoVista.Core.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Bson;
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
            var conflict = new ApplicationDataMutationResult(ApplicationDataMutationStatus.Conflict);

            applicationData
                .Setup(store => store.UpdateIfVersionAsync(record, stale, It.IsAny<CancellationToken>()))
                .ReturnsAsync(conflict);

            var store = new StorageRetentionPolicyStore(applicationData.Object);
            var result = await store.UpdateIfVersionAsync(record, stale);

            Assert.AreEqual(ApplicationDataMutationStatus.Conflict, result.Status);
            Assert.IsNull(result.ConcurrencyToken);
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

        public sealed class TestActivityRecord : IActivityRecord
        {
            public string Id { get; set; }
            public string OrganizationId { get; set; }
            public string Organization { get; set; }
            public DateTime CreationDate { get; set; }
        }
    }
}
