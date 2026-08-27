using LagoVista.CloudStorage.Storage;
using LagoVista.CloudStorage.StorageProviders;
using Microsoft.Azure.Cosmos;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading.Tasks;

namespace LagoVista.StorageProvider.Tests.Cosmos
{
    [TestClass]
    [DoNotParallelize]
    [TestCategory("Cosmos")]
    [TestCategory("CosmosInfrastructure")]
    public class CosmosInfrastructureIntegrationTests
    {
        private const string Endpoint = "https://localhost:18081/";
        private const string AccessKey = "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==";

        [TestMethod]
        [TestCategory("CosmosClientProvider")]
        public async Task ClientProvider_ConnectsAndReusesNormalizedEndpointAsync()
        {
            using var provider = new CosmosClientProvider();
            var first = provider.GetClient(Endpoint, AccessKey);
            var second = provider.GetClient(Endpoint.TrimEnd('/'), AccessKey);

            Assert.AreSame(first, second);
            var account = await first.ReadAccountAsync();
            Assert.IsNotNull(account);
        }

        [TestMethod]
        [TestCategory("CosmosClientProvider")]
        public void ClientProvider_RejectsMissingConnectionMaterial()
        {
            using var provider = new CosmosClientProvider();
            Assert.ThrowsExactly<ArgumentException>(() => provider.GetClient(null, AccessKey));
            Assert.ThrowsExactly<ArgumentException>(() => provider.GetClient(Endpoint, null));
        }

        [TestMethod]
        [TestCategory("CosmosProvisioning")]
        public async Task Provisioner_CreatesFreshDatabaseAndContainerRepeatSafelyAsync()
        {
            using var provider = new CosmosClientProvider();
            var client = provider.GetClient(Endpoint, AccessKey);
            var databaseName = $"cosmos_recovery_{Guid.NewGuid():N}";
            var collectionName = $"{databaseName}_Collections";
            var provisioner = new CosmosDocumentCollectionProvisioner();

            try
            {
                await provisioner.EnsureExistsAsync(client, Endpoint, databaseName, collectionName, "/EntityType");
                await provisioner.EnsureExistsAsync(client, Endpoint, databaseName, collectionName, "/EntityType");

                var database = client.GetDatabase(databaseName);
                var databaseResponse = await database.ReadAsync();
                Assert.AreEqual(databaseName, databaseResponse.Resource.Id);

                var containerResponse = await database.GetContainer(collectionName).ReadContainerAsync();
                Assert.AreEqual(collectionName, containerResponse.Resource.Id);
                Assert.AreEqual("/EntityType", containerResponse.Resource.PartitionKeyPath);
            }
            finally
            {
                await DeleteDatabaseIfExistsAsync(client, databaseName);
            }
        }

        [TestMethod]
        [TestCategory("CosmosProvisioning")]
        public async Task Provisioner_RejectsExistingContainerWithWrongPartitionKeyAsync()
        {
            using var provider = new CosmosClientProvider();
            var client = provider.GetClient(Endpoint, AccessKey);
            var databaseName = $"cosmos_partition_{Guid.NewGuid():N}";
            var collectionName = $"{databaseName}_Collections";

            try
            {
                var database = (await client.CreateDatabaseIfNotExistsAsync(databaseName)).Database;
                await database.CreateContainerIfNotExistsAsync(new ContainerProperties(collectionName, "/WrongPartition"));
                var provisioner = new CosmosDocumentCollectionProvisioner();

                await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => provisioner.EnsureExistsAsync(client, Endpoint, databaseName, collectionName, "/EntityType"));
            }
            finally
            {
                await DeleteDatabaseIfExistsAsync(client, databaseName);
            }
        }

        private static async Task DeleteDatabaseIfExistsAsync(CosmosClient client, string databaseName)
        {
            try
            {
                await client.GetDatabase(databaseName).DeleteAsync();
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
            }
        }
    }
}
