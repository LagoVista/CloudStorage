using LagoVista.CloudStorage.Storage.Migration;
using LagoVista.Core;
using LagoVista.Core.Models;
using LagoVista.Core.Models.UIMetaData;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Bson;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace LagoVista.StorageProvider.Tests.Migration
{
    [TestClass]
    public class DocumentMigrationTransformerTests
    {
        private sealed class TestEntityTypeResolver : IEntityTypeResolver
        {
            public Type GetEntityType(string entityType)
            {
                if (TryGetEntityType(entityType, out var modelType)) return modelType;
                throw new KeyNotFoundException(entityType);
            }

            public bool TryGetEntityType(string entityType, out Type modelType)
            {
                if (String.Equals(entityType, nameof(MigrationTestEntity), StringComparison.OrdinalIgnoreCase))
                {
                    modelType = typeof(MigrationTestEntity);
                    return true;
                }

                if (String.Equals(entityType, nameof(MigrationShadowEntity), StringComparison.OrdinalIgnoreCase))
                {
                    modelType = typeof(MigrationShadowEntity);
                    return true;
                }

                if (String.Equals(entityType, nameof(MigrationJObjectEntity), StringComparison.OrdinalIgnoreCase))
                {
                    modelType = typeof(MigrationJObjectEntity);
                    return true;
                }

                if (String.Equals(entityType, nameof(MigrationEntityBase), StringComparison.OrdinalIgnoreCase))
                {
                    modelType = typeof(MigrationEntityBase);
                    return true;
                }

                if (String.Equals(entityType, nameof(MigrationTimestampEntity), StringComparison.OrdinalIgnoreCase))
                {
                    modelType = typeof(MigrationTimestampEntity);
                    return true;
                }

                if (String.Equals(entityType, nameof(MigrationLegacyGuidEntity), StringComparison.OrdinalIgnoreCase))
                {
                    modelType = typeof(MigrationLegacyGuidEntity);
                    return true;
                }

                if (String.Equals(entityType, nameof(MigrationStateSetEntity), StringComparison.OrdinalIgnoreCase))
                {
                    modelType = typeof(MigrationStateSetEntity);
                    return true;
                }

                if (String.Equals(entityType, nameof(MigrationOrgNamespaceEntity), StringComparison.OrdinalIgnoreCase))
                {
                    modelType = typeof(MigrationOrgNamespaceEntity);
                    return true;
                }

                modelType = null;
                return false;
            }
        }

        private sealed class MigrationTestEntity
        {
            public string Id { get; set; }
            public string EntityType { get; set; }
            public string Name { get; set; }
            public MigrationTestHeader Status { get; set; }

            public string DisplayName => Name?.ToUpperInvariant();
        }

        private sealed class MigrationTestHeader
        {
            public int Value { get; set; }
            public string Text { get; set; }
            public bool HasValue => Value != 0;
        }

        private class MigrationShadowBase
        {
            public string Key { get; set; }
            public string BaseValue { get; set; }
        }

        private sealed class MigrationShadowDerived : MigrationShadowBase
        {
            public new int Key { get; set; }
            public string DerivedValue { get; set; }
        }

        private sealed class MigrationShadowEntity
        {
            public string Id { get; set; }
            public string EntityType { get; set; }
            public List<MigrationShadowDerived> Items { get; set; }
        }

        private sealed class MigrationJObjectEntity
        {
            public string Id { get; set; }
            public string EntityType { get; set; }
            public List<KeyValuePair<string, object>> Mappings { get; set; }
        }

        private sealed class MigrationEntityBase : EntityBase
        {
        }

        private sealed class MigrationTimestampEntity
        {
            public string Id { get; set; }
            public string EntityType { get; set; }
            public List<MigrationTimestampChild> Items { get; set; }
        }

        private sealed class MigrationTimestampChild
        {
            public UtcTimestamp CreationDate { get; set; }
            public UtcTimestamp? LastUpdated { get; set; }
            public string Note { get; set; }
        }

        [AllowLegacyGuidDocumentId]
        private sealed class MigrationLegacyGuidEntity : EntityBase
        {
        }

        private sealed class MigrationStateSetEntity
        {
            public string Id { get; set; }
            public string EntityType { get; set; }
            public EntityHeader<StateSet> StateSet { get; set; }
        }

        private sealed class StateSet
        {
            public LagoVistaKey Key { get; set; }
            public string Name { get; set; }
        }

        private sealed class MigrationOrgNamespaceEntity
        {
            public string Id { get; set; }
            public string EntityType { get; set; }
            public OrgNamespace Namespace { get; set; }
            public string Note { get; set; }
        }

        [TestMethod]
        public void TransformUsesClrAndMongoContractsAndDropsComputedJsonProperties()
        {
            var source = JObject.Parse(
                @"{
                    'id': 'ABC123',
                    'EntityType': 'MigrationTestEntity',
                    'Name': 'alpha',
                    'DisplayName': 'ALPHA',
                    'Status': {
                        'Value': 2,
                        'Text': 'Ready',
                        'HasValue': true
                    },
                    '_rid': 'cosmos-rid',
                    '_etag': 'cosmos-etag'
                }");

            var transformer = new DocumentMigrationTransformer(new TestEntityTypeResolver());

            var success = transformer.TryTransform(source, out var target, out var error);

            Assert.IsTrue(success, error);
            Assert.IsNotNull(target);
            Assert.AreEqual("ABC123", target["_id"].AsString);
            Assert.AreEqual(nameof(MigrationTestEntity), target["EntityType"].AsString);
            Assert.AreEqual("alpha", target["Name"].AsString);
            Assert.IsFalse(target.Contains("DisplayName"));
            Assert.IsFalse(target.Contains("_rid"));
            Assert.IsFalse(target.Contains("_etag"));

            var status = target["Status"].AsBsonDocument;
            Assert.AreEqual(2, status["Value"].AsInt32);
            Assert.AreEqual("Ready", status["Text"].AsString);
            Assert.IsFalse(status.Contains("HasValue"));
        }


        [TestMethod]
        public void TransformUsesMostDerivedShadowedMemberWithoutChangingClrModel()
        {
            var source = JObject.Parse(
                @"{
                    'id': 'SHADOW1',
                    'EntityType': 'MigrationShadowEntity',
                    'Items': [{
                        'Key': 42,
                        'BaseValue': 'base',
                        'DerivedValue': 'derived'
                    }]
                }");

            var transformer = new DocumentMigrationTransformer(new TestEntityTypeResolver());

            var success = transformer.TryTransform(source, out var target, out var error);

            Assert.IsTrue(success, error);
            var nested = target["Items"].AsBsonArray[0].AsBsonDocument;
            Assert.AreEqual(42, nested["Key"].AsInt32);
            Assert.AreEqual("base", nested["BaseValue"].AsString);
            Assert.AreEqual("derived", nested["DerivedValue"].AsString);
        }

        [TestMethod]
        public void TransformRoundTripsJObjectPayload()
        {
            var source = JObject.Parse(
                @"{
                    'id': 'JSON1',
                    'EntityType': 'MigrationJObjectEntity',
                    'Mappings': [{
                        'Key': 'payload',
                        'Value': {
                            'source': 'sensor-a',
                            'reading': 12.5,
                            'nested': { 'active': true }
                        }
                    }]
                }");

            var transformer = new DocumentMigrationTransformer(new TestEntityTypeResolver());

            var success = transformer.TryTransform(source, out var target, out var error);

            Assert.IsTrue(success, error);
            var mapping = target["Mappings"].AsBsonArray[0].AsBsonDocument;
            var payload = mapping["v"].AsBsonDocument;
            Assert.AreEqual("sensor-a", payload["source"].AsString);
            Assert.AreEqual(12.5, payload["reading"].AsDouble, 0.001);
            Assert.IsTrue(payload["nested"].AsBsonDocument["active"].AsBoolean);
        }

        [TestMethod]
        public void TransformUsesCosmosDocumentIdWhenLegacyStoredIdDisagrees()
        {
            var source = JObject.Parse(
                @"{
                    'id': '0123456789ABCDEF0123456789ABCDEF',
                    'EntityType': 'MigrationEntityBase',
                    'StoredId': 'fed695f2-2d20-432d-983a-4a81b678426e'
                }");

            var transformer = new DocumentMigrationTransformer(new TestEntityTypeResolver());

            var success = transformer.TryTransform(source, out var target, out var error);

            Assert.IsTrue(success, error);
            Assert.AreEqual("0123456789ABCDEF0123456789ABCDEF", target["_id"].AsString);
            Assert.AreEqual("0123456789ABCDEF0123456789ABCDEF", target["StoredId"].AsString);
        }

        [TestMethod]
        public void TransformNormalizesLegacyUtcTimestampStringsOnlyForUtcTimestampProperties()
        {
            var source = JObject.Parse(
                @"{
                    'id': 'TIME1',
                    'EntityType': 'MigrationTimestampEntity',
                    'Items': [{
                        'CreationDate': '05/08/2019 10:45:09',
                        'LastUpdated': '03/30/2020 17:57:09',
                        'Note': '03/30/2020 17:57:09'
                    }]
                }");

            var transformer = new DocumentMigrationTransformer(new TestEntityTypeResolver());

            var success = transformer.TryTransform(source, out var target, out var error);

            Assert.IsTrue(success, error);
            var item = target["Items"].AsBsonArray[0].AsBsonDocument;
            Assert.AreEqual("2019-05-08T10:45:09.000Z", item["CreationDate"].AsString);
            Assert.AreEqual("2020-03-30T17:57:09.000Z", item["LastUpdated"].AsString);
            Assert.AreEqual("03/30/2020 17:57:09", item["Note"].AsString);
        }

        [TestMethod]
        public void TransformAllowsLegacyGuidDocumentIdForOptedInEntity()
        {
            const string legacyId = "1647f003-5ee9-4dc8-be05-13c0c77bb4cc";
            var source = JObject.Parse(
                $@"{{
                    'id': '{legacyId}',
                    'EntityType': 'MigrationLegacyGuidEntity'
                }}");

            var transformer = new DocumentMigrationTransformer(new TestEntityTypeResolver());

            var success = transformer.TryTransform(source, out var target, out var error);

            Assert.IsTrue(success, error);
            Assert.AreEqual(legacyId, target["_id"].AsString);
        }

        [TestMethod]
        public void TransformFillsMissingEmbeddedStateSetKeyFromEntityHeaderId()
        {
            var source = JObject.Parse(
                @"{
                    'id': 'STATESET1',
                    'EntityType': 'MigrationStateSetEntity',
                    'StateSet': {
                        'HasValue': true,
                        'Value': {
                            'Key': null,
                            'Name': null
                        },
                        'Id': 'dronaffiliation',
                        'Text': 'Drone Affiliation'
                    }
                }");

            var transformer = new DocumentMigrationTransformer(new TestEntityTypeResolver());

            var success = transformer.TryTransform(source, out var target, out var error);

            Assert.IsTrue(success, error);
            var stateSet = target["StateSet"].AsBsonDocument["Value"].AsBsonDocument;
            Assert.AreEqual("dronaffiliation", stateSet["Key"].AsString);
        }

        [TestMethod]
        public void TransformFillsMissingEmbeddedStateSetKeyFromValueIdWhenHeaderIdentityIsMissing()
        {
            var source = JObject.Parse(
                @"{
                    'id': 'STATESET2',
                    'EntityType': 'MigrationStateSetEntity',
                    'StateSet': {
                        'HasValue': false,
                        'Value': {
                            'Key': null,
                            'id': '6AA35E8626D7401DAB12E3180DEE7C93',
                            'Name': null
                        },
                        'Id': null,
                        'Text': null
                    }
                }");

            var transformer = new DocumentMigrationTransformer(new TestEntityTypeResolver());

            var success = transformer.TryTransform(source, out var target, out var error);

            Assert.IsTrue(success, error);
            var stateSet = target["StateSet"].AsBsonDocument["Value"].AsBsonDocument;
            Assert.AreEqual("stateset-6aa35e8626d7401dab12e3180dee7c93", stateSet["Key"].AsString);
        }

        [TestMethod]
        public void TransformPadsShortLegacyOrgNamespaceWithoutChangingOrdinaryStrings()
        {
            var source = JObject.Parse(
                @"{
                    'id': 'ORGNS1',
                    'EntityType': 'MigrationOrgNamespaceEntity',
                    'Namespace': 'ker',
                    'Note': 'ker'
                }");

            var transformer = new DocumentMigrationTransformer(new TestEntityTypeResolver());

            var success = transformer.TryTransform(source, out var target, out var error);

            Assert.IsTrue(success, error);
            Assert.AreEqual("kernsp", target["Namespace"].AsString);
            Assert.AreEqual("ker", target["Note"].AsString);
        }

        [TestMethod]
        public void TransformFailsClosedWhenEntityTypeCannotBeResolved()
        {
            var source = JObject.Parse(
                @"{
                    'id': 'ABC123',
                    'EntityType': 'UnknownEntity'
                }");

            var transformer = new DocumentMigrationTransformer(new TestEntityTypeResolver());

            var success = transformer.TryTransform(source, out var target, out var error);

            Assert.IsFalse(success);
            Assert.IsNull(target);
            StringAssert.Contains(error, "Could not resolve EntityType");
        }
    }
}
