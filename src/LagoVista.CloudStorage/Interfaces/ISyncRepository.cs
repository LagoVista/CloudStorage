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
        Task<IReadOnlyList<SyncEntitySummary>> GetSummariesAsync(
            string entityType,
            string ownerOrganizationId = null,
            string search = null,
            int take = 200,
            CancellationToken ct = default);

        /// <summary>
        /// Retrieve a full JSON document by id. Returned JSON should include the store's concurrency token if present
        /// (Cosmos will include "_etag" in the returned body).
        /// </summary>
        Task<string> GetOwnedJsonByIdAsync(string id, string ownerOrganizationId, CancellationToken ct = default);
        Task<string> GetJsonByIdAsync(string id, CancellationToken ct = default);
        Task<string> GetJsonByEntityTypeAndKeyAsync(string key, string entityType, string ownerOrganizationId, CancellationToken ct = default);

        /// <summary>
        /// Upsert a raw JSON document. If expectedETag is provided and the store supports optimistic concurrency,
        /// the upsert must fail if the current server-side etag does not match.
        /// </summary>
        Task<SyncUpsertResult> UpsertJsonAsync(string json, string expectedETag = null, CancellationToken ct = default);
        Task<SyncUpsertResult> UpsertJsonAsync(string json, EntityHeader org, EntityHeader user, CancellationToken ct = default);
        Task<EntityHeader> GetEntityHeaderForRecordAsync(string id, CancellationToken ct = default);
        Task<InvokeResult<EhResolvedEntity>> ResolveEntityHeadersAsync(string id, CancellationToken ct = default, bool dryRun = false);
        Task<InvokeResult<List<EhResolvedEntity>>> ResolveEntityHeadersAsync(string entityType, string continuationToken, int pageSize = 100, int maxPagesThisRun = 10, CancellationToken ct = default, bool dryRun = false);
        Task<InvokeResult<NodeLocatorResult>> AddNodeLocatorsAsync(string continuationToken, int pageSize = 100, int maxPagesThisRun = 10, CancellationToken ct = default, bool dryRun = false);

        Task<string> ScanContainerAsync(Func<CosmosScanRow, CancellationToken, Task> handleRowAsync,
            string continuationToken = null, string entityType = null, int pageSize = 100, int maxPagesThisRun = 10, string fixedPartitionKey = null, CancellationToken ct = default);
    }
}
