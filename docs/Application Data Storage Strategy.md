# Application Data Storage Strategy

## Purpose

Application Data Storage is the CloudStorage capability for durable, mutable, queryable application records that do not require relational transaction semantics and are not rich LagoVista entities.

The semantic contract is:

```csharp
IApplicationDataStore<TRecord>
    where TRecord : IApplicationDataRecord
```

The initial and preferred provider is **MongoDB**.

Application repositories should depend on `IApplicationDataStore<TRecord>` rather than on MongoDB APIs. Provider choice, connection details, collection management, indexes, serialization, and schema reconciliation belong inside CloudStorage and application composition.

## What belongs here

Application Data Storage is intended for record-shaped application state with these characteristics:

- durable business/application data;
- records are created, updated, queried, and deleted over time;
- query patterns are known by the owning repository and can be declared explicitly;
- the data does not need joins, foreign keys, or multi-table relational transactions;
- the data is more structured and durable than scratch/cache state;
- the data is not an immutable event/activity stream;
- the data is not a rich LagoVista entity graph requiring the entity-storage infrastructure.

Typical candidates are the mutable Azure Table Storage workloads that historically used flat records and queryable columns but do not need to remain in Azure Table Storage.

## Provider decision

Application Data Storage lives in **MongoDB**.

MongoDB is a strong fit because it provides:

- one collection per record type;
- direct POCO/document persistence;
- flexible additive record evolution;
- explicit indexes for repository query patterns;
- efficient point reads and mutable document updates;
- no per-collection platform cost penalty in the self-hosted environment;
- a natural migration path from record-shaped Azure Table Storage workloads.

This capability may share Mongo cluster/client infrastructure with Scratch Storage, but it remains a separate semantic capability with separate settings and lifecycle expectations.

## Record contract

The minimum record contract is intentionally small:

```csharp
public interface IApplicationDataRecord
{
    string Id { get; set; }
    EntityHeader Organization { get; set; }
    DateTime CreationDate { get; set; }
    DateTime LastUpdatedDate { get; set; }
}
```

`EntityHeader Organization` follows the normal LagoVista application model.

`CreationDate` and `LastUpdatedDate` follow house naming. UTC is implied by platform convention.

Do not grow this interface into another `EntityBase`. Additional fields belong on the concrete record type because the owning application requires them, not because CloudStorage wants a richer universal base record.

## Store contract

The application-facing contract intentionally distinguishes insert from update:

```csharp
public interface IApplicationDataStore<TRecord>
    where TRecord : IApplicationDataRecord
{
    Task<TRecord> GetAsync(StorageKey key, CancellationToken cancellationToken = default);
    Task InsertAsync(TRecord record, CancellationToken cancellationToken = default);
    Task UpdateAsync(TRecord record, CancellationToken cancellationToken = default);
    Task DeleteAsync(StorageKey key, CancellationToken cancellationToken = default);
    Task<StoragePageResult<TRecord>> QueryAsync(
        StorageQuery<TRecord> query,
        CancellationToken cancellationToken = default);
}
```

Insert and update remain separate operations because Application Data represents durable application state. The API should not silently collapse create/update semantics into an upsert.

## Physical Mongo model

Each concrete `TRecord` maps to its own MongoDB collection.

Conceptually:

```text
CustomerPreferenceRecord
    -> customer_preference_record collection

NotificationRuleRecord
    -> notification_rule_record collection

SomeApplicationRecord
    -> some_application_record collection
```

Collection naming should be deterministic and owned by CloudStorage. Applications should not construct collection names themselves.

### POCO-first persistence

The preferred implementation is direct persistence of the application record POCO.

```text
repository model / record POCO
        |
        +--> IApplicationDataStore<TRecord>
                |
                +--> Mongo serializer / collection
```

Avoid introducing a provider DTO and mapping layer unless the physical representation has a concrete requirement that cannot reasonably be handled through Mongo serialization configuration.

The provider may own BSON naming, ignored fields, conventions, or other serialization mechanics without leaking those concerns into ordinary repositories.

## Storage definition

Application Data repositories declare the storage shape they need through `FlatStorageDefinition<TRecord>` during registration.

At minimum, Application Data requires a key:

```csharp
services.AddApplicationDataStore<MyRecord, MongoApplicationDataStore<MyRecord>>(
    definition => definition
        .KeyBy(x => x.Id)
        .Index(x => x.SomeQueryableField));
```

Useful provider-neutral metadata includes:

- `KeyBy(...)` for logical identity;
- `PartitionBy(...)` where a logical scope is useful to the provider/query contract;
- `Index(...)` for declared queryable fields;
- `RetainFor(...)` only for an application record type whose lifecycle explicitly requires automatic retention.

Time bucketing is primarily an Activity Record concern and should not be introduced into Application Data without a real workload that requires it.

## Index strategy

Indexes are declared by the owning repository/model registration, not hand-built ad hoc inside business code.

The Mongo provider should:

1. derive the required index set from the registered storage definition;
2. create missing indexes idempotently;
3. preserve unrelated/legacy indexes unless an explicit migration removes them;
4. fail clearly on incompatible definitions that cannot be reconciled safely;
5. never require application repositories to issue Mongo index commands.

