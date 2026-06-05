using LagoVista.Core.Models;
using LagoVista.Core.Validation;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.CloudStorage.Interfaces
{
    public interface IEntityUtilsRepository
    {
        Task<InvokeResult> IndexEntityAsync(string id, EntityHeader org, EntityHeader user, CancellationToken ct);
        Task<InvokeResult> UpsertAiEntitySessionAsync(string id, AiEntitySession session, CancellationToken ct);
        Task<InvokeResult> PatchEntityFieldsAsync(string id, Dictionary<string, JToken> fields, EntityHeader user, CancellationToken ct);
        Task<InvokeResult> CalculateHashAsync(string id, CancellationToken ct);
    }
}
