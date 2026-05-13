using LagoVista.CloudStorage.Interfaces;
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

namespace LagoVista.CloudStorage.Storage
{
    public class EntityDetailResponseFactory : IEntityDetailResponseFactory
    {
        private readonly IEntityTypeResolver _entityTypeResolver;
        private readonly IStorageUtils _entityJsonLoader;
        private readonly ISecurity _security;

        public EntityDetailResponseFactory(IEntityTypeResolver entityTypeResolver, IStorageUtils entityJsonLoader, ISecurity security)
        {
            _entityTypeResolver = entityTypeResolver ?? throw new ArgumentNullException(nameof(entityTypeResolver));
            _entityJsonLoader = entityJsonLoader ?? throw new ArgumentNullException(nameof(entityJsonLoader));
            _security = security ?? throw new ArgumentNullException(nameof(security));  
        }


        private static string GetEntityType(JObject json)
        {
            var entityType = json.SelectToken("EntityType")?.Value<string>();

            if (String.IsNullOrWhiteSpace(entityType))
                throw new InvalidOperationException("EntityType was not found on the stored entity.");

            return entityType;
        }

        private static void AuthorizeOwnerOrganization(JObject json, EntityHeader org)
        {
            var ownerOrgId = json.SelectToken("OwnerOrganization.Id")?.Value<string>();

            if (String.IsNullOrWhiteSpace(ownerOrgId) || ownerOrgId != org.Id)
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
            var json = await _entityJsonLoader.GetJobjectByIdAsync(id);

            AuthorizeOwnerOrganization(json, org);

            var actualEntityType = GetEntityType(json);
            ValidateRequestedEntityType(requestedEntityType, actualEntityType);

            var modelType = _entityTypeResolver.GetEntityType(actualEntityType);
            var model = JsonConvert.DeserializeObject(json.ToString(), modelType);

            await _security.AuthorizeAsync(user, org, modelType, Core.Validation.Actions.Read);
            await _security.LogEntityActionAsync(id, actualEntityType, "Read", user, org);

            return (modelType, model);
        }

        public async Task<(Type ModelType, object Model)> LoadModelAsync(string id,  EntityHeader user, EntityHeader org)
        {
            var json = await _entityJsonLoader.GetJobjectByIdAsync(id);

            AuthorizeOwnerOrganization(json, org);

            var actualEntityType = GetEntityType(json);
            
            var modelType = _entityTypeResolver.GetEntityType(actualEntityType);
            var model = JsonConvert.DeserializeObject(json.ToString(), modelType);

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