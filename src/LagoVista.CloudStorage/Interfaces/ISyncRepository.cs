using LagoVista.CloudStorage.Models;
using LagoVista.Core.Models;
using LagoVista.Core.Validation;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static LagoVista.CloudStorage.Storage.CosmosSyncRepository;

namespace LagoVista.CloudStorage.Interfaces
{
    public interface ISyncRepository
    {
        Task<InvokeResult> SetEntityHashAsync(string id, CancellationToken ct = default);
        Task<IReadOnlyList<SyncEntitySummary>> GetSummariesAsync(string entityType, string ownerOrganizationId = null, string search = null, int take = 200, CancellationToken ct = default);

        Task<string> GetOwnedJsonByIdAsync(string id, string ownerOrganizationId, CancellationToken ct = default);
        Task<string> GetJsonByIdAsync(string id, CancellationToken ct = default);
        Task<string> GetJsonByEntityTypeAndKeyAsync(string key, string entityType, string ownerOrganizationId, CancellationToken ct = default);

        Task<SyncUpsertResult> UpsertJsonAsync(string json, string expectedETag = null, CancellationToken ct = default);
        Task<SyncUpsertResult> UpsertJsonAsync(string json, EntityHeader org, EntityHeader user, CancellationToken ct = default);
        Task<EntityHeader> GetEntityHeaderForRecordAsync(string id, CancellationToken ct = default);
        Task<InvokeResult<EhResolvedEntity>> ResolveEntityHeadersAsync(string id, CancellationToken ct = default, bool dryRun = false);
        Task<InvokeResult<List<EhResolvedEntity>>> ResolveEntityHeadersAsync(string entityType, string continuationToken, int pageSize = 100, int maxPagesThisRun = 10, CancellationToken ct = default, bool dryRun = false);
        Task<InvokeResult<NodeLocatorResult>> AddNodeLocatorsAsync(string continuationToken, int pageSize = 100, int maxPagesThisRun = 10, CancellationToken ct = default, bool dryRun = false);

        Task<InvokeResult<EntityDeleteResult>> DeleteByEntityTypeAsync(string entityType, string continuationToken, bool dryRun, int pageSize = 100, int maxPagesThisRun = 10, CancellationToken ct = default);

        Task<string> ScanContainerAsync(Func<CosmosScanRow, CancellationToken, Task> handleRowAsync,
            string continuationToken = null, string entityType = null, int pageSize = 100, int maxPagesThisRun = 10, string fixedPartitionKey = null, CancellationToken ct = default);

        Task<InvokeResult> PatchEntityAsync(PatchRequest request, EntityHeader org, EntityHeader user, CancellationToken ct = default);
    }
}
