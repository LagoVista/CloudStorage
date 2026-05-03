using LagoVista.CloudStorage.Interfaces;
using LagoVista.Core.Interfaces;
using LagoVista.Core.Models;
using LagoVista.Core.PlatformSupport;
using LagoVista.Core.Validation;
using Microsoft.Azure.Cosmos;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static LagoVista.Core.Attributes.EntityDescriptionAttribute;

namespace LagoVista.CloudStorage.Storage
{
    public class EntityUtilsRepository : IEntityUtilsRepository
    {
        private readonly IEntityDetailResponseFactory _entityDetailResponseFactory;
        private readonly CosmosClient _client;
        private readonly Container _container;
        private readonly ILogger _logger;
        private readonly ISyncConnectionSettings _options;
        private readonly ICacheProvider _cacheProvider;
        private readonly IRagIndexingServices _ragIndexingServices;
        public const string FIXED_PARITIONKEY = null;
        public const string ALL_MODULES_CACHE_KEY = "NUVIOT_ALL_MODULES";
        public const string MODULE_CACHE_KEY = "NUVIOT_MODULE_";
        private readonly string _dbName;


        public EntityUtilsRepository(ISyncConnectionSettings options, IEntityDetailResponseFactory entityDetailResponseFactory, ICacheProvider cacheProvider,  ILogger logger, IRagIndexingServices ragIndexingServices )
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _entityDetailResponseFactory = entityDetailResponseFactory ?? throw new ArgumentNullException(nameof(entityDetailResponseFactory));
            _cacheProvider = cacheProvider ?? throw new ArgumentNullException(nameof(cacheProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _ragIndexingServices = ragIndexingServices ?? throw new ArgumentNullException(nameof(ragIndexingServices));
            _dbName = _options.SyncConnectionSettings.ResourceName;

            _client = new CosmosClient(_options.SyncConnectionSettings.Uri, _options.SyncConnectionSettings.AccessKey, new CosmosClientOptions
            {
            });

            _container = _client.GetContainer(_options.SyncConnectionSettings.ResourceName, $"{_options.SyncConnectionSettings.ResourceName}_Collections");
        }

        private string GetCacheKey(string entityType, string id)
        {
            return $"{_dbName}-{entityType}-{id}".ToLower();
        }

        public async Task<InvokeResult> CalculateHashAsync(string id, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("id is required.", nameof(id));

            // Query-by-id avoids needing partitionKey. Small datasets -> acceptable.
            const string sql = "SELECT * FROM c WHERE c.id = @id";
            var qd = new QueryDefinition(sql).WithParameter("@id", id.Trim());

            var requestOptions = new QueryRequestOptions
            {
                MaxItemCount = 1
            };

            if (!string.IsNullOrWhiteSpace(FIXED_PARITIONKEY))
            {
                requestOptions.PartitionKey = new PartitionKey(FIXED_PARITIONKEY);
            }

            using var iterator = _container.GetItemQueryIterator<JObject>(qd, requestOptions: requestOptions);

            JObject doc = null;

            while (iterator.HasMoreResults)
            {
                var page = await iterator.ReadNextAsync(ct).ConfigureAwait(false);
                doc = page.Resource?.FirstOrDefault();
                if (doc == null) continue;
            }

            var sha256Hex = EntityHasher.CalculateHash(doc);

            var sha256HexJsonPropertyName = nameof(IEntityBase.Sha256Hex);

            var partitionKey = !string.IsNullOrWhiteSpace(FIXED_PARITIONKEY)
                ? new PartitionKey(FIXED_PARITIONKEY)
                : new PartitionKey(doc.Value<string>("partitionKey")); // replace with your real PK property

            var patchOperations = new[]
            {
                PatchOperation.Set($"/{sha256HexJsonPropertyName}", sha256Hex)
            };

            await _container.PatchItemAsync<JObject>(id, partitionKey: partitionKey, patchOperations: patchOperations,
                requestOptions: new PatchItemRequestOptions
                {
                    EnableContentResponseOnWrite = false
                },
                cancellationToken: ct).ConfigureAwait(false);

            var entityType = doc["EntityType"]?.Value<string>()?.Trim();
            var key = doc["Key"]?.Value<string>()?.Trim();

            await _cacheProvider.RemoveAsync(GetCacheKey(entityType, id));
            if (entityType == "Module")
            {
                await _cacheProvider.RemoveAsync(ALL_MODULES_CACHE_KEY);
                await _cacheProvider.RemoveAsync($"{MODULE_CACHE_KEY}{key}");
            }

            return InvokeResult.Success;
        }

        public async Task<InvokeResult> IndexEntityAsync(string id, EntityHeader org, EntityHeader user, CancellationToken ct)
        {
            var modelResult = await _entityDetailResponseFactory.LoadModelAsync(id, user, org);
            if (modelResult.Model is IEntityBase model)
            {
                await _ragIndexingServices.IndexAsync(model);
                return InvokeResult.Success;
            }
            else
                return InvokeResult.FromError("not a entity");
        }
    }
}
