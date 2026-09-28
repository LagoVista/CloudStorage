using System;
using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.CloudStorage.Storage
{
    /// <summary>
    /// Operational record with an optimistic-concurrency token.
    /// Version is owned by the conditional store and increments after each successful replacement.
    /// </summary>
    public interface IConditionalOperationalDataRecord : IOperationalDataRecord
    {
        long Version { get; set; }
    }

    /// <summary>
    /// Result from a conditional mutation. Applied is false for normal contention.
    /// Current contains the latest record when available.
    /// </summary>
    public sealed class ConditionalMutationResult<TRecord>
        where TRecord : class, IConditionalOperationalDataRecord
    {
        public ConditionalMutationResult(bool applied, TRecord current)
        {
            Applied = applied;
            Current = current;
        }

        public bool Applied { get; }
        public TRecord Current { get; }
    }

    /// <summary>
    /// Compare-and-set operational storage for claims, leases, slots and other coordination state.
    /// Implementations must provide atomic create-if-absent, version-guarded replace, and version-guarded delete.
    /// </summary>
    public interface IConditionalOperationalDataStore<TRecord>
        where TRecord : class, IConditionalOperationalDataRecord
    {
        Task<TRecord> GetAsync(string organizationId, string id, CancellationToken cancellationToken = default);
        Task<StoragePageResult<TRecord>> QueryAsync(StorageQuery<TRecord> query, CancellationToken cancellationToken = default);
        Task<ConditionalMutationResult<TRecord>> TryCreateAsync(TRecord record, CancellationToken cancellationToken = default);
        Task<ConditionalMutationResult<TRecord>> TryReplaceAsync(TRecord record, long expectedVersion, CancellationToken cancellationToken = default);
        Task<ConditionalMutationResult<TRecord>> TryDeleteAsync(string organizationId, string id, long expectedVersion, CancellationToken cancellationToken = default);
    }
}
