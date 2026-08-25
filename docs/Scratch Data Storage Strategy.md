# Scratch Data Storage Strategy

## Purpose

Scratch Data Storage is the CloudStorage capability for small, mutable, durable working records that behave like a persistent cache or application scratch pad.

The semantic contract is a single composed capability:

```csharp
IScratchStore
```

Record type is supplied on each operation:

```csharp
await store.UpsertAsync(record);
var record = await store.GetAsync<MyScratchRecord>(key);
var page = await store.QueryAsync(new StorageQuery<MyScratchRecord>());
```

The initial and preferred provider is **MongoDB**.

Scratch remains intentionally distinct from Application Data even when both share the same Mongo cluster and low-level provider machinery. The distinction is about lifecycle and meaning.

## Core architectural rule

Repositories use Scratch through composition rather than inheriting provider storage classes.

```text
Repository
    HAS-A IScratchStore
```

The repository expresses domain intent. CloudStorage owns collection selection, serialization, indexes, paging, TTL behavior, and provider mechanics.

## What belongs here

Scratch Storage is intended for data with these characteristics:

- mutable working state;
- small or moderate record size;
- point read / upsert / delete are dominant operations;
- data should survive normal process/pod restarts;
- losing it may be inconvenient or expensive but should not destroy authoritative business history;
- automatic expiration/cleanup is often desirable;
- query surface is intentionally small;
- the record does not need rich entity behavior or a large domain graph.

Examples include durable workflow scratch state, intermediate results, coordination state, resumable context, processing checkpoints, and cache-like records where recomputation is possible.

Scratch must not become a dumping ground for records whose lifecycle has not been understood. Records that become authoritative or broadly queryable should normally move to Application Data.

## Provider decision

Scratch Storage lives in **MongoDB**.

MongoDB fits this workload because it provides:

- inexpensive collections in the self-hosted environment;
- direct POCO/document persistence;
- natural upsert semantics;
- TTL indexes;
- flexible additive record evolution;
- simple indexes for narrow query surfaces;
- durable storage without relational schema requirements.

Scratch may share Mongo client/server infrastructure with Application Data while retaining independent settings, databases, credentials, retention behavior, and future operational placement.

## Record contract

The minimum contract is intentionally tiny:

```csharp
public interface IScratchDataRecord
{
    NormalizedId32 Id { get; set; }
    EntityHeader Organization { get; set; }
}
```

The common storage invariants are:

```text
identity     => Id
organization => Organization.Id
```

Scratch does not require universal creation/update timestamps because those are not part of the semantic promise. Concrete records may add timestamps or state markers when their workload requires them.

Do not grow `IScratchDataRecord` into another `EntityBase`.

## Store contract

Scratch deliberately uses upsert semantics:

```csharp
public interface IScratchStore
{
    Task<TRecord> GetAsync<TRecord>(StorageKey key, CancellationToken cancellationToken = default)
        where TRecord : class, IScratchDataRecord;

    Task UpsertAsync<TRecord>(TRecord record, CancellationToken cancellationToken = default)
        where TRecord : class, IScratchDataRecord;

    Task DeleteAsync<TRecord>(StorageKey key, CancellationToken cancellationToken = default)
        where TRecord : class, IScratchDataRecord;

    Task<StoragePageResult<TRecord>> QueryAsync<TRecord>(
        StorageQuery<TRecord> query,
        CancellationToken cancellationToken = default)
        where TRecord : class, IScratchDataRecord;
}
```

There is one `IScratchStore` DI registration. Adding a new scratch record type does not require registering `IScratchStore<TRecord>`.

`UpsertAsync` is a deliberate semantic difference from Application Data. Scratch callers care that the latest working state exists, not whether the operation was technically a create or update.

## Deterministic physical identity

Each concrete scratch record type maps to exactly one Mongo collection through `StorageRecordIdentity`:

```text
WorkflowScratchRecord      -> WorkflowScratchRecord collection
ProcessingCheckpointRecord -> ProcessingCheckpointRecord collection
TemporaryContextRecord     -> TemporaryContextRecord collection
```

Callers never provide or override collection names during CRUD/query operations.

Changing the persisted CLR type name is therefore a storage migration decision.

## POCO-first persistence

Scratch persists the application POCO directly:

```text
scratch POCO
    |
    +--> IScratchStore
            |
            +--> Mongo serializer / deterministic collection
```

Provider DTOs and mapping layers should not be added without a concrete physical-storage requirement.

Mongo/BSON mechanics stay inside CloudStorage.

## Record configuration

Scratch records are storable without per-record registration. Identity, organization scope, serialization, and collection name are deterministic conventions.

Optional configuration only declares additional behavior such as indexes and retention:

```csharp
services.ConfigureScratchData<AgentExecutionState>(definition =>
{
    definition.Index(x => x.AgentSessionId);
    definition.RetainFor(TimeSpan.FromDays(14));
});
```

Nested selectors are supported.

Configuration must not override collection identity, key identity, or organization identity.

## Retention and TTL

Retention is a first-class Scratch concern.

When `RetainFor(...)` is configured, the Mongo provider maintains a provider-owned expiration field and a TTL index. The scratch POCO does not need to expose Mongo expiration mechanics.

Conceptually:

