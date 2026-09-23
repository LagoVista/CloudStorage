using LagoVista.CloudStorage.Interfaces;
using LagoVista.CloudStorage.Models.Storage;
using LagoVista.Core.Interfaces;
using LagoVista.Core.Models;
using LagoVista.Core.Models.AIMetaData;
using LagoVista.Core.Models.UIMetaData;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;
using System.Reflection;
using System.Threading.Tasks;

namespace LagoVista.CloudStorage.Repositories
{
    public class EntityDetailResponseFactory : IEntityDetailResponseFactory
    {
        private readonly IEntityTypeResolver _entityTypeResolver;
        private readonly IDocumentStorageClient _storageClient;
        private readonly ISecurity _security;

        public EntityDetailResponseFactory(IEntityTypeResolver entityTypeResolver, IDocumentStorageClientProvider documentStorageClientProvider, ISecurity security)
        {
            _entityTypeResolver = entityTypeResolver ?? throw new ArgumentNullException(nameof(entityTypeResolver));
            _storageClient = documentStorageClientProvider?.GetClient() ?? throw new ArgumentNullException(nameof(documentStorageClientProvider));
            _security = security ?? throw new ArgumentNullException(nameof(security));
        }


        private static void AuthorizeOwnerOrganization(EntityIdentityProjection identity, EntityHeader org)
        {
            var ownerOrgId = identity?.OwnerOrganization?.Id;

            if (String.IsNullOrWhiteSpace(ownerOrgId) || !String.Equals(ownerOrgId, org.Id, StringComparison.OrdinalIgnoreCase))
                throw new UnauthorizedAccessException();
        }

        private static void ValidateRequestedEntityType(string requestedEntityType, string actualEntityType)
        {

            if (!String.IsNullOrWhiteSpace(requestedEntityType) &&
                !String.Equals(requestedEntityType, actualEntityType, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Requested entity type does not match stored entity type.");
            }
        }

        public async Task<(Type ModelType, object Model)> LoadModelAsync(string id, string requestedEntityType, EntityHeader user, EntityHeader org)
        {
            var identity = await _storageClient.GetDocumentProjectionAsync<EntityIdentityProjection>(id, true).ConfigureAwait(false);
            AuthorizeOwnerOrganization(identity, org);

            var actualEntityType = identity.EntityType;
            if (String.IsNullOrWhiteSpace(actualEntityType))
                throw new InvalidOperationException("EntityType was not found on the stored entity.");

            ValidateRequestedEntityType(requestedEntityType, actualEntityType);

            var modelType = _entityTypeResolver.GetEntityType(actualEntityType);
            var model = await _storageClient.GetDocumentAsync(modelType, actualEntityType, id, true).ConfigureAwait(false);

            await _security.AuthorizeAsync(user, org, modelType, Core.Validation.Actions.Read);
            await _security.LogEntityActionAsync(id, actualEntityType, "Read", user, org);

            return (modelType, model);
        }

        public async Task<(Type ModelType, object Model)> LoadModelAsync(string id,  EntityHeader user, EntityHeader org)
        {
            var identity = await _storageClient.GetDocumentProjectionAsync<EntityIdentityProjection>(id, true).ConfigureAwait(false);
            AuthorizeOwnerOrganization(identity, org);

            var actualEntityType = identity.EntityType;
            if (String.IsNullOrWhiteSpace(actualEntityType))
                throw new InvalidOperationException("EntityType was not found on the stored entity.");

            var modelType = _entityTypeResolver.GetEntityType(actualEntityType);
            var model = await _storageClient.GetDocumentAsync(modelType, actualEntityType, id, true).ConfigureAwait(false);

            await _security.AuthorizeAsync(user, org, modelType, Core.Validation.Actions.Read);
            await _security.LogEntityActionAsync(id, actualEntityType, "Read", user, org);

            return (modelType, model);
        }


        public Task<JObject> CreateAiDetailResponseAsync(Type modelType, object model, EntityHeader org, EntityHeader user)
        {
            if (modelType == null) throw new ArgumentNullException(nameof(modelType));
            if (model == null) throw new ArgumentNullException(nameof(model));

            var method = GetType().GetMethod(nameof(CreateTypedAiDetailedResponseAsync), BindingFlags.Instance | BindingFlags.NonPublic);
            if (method == null)
                throw new MissingMethodException(nameof(EntityDetailResponseFactory), nameof(CreateTypedAiDetailedResponseAsync));

            var typedMethod = method.MakeGenericMethod(modelType);
            return (Task<JObject>)typedMethod.Invoke(this, new[] { model, org, user });
        }

        public Task<JObject> CreateFormDetailResponseAsync(Type modelType, object model, EntityHeader org, EntityHeader user)
        {
            if (modelType == null) throw new ArgumentNullException(nameof(modelType));
            if (model == null) throw new ArgumentNullException(nameof(model));

            var method = GetType().GetMethod(nameof(CreateTypedFormDetailAsync), BindingFlags.Instance | BindingFlags.NonPublic);
            if (method == null)
                throw new MissingMethodException(nameof(EntityDetailResponseFactory), nameof(CreateTypedFormDetailAsync));

            var typedMethod = method.MakeGenericMethod(modelType);
            return (Task<JObject>)typedMethod.Invoke(this, new[] { model, org, user });
        }

        private Task<JObject> CreateTypedFormDetailAsync<TModel>(object model, EntityHeader org, EntityHeader user) where TModel : class, new()
        {
            var response = DetailResponse<TModel>.Create((TModel)model);
            return Task.FromResult(JObject.FromObject(response, CamelCaseJsonSerializer));
        }

        private Task<JObject> CreateTypedAiDetailedResponseAsync<TModel>(object model, EntityHeader org, EntityHeader user) where TModel : class, new()
        {
            var response = AiDetailResponse<TModel>.Create((TModel)model);
            return Task.FromResult(JObject.FromObject(response, CamelCaseJsonSerializer));
        }

        public async Task<JObject> GetAiDetailResponseAsync(string entityType, string id, EntityHeader org, EntityHeader user)
        {
            var loaded = await LoadModelAsync(id, entityType, user, org);
            return await CreateAiDetailResponseAsync(loaded.ModelType, loaded.Model, org, user);
        }

        public async Task<JObject> GetFormDetailResponseAsync(string entityType, string id, EntityHeader org, EntityHeader user)
        {
            var loaded = await LoadModelAsync(id, entityType, user, org);
            return await CreateFormDetailResponseAsync(loaded.ModelType, loaded.Model, org, user);
        }

        public async Task<JObject> GetAiDetailResponseAsync(string id, EntityHeader org, EntityHeader user)
        {
            var loaded = await LoadModelAsync(id, null, user, org);
            return await CreateAiDetailResponseAsync(loaded.ModelType, loaded.Model, org, user);
        }

        public async Task<JObject> GetFormDetailResponseAsync(string id, EntityHeader org, EntityHeader user)
        {
            var loaded = await LoadModelAsync(id, null, user, org);
            return await CreateFormDetailResponseAsync(loaded.ModelType, loaded.Model, org, user);
        }

        private static readonly JsonSerializer CamelCaseJsonSerializer = JsonSerializer.Create(new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            NullValueHandling = NullValueHandling.Ignore
        });
    }
}