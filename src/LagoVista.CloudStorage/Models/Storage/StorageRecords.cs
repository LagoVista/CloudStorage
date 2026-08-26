using System;
using LagoVista.Core.Models;
using LagoVista.Core;

namespace LagoVista.CloudStorage.Storage
{
    /// <summary>
    /// Minimal record contract for mutable scratch data.
    /// Storage identity is deterministic: Id is the record key and Organization.Id
    /// is the canonical organization scope path.
    /// </summary>
    public interface IScratchDataRecord
    {
        NormalizedId32 Id { get; set; }
        EntityHeader Organization { get; set; }
    }

    /// <summary>
    /// Minimal record contract for durable mutable application data.
    /// Storage identity is deterministic: Id is the record key and Organization.Id
    /// is the canonical organization scope path. CloudStorage owns timestamp invariants.
    /// </summary>
    public interface IApplicationDataRecord
    {
        NormalizedId32 Id { get; set; }
        EntityHeader Organization { get; set; }
        UtcTimestamp CreationDate { get; set; }
        UtcTimestamp LastUpdatedDate { get; set; }
    }

    /// <summary>
    /// Minimal record contract for mutable operational data. Operational records
    /// are small, standalone, row-like records keyed by organization plus Id.
    /// CloudStorage owns CreationDate and LastUpdatedDate invariants.
    /// </summary>
    public interface IOperationalDataRecord
    {
        string Id { get; set; }
        string OrganizationId { get; set; }
        DateTime CreationDate { get; set; }
        DateTime LastUpdatedDate { get; set; }
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
