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
        Task<InvokeResult<Dictionary<string, JToken>>> GetEntityFieldsAsync(string id, IEnumerable<string> fieldNames, CancellationToken ct);

        Task<InvokeResult<List<JObject>>> GetEntitiesWithEmptyFieldAsync(string entityType, string fieldName, string orgId, int maxItems, CancellationToken ct);

        Task<InvokeResult<List<EntityChecklistCandidateSummary>>> GetEntitiesWithCompletedChecklistStepsAsync(string entityType, string orgId, IEnumerable<string> checklistStepKeys, int maxItems, CancellationToken ct);

        Task<InvokeResult<List<EntityChecklistCandidateSummary>>> GetEntitiesWithCompletedChecklistStepsAsync(string entityType, string orgId, IEnumerable<EntityChecklistStep> checklistSteps, int maxItems, CancellationToken ct);

        Task<InvokeResult<List<EntityChecklistCandidateSummary>>> GetEntitiesReadyForChecklistStepAsync(string entityType, string orgId, IEnumerable<string> requiredCompletedStepKeys, string targetIncompleteStepKey, int maxItems, CancellationToken ct);
    }

    public class EntityChecklistCandidateSummary
    {
        public string Id { get; set; }

        public string EntityType { get; set; }

        public string Name { get; set; }

        public string Key { get; set; }

        public string Description { get; set; }
    }
}
