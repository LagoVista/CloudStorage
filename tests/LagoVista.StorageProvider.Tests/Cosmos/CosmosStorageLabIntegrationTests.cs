using LagoVista.CloudStorage.Storage;
using Microsoft.Azure.Cosmos;
using NUnit.Framework;
using System;
using System.Threading.Tasks;

namespace LagoVista.CloudStorage.Tests
{
    [NonParallelizable]
    [Category("Integration")]
    [Category("CosmosSandbox")]
    public class CosmosStorageLabIntegrationTests
    {
        [Test]
        public async Task CosmosEmulator_CreateReadAndDelete_WorksEndToEnd()
        {
            var settings = StorageLabConnections.TestCosmosDocumentStorage;
            var databaseName = $"CloudStorageLab_{Guid.NewGuid():N}";
            using var provider = new CosmosClientProvider();
            var client = provider.GetClient(settings.Uri, settings.AccessKey);
            Database database = null;

            try
            {
                var databaseResponse = await client.CreateDatabaseIfNotExistsAsync(databaseName);
                database = databaseResponse.Database;
                var containerResponse = await database.CreateContainerIfNotExistsAsync("Documents", "/pk");
                var container = containerResponse.Container;
                var document = new SandboxDocument { id = Guid.NewGuid().ToString("N"), pk = "sandbox", Name = "Cosmos Sandbox" };

                await container.CreateItemAsync(document, new PartitionKey(document.pk));
                var loaded = await container.ReadItemAsync<SandboxDocument>(document.id, new PartitionKey(document.pk));

                Assert.That(loaded.Resource.id, Is.EqualTo(document.id));
                Assert.That(loaded.Resource.Name, Is.EqualTo("Cosmos Sandbox"));
            }
            finally
            {
                if (database != null) await database.DeleteAsync();
            }
        }

        private sealed class SandboxDocument
        {
            public string id { get; set; }
            public string pk { get; set; }
            public string Name { get; set; }
        }
    }
}
