using Microsoft.Azure.Cosmos;
using NUnit.Framework;
using System;
using System.Net.Http;
using System.Net.Security;
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
            using var client = CreateClient(settings.Uri, settings.AccessKey);

            try
            {
                var databaseResponse = await client.CreateDatabaseIfNotExistsAsync(databaseName);
                var containerResponse = await databaseResponse.Database.CreateContainerIfNotExistsAsync("Documents", "/pk");
                var container = containerResponse.Container;
                var document = new SandboxDocument { id = Guid.NewGuid().ToString("N"), pk = "sandbox", Name = "Cosmos Sandbox" };

                await container.CreateItemAsync(document, new PartitionKey(document.pk));
                var loaded = await container.ReadItemAsync<SandboxDocument>(document.id, new PartitionKey(document.pk));

                Assert.That(loaded.Resource.id, Is.EqualTo(document.id));
                Assert.That(loaded.Resource.Name, Is.EqualTo("Cosmos Sandbox"));
            }
            finally
            {
                await client.GetDatabase(databaseName).DeleteAsync();
            }
        }

        private static CosmosClient CreateClient(string endpoint, string key)
        {
            return new CosmosClient(endpoint, key, new CosmosClientOptions
            {
                ConnectionMode = ConnectionMode.Gateway,
                HttpClientFactory = () => new HttpClient(new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = (request, certificate, chain, errors) => request.RequestUri != null && request.RequestUri.IsLoopback && (errors == SslPolicyErrors.None || certificate != null)
                })
            });
        }

        private sealed class SandboxDocument
        {
            public string id { get; set; }
            public string pk { get; set; }
            public string Name { get; set; }
        }
    }
}
