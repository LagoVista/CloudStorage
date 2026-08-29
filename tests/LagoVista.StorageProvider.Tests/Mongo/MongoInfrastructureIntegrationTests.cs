using LagoVista.CloudStorage.Storage;
using LagoVista.CloudStorage.Storage.StorageProviders.Mongo;
using LagoVista.CloudStorage.StorageProviders;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace LagoVista.StorageProvider.Tests.Mongo
{
    [TestClass]
    [DoNotParallelize]
    [TestCategory("Mongo")]
    [TestCategory("MongoInfrastructure")]
    public sealed class MongoInfrastructureIntegrationTests
    {
        private const string ConnectionString = "mongodb://nuviot-test:nuviot-test-password@localhost:27018/?authSource=admin";
        private readonly MongoClient _cleanupClient = new MongoClient(ConnectionString);
        private string _databaseName;

        [TestInitialize]
        public void Setup()
        {
            _databaseName = $"mongo_infra_{Guid.NewGuid():N}";
        }

        [TestCleanup]
        public async Task TearDownAsync()
        {
            if (!String.IsNullOrWhiteSpace(_databaseName))
                await _cleanupClient.DropDatabaseAsync(_databaseName);
        }

        [TestMethod]
        public async Task StorageClientFactory_ReusesClientAndConnectsToRequestedDatabase()
        {
            var factory = new MongoStorageClientFactory();

            var first = factory.GetClient(ConnectionString);
            var second = factory.GetClient(ConnectionString);
            Assert.AreSame(first, second);

            var database = factory.GetDatabase(ConnectionString, _databaseName);
            Assert.AreEqual(_databaseName, database.DatabaseNamespace.DatabaseName);

            var ping = await database.RunCommandAsync<BsonDocument>(new BsonDocument("ping", 1));
            Assert.AreEqual(1.0, ping["ok"].ToDouble());
        }

        [TestMethod]
        public async Task DocumentCollectionProvisioner_CreatesCollection_AndRepeatedEnsureIsSafe()
        {
            var collectionName = "documents";
            var provisioner = new MongoDocumentCollectionProvisioner();

            await provisioner.EnsureExistsAsync(ConnectionString, _databaseName, collectionName);
            await provisioner.EnsureExistsAsync(ConnectionString, _databaseName, collectionName);

            var database = _cleanupClient.GetDatabase(_databaseName);
            var names = (await database.ListCollectionNamesAsync()).ToList();
            CollectionAssert.Contains(names, collectionName);
        }

        [TestMethod]
        public async Task DocumentCollectionProvisioner_ToleratesCollectionCreatedBeforeEnsure()
        {
            var collectionName = "race_winner";
            var database = _cleanupClient.GetDatabase(_databaseName);
            await database.CreateCollectionAsync(collectionName);

            var provisioner = new MongoDocumentCollectionProvisioner();
            await provisioner.EnsureExistsAsync(ConnectionString, _databaseName, collectionName);

            var names = (await database.ListCollectionNamesAsync()).ToList();
            CollectionAssert.Contains(names, collectionName);
        }
    }
}
