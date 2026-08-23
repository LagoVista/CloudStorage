using LagoVista.Core.Models;

namespace LagoVista.CloudStorage.Tests
{
    internal static class StorageLabConnections
    {
        public const string CosmosEndpoint = "https://localhost:18081/";
        public const string CosmosKey = "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==";
        public const string CosmosDatabase = "CloudStorageLab";

        public static ConnectionSettings TestCosmosDocumentStorage => new ConnectionSettings
        {
            Uri = CosmosEndpoint,
            AccessKey = CosmosKey,
            ResourceName = CosmosDatabase
        };
    }
}
