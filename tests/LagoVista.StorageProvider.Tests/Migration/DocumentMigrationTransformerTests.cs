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

                if (String.Equals(entityType, nameof(MigrationThrowingSetterEntity), StringComparison.OrdinalIgnoreCase))
                {
                    modelType = typeof(MigrationThrowingSetterEntity);
                    return true;
                }

                if (String.Equals(entityType, nameof(MigrationLegacyEntityBase), StringComparison.OrdinalIgnoreCase))
                {
                    modelType = typeof(MigrationLegacyEntityBase);
                    return true;
                }

                if (String.Equals(entityType, nameof(MigrationNestedIdEntity), StringComparison.OrdinalIgnoreCase))
                {
                    modelType = typeof(MigrationNestedIdEntity);
                    return true;
                }

                if (String.Equals(entityType, nameof(MigrationLegacyKeyAndHeaderEntity), StringComparison.OrdinalIgnoreCase))
                {
                    modelType = typeof(MigrationLegacyKeyAndHeaderEntity);
                    return true;
                }

                if (String.Equals(entityType, nameof(MigrationLegacyKeyEntity), StringComparison.OrdinalIgnoreCase))
                {
                    modelType = typeof(MigrationLegacyKeyEntity);
                    return true;
                }

                if (String.Equals(entityType, nameof(MigrationPropertyBagEntity), StringComparison.OrdinalIgnoreCase))
                {
                    modelType = typeof(MigrationPropertyBagEntity);
                    return true;
                }

                if (String.Equals(entityType, nameof(MigrationShadowedEntity), StringComparison.OrdinalIgnoreCase))
                {
                    modelType = typeof(MigrationShadowedEntity);
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

        private sealed class MigrationShadowedEntity : EntityBase
        {
            public new List<EntityHeader> Labels { get; set; } = new List<EntityHeader>();
        }

        private sealed class MigrationLegacyKeyEntity
        {
            public string Id { get; set; }
            public string EntityType { get; set; }
            public LagoVistaKey Key { get; set; }
            public List<MigrationLegacyKeyChild> Items { get; set; }
        }

        private sealed class MigrationLegacyKeyChild
        {
            public LagoVistaKey Key { get; set; }
        }

        private sealed class MigrationPropertyBagEntity
        {
            public string Id { get; set; }
            public string EntityType { get; set; }
            public Dictionary<string, object> PropertyBag { get; set; }
        }

        private enum MigrationHeaderState
        {
            Offline,
            Online
        }

        private sealed class MigrationLegacyKeyAndHeaderEntity
        {
            public string Id { get; set; }
            public string EntityType { get; set; }
            public LagoVistaKey Key { get; set; }
            public EntityHeader<MigrationHeaderState> Status { get; set; }
        }

        private sealed class MigrationNestedIdEntity
        {
            public string Id { get; set; }
            public string EntityType { get; set; }
            public List<MigrationNestedIdChild> Items { get; set; }
        }

        private sealed class MigrationNestedIdChild
        {
            public NormalizedId32 Id { get; set; }
            public NormalizedId32? OptionalId { get; set; }
            public string Name { get; set; }
        }

        private sealed class MigrationLegacyEntityBase : EntityBase
        {
            public MigrationLegacyEntityBase Child { get; set; }
        }

        private sealed class MigrationThrowingSetterEntity
        {
            public string Id { get; set; }
            public string EntityType { get; set; }

            private string _value;
            public string Value
            {
                get => _value;
                set => throw new InvalidOperationException("inner setter failure");
            }
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
        public void TransformPreservesMongoIdForEntityBaseWithShadowedMembers()
        {
            var source = JObject.Parse(
                @"{
                    'id': 'AABBCCDDEEFF00112233445566778899',
                    'EntityType': 'MigrationShadowedEntity',
                    'Labels': []
                }");

            var transformer = new DocumentMigrationTransformer(new TestEntityTypeResolver());

            var success = transformer.TryTransform(source, out var target, out var error);

            Assert.IsTrue(success, error);
            Assert.AreEqual("AABBCCDDEEFF00112233445566778899", target["_id"].AsString);
            Assert.AreEqual("MigrationShadowedEntity", target["EntityType"].AsString);
        }

        [TestMethod]
        public void TransformRepairsInvalidLegacyKeysDeterministically()
        {
            var source = JObject.Parse(
                @"{
                    'id': 'KEY1',
                    'EntityType': 'MigrationLegacyKeyEntity',
                    'Key': '-1000',
                    'Items': [
                        { 'Key': 'g***cmp' },
                        { 'Key': null }
                    ]
                }");

            var transformer = new DocumentMigrationTransformer(new TestEntityTypeResolver());

            var firstSuccess = transformer.TryTransform(source, out var first, out var firstError);
            var secondSuccess = transformer.TryTransform(source, out var second, out var secondError);

            Assert.IsTrue(firstSuccess, firstError);
            Assert.IsTrue(secondSuccess, secondError);

            Assert.AreEqual(first["Key"].AsString, second["Key"].AsString);
            Assert.AreEqual(first["Items"].AsBsonArray[0].AsBsonDocument["Key"].AsString,
                second["Items"].AsBsonArray[0].AsBsonDocument["Key"].AsString);
            Assert.AreEqual(first["Items"].AsBsonArray[1].AsBsonDocument["Key"].AsString,
                second["Items"].AsBsonArray[1].AsBsonDocument["Key"].AsString);

            Assert.IsTrue(TryValidKey(first["Key"].AsString));
            Assert.IsTrue(TryValidKey(first["Items"].AsBsonArray[0].AsBsonDocument["Key"].AsString));
            Assert.IsTrue(TryValidKey(first["Items"].AsBsonArray[1].AsBsonDocument["Key"].AsString));
        }

        [TestMethod]
        public void TransformConvertsJTokenPropertyBagValuesToNativeClrPayloads()
        {
            var source = JObject.Parse(
                @"{
                    'id': 'BAG1',
                    'EntityType': 'MigrationPropertyBagEntity',
                    'PropertyBag': {
                        'name': 'alpha',
                        'count': 3,
                        'nested': { 'enabled': true },
                        'items': [1, 'two', { 'three': 3 }]
                    }
                }");

            var transformer = new DocumentMigrationTransformer(new TestEntityTypeResolver());

            var success = transformer.TryTransform(source, out var target, out var error);

            Assert.IsTrue(success, error);
            var bag = target["PropertyBag"].AsBsonDocument;
            Assert.AreEqual("alpha", bag["name"].AsString);
            Assert.AreEqual(3L, bag["count"].ToInt64());
            Assert.IsTrue(bag["nested"].AsBsonDocument["enabled"].AsBoolean);
            Assert.AreEqual("two", bag["items"].AsBsonArray[1].AsString);
            Assert.AreEqual(3L, bag["items"].AsBsonArray[2].AsBsonDocument["three"].ToInt64());
        }

        private static bool TryValidKey(string value)
        {
            try
            {
                var key = new LagoVistaKey(value);
                return !String.IsNullOrWhiteSpace(key.Value);
            }
            catch
            {
                return false;
            }
        }

        [TestMethod]
        public void TransformRepairsLegacyLagoVistaKeyAndEnumHeaderId()
        {
            var source = JObject.Parse(
                @"{
                    'id': 'LEGACY1',
                    'EntityType': 'MigrationLegacyKeyAndHeaderEntity',
                    'Key': '-1000',
                    'Status': {
                        'Id': 'offline',
                        'Text': 'Offline'
                    }
                }");

            var transformer = new DocumentMigrationTransformer(new TestEntityTypeResolver());

            var success = transformer.TryTransform(source, out var target, out var error);

            Assert.IsTrue(success, error);
            Assert.AreEqual("key-1000", target["Key"].AsString);
            Assert.AreEqual("Offline", target["Status"].AsBsonDocument["Id"].AsString);
        }

        [TestMethod]
        public void TransformAssignsStableIdsToMissingNestedNonNullableNormalizedIds()
        {
            var source = JObject.Parse(
                @"{
                    'id': 'NESTED1',
                    'EntityType': 'MigrationNestedIdEntity',
                    'Items': [
                        { 'Id': null, 'OptionalId': null, 'Name': 'one' },
                        { 'Id': '', 'OptionalId': '', 'Name': 'two' }
                    ]
                }");

            var transformer = new DocumentMigrationTransformer(new TestEntityTypeResolver());

            var firstSuccess = transformer.TryTransform(source, out var first, out var firstError);
            var secondSuccess = transformer.TryTransform(source, out var second, out var secondError);

            Assert.IsTrue(firstSuccess, firstError);
            Assert.IsTrue(secondSuccess, secondError);

            var firstItems = first["Items"].AsBsonArray;
            var secondItems = second["Items"].AsBsonArray;

            var firstId0 = firstItems[0].AsBsonDocument["Id"].AsString;
            var firstId1 = firstItems[1].AsBsonDocument["Id"].AsString;

            Assert.IsTrue(NormalizedId32.IsNormalizedId32(firstId0));
            Assert.IsTrue(NormalizedId32.IsNormalizedId32(firstId1));
            Assert.AreNotEqual(firstId0, firstId1);
            Assert.AreEqual(firstId0, secondItems[0].AsBsonDocument["Id"].AsString);
            Assert.AreEqual(firstId1, secondItems[1].AsBsonDocument["Id"].AsString);

            Assert.IsTrue(firstItems[0].AsBsonDocument["OptionalId"].IsBsonNull);
            Assert.IsTrue(firstItems[1].AsBsonDocument["OptionalId"].IsBsonNull);
        }

        [DataTestMethod]
        [DataRow("a1cc0c68-262e-4e9f-ac7a-84710384eeed", "A1CC0C68262E4E9FAC7A84710384EEED")]
        [DataRow("60d6133d913a46199cea14f0d395dc63", "60D6133D913A46199CEA14F0D395DC63")]
        public void TransformNormalizesLegacyEntityBaseDocumentIds(string sourceId, string expectedId)
        {
            var source = JObject.Parse(
                $@"{{
                    'id': '{sourceId}',
                    'EntityType': 'MigrationLegacyEntityBase',
                    'Key': 'legacyentity'
                }}");

            var transformer = new DocumentMigrationTransformer(new TestEntityTypeResolver());

            var success = transformer.TryTransform(source, out var target, out var error);

            Assert.IsTrue(success, error);
            Assert.AreEqual(expectedId, target["_id"].AsString);
        }

        [TestMethod]
        public void TransformNormalizesNestedLegacyEntityBaseDocumentIds()
        {
            var source = JObject.Parse(
                @"{
                    'id': 'AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA',
                    'EntityType': 'MigrationLegacyEntityBase',
                    'Key': 'parententity',
                    'Child': {
                        'id': 'c94a7362-c7fb-47d0-89a6-88a09d68c7a9',
                        'EntityType': 'MigrationLegacyEntityBase',
                        'Key': 'childentity'
                    }
                }");

            var transformer = new DocumentMigrationTransformer(new TestEntityTypeResolver());

            var success = transformer.TryTransform(source, out var target, out var error);

            Assert.IsTrue(success, error);
            Assert.AreEqual(
                "C94A7362C7FB47D089A688A09D68C7A9",
                target["Child"].AsBsonDocument["_id"].AsString);
        }

        [TestMethod]
        public void TransformFailureIncludesInnerExceptionDetails()
        {
            var source = JObject.Parse(
                @"{
                    'id': 'THROW1',
                    'EntityType': 'MigrationThrowingSetterEntity',
                    'Value': 'boom'
                }");

            var transformer = new DocumentMigrationTransformer(new TestEntityTypeResolver());

            var success = transformer.TryTransform(source, out _, out var error);

            Assert.IsFalse(success);
            StringAssert.Contains(error, "inner setter failure");
            StringAssert.Contains(error, "TargetInvocationException");
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

        [DataTestMethod]
        [DataRow("1steven", "org1steven")]
        [DataRow("2wtech", "org2wtech")]
        [DataRow("plant_monitor", "plantmonitor")]
        [DataRow("ADMATECH ", "admatech")]
        [DataRow("Heer parmar", "heerparmar")]
        [DataRow("nguyễnthịngọc", "nguyenthngoc")]
        public void TransformNormalizesCommonLegacyOrgNamespaceShapes(string sourceNamespace, string expectedNamespace)
        {
            var source = JObject.Parse(
                $@"{{
                    'id': 'ORGNS2',
                    'EntityType': 'MigrationOrgNamespaceEntity',
                    'Namespace': '{sourceNamespace}'
                }}");

            var transformer = new DocumentMigrationTransformer(new TestEntityTypeResolver());

            var success = transformer.TryTransform(source, out var target, out var error);

            Assert.IsTrue(success, error);
            Assert.AreEqual(expectedNamespace, target["Namespace"].AsString);
        }

        [TestMethod]
        public void TransformUsesStableFallbackForLegacyOrgNamespaceWithoutAsciiCharacters()
        {
            var source = JObject.Parse(
                @"{
                    'id': 'ORGNS3',
                    'EntityType': 'MigrationOrgNamespaceEntity',
                    'Namespace': 'глухов'
                }");

            var transformer = new DocumentMigrationTransformer(new TestEntityTypeResolver());

            var success = transformer.TryTransform(source, out var target, out var error);

            Assert.IsTrue(success, error);
            var normalized = target["Namespace"].AsString;
            Assert.IsTrue(OrgNamespace.IsValid(normalized));
            StringAssert.StartsWith(normalized, "org");
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