```text
Upsert scratch record
    + configured retention
    -> provider calculates expiration timestamp
    -> provider-owned _storageExpiresUtc field
    -> Mongo TTL index
```

Every upsert refreshes the provider-owned expiration timestamp for that record.

Scratch records without `RetainFor(...)` remain durable until explicitly deleted.

The provider-owned field is ignored when deserializing the scratch POCO so persistence remains POCO-first from the application's perspective.

## Index strategy

Scratch indexes should remain sparse.

Mongo has efficient `_id` identity and CloudStorage creates the canonical `Organization.Id` index for mutable record collections.

Additional indexes are declared only for real lookup/query patterns:

```csharp
services.ConfigureScratchData<MyScratchRecord>(definition =>
    definition.Index(x => x.SomeLookupField));
```

When retention is configured, CloudStorage also creates the TTL index.

If a scratch workload develops many query dimensions or reporting requirements, that is a signal it may actually be Application Data.

## Query behavior

`StorageQuery<TRecord>` is the provider-neutral query surface.

Typical Scratch queries are:

- get by key;
- query within `Organization.Id`;
- equality lookup on one or more declared indexed fields;
- sort/page through a bounded working set.

CloudStorage translates typed property paths and supported operators into Mongo queries. Unsupported behavior should fail explicitly rather than silently falling back to broad client-side filtering.

Paging uses `StoragePageResult<TRecord>` and opaque continuation tokens.

## Upsert behavior

`UpsertAsync` is atomic for a single logical record replacement/upsert.

If the record does not exist it is created. If it exists its current document is replaced according to the provider's serialization strategy.

Scratch does not automatically promise:

- compare-and-swap semantics;
- cross-record transactions;
- event history;
- conflict-free merging.

Those requirements should be modeled deliberately when needed.

## Configuration

Scratch has an independent configuration section:

```text
ScratchStorage
```

represented by:

```csharp
IScratchStorageSettings
ScratchStorageSettings
```

`AddScratchStorageConnection()` registers the settings, shared Mongo client factory, and the single scoped `IScratchStore` Mongo implementation.

## Relationship to Application Data

Both implementations use the same low-level Mongo record core while preserving separate semantic APIs:

| Scratch | Application Data |
| --- | --- |
| `IScratchStore` | `IApplicationDataStore` |
| `IScratchDataRecord` | `IApplicationDataRecord` |
| upsert-oriented | explicit insert/update |
| reconstructable working state | authoritative application state |
| retention/TTL common | retention unusual |
| timestamps workload-specific | creation/update timestamps invariant |
| narrow query requirements | broader declared querying |

Do not merge these semantic capabilities just because they share a provider implementation core.

## Relationship to Mongo entity storage

Scratch Storage is not the rich Mongo entity-storage modernization.

Entity storage owns first-class LagoVista entities and entity-specific repository behavior.

Scratch owns lightweight runtime POCOs through `IScratchStore` and should not reintroduce `DocumentDBRepoBase`-style inheritance.

## Migration from existing storage

Some Azure Table or embedded runtime state is really durable scratch state. Those workloads should migrate only when their lifecycle matches this contract.

A typical conversion is:

1. identify the working-state record and useful lifetime;
2. make it implement `IScratchDataRecord`;
3. declare only required indexes and optional retention;
4. inject `IScratchStore` into the owning repository/service;
5. replace provider-specific CRUD with `GetAsync<T>`, `UpsertAsync`, `DeleteAsync<T>`, and `QueryAsync<T>`;
6. remove provider DTO/mapping layers that no longer serve a purpose;
7. decide whether old scratch data should migrate or simply be recreated.

## Testing strategy

Fast contract tests cover deterministic identity, nested selectors, DI composition, and record configuration.

Mongo-specific behavior should be covered with a Docker-backed integration harness, including:

- authenticated connection through `ScratchStorageSettings`;
- deterministic separate collections for multiple record types;
- upsert creates a record;
- second upsert replaces the same record;
- get/delete;
- organization-scoped lookup;
- declared nested indexes;
- sorting and paging;
- TTL index creation;
- expiration with a short retention window;
- retention refresh on upsert;
- additive POCO evolution;
- idempotent initialization;
- persistence across Mongo restart for non-expired records.

## Operational expectations

Scratch is durable working storage, not disposable process memory.

The underlying Mongo service participates in normal platform durability appropriate to that service. Consumers should nevertheless design scratch state so it can generally be reconstructed from authoritative data when necessary.

## Non-goals

Scratch Storage is not intended to become:

- the rich entity/document repository stack;
- a general authoritative application database;
- an append-only activity/history store;
- a metrics store;
- an account ledger;
- a relational transaction layer;
- an arbitrary Mongo API exposed to application repositories.

## Definition of done

The Mongo Scratch capability is infrastructure-ready when:

- one non-generic `IScratchStore` has a production Mongo implementation;
- `ScratchStorage` settings and DI are complete;
- record identity and organization scope are deterministic conventions;
- each record type maps deterministically to its own collection;
- direct POCO persistence is normal;
- no per-record store DI registration is required;
- upsert/get/delete/query/paging semantics are integration-tested;
- declared indexes reconcile safely and idempotently;
- provider-owned TTL behavior is integration-tested;
- migration/rebuild tooling remains separate from normal runtime behavior.

Individual workload conversions can then proceed independently based on lifecycle and value.
