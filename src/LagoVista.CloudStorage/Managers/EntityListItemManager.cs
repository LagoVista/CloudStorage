using LagoVista.CloudStorage.Interfaces;
using LagoVista.CloudStorage.Models;
using LagoVista.Core.Interfaces;
using LagoVista.Core.Models;
using LagoVista.Core.Models.UIMetaData;
using LagoVista.Core.Validation;
using System;
using System.Threading.Tasks;

namespace LagoVista.CloudStorage.Managers
{
    public class EntityListItemManager : IEntityListItemManager
    {
        private readonly IEntityTypeResolver _entityTypeResolver;
        private readonly IEntityListItemRepoFactory _repoFactory;
        private readonly IEntityListItemCache _cache;
        private readonly ISecurity _security;

        public EntityListItemManager(IEntityTypeResolver entityTypeResolver, IEntityListItemRepoFactory repoFactory, IEntityListItemCache cache, ISecurity security)
        {
            _entityTypeResolver = entityTypeResolver ?? throw new ArgumentNullException(nameof(entityTypeResolver));
            _repoFactory = repoFactory ?? throw new ArgumentNullException(nameof(repoFactory));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _security = security ?? throw new ArgumentNullException(nameof(security));
        }

        public async Task<ListResponse<EntityListItem>> GetListItemsAsync(string entityType, EntityHeader org, EntityHeader user, ListRequest listRequest)
        {
            ValidateRequest(entityType, org, user, listRequest);

            var modelType = _entityTypeResolver.GetEntityType(entityType);
            await _security.AuthorizeOrgAccessAsync(user, org, modelType, Actions.Read);

            var cachedResponse = await _cache.GetListItemsAsync(org.Id, modelType.Name, listRequest);
            if (cachedResponse != null)
                return cachedResponse;

            var repo = _repoFactory.Create(modelType);
            var response = await repo.GetListItemsAsync(org.Id, listRequest);

            if (response.Successful)
                await _cache.SetListItemsAsync(org.Id, modelType.Name, listRequest, response);

            return response;
        }

        public async Task<ListResponse<EntityHeader>> GetEntityHeadersAsync(string entityType, EntityHeader org, EntityHeader user, ListRequest listRequest)
        {
            ValidateRequest(entityType, org, user, listRequest);

            var modelType = _entityTypeResolver.GetEntityType(entityType);
            await _security.AuthorizeOrgAccessAsync(user, org, modelType, Actions.Read);

            var cachedResponse = await _cache.GetEntityHeadersAsync(org.Id, modelType.Name, listRequest);
            if (cachedResponse != null)
                return cachedResponse;

            var repo = _repoFactory.Create(modelType);
            var response = await repo.GetEntityHeadersAsync(org.Id, listRequest);

            if (response.Successful)
                await _cache.SetEntityHeadersAsync(org.Id, modelType.Name, listRequest, response);

            return response;
        }

        private static void ValidateRequest(string entityType, EntityHeader org, EntityHeader user, ListRequest listRequest)
        {
            if (String.IsNullOrWhiteSpace(entityType)) throw new ArgumentNullException(nameof(entityType));
            if (EntityHeader.IsNullOrEmpty(org)) throw new ArgumentNullException(nameof(org));
            if (EntityHeader.IsNullOrEmpty(user)) throw new ArgumentNullException(nameof(user));
            if (listRequest == null) throw new ArgumentNullException(nameof(listRequest));
        }
    }
}
