using LagoVista.CloudStorage.Storage;
using LagoVista.CloudStorage.Storage.StorageProviders.Mongo;
using LagoVista.Core;
using LagoVista.Core.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using System;
using System.Reflection;

namespace LagoVista.StorageProvider.Tests.Mongo
{
    [TestClass]
    [DoNotParallelize]
    public sealed class VisitorFsmBsonRegistrationOrderTests
    {
        [TestInitialize]
        public void InitializeCloudStorageBsonSerializers()
        {
            // Match MongoMutableRecordStore constructor initialization before either test path.
            var serializationType = typeof(MongoApplicationDataStore).Assembly.GetType(
                "LagoVista.CloudStorage.Storage.StorageProviders.Mongo.MongoBsonSerialization", true);
            var configure = serializationType.GetMethod("Configure", BindingFlags.Public | BindingFlags.Static);
            Assert.IsNotNull(configure, "Expected actual CloudStorage Mongo BSON initialization.");
            configure.Invoke(null, null);
        }

        [TestMethod]
        public void ApplicationDataFirst_AllowsProviderOwnedVersionMetadata()
        {
            InvokeProviderCompatibility<DirectFirstState>();
            var record = BsonSerializer.Deserialize<DirectFirstState>(
                new BsonDocument { { "Key", "welcome" }, { "_storageVersion", "version-1" } });

            Assert.AreEqual("welcome", record.Key);
        }

        [TestMethod]
        public void NestedPersonaFirst_ExposesExistingClassMapCompatibilityGap()
        {
            // The persona/document path serializes an embedded FSM state before the
            // same CLR type is first used as a standalone ApplicationData record.
            new PersonaEnvelope
            {
                ResolvedState = new NestedFirstState { Key = "welcome" }
            }.ToBsonDocument();

            Assert.IsTrue(BsonClassMap.IsClassMapRegistered(typeof(NestedFirstState)),
                "The persona path must initialize the nested state BSON class map.");

            // ApplicationData's current compatibility method returns immediately
            // when that class map already exists.
            InvokeProviderCompatibility<NestedFirstState>();

            Assert.ThrowsException<MongoDB.Bson.BsonSerializationException>(() =>
                BsonSerializer.Deserialize<NestedFirstState>(
                    new BsonDocument { { "Key", "welcome" }, { "_storageVersion", "version-1" } }));
        }

        private static void InvokeProviderCompatibility<TRecord>() where TRecord : class
        {
            var storeType = typeof(MongoApplicationDataStore).Assembly.GetType(
                "LagoVista.CloudStorage.Storage.StorageProviders.Mongo.MongoMutableRecordStore", true);
            var method = storeType.GetMethod("EnsureProviderOwnedExtraElementCompatibility",
                BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(method, "Expected actual CloudStorage compatibility registration method.");
            method.MakeGenericMethod(typeof(TRecord)).Invoke(null, null);
        }

        private sealed class PersonaEnvelope
        {
            public NestedFirstState ResolvedState { get; set; }
        }

        private sealed class DirectFirstState : IApplicationDataRecord
        {
            public NormalizedId32 Id { get; set; }
            public EntityHeader Organization { get; set; }
            public UtcTimestamp CreationDate { get; set; }
            public UtcTimestamp LastUpdatedDate { get; set; }
            public string Key { get; set; }
        }

        private sealed class NestedFirstState : IApplicationDataRecord
        {
            public NormalizedId32 Id { get; set; }
            public EntityHeader Organization { get; set; }
            public UtcTimestamp CreationDate { get; set; }
            public UtcTimestamp LastUpdatedDate { get; set; }
            public string Key { get; set; }
        }
    }
}
