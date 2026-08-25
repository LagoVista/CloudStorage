using LagoVista.Core.Models;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace LagoVista.CloudStorage.Interfaces
{
    public interface IEntityDetailResponseFactory
    {
        Task<JObject> CreateAiDetailResponseAsync(Type modelType, object model, EntityHeader org, EntityHeader user);
        Task<JObject> CreateFormDetailResponseAsync(Type modelType, object model, EntityHeader org, EntityHeader user);
        Task<JObject> GetAiDetailResponseAsync(string entityType, string id, EntityHeader org, EntityHeader user);
        Task<JObject> GetFormDetailResponseAsync(string id, EntityHeader org, EntityHeader user);
        Task<JObject> GetAiDetailResponseAsync(string id, EntityHeader org, EntityHeader user);
        Task<JObject> GetFormDetailResponseAsync(string entityType, string id, EntityHeader org, EntityHeader user);
        Task<(Type ModelType, object Model)> LoadModelAsync(string id, string requestedEntityType, EntityHeader user, EntityHeader org);
        Task<(Type ModelType, object Model)> LoadModelAsync(string id, EntityHeader user, EntityHeader org);
    }
}
