using LagoVista.CloudStorage.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.CloudStorage.Interfaces
{
    public interface ISyncRepository
    {
        Task<IReadOnlyList<SyncEntitySummary>> GetSummariesAsync(
            string entityType,
            string search = null,
            int take = 200,
            CancellationToken ct = default);

        /// <summary>
        /// Retrieve a full JSON document by id. Returned JSON should include the store's concurrency token if present
        /// (Cosmos will include "_etag" in the returned body).
        /// </summary>
        Task<string> GetJsonByIdAsync(string id, CancellationToken ct = default);

        /// <summary>
        /// Upsert a raw JSON document. If expectedETag is provided and the store supports optimistic concurrency,
        /// the upsert must fail if the current server-side etag does not match.
        /// </summary>
        Task<SyncUpsertResult> UpsertJsonAsync(string json, string expectedETag = null, CancellationToken ct = default);
    }
}
