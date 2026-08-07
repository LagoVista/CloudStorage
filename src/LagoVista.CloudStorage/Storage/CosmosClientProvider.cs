using LagoVista.CloudStorage.Interfaces;
using Microsoft.Azure.Cosmos;
using System;
using System.Collections.Concurrent;

namespace LagoVista.CloudStorage.Storage
{
    public sealed class CosmosClientProvider : ICosmosClientProvider, IDisposable
    {
        private readonly ConcurrentDictionary<string, CosmosClient> _clients = new ConcurrentDictionary<string, CosmosClient>(StringComparer.OrdinalIgnoreCase);

        public CosmosClient GetClient(string uri, string accessKey)
        {
            if (String.IsNullOrWhiteSpace(uri))
                throw new ArgumentException("Cosmos URI is required.", nameof(uri));

            if (String.IsNullOrWhiteSpace(accessKey))
                throw new ArgumentException("Cosmos access key is required.", nameof(accessKey));

            var normalizedUri = uri.Trim().TrimEnd('/');

            return _clients.GetOrAdd(
                normalizedUri,
                _ => new CosmosClient(
                    normalizedUri,
                    accessKey,
                    new CosmosClientOptions
                    {
                        ConnectionMode = ConnectionMode.Direct
                    }));
        }

        public void Dispose()
        {
            foreach (var client in _clients.Values)
                client.Dispose();

            _clients.Clear();
        }
    }
}
