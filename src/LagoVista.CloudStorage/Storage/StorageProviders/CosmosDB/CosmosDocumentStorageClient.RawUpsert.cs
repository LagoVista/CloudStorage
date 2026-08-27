using LagoVista.CloudStorage.DocumentDB;
using LagoVista.CloudStorage.Exceptions;
using LagoVista.CloudStorage.Models;
using Microsoft.Azure.Cosmos;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.CloudStorage.StorageProviders
{
    public sealed partial class CosmosDocumentStorageClient
    {
        public async Task<SyncUpsertResult> UpsertDocumentAsync(JObject document, string expectedETag = null, CancellationToken cancellationToken = default)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            var id = document.Value<string>("id");
            var entityType = document.Value<string>("EntityType");
            if (String.IsNullOrWhiteSpace(id)) throw new ArgumentException("Document id is required.", nameof(document));
            if (String.IsNullOrWhiteSpace(entityType)) throw new ArgumentException("Document EntityType is required.", nameof(document));

            var resolver = new DocumentCollectionNameResolver();
            if (!resolver.TryResolve(_settings.DatabaseName, entityType, out var collectionName)) throw new InvalidOperationException($"Could not resolve Cosmos collection for entity type '{entityType}'.");
            var container = _cosmosClientProvider.GetClient(_settings.Endpoint, _settings.AccessKey).GetContainer(_settings.DatabaseName, collectionName);
            var options = new ItemRequestOptions();
            if (!String.IsNullOrWhiteSpace(expectedETag)) options.IfMatchEtag = expectedETag;

            try
            {
                using var stream = new MemoryStream(Encoding.UTF8.GetBytes(document.ToString(Formatting.None)));
                using var response = await container.UpsertItemStreamAsync(stream, new PartitionKey(entityType), options, cancellationToken).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode) throw new InvalidOperationException($"Document upsert failed with status code {(int)response.StatusCode}.");
                return new SyncUpsertResult { Id = id, ETag = response.Headers.ETag, StatusCode = (int)response.StatusCode, RequestCharge = response.Headers.RequestCharge };
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.PreconditionFailed || ex.StatusCode == HttpStatusCode.Conflict)
            {
                throw new ContentModifiedException { EntityType = entityType, Id = id };
            }
        }
    }
}