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

        Task<InvokeResult<List<JObject>>> GetEntityReadinessCandidatesAsync(string entityType, string orgId, CancellationToken ct);

        Task<InvokeResult<List<EntityChecklistCandidateSummary>>> GetEntitiesWithCompletedChecklistStepsAsync(string entityType, string orgId, IEnumerable<string> checklistStepKeys, int maxItems, CancellationToken ct);

        Task<InvokeResult<List<EntityChecklistCandidateSummary>>> GetEntitiesWithCompletedChecklistStepsAsync(string entityType, string orgId, IEnumerable<EntityChecklistStep> checklistSteps, int maxItems, CancellationToken ct);

        Task<InvokeResult<List<EntityChecklistCandidateSummary>>> GetEntitiesReadyForChecklistStepAsync(string entityType, string orgId, IEnumerable<string> requiredCompletedStepKeys, string targetIncompleteStepKey, int maxItems, CancellationToken ct);

        Task<InvokeResult<int>> CountEntitiesByTypeAsync(string entityType, string orgId, CancellationToken ct);

        Task<InvokeResult<int>> CountEntitiesWithCompletedChecklistStepsAsync(string entityType, string orgId, IEnumerable<string> checklistStepKeys, CancellationToken ct);

        Task<InvokeResult<int>> CountEntitiesReadyForChecklistStepAsync(string entityType, string orgId, IEnumerable<string> requiredCompletedStepKeys, string targetIncompleteStepKey, CancellationToken ct);
        
        Task<InvokeResult<int>> CountEntitiesReadyForChecklistStepAsync(string entityType, string orgId, IEnumerable<string> requiredCompletedStepKeys, IEnumerable<string> targetIncompleteStepKeys, CancellationToken ct);

        Task<InvokeResult<List<EntityChecklistCandidateSummary>>> GetEntitiesReadyForChecklistStepAsync(string entityType, string orgId, IEnumerable<string> requiredCompletedStepKeys, IEnumerable<string> targetIncompleteStepKeys, int maxItems, CancellationToken ct);
        Task<InvokeResult<List<JObject>>> GetEntityReadinessScorecardCandidatesAsync(IEnumerable<string> entityTypes, string orgId, CancellationToken ct);


        Task<InvokeResult<List<EntityChecklistBlockedCandidateSummary>>> GetEntitiesBlockedByChecklistPrerequisitesAsync(string entityType, string orgId, IEnumerable<string> requiredCompletedStepKeys, int maxItems, CancellationToken ct);
    }


}
