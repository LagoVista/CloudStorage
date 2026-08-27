using LagoVista.CloudStorage.DocumentDB;
using LagoVista.CloudStorage.Storage;
using LagoVista.CloudStorage.Storage.ConnectionSettings;
using LagoVista.CloudStorage.Storage.StorageProviders;
using LagoVista.CloudStorage.StorageProviders;
using LagoVista.StorageProvider.Tests.DocumentStorage;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LagoVista.StorageProvider.Tests.Mongo
{
    [TestClass]
    [DoNotParallelize]
    [TestCategory("Mongo")]
    [TestCategory("DocumentStorageContract")]
    public class MongoDocumentStorageClientContractTests
    {
        private MongoDocumentStorageConnectionSettings _settings;
        private MongoClient _cleanupClient;
        private MongoDocumentStorageClient _client;

        [TestInitialize]
        public void Setup()
        {
            _settings = new MongoDocumentStorageConnectionSettings
            {
                Hosts = new List<string> { "localhost" }.AsReadOnly(),
                Port = 27018,
                UserName = "nuviot-test",
                Password = "nuviot-test-password",
                AuthenticationDatabase = "admin",
                DatabaseName = $"doc_contract_{Guid.NewGuid():N}"
            };

            var factory = new MongoStorageClientFactory();
            _client = new MongoDocumentStorageClient(_settings, new DocumentCollectionNameResolver(), factory);
            _cleanupClient = new MongoClient(_settings.BuildConnectionString());
        }

        [TestCleanup]
        public async Task Cleanup()
        {
            if (_cleanupClient != null && _settings != null && !String.IsNullOrWhiteSpace(_settings.DatabaseName))
                await _cleanupClient.DropDatabaseAsync(_settings.DatabaseName);
        }

        [TestMethod]
        public Task DatabaseIdentity_MatchesConfiguredDatabase() =>
            DocumentStorageClientContract.DatabaseIdentityAsync(_client, _settings.DatabaseName);

        [TestMethod]
        public Task CrudLifecycle_SatisfiesSharedContract() =>
            DocumentStorageClientContract.CrudLifecycleAsync(_client, "ORG1");

        [TestMethod]
        public Task NotFoundSemantics_SatisfySharedContract() =>
            DocumentStorageClientContract.NotFoundSemanticsAsync(_client, "ORG1");

        [TestMethod]
        public Task QueryAndPaging_SatisfySharedContract() =>
            DocumentStorageClientContract.QueryAndPagingAsync(_client);

        [TestMethod]
        public Task OptimisticConcurrency_SatisfiesSharedContract() =>
            DocumentStorageClientContract.OptimisticConcurrencyAsync(_client);

        [TestMethod]
        public Task Patch_SatisfiesSharedContract() =>
            DocumentStorageClientContract.PatchAsync(_client);

        [TestMethod]
        public Task ProjectionAndKeyLookup_SatisfySharedContract() =>
            DocumentStorageClientContract.ProjectionAndKeyLookupAsync(_client);

        [TestMethod]
        public Task OwnedLookup_SatisfiesSharedContract() =>
            DocumentStorageClientContract.OwnedLookupAsync(_client);

        [TestMethod]
        public Task RawDocumentAndPage_SatisfySharedContract() =>
            DocumentStorageClientContract.RawDocumentAndPageAsync(_client);

        [TestMethod]
        public Task KnownEntityQueries_SatisfySharedContract() =>
            DocumentStorageClientContract.KnownEntityQueriesAsync(_client);

        [TestMethod]
        public Task SummaryQuery_SatisfiesSharedContract() =>
            DocumentStorageClientContract.SummaryQueryAsync(_client);

        [TestMethod]
        public Task QueryAll_SatisfiesSharedContract() =>
            DocumentStorageClientContract.QueryAllAsync(_client);
    }
}