`Id`/Mongo `_id` identity should remain efficient by construction. Organization-scoped access patterns should be indexed where the repository actually queries by organization.

Do not create indexes on every field "just in case." Indexes are part of the repository's declared query contract and carry write/storage cost.

## Timestamp behavior

CloudStorage should own the persistence invariants for the common timestamps:

- `InsertAsync` establishes `CreationDate` and `LastUpdatedDate` consistently when the provider is responsible for timestamping;
- `UpdateAsync` advances `LastUpdatedDate` while preserving `CreationDate`;
- UTC is assumed.

The exact ownership rule should be consistent across all Application Data implementations and covered by contract tests before consumer migration begins.

## Query behavior

`StorageQuery<TRecord>` is the provider-neutral query surface.

Application repositories should express queries through typed selectors rather than Mongo filter documents. CloudStorage translates supported operators into Mongo filters.

The provider should reject query shapes it cannot support safely rather than silently performing broad client-side filtering.

Paging uses `StoragePageResult<TRecord>` and an opaque continuation token. Consumers must not depend on Mongo cursor internals.

## Update and concurrency semantics

The first implementation should provide correct single-record insert/update/delete semantics.

Application Data does **not** automatically imply:

- relational transactions;
- cross-record atomicity;
- event sourcing;
- account-ledger semantics.

If a workload requires compare-and-swap/optimistic concurrency, that should be added deliberately to the semantic contract rather than smuggled through provider-specific Mongo behavior.

## Configuration

Application Data has an independent settings contract and configuration section:

```text
ApplicationDataStorage
```

The current settings are represented by:

```csharp
IApplicationDataStorageSettings
ApplicationDataStorageSettings
```

Application Data and Scratch may point to the same physical Mongo server while using different configuration keys, databases, credentials, or future policies.

That separation is intentional. Sharing a server must not merge the semantics of the two capabilities.

## Relationship to Mongo entity storage

This strategy is **not** the Mongo entity-storage modernization.

The entity-storage lane handles rich first-class LagoVista entities, existing Cosmos/document repository behavior, provider parity, and entity-specific storage concerns.

Application Data Storage handles simple record-shaped persistence behind `IApplicationDataStore<TRecord>`.

The two lanes may share only clearly reusable low-level Mongo plumbing such as client lifecycle/pooling. Do not refactor `DocumentDBRepoBase`, `MongoDocumentCollection`, or entity conversion machinery as part of implementing Application Data Storage.

## Migration from Azure Table Storage

Migration is an external/operational concern, not hidden behavior inside `IApplicationDataStore<TRecord>`.

A migration utility may support:

```text
Azure Table Storage
       |
       +--> selected/all records
                |
                +--> IApplicationDataStore<TRecord> / migration adapter
                           |
                           +--> MongoDB
```

Migration should support validation and repeatability and should not require the runtime store to dual-write indefinitely.

Consumer repositories can be converted mechanically once the store implementation is stable:

1. identify the existing record model and query patterns;
2. make the record implement `IApplicationDataRecord`;
3. declare key/index requirements during DI registration;
4. inject `IApplicationDataStore<TRecord>` into the repository;
5. remove Azure Table provider mechanics from the repository;
6. validate behavior against migrated/recreated data.

## Testing strategy

Most semantic behavior should be covered by fast unit/contract tests. Mongo-specific behavior should be covered by Docker-backed integration tests.

Important integration coverage includes:

- authenticated connection through `ApplicationDataStorageSettings`;
- collection creation/use;
- insert then get;
- duplicate insert behavior;
- update preserves `CreationDate` and advances `LastUpdatedDate`;
- delete;
- declared index creation/reconciliation;
- typed indexed queries;
- paging and continuation tokens;
- additive POCO evolution;
- startup/idempotent initialization;
- persistence across Mongo container restart when the Docker harness uses durable test storage.

Critical provider implementation paths should use LagoVista's `[CriticalCoverage]` marker where a regression would threaten data correctness or migration safety.

## Non-goals

Application Data Storage is not intended to become:

- the rich entity/document repository stack;
- a relational database abstraction;
- an append-only activity store;
- an account ledger;
- a metrics/time-series store;
- an arbitrary Mongo escape hatch exposed to application repositories.

## Definition of done

The Mongo Application Data capability is ready for first workload migration when:

- `IApplicationDataStore<TRecord>` has a production Mongo implementation;
- `ApplicationDataStorage` settings and DI are complete;
- one record type maps deterministically to one collection;
- direct POCO persistence is the normal path;
- declared indexes reconcile safely and idempotently;
- CRUD, typed query, and paging semantics are integration-tested;
- timestamp behavior is defined and tested;
- the Docker Mongo integration harness can run the provider suite repeatably;
- the implementation does not depend on or refactor the concurrent Mongo entity-storage modernization;
- migration tooling can populate the store independently of normal runtime repository behavior.

At that point the provider is considered infrastructure-ready. Individual Azure Table/application repository migrations are separate work items.