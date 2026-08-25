# Application Data Storage Strategy

## Purpose

Application Data Storage is the CloudStorage capability for durable, mutable, queryable application records that do not require relational transaction semantics and are not rich LagoVista entities.

The semantic contract is a single composed capability:

```csharp
IApplicationDataStore
```

Record type is supplied on each operation:

```csharp
await store.InsertAsync(record);
await store.UpdateAsync(record);
var record = await store.GetAsync<MyRecord>(key);
var page = await store.QueryAsync(new StorageQuery<MyRecord>());
```

The initial and preferred provider is **MongoDB**.

Application repositories depend on `IApplicationDataStore`, not on MongoDB APIs and not on a closed generic storage service. Provider choice, connection details, collection management, serialization, indexes, paging, and timestamp invariants belong inside CloudStorage.

## Core architectural rule

Repositories use storage through composition rather than inheritance.

```text
OLD

Repository
    IS-A storage implementation

NEW

Repository
    HAS-A storage capability
```

For example:

```csharp
public sealed class VtmMeetingRepo : IVtmMeetingRepo
{
    private readonly IApplicationDataStore _store;

    public VtmMeetingRepo(IApplicationDataStore store)
    {
        _store = store;
    }

    public Task AddAsync(VtmMeeting meeting)
        => _store.InsertAsync(meeting);
}
```

The repository owns domain query intent. CloudStorage owns persistence mechanics.

## What belongs here

Application Data Storage is intended for record-shaped application state with these characteristics:

- durable authoritative business/application data;
- records are created, updated, queried, and deleted over time;
- query patterns are known by the owning repository and can be declared explicitly;
- the data does not need joins, foreign keys, or multi-table relational transactions;
- the data is more authoritative and durable than scratch/cache state;
- the data is not an immutable event/activity stream;
- the data is not a rich LagoVista entity graph requiring entity-storage infrastructure.

Typical candidates include mutable Azure Table Storage workloads and runtime objects that historically inherited `EntityBase` primarily to obtain persistence.

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

Application Data may share Mongo client/server infrastructure with Scratch Storage, but it remains a separate semantic capability with separate settings and lifecycle expectations.

## Record contract

The common record contract is intentionally small and deterministic:

```csharp
public interface IApplicationDataRecord
{
    NormalizedId32 Id { get; set; }
    EntityHeader Organization { get; set; }
    UtcTimestamp CreationDate { get; set; }
    UtcTimestamp LastUpdatedDate { get; set; }
}
```

The common storage invariants are:

```text
identity          => Id
organization      => Organization.Id
created timestamp => CreationDate
updated timestamp => LastUpdatedDate
```

`Name` is deliberately not part of the storage contract. A concrete application record may be named when its domain requires that behavior, and repositories may sort/query by that field normally.

Do not grow `IApplicationDataRecord` into another `EntityBase`. Additional fields belong on the concrete record type because the owning application requires them.

## Store contract

Application Data intentionally distinguishes insert from update:

```csharp
public interface IApplicationDataStore
{
    Task<TRecord> GetAsync<TRecord>(StorageKey key, CancellationToken cancellationToken = default)
        where TRecord : class, IApplicationDataRecord;

    Task InsertAsync<TRecord>(TRecord record, CancellationToken cancellationToken = default)
        where TRecord : class, IApplicationDataRecord;

    Task UpdateAsync<TRecord>(TRecord record, CancellationToken cancellationToken = default)
        where TRecord : class, IApplicationDataRecord;

    Task DeleteAsync<TRecord>(StorageKey key, CancellationToken cancellationToken = default)
        where TRecord : class, IApplicationDataRecord;

    Task<StoragePageResult<TRecord>> QueryAsync<TRecord>(
        StorageQuery<TRecord> query,
        CancellationToken cancellationToken = default)
        where TRecord : class, IApplicationDataRecord;
}
```

There is one `IApplicationDataStore` registration. Adding a new record type does not require registering `IApplicationDataStore<TRecord>`.

## Deterministic physical identity

Each concrete record type maps to exactly one MongoDB collection. The mapping is deterministic and owned by CloudStorage:

```text
VtmMeeting       -> VtmMeeting collection
ProducedArtifact -> ProducedArtifact collection
SopWorkItem      -> SopWorkItem collection
```

Callers never provide a collection name to CRUD/query methods.

The current convention derives collection identity directly from the CLR type name through `StorageRecordIdentity`. Changing a persisted CLR type name is therefore a storage migration decision, not a per-repository configuration tweak.

## POCO-first persistence

The normal path is direct persistence of the application record POCO:

```text
record POCO
    |
    +--> IApplicationDataStore
            |
            +--> Mongo serializer / deterministic collection
```

Provider DTOs and mapping layers should not be introduced unless a concrete physical representation requirement makes them necessary.

This specifically allows migrations such as:

```text
ProducedArtifact
    -> ProducedArtifactTableDto
    -> Azure Table Storage
```

becoming:

```text
ProducedArtifact
    -> IApplicationDataStore
    -> MongoDB
```

with the storage DTO removed.

## Record configuration

Record registration is **not required to make a type storable**. Identity, organization scope, timestamps, serialization, and collection name are conventions.

Optional configuration only declares additional storage behavior such as indexes:

```csharp
services.ConfigureApplicationData<VtmMeeting>(definition =>
{
    definition.Index(x => x.AgentSession.Id);
    definition.Index(x => x.Archived);
});
```

Nested selectors such as `Organization.Id` and `AgentSession.Id` are supported.

