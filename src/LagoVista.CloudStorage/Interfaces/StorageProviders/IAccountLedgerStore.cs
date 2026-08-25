using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.CloudStorage.Storage
{
    /// <summary>
    /// Atomic append-only credit/debit ledger. Implementations own the authoritative
    /// running balance and integrity chain for each account + transaction type.
    /// </summary>
    public interface IAccountLedgerStore<TRecord>
        where TRecord : IAccountLedgerRecord
    {
        Task<AccountLedgerEntry<TRecord>> AddTransactionAsync(TRecord transaction, CancellationToken cancellationToken = default);
        Task<decimal> GetBalanceAsync(string organizationId, string accountId, string transactionType, CancellationToken cancellationToken = default);
        Task<StoragePageResult<AccountLedgerEntry<TRecord>>> QueryAsync(AccountLedgerQuery query, CancellationToken cancellationToken = default);
    }
}
