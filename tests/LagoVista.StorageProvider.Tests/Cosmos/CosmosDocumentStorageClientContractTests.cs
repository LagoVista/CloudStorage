using LagoVista.CloudStorage.Storage;
using LagoVista.CloudStorage.StorageProviders;
using LagoVista.StorageProvider.Tests.DocumentStorage;
using Microsoft.Azure.Cosmos;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading.Tasks;

namespace LagoVista.StorageProvider.Tests.Cosmos
{
    [TestClass]
    [DoNotParallelize]
    [TestCategory("Cosmos")]
    [TestCategory("CosmosDocumentStorageContract")]
    public class CosmosDocumentStorageClientContractTests
    {
        private const string Endpoint = "https://localhost:18081/";
        private const string AccessKey = "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==";
        private CosmosClientProvider _provider;
        private CosmosDocumentStorageClient _client;
        private TestCosmosConnectionSettings _settings;

        [TestInitialize]
        public async Task SetupAsync()
        {
            _provider = new CosmosClientProvider();
            _settings = new TestCosmosConnectionSettings($"cosmos_contract_{Guid.NewGuid():N}");
            var cosmosClient = _provider.GetClient(_settings.Endpoint, _settings.AccessKey);
            var provisioner = new CosmosDocumentCollectionProvisioner();
            await provisioner.EnsureExistsAsync(cosmosClient, _settings.Endpoint, _settings.DatabaseName, $"{_settings.DatabaseName}_Collections", "/EntityType");
            _client = new CosmosDocumentStorageClient(_settings, _provider);
        }

        [TestCleanup]
        public async Task CleanupAsync()
        {
            if (_provider == null || _settings == null) return;

            try
            {
                var client = _provider.GetClient(_settings.Endpoint, _settings.AccessKey);
                await client.GetDatabase(_settings.DatabaseName).DeleteAsync();
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
            }
            finally
            {
                _provider.Dispose();
            }
        }

        [TestMethod]
        public Task DatabaseIdentity_MatchesConfiguredDatabase() => DocumentStorageClientContract.DatabaseIdentityAsync(_client, _settings.DatabaseName);

        [TestMethod]
        public Task CrudLifecycle_SatisfiesSharedContract() => DocumentStorageClientContract.CrudLifecycleAsync(_client, nameof(ContractDocumentEntity));

        [TestMethod]
        public Task NotFoundSemantics_SatisfySharedContract() => DocumentStorageClientContract.NotFoundSemanticsAsync(_client, nameof(ContractDocumentEntity));

        [TestMethod]
        public Task QueryAndPaging_SatisfySharedContract() => DocumentStorageClientContract.QueryAndPagingAsync(_client);

        [TestMethod]
        public Task OptimisticConcurrency_SatisfiesSharedContract() => DocumentStorageClientContract.OptimisticConcurrencyAsync(_client);

        [TestMethod]
        public Task Patch_SatisfiesSharedContract() => DocumentStorageClientContract.PatchAsync(_client);

        [TestMethod]
        public Task ProjectionAndKeyLookup_SatisfySharedContract() => DocumentStorageClientContract.ProjectionAndKeyLookupAsync(_client);

        [TestMethod]
        public Task OwnedLookup_SatisfiesSharedContract() => DocumentStorageClientContract.OwnedLookupAsync(_client);

        [TestMethod]
        public Task RawDocumentAndPage_SatisfySharedContract() => DocumentStorageClientContract.RawDocumentAndPageAsync(_client);

        [TestMethod]
        public Task KnownEntityQueries_SatisfySharedContract() => DocumentStorageClientContract.KnownEntityQueriesAsync(_client);

        [TestMethod]
        public Task SummaryQuery_SatisfiesSharedContract() => DocumentStorageClientContract.SummaryQueryAsync(_client);

        [TestMethod]
        public Task QueryAll_SatisfiesSharedContract() => DocumentStorageClientContract.QueryAllAsync(_client);

        private sealed class TestCosmosConnectionSettings : ICosmosConnectionSettings
        {
            public TestCosmosConnectionSettings(string databaseName)
            {
                DatabaseName = databaseName;
            }

            public string Endpoint => CosmosDocumentStorageClientContractTests.Endpoint;
            public string AccessKey => CosmosDocumentStorageClientContractTests.AccessKey;
            public string DatabaseName { get; }
        }
    }
}
