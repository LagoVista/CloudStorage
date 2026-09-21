using LagoVista.CloudStorage.Storage.Migration;
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
            public MigrationShadowDerived Nested { get; set; }
        }

        private sealed class MigrationJObjectEntity
        {
            public string Id { get; set; }
            public string EntityType { get; set; }
            public JObject Payload { get; set; }
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
                    'Nested': {
                        'Key': 42,
                        'BaseValue': 'base',
                        'DerivedValue': 'derived'
                    }
                }");

            var transformer = new DocumentMigrationTransformer(new TestEntityTypeResolver());

            var success = transformer.TryTransform(source, out var target, out var error);

            Assert.IsTrue(success, error);
            var nested = target["Nested"].AsBsonDocument;
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
                    'Payload': {
                        'source': 'sensor-a',
                        'reading': 12.5,
                        'nested': { 'active': true }
                    }
                }");

            var transformer = new DocumentMigrationTransformer(new TestEntityTypeResolver());

            var success = transformer.TryTransform(source, out var target, out var error);

            Assert.IsTrue(success, error);
            var payload = target["Payload"].AsBsonDocument;
            Assert.AreEqual("sensor-a", payload["source"].AsString);
            Assert.AreEqual(12.5, payload["reading"].AsDouble, 0.001);
            Assert.IsTrue(payload["nested"].AsBsonDocument["active"].AsBoolean);
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