Application Data should not use configuration to override collection identity, key identity, or organization identity.

## Index strategy

Mongo always has efficient `_id` identity and CloudStorage creates the canonical `Organization.Id` index for mutable record collections.

Additional indexes are declared through `ConfigureApplicationData<TRecord>()` for real repository query paths.

The Mongo provider should:

1. derive required indexes from the registered definition;
2. create missing indexes idempotently;
3. preserve unrelated indexes unless an explicit migration removes them;
4. fail clearly on incompatible definitions;
5. never require domain repositories to issue Mongo index commands.

Do not index every property just in case.

## Timestamp behavior

CloudStorage owns the common timestamp invariants:

- `InsertAsync` establishes both `CreationDate` and `LastUpdatedDate`;
- `UpdateAsync` preserves the stored `CreationDate` and advances `LastUpdatedDate`;
- timestamps use `UtcTimestamp`.

Consumers do not need provider-specific timestamp logic.

## Query behavior

`StorageQuery<TRecord>` is the provider-neutral query surface.

Repositories express queries using typed selectors:

```csharp
var query = new StorageQuery<VtmMeeting>()
    .Where(x => x.Organization.Id, StorageFilterOperator.Equal, organizationId)
    .Where(x => x.Archived, StorageFilterOperator.Equal, false)
    .OrderBy(x => x.LastUpdatedDate, StorageSortDirection.Descending);
```

CloudStorage translates supported property paths and operators into Mongo queries.

Paging uses `StoragePageResult<TRecord>` and an opaque continuation token. Consumers must not interpret provider-owned token contents.

## Update and concurrency semantics

The initial implementation provides correct single-record insert/update/delete semantics.

Application Data does **not** automatically imply:

- relational transactions;
- cross-record atomicity;
- event sourcing;
- account-ledger semantics;
- compare-and-swap concurrency.

Workloads that require optimistic concurrency should add that semantic deliberately rather than leaking provider-specific Mongo behavior.

## Configuration

Application Data has an independent settings section:

```text
ApplicationDataStorage
```

represented by:

```csharp
IApplicationDataStorageSettings
ApplicationDataStorageSettings
```

`AddApplicationDataStorageConnection()` registers the settings, shared Mongo client factory, and the single scoped `IApplicationDataStore` Mongo implementation.

Application Data and Scratch may point to the same Mongo server while using different configuration keys, databases, credentials, and future operational policies.

## Relationship to Scratch Storage

Application Data and Scratch share low-level mutable Mongo infrastructure but expose different semantics:

| Application Data | Scratch |
| --- | --- |
| authoritative application state | reconstructable working state |
| explicit insert/update | upsert-oriented |
| timestamps are invariant | timestamps are workload-specific |
| retention is unusual | retention/TTL is common |
| broader declared querying | intentionally narrow querying |

Sharing a Mongo server or internal provider core must not merge these semantic contracts.

## Relationship to Mongo entity storage

Application Data Storage is **not** the rich Mongo entity-storage modernization.

Entity storage handles rich first-class LagoVista entities, entity-specific repository behavior, and Cosmos/Mongo parity.

Application Data handles simple durable runtime/application records behind `IApplicationDataStore`.

The lanes may share proven low-level Mongo serialization/client plumbing, but Application Data should not reintroduce `DocumentDBRepoBase`-style inheritance.

## Migration from Azure Table Storage or EntityBase persistence

Migration is an explicit operational concern, not hidden behavior inside the runtime store.

A typical repository conversion is:

1. identify the existing record model and query patterns;
2. make the model implement `IApplicationDataRecord`;
3. replace duplicate organization-id storage with canonical `Organization` where appropriate;
4. declare only the indexes required by domain queries;
5. inject `IApplicationDataStore` into the repository;
6. replace inherited/provider CRUD calls with composed store calls;
7. remove provider DTO/mapping layers that no longer have a purpose;
8. migrate/recreate existing data explicitly;
9. validate behavior before removing the old provider path.

## Testing strategy

Fast unit/contract tests cover conventions and provider-neutral behavior. Mongo-specific behavior should be covered by a Docker-backed integration harness.

Important Mongo integration coverage includes:

- authenticated connection through `ApplicationDataStorageSettings`;
- deterministic collection selection for multiple record types;
- insert then get;
- duplicate insert behavior;
- update preserves `CreationDate` and advances `LastUpdatedDate`;
- delete;
- organization scoping;
- nested declared index creation/reconciliation;
- typed indexed queries;
- sorting;
- paging and continuation tokens;
- additive POCO evolution;
- startup/idempotent index initialization;
- persistence across Mongo restart when the harness uses durable storage.

## Non-goals

Application Data Storage is not intended to become:

- the rich entity/document repository stack;
- a relational database abstraction;
- an append-only activity/history store;
- an account ledger;
- a metrics/time-series store;
- an arbitrary Mongo escape hatch exposed to business repositories.

## Definition of done

The Mongo Application Data capability is infrastructure-ready when:

- one non-generic `IApplicationDataStore` has a production Mongo implementation;
- `ApplicationDataStorage` settings and DI are complete;
- record identity and organization scope are deterministic conventions;
- one CLR record type maps deterministically to one collection;
- direct POCO persistence is the normal path;
- no per-record store DI registration is required;
- nested declared indexes reconcile safely and idempotently;
- CRUD, typed query, sorting, and paging semantics are integration-tested;
- timestamp behavior is defined and tested;
- migration tooling remains separate from normal runtime storage behavior.

Individual consumer migrations remain separate work items after the provider is infrastructure-ready.
