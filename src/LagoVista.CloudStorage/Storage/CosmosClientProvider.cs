using LagoVista.CloudStorage.Interfaces;
using Microsoft.Azure.Cosmos;
using System;
using System.Collections.Concurrent;
using System.Net.Http;
using System.Net.Security;

namespace LagoVista.CloudStorage.Storage
{
    public sealed class CosmosClientProvider : ICosmosClientProvider, IDisposable
    {
        public static CosmosClientProvider Shared { get; } = new CosmosClientProvider();

        private readonly ConcurrentDictionary<string, CosmosClient> _clients = new ConcurrentDictionary<string, CosmosClient>(StringComparer.OrdinalIgnoreCase);

        public CosmosClient GetClient(string uri, string accessKey)
        {
            if (String.IsNullOrWhiteSpace(uri))
                throw new ArgumentException("Cosmos URI is required.", nameof(uri));

            if (String.IsNullOrWhiteSpace(accessKey))
                throw new ArgumentException("Cosmos access key is required.", nameof(accessKey));

            var normalizedUri = uri.Trim().TrimEnd('/');
            return _clients.GetOrAdd(normalizedUri, _ => new CosmosClient(normalizedUri, accessKey, CreateClientOptions(normalizedUri)));
        }

        private static CosmosClientOptions CreateClientOptions(string uri)
        {
            if (Uri.TryCreate(uri, UriKind.Absolute, out var endpoint) && endpoint.IsLoopback)
            {
                return new CosmosClientOptions
                {
                    ConnectionMode = ConnectionMode.Gateway,
                    HttpClientFactory = () => new HttpClient(new HttpClientHandler
                    {
                        ServerCertificateCustomValidationCallback = (request, certificate, chain, errors) => request.RequestUri != null && request.RequestUri.IsLoopback && (errors == SslPolicyErrors.None || certificate != null)
                    })
                };
            }

            return new CosmosClientOptions { ConnectionMode = ConnectionMode.Direct };
        }

        public void Dispose()
        {
            foreach (var client in _clients.Values)
                client.Dispose();

            _clients.Clear();
        }
    }
}
