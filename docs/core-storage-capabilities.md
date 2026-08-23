# Core storage capabilities

CloudStorage exposes semantic application-storage capabilities. Consumers choose the behavior they need; provider details remain inside CloudStorage and application composition.

## Capability map

| Capability | Record contract | Initial provider | Purpose |
| --- | --- | --- | --- |
| `IActivityRecordStore<TRecord>` | `IActivityRecord` | Cassandra 5 | Immutable, high-volume things that happened |
| `IScratchStore<TRecord>` | `IScratchDataRecord` | MongoDB | Small mutable scratch/durable-cache records |
| `IApplicationDataStore<TRecord>` | `IApplicationDataRecord` | MongoDB | Durable mutable application records |
| `IMetricsStore` | `MetricRecord` + definitions | PostgreSQL + TimescaleDB | Dimensional numeric measurements and aggregates |
| `IAccountLedgerStore<TRecord>` | `IAccountLedgerRecord` | PostgreSQL | Atomic credit/debit account ledgers |

These contracts are deliberately not interchangeable. Their mutation, consistency, scale, and query semantics differ.

## Record terminology

Storage qualifiers use `Record`, not `Entity`. LagoVista entities commonly represent richer domain graphs; these interfaces require only persistence invariants.

Do not recreate `EntityBase` through interface accretion.

House timestamps are `CreationDate` and `LastUpdatedDate`. UTC is implied by platform convention.

## Activity Records

```csharp
public interface IActivityRecord
{
    string Id { get; set; }
    string OrganizationId { get; set; }
    string Organization { get; set; }
    DateTime CreationDate { get; set; }
}
```

`CreationDate` is the canonical activity timestamp. Each concrete record type maps to its own Cassandra table. The store exposes insert, batch insert, and history query only.

Use for immutable application activity at very high volume. Do not use for mutable business state.

## Scratch

```csharp
public interface IScratchDataRecord
{
    string Id { get; set; }
    EntityHeader Organization { get; set; }
}
```

The store exposes get, upsert, delete, and simple query behavior. TTL/expiration may be provider configuration.

Scratch remains a separate semantic capability even when it shares a Mongo server/client with Application Data.

## Application Data

```csharp
public interface IApplicationDataRecord
{
    string Id { get; set; }
    EntityHeader Organization { get; set; }
    DateTime CreationDate { get; set; }
    DateTime LastUpdatedDate { get; set; }
}
```

The store exposes explicit insert/update/delete plus indexed query behavior. This is the general durable mutable record store, not a relational transaction system.

## Metrics

`IMetricsStore` records numeric observations identified by organization, metric, timestamp, value, and declared flexible dimensions.

Metric definitions govern legal dimension keys. The public API does not expose SQL, Timescale functions, or the physical dimension representation.

Prometheus remains operational monitoring. Selected low-cardinality business metrics may be projected there, but arbitrary application metrics remain in the metrics store.

## Account Ledger

```csharp
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
```

The caller provides transaction intent. The store owns the authoritative resulting balance and integrity-chain metadata.

Exactly one of credit or debit must be supplied. Implementations must serialize competing writes to the same logical account ledger atomically while allowing independent ledgers to proceed concurrently.

The account is intentionally generic. IoT devices are one consumer, not part of the storage abstraction.

## Configuration boundaries

Semantic settings remain independently configurable:

```text
CassandraStorage
ScratchStorage
ApplicationDataStorage
MetricsStorage
AccountLedgerStorage
```

Two capabilities may point to the same physical server while remaining separate contracts. Shared transport/client pooling is an implementation detail.

## Strategic usage

Application repositories/services should be ordinary classes using constructor injection:

```text
business code
    |
    +--> semantic CloudStorage contract
              |
              +--> provider implementation
                        |
                        +--> typed settings / pooled client
```

Avoid provider switches, connection strings, CQL, Mongo query documents, Npgsql, SQL, and Azure Table key mechanics in ordinary business repositories.

## Current implementation cards

The implementation roadmap is maintained in `nuviot/k8s` Card 12:

- 12d Activity Records / Cassandra
- 12e Scratch / MongoDB
- 12f Application Data / MongoDB
- 12j Metrics / PostgreSQL + TimescaleDB
- 12k Account Ledger / PostgreSQL

Azure Table migration and repository conversion are tracked separately so provider migration logic never becomes hidden runtime-store behavior.
