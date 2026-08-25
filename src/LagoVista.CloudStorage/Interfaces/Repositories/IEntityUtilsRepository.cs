using LagoVista.Core;
using LagoVista.Core.Models;
using LagoVista.Core.Models.EntityReadiness;
using LagoVista.Core.Validation;
using MongoDB.Bson.Serialization.IdGenerators;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.CloudStorage.Interfaces
{
    public interface IEntityUtilsRepository
    {
        Task<InvokeResult<EntityHeaderStatusPatchResult>> PatchEntityHeaderStatusAsync(string entityType, string orgId, IEnumerable<string> currentStatusIds, JObject desiredStatus, EntityHeader user, bool dryRun, int maxItems, CancellationToken ct);
        Task<EntityBase> GetEntityBaseAsync(string id, EntityHeader org, CancellationToken ct = default);
        Task<List<EntityBaseSummary>> GetEntityBasesAsync(string entityType, EntityHeader org, CancellationToken ct = default);
        Task<List<EntityCoreSummary>> GetEntityCoreAsync(string entityType, EntityHeader org, CancellationToken ct = default);

        Task<InvokeResult> IndexEntityAsync(string id, EntityHeader org, EntityHeader user, CancellationToken ct = default);
        Task<InvokeResult> UpsertAiEntitySessionAsync(string id, AiEntitySession session, CancellationToken ct = default);
        Task<InvokeResult> PatchEntityFieldsAsync(string id, Dictionary<string, JToken> fields, EntityHeader user, CancellationToken ct = default);
        Task<InvokeResult> CalculateHashAsync(string id, CancellationToken ct = default);
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

        Task<InvokeResult<List<EntityChecklistCandidateSummary>>> GetEntitiesWithCompletedChecklistStepsAsync(string entityType, string orgId, IEnumerable<string> checklistStepKeys, string targetChecklistStepKey, int maxItems, CancellationToken ct);

        Task<InvokeResult<List<JObject>>> GetEntitiesByTypeAsync(string entityType, string orgId, CancellationToken ct);
        Task<JObject> GetEntityByIdAsync(string entityType, string entityId, string orgId, CancellationToken token);

        Task<JObject> GetEntityByIdAsync(string entityId, string orgId, CancellationToken token);
        Task<InvokeResult> PatchMasterStatusAsync(string id, MasterEntityStatus masterStatus, EntityHeader user, CancellationToken ct);

        Task<bool> IsKeyUniqueAsync(string entityType, string orgId, string key, string id = null, CancellationToken ct = default);
        Task<bool> IsTlaUniqueAsync(string entityType, string orgId, string tla, string id = null, CancellationToken ct = default);
        Task<string> GenerateUniqueKeyAsync(string entityType, string orgId, string name, string key = null, string id = null, CancellationToken ct = default);
        Task<string> GenerateUniqueTlaAsync(string entityType, string orgId, string name, string tla = null, string id = null, CancellationToken ct = default);
    }

    public class EntityCoreSummary
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Key { get; set; }
        public string Tla { get; set; }
        public EntityHeader Category { get; set; }
        public string Purpose { get; set; }
        public string Description { get; set; }
        public string PurposeSummary { get; set; }
        public List<EntityReadinessCheckState> ReadinessChecks { get; set; } = new List<EntityReadinessCheckState>();

        public List<EntityChecklistStatus> ChecklistStatus { get; set; } = new List<EntityChecklistStatus>();
        public List<EntityOwnershipPoint> OwnershipPoints { get; set; }
    }


    public sealed class EntityHeaderStatusPatchResult
    {
        public string EntityType { get; set; }

        public string FieldName { get; set; } = "Status";

        public string DesiredStatusId { get; set; }

        public int Found { get; set; }

        public int Matched { get; set; }

        public int Updated { get; set; }

        public int Skipped { get; set; }

        public int Errors { get; set; }

        public List<string> UpdatedIds { get; set; } = new List<string>();

        public List<string> SkippedIds { get; set; } = new List<string>();

        public List<string> ErrorMessages { get; set; } = new List<string>();
    }

    public sealed class EntityBaseSummary
    {
        public string Id { get; set; }

        public string EntityType { get; set; }

        public string Name { get; set; }

        public string Key { get; set; }

        public string Description { get; set; }

        public LagoVistaIcon Icon { get; set; }

        public EntityHeader Category { get; set; }

        public bool IsDraft { get; set; }

        public bool IsDeprecated { get; set; }

        public MasterEntityStatus MasterStatus { get; set; }
        public List<EntityReadinessCheckState> ReadinessChecks { get; set; } = new List<EntityReadinessCheckState>();

        public EntityReadinessStatus ReadinessStatus { get; set; }

        public List<EntityChecklistStatus> ChecklistStatus { get; set; } = new List<EntityChecklistStatus>();

        public UtcTimestamp CreationDate { get; set; }

        public UtcTimestamp LastUpdatedDate { get; set; }

        public int Revision { get; set; }
    }
}
