// --- BEGIN CODE INDEX META (do not edit) ---
// ContentHash: 2d3c3740f965c6ffa5f3224db7385dd5d0d8118d63662437af38c4968a189eb6
// IndexVersion: 2
// --- END CODE INDEX META ---
using LagoVista.CloudStorage.Interfaces;
using LagoVista.CloudStorage.Models;
using LagoVista.CloudStorage.Utils;
using LagoVista.Core;
using LagoVista.Core.Exceptions;
using LagoVista.Core.Interfaces;
using LagoVista.Core.Models;
using LagoVista.Core.PlatformSupport;
using LagoVista.Core.Validation;
using LagoVista.IoT.Logging.Loggers;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.CloudStorage.Storage
{

    public class StorageUtils : IStorageUtils
    {
        string _endPoint;
        private string _sharedKey;
        private string _dbName;
        private readonly IAdminLogger _logger;
        private string _collectionName;
        private CosmosClient _client;
        private readonly ICacheProvider _cacheProvider;
        private readonly INodeLocatorTableReader _nodeLocator;


        public StorageUtils(IAdminLogger logger, INodeLocatorTableReader nodeLocator, ICacheProvider cacheProvider)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));    
            _cacheProvider = cacheProvider ?? throw new ArgumentNullException(nameof(cacheProvider));
            _nodeLocator = nodeLocator ?? throw new ArgumentNullException(nameof(nodeLocator));
        }


        public StorageUtils(Uri endpoint, String sharedKey, String dbName, IAdminLogger logger, ICacheProvider cacheProvider = null)
        {
            _sharedKey = sharedKey ?? throw new ArgumentNullException(nameof(sharedKey));
            _dbName = dbName ?? throw new ArgumentNullException(nameof(dbName));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _cacheProvider = cacheProvider;
            _collectionName = _dbName + "_Collections";

            _endPoint = endpoint.ToString();
        }

        public void SetConnection(IConnectionSettings connectionSettings)
        {
            _endPoint = connectionSettings.Uri;
            _sharedKey = connectionSettings.AccessKey;
            _dbName = connectionSettings.ResourceName;
            _collectionName = _dbName + "_Collections";
        }

        protected CosmosClient GetDocumentClient()
        {

            if (_client == null)
            {
                var connectionPolicy = new CosmosClientOptions();
                _client = new CosmosClient(_endPoint, _sharedKey, connectionPolicy);
            }
            return _client;
        }

        protected CosmosClient Client
        {
            get { return GetDocumentClient(); }
        }


        private string GetCacheKey(string entityname, string id)
        {
            return $"{_dbName}-{entityname}-{id}".ToLower();
        }


        protected Task<Database> GetDatabase(CosmosClient client)
        {
            if (String.IsNullOrEmpty(_dbName))
            {
                var ex = new InvalidOperationException($"Invalid or missing database name information on {GetType().Name}");
                _logger.AddException($"{GetType().Name}_CTor", ex);
                throw ex;
            }

            return Task.FromResult(_client.GetDatabase(_dbName));
        }
        public async Task<TEntity> FindWithKeyAsync<TEntity>(string key, IEntityHeader org, bool throwOnNotFound = true) where TEntity : class, IIDEntity, INoSQLEntity, IKeyedEntity, IOwnedEntity
        {
            var sw = Stopwatch.StartNew();
            var container = Client.GetContainer(_dbName, _collectionName);
            var linqQuery = container.GetItemLinqQueryable<TEntity>()
                    .Where(doc => doc.Key == key && doc.OwnerOrganization.Id == org.Id && doc.EntityType == typeof(TEntity).Name);

            using (var iterator = linqQuery.ToFeedIterator<TEntity>())
            {
                if (iterator.HasMoreResults)
                {
                    var response = await iterator.ReadNextAsync();
                    Console.WriteLine($"[StorageUtils__FindWithKeyAsync] - Success found {key} of type {typeof(TEntity).Name} for organization {org.Text} in {sw.Elapsed.TotalMilliseconds}ms");
                    return response.SingleOrDefault();
                }
            }

            Console.WriteLine($"[StorageUtils__FindWithKeyAsync] - Failed did not find {key} of type {typeof(TEntity).Name} for organization {org.Text} in {sw.Elapsed.TotalMilliseconds}ms");

            return null;
        }

        public async Task<IStandardModel> FindWithKeyAsync(string objectType, string key, IEntityHeader org, bool throwOnNotFound = true)
        {
            var sw = Stopwatch.StartNew();
            var container = Client.GetContainer(_dbName, _collectionName);
            var linqQuery = container.GetItemLinqQueryable<IStandardModel>()
                    .Where(doc => doc.Key == key && doc.OwnerOrganization.Id == org.Id && doc.EntityType == objectType);

            using (var iterator = linqQuery.ToFeedIterator<IStandardModel>())
            {
                if (iterator.HasMoreResults)
                {
                    var response = await iterator.ReadNextAsync();
                    Console.WriteLine($"[StorageUtils__FindWithKeyAsync] - Success found {key} of type {objectType} for organization {org.Text} in {sw.Elapsed.TotalMilliseconds}ms");
                    return response.SingleOrDefault();
                }
            }

            Console.WriteLine($"[StorageUtils__FindWithKeyAsync] - Failed did not find {key} of type {objectType} for organization {org.Text} in {sw.Elapsed.TotalMilliseconds}ms");

            return null;
        }


        public async Task<RatedEntity> ClearRatingAsync(string id, EntityHeader org, EntityHeader user)
        {
            var container = Client.GetContainer(_dbName, _collectionName);
            var item = await container.ReadItemAsync<EntityBase>(id, PartitionKey.None);

            var ratings = item.Resource;

            ratings.Ratings = ratings.Ratings.Where(rt => rt.User.Id != user.Id).ToList();

            ratings.Stars = ratings.Ratings.Count > 0 ? ratings.Ratings.Average(rat => rat.Stars) : 0;
            ratings.RatingsCount = ratings.Ratings.Count;

            var operations = new List<PatchOperation>()
            {
                PatchOperation.Set($"/{nameof(IRatedEntity.Stars)}", ratings.Stars),
                PatchOperation.Set($"/{nameof(IRatedEntity.RatingsCount)}", ratings.RatingsCount),
                PatchOperation.Set($"/{nameof(IRatedEntity.Ratings)}", ratings.Ratings),
            };

            await _cacheProvider.RemoveAsync(GetCacheKey(ratings.EntityType, id));
     
            var response = await container.PatchItemAsync<RatedEntity>(id, PartitionKey.None, operations);
            return response.Resource;
        }

        public async Task<RatedEntity> AddRatingAsync(string id, int rating, EntityHeader org, EntityHeader user)
        {
            var sw = Stopwatch.StartNew();
            var container = Client.GetContainer(_dbName, _collectionName);
            var item = await container.ReadItemAsync<EntityBase>(id, PartitionKey.None);

            var ratings = item.Resource;

            var existing = ratings.Ratings.FirstOrDefault(rat => rat.User.Id == user.Id);
            if (existing != null)
            {
                existing.Stars = rating;
                existing.TimeStamp = DateTime.UtcNow.ToJSONString();
            }
            else
            {
                ratings.Ratings.Add(new EntityRating()
                {
                    Stars = rating,
                    TimeStamp = DateTime.UtcNow.ToJSONString(),
                    User = user,
                });
            }

            ratings.Stars = ratings.Ratings.Average(rat => rat.Stars);
            ratings.RatingsCount = ratings.Ratings.Count;

            var operations = new List<PatchOperation>()
            {
                PatchOperation.Set($"/{nameof(IRatedEntity.Stars)}", ratings.Stars),
                PatchOperation.Set($"/{nameof(IRatedEntity.RatingsCount)}", ratings.RatingsCount),
                PatchOperation.Set($"/{nameof(IRatedEntity.Ratings)}", ratings.Ratings),
            };

            await _cacheProvider.RemoveAsync(GetCacheKey(ratings.EntityType, id));

            var response = await container.PatchItemAsync<RatedEntity>(id, PartitionKey.None, operations);
            return response.Resource;
        }

        public async Task<InvokeResult> SetEntityPublicAsync(string id, EntityHeader org, EntityHeader user)
        {
            _logger.Trace($"{this.Tag()}  - Setting entity {id} public by user {user.Text} in org {org.Text}");

            var sw = Stopwatch.StartNew();
            var container = Client.GetContainer(_dbName, _collectionName);
            var item = await container.ReadItemAsync<EntityBase>(id, PartitionKey.None);
            _logger.Trace($"{this.Tag()} - found item {id} - {item.Resource.Name}");

            var operations = new List<PatchOperation>()
            {
                PatchOperation.Set($"/{nameof(EntityBase.IsPublic)}", true),
                PatchOperation.Set($"/{nameof(EntityBase.PublicPromotionDate)}", DateTime.UtcNow.ToJSONString()),
                PatchOperation.Set($"/{nameof(EntityBase.PublicPromotedBy)}", user),
            };

            await _cacheProvider.RemoveAsync(GetCacheKey(item.Resource.EntityType, id));

            var response = await container.PatchItemAsync<RatedEntity>(id, PartitionKey.None, operations);
            _logger.Trace($"{this.Tag()} - response {response.StatusCode} setting {id} to public in {sw.Elapsed.TotalMilliseconds}ms");   

            return InvokeResult.Success;
        }

        public async Task<InvokeResult> SetCategoryAsync(string id, EntityHeader category, EntityHeader org, EntityHeader user)
        {
            var sw = Stopwatch.StartNew();
            var container = Client.GetContainer(_dbName, _collectionName);
            var response = await container.ReadItemAsync<EntityBase>(id, PartitionKey.None);
            var item = response.Resource;
            
            var operations = new List<PatchOperation>()
            {
                PatchOperation.Set($"/{nameof(ICategorized.Category)}", category),
            };

            var result = await container.PatchItemAsync<EntityBase>(id, PartitionKey.None, operations);
            Console.WriteLine("UPDATED CATEGORY===> "+ result.Resource.Category.Text + " id ==> " + result.Resource.Id);
            await _cacheProvider.RemoveAsync(GetCacheKey(item.EntityType, id));

            return InvokeResult.Success;
        }


        public async Task<TEntity> FindWithKeyAsync<TEntity>(string key) where TEntity : class, IIDEntity, INoSQLEntity, IKeyedEntity, IOwnedEntity
        {
            var container = Client.GetContainer(_dbName, _collectionName);
            var linqQuery = container.GetItemLinqQueryable<TEntity>()
                    .Where(doc => doc.Key == key && doc.IsPublic && doc.EntityType == typeof(TEntity).Name);

            using (var iterator = linqQuery.ToFeedIterator<TEntity>())
            {
                if (iterator.HasMoreResults)
                {
                    var response = await iterator.ReadNextAsync();
                    return response.SingleOrDefault();
                }
            }

            return null;
        }

        public async Task<TEntity> FindWithIdAsync<TEntity>(string id, string ownerId) where TEntity : class, IIDEntity, INoSQLEntity, IKeyedEntity, IOwnedEntity
        {
            try
            {
                var container = Client.GetContainer(_dbName, _collectionName);
                var response = await container.ReadItemAsync<TEntity>(id, PartitionKey.None);

                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    var entity = response.Resource;

                    if(entity == null)
                    {
                        Console.WriteLine($"[StorageUtils__FindWithIdAsync] Could not load record of type {typeof(TEntity).Name} with id: {id}");
                        return default;
                    }

                    if (entity.EntityType != typeof(TEntity).Name)
                    {
                        return default;
                    }

                    if (typeof(TEntity).Name != "Organization" && (entity.OwnerOrganization.Id != ownerId || entity.IsPublic))
                        throw new NotAuthorizedException($"Invalid object access by incorrect organization, object org: {entity.OwnerOrganization.Id} - request org: {ownerId}");

                    return entity;
                }
                else
                {
                    return default;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                throw;
            }

        }

        public async Task DeleteByKeyIfExistsAsync<TEntity>(string key, IEntityHeader org) where TEntity : class, IIDEntity, INoSQLEntity, IKeyedEntity, IOwnedEntity
        {
            var item = await FindWithKeyAsync<TEntity>(key, org);
            if (item != null)
                await DeleteAsync<TEntity>(item.Id, org);
        }


        public async Task UpsertDocumentAsync<TEntity>(TEntity obj) where TEntity : class, IIDEntity, INoSQLEntity, IKeyedEntity, IOwnedEntity
        {
            var container = Client.GetContainer(_dbName, _collectionName);
            obj.EntityType = typeof(TEntity).Name;
            obj.DatabaseName = _dbName;

            var response = await container.UpsertItemAsync(obj);
            if (response.StatusCode != System.Net.HttpStatusCode.Created)
            {
                throw new Exception("Could not upsert document.");
            }
        }

        public async Task DeleteAsync<TEntity>(string id, IEntityHeader org) where TEntity : class, IIDEntity, INoSQLEntity, IKeyedEntity, IOwnedEntity
        {
            var container = Client.GetContainer(_dbName, _collectionName);

            var response = await container.ReadItemAsync<TEntity>(id, PartitionKey.None);

            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                var json = response.Resource.ToString();

                if (String.IsNullOrEmpty(json))
                {
                    _logger.AddCustomEvent(LogLevel.Error, "DocumentDBRepoBase_GetDocumentAsync", $"Empty Response Content", new KeyValuePair<string, string>("entityType", typeof(TEntity).Name), new KeyValuePair<string, string>("id", id));
                    throw new RecordNotFoundException(typeof(TEntity).Name, id);
                }

                if (org.Id != response.Resource.OwnerOrganization.Id)
                {
                    throw new NotAuthorizedException($"Attempt to delete record of type {typeof(TEntity).Name} owned by org {response.Resource.OwnerOrganization.Text} by org {org.Text}");
                }

                await container.DeleteItemAsync<TEntity>(id, PartitionKey.None);
            }
        }

        public const string NOT_FOUND_ID = "09AE184AE5374B40B0E174D8F4956653";
        public const string NOT_FOUND_OWNER_ORG_ID = "00000000000000000000000000000000";
        public const string NOT_FOUND_KEY = "recordnotfound";
        public const string NOT_FOUND_TEXT = "Record Not Found";
        public const string NOT_FOUND_ENTITYTYPE = "RecordNotFound";


        public async Task<string> GetJsonByIdAsync(string id, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("id is required.", nameof(id));

            // Query-by-id avoids needing partitionKey. Small datasets -> acceptable.
            const string sql = "SELECT * FROM c WHERE c.id = @id";
            var qd = new QueryDefinition(sql)
                .WithParameter("@id", id.Trim());

            var requestOptions = new QueryRequestOptions
            {
                MaxItemCount = 1
            };

            
            var container = Client.GetContainer(_dbName, _collectionName);

            using var iterator = container.GetItemQueryIterator<JObject>(
                qd,
                requestOptions: requestOptions);

            while (iterator.HasMoreResults)
            {
                var page = await iterator.ReadNextAsync(ct).ConfigureAwait(false);
                var doc = page.Resource?.FirstOrDefault();
                if (doc == null) continue;

                // Return raw JSON for UI side-by-side display.
                return doc.ToString(Formatting.Indented);
            }

            return null;
        }

        public async Task<EntityGraph> GetEntityGraphAsync(string id, EntityHeader org, EntityHeader user)
        {
            if(String.IsNullOrEmpty(id)) throw new ArgumentException("id is required.", nameof(id));

            _logger.Trace($"{this.Tag()} - Getting entity graph for {id} in org {org.Text} for user {user.Text}.");
            var json = await GetJsonByIdAsync(id);
            if(String.IsNullOrEmpty(json))
            {
                var node = await _nodeLocator.TryGetAsync(id);
                if (node == null)
                {
                    return null;
                }

                var childJson = await GetJsonByIdAsync(node.RootId);
                if(childJson == null)
                {
                    return null;
                }

                var hostEntity = JsonConvert.DeserializeObject<EntityBase>(childJson);
                var childEh = EntityHeaderJsonPathExtractor.TryExtract(childJson, node.NodePath);
                return new EntityGraph()
                {
                    Id = node.NodeId,
                    EntityType = node.NodeEntityType ?? node.NodeType,
                    Text = childEh?.Text ?? "Unknown",
                    HostEntityName = hostEntity.Name,
                    HostEntityType = hostEntity.EntityType,
                    HostEntityId = hostEntity.Id,
                    IsHostEntityPublic = hostEntity.IsPublic,
                    IsPublic = hostEntity.IsPublic,
                    OwnerOrgId = hostEntity.OwnerOrganization.Id,
                };
            }  

            var entity = JsonConvert.DeserializeObject<EntityBase>(json);
            if (entity.EntityType == "Organization" || entity.EntityType == "AppUser")
                return null;
            
            if(entity.OwnerOrganization.Id != org.Id && !entity.IsPublic)
            {
                throw new NotAuthorizedException($"Attempt to load entity graph for {entity.Name} of type {entity.EntityType} owned by org {entity.OwnerOrganization.Text} by org {org.Text}");
            }

            _logger.Trace($"{this.Tag()} - Loaded entity {entity.Name} for {id} in org {org.Text} for user {user.Text}.");

            var token = JToken.Parse(json);
            var topLevel = new EntityGraph()
            {
                EntityType = entity.EntityType,
                IsPublic = entity.IsPublic,
                Text = entity.Name,
                Id = entity.Id,
                Key = entity.Key,
                OwnerOrgId = entity.OwnerOrganization?.Id,
            };
            
            var nodes = EntityHeaderJson.FindEntityHeaderNodes(token);
            foreach(var node in nodes)
            {
                if(node.Path.EndsWith("CreatedBy") || node.Path.EndsWith("LastUpdatedBy") || node.Path.EndsWith("OwnerOrganization"))
                {
                    _logger.Trace($"{this.Tag()} - {node.NormalizedPath} - Skipping audit field.");
                    continue;
                }

                _logger.Trace($"{this.Tag()} - {node.NormalizedPath} - Requesting child node {node.Text} with {node.Id}");

                var child = await GetEntityGraphAsync(node.Id, org, user);
                if(child != null)
                    topLevel.Children.Add(child);
            }

            return topLevel;
        }


        public async Task<List<TEntity>> FindByTypeAsync<TEntity>(string entityType, IEntityHeader org) where TEntity : class, IIDEntity, INamedEntity, IOwnedEntity, INoSQLEntity, IKeyedEntity
        {
            var sw = Stopwatch.StartNew();
            var container = Client.GetContainer(_dbName, _collectionName);
            var linqQuery = container.GetItemLinqQueryable<TEntity>()
                    .Where(doc => doc.EntityType == entityType && (doc.OwnerOrganization.Id == org.Id || doc.IsPublic));

            Console.WriteLine($"[StorageUtils__FindWithKeyAsync] - Query {linqQuery}");

            var entities = new List<TEntity>();

            using (var iterator = linqQuery.ToFeedIterator<TEntity>())
            {
                if (iterator.HasMoreResults)
                {
                    return (await iterator.ReadNextAsync()).ToList();
                }
            }

            Console.WriteLine($"[StorageUtils__FindWithKeyAsync] - Failed did not find {entityType} of type {typeof(TEntity).Name} for organization {org.Text} in {sw.Elapsed.TotalMilliseconds}ms");

            return null;
        }
    }
}
