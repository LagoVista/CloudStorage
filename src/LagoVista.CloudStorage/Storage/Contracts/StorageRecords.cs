using System;
using LagoVista.Core.Models;

namespace LagoVista.CloudStorage.Storage
{
    /// <summary>
    /// Minimal record contract for mutable scratch data.
    /// </summary>
    public interface IScratchDataRecord
    {
        string Id { get; set; }
        EntityHeader Organization { get; set; }
    }

    /// <summary>
    /// Minimal record contract for durable mutable application data.
    /// </summary>
    public interface IApplicationDataRecord
    {
        string Id { get; set; }
        EntityHeader Organization { get; set; }
        DateTime CreationDate { get; set; }
        DateTime LastUpdatedDate { get; set; }
    }

    /// <summary>
    /// Minimal record contract for immutable activity records.
    /// Each concrete record type maps to its own provider table/collection.
    /// </summary>
    public interface IActivityRecord
    {
        string Id { get; set; }
        string OrganizationId { get; set; }
        string Organization { get; set; }
        DateTime CreationDate { get; set; }
    }

    /// <summary>
    /// Minimal transaction supplied to an account ledger. The store owns the
    /// authoritative running balance and integrity-chain metadata.
    /// </summary>
    public interface IAccountLedgerRecord
    {
        string Id { get; set; }
        string OrganizationId { get; set; }
        string Organization { get; set; }
        string AccountId { get; set; }
        string Account { get; set; }
        string TransactionType { get; set; }
        decimal? CreditAmount { get; set; }
        decimal? DebitAmount { get; set; }
        DateTime CreationDate { get; set; }
    }
}
