# Scratch Data Storage Strategy

## Purpose

Scratch Data Storage is the CloudStorage capability for small, mutable, durable working records that behave like a persistent cache or application scratch pad.

The semantic contract is:

```csharp
IScratchStore<TRecord>
    where TRecord : IScratchDataRecord
```

The initial and preferred provider is **MongoDB**.

Scratch Storage is intentionally distinct from Application Data Storage even when both use the same Mongo cluster. The distinction is about **lifecycle and meaning**, not the database product underneath them.

## What belongs here

Scratch Storage is intended for data with these characteristics:

- mutable working state;
- small or moderate record size;
- point read / upsert / delete are the dominant operations;
- data is useful and should survive ordinary process/pod restarts;
- losing the data may be inconvenient, but should not represent loss of authoritative business history;
- automatic expiration/cleanup is often desirable;
- query surface is intentionally small;
- the record does not need rich entity behavior or a large domain graph.

Examples include durable workflow scratch state, short-lived intermediate results, coordination state, resumable working context, or cache-like records where recomputation is possible but expensive or inconvenient.

Scratch Storage should not become a dumping ground for data whose real lifecycle has not been understood. If records become authoritative, broadly queryable business state, they likely belong in Application Data Storage instead.

## Provider decision

Scratch Storage lives in **MongoDB**.

MongoDB fits the scratch workload well because it provides:

- inexpensive collections in the self-hosted environment;
- direct POCO/document persistence;
- natural upsert semantics;
- TTL indexes for automatic cleanup;
- flexible additive record evolution;
- simple indexes for the small query surface;
- durable storage without requiring a relational schema.

Scratch Storage may share Mongo client/server infrastructure with Application Data Storage, but it retains separate settings, contracts, collection policies, and retention behavior.

## Record contract

The minimum record contract is intentionally tiny:

```csharp
public interface IScratchDataRecord
{
    string Id { get; set; }
    EntityHeader Organization { get; set; }
}
```

The interface does not require `CreationDate` or `LastUpdatedDate` because scratch records are not expected to provide a universal audit/history contract.

A concrete scratch record may include timestamps, expiration fields, state markers, or other lifecycle metadata when its workload requires them.

Do not grow `IScratchDataRecord` into another `EntityBase`. Scratch records should remain lightweight.

## Store contract

Scratch intentionally uses upsert semantics:

```csharp
public interface IScratchStore<TRecord>
    where TRecord : IScratchDataRecord
{
    Task<TRecord> GetAsync(StorageKey key, CancellationToken cancellationToken = default);
    Task UpsertAsync(TRecord record, CancellationToken cancellationToken = default);
    Task DeleteAsync(StorageKey key, CancellationToken cancellationToken = default);
    Task<StoragePageResult<TRecord>> QueryAsync(
        StorageQuery<TRecord> query,
        CancellationToken cancellationToken = default);
}
```

`UpsertAsync` is a deliberate semantic difference from Application Data Storage. Scratch callers generally care that the current working record exists with the latest state; they do not need create/update lifecycle distinction from the storage API.

## Physical Mongo model

Each concrete scratch record type maps to its own MongoDB collection.

Conceptually:

```text
WorkflowScratchRecord
    -> workflow_scratch_record collection

ProcessingCheckpointRecord
    -> processing_checkpoint_record collection

TemporaryContextRecord
    -> temporary_context_record collection
```

Collection naming should be deterministic and owned by CloudStorage. Callers should not construct Mongo collection names.

### POCO-first persistence

Scratch should normally persist the application record POCO directly:

```text
scratch record POCO
      |
      +--> IScratchStore<TRecord>
              |
              +--> Mongo serializer / collection
```

Avoid introducing provider DTOs and mapping layers unless a concrete provider requirement justifies them.

Mongo/BSON concerns remain inside the provider implementation.

## Storage definition

Scratch repositories declare their storage requirements during registration through `FlatStorageDefinition<TRecord>`.

A scratch store requires a logical key:

```csharp
services.AddScratchStore<MyScratchRecord, MongoScratchStore<MyScratchRecord>>(
    definition => definition
        .KeyBy(x => x.Id)
        .Index(x => x.SomeLookupField)
        .RetainFor(TimeSpan.FromDays(7)));
```

Useful provider-neutral metadata includes:

- `KeyBy(...)` for logical identity;
- `PartitionBy(...)` where organization/scope should be part of the logical access contract;
- `Index(...)` for the intentionally small set of queryable fields;
- `RetainFor(...)` for automatic expiration.

`BucketBy(...)` is generally not appropriate for Scratch Storage. If the workload is an ever-growing time stream, it is likely an Activity Record workload instead.

## Retention and TTL

Retention is a first-class Scratch concern.

When `RetainFor(...)` is configured, the Mongo provider should implement it with a TTL index or equivalent provider-native mechanism.

The implementation must make the expiration model explicit. Mongo TTL indexes operate on a date field, so CloudStorage must establish a deterministic expiration timestamp strategy rather than relying on callers to remember provider-specific TTL rules.

Two acceptable logical approaches are:

```text
record has provider-neutral expiration/lifecycle timestamp
        + retention duration
        -> Mongo TTL index
```

or

```text
provider persists an internal expiration field
calculated from the configured retention policy
        -> Mongo TTL index
```

The public scratch contract should not expose Mongo TTL-index mechanics.

Retention changes should reconcile idempotently. Missing TTL indexes should be created. Incompatible index/policy changes should fail clearly or use an explicit migration path rather than silently deleting data.

Scratch records without `RetainFor(...)` are durable until explicitly deleted.

## Index strategy

Scratch indexes should be intentionally sparse.

The provider should:

1. create the key/identity path efficiently;
2. create indexes declared with `Index(...)`;
3. create retention/TTL indexes when configured;
4. reconcile missing indexes idempotently;
5. preserve unrelated legacy indexes unless explicitly migrated;
6. reject unsafe/incompatible index drift rather than silently rebuilding it.

Do not index every property. Scratch is meant to have a small query surface.

If a scratch workload develops many query dimensions, complex filtering, or broad reporting requirements, that is a useful signal that the records may actually be Application Data.

## Organization scoping

`IScratchDataRecord` carries:

```csharp
EntityHeader Organization
```

Organization should remain part of the logical access boundary for tenant-owned scratch data.

Where records are queried by organization, registration should declare the relevant partition/index requirement so the Mongo provider can create an efficient access path.

CloudStorage should not infer arbitrary tenant filters from naming conventions in application code.

## Query behavior

`StorageQuery<TRecord>` is the provider-neutral query surface.

Scratch query support should remain deliberately modest. Typical operations are:

- get by key;
- query within an organization/scope;
- equality lookup on one or more declared indexed fields;
- page through a bounded set of working records.

Provider implementations should reject unsupported or dangerous query shapes instead of falling back to collection scans/client-side filtering without an explicit decision.

Paging uses `StoragePageResult<TRecord>` and opaque continuation tokens. Mongo cursor details must not leak to callers.

## Upsert behavior

`UpsertAsync` should be atomic for a single logical scratch record.

If the record does not exist, it is created. If it exists, the current document is replaced/updated according to the provider's defined serialization strategy.

Scratch does not automatically promise:

- compare-and-swap semantics;
- cross-record transactions;
- event history;
- conflict-free merging.

If a particular workload needs optimistic concurrency, that requirement should be modeled deliberately rather than assumed because Mongo supports additional mechanisms.

## Configuration

Scratch has an independent settings contract and configuration section:

```text
ScratchStorage
```

The current settings are represented by:

```csharp
IScratchStorageSettings
ScratchStorageSettings
```

Scratch Storage and Application Data Storage may point to the same Mongo server today. Duplicate configuration keys are intentional because the semantic capabilities may diverge later in database name, credentials, retention policies, scaling, or operational placement.

## Relationship to Application Data Storage

Both initial implementations use MongoDB, but the APIs intentionally encode different behavior.

| Scratch | Application Data |
| --- | --- |
| `IScratchStore<TRecord>` | `IApplicationDataStore<TRecord>` |
| `IScratchDataRecord` | `IApplicationDataRecord` |
| Upsert-oriented | Explicit insert/update |
| Small working-state surface | Durable application-state surface |
| Retention/expiration common | Retention workload-specific |
| Loss may be recoverable/inconvenient | Data is authoritative application state |
| Narrow query requirements | Broader declared indexed querying |

Do not merge these stores just because their first provider is MongoDB.

## Relationship to Mongo entity storage

Scratch Storage is **not** part of the rich Mongo entity-storage modernization.

The entity-storage lane owns first-class LagoVista entity persistence, Cosmos/Mongo provider parity, document/entity behavior, and its migration mechanics.

Scratch Storage handles lightweight `IScratchDataRecord` POCOs through `IScratchStore<TRecord>`.

The two lanes may share proven low-level Mongo connection/client infrastructure, but Scratch implementation must not refactor `DocumentDBRepoBase`, `MongoDocumentCollection`, or active entity-conversion work.

## Migration from Azure Table Storage

Some existing Azure Table workloads are really scratch/durable-cache workloads. Those should migrate to Scratch only when their lifecycle matches this semantic contract.

Migration utilities remain separate from the normal runtime store:

```text
Azure Table Storage
       |
       +--> selected/all scratch records
                |
                +--> migration adapter
                         |
                         +--> Mongo Scratch collection
```

A migration may import all records or a bounded recent window depending on the workload's useful lifetime.

If old scratch data has no continuing value, recreating the working set may be preferable to a full historical migration.

## Testing strategy

Most contract/definition validation should remain fast unit tests. Mongo behavior should use the existing Docker-backed Mongo integration harness.

Important integration coverage includes:

- authenticated connection through `ScratchStorageSettings`;
- collection creation/use;
- upsert creates a new record;
- second upsert replaces/updates the same logical record;
- get by key;
- delete;
- organization-scoped lookup;
- declared index creation/reconciliation;
- typed indexed queries;
- paging and continuation tokens;
- TTL/retention index creation;
- expiration behavior with a short test retention window;
- additive POCO evolution;
- initialization is idempotent;
- persistence across Mongo restart for non-expired records.

Critical provider paths should use LagoVista's `[CriticalCoverage]` marker where a regression could cause unintended retention, record loss, or isolation/query errors.

## Operational expectations

Scratch is durable working storage, not disposable process memory.

The Mongo deployment therefore participates in the normal platform durability/DR posture appropriate to the shared Mongo service. At the same time, consumer design should recognize that scratch data should generally be reconstructable or recoverable from authoritative state.

This distinction lets us avoid over-engineering scratch records while still preventing ordinary pod/process restarts from erasing useful work.

## Non-goals

Scratch Storage is not intended to become:

- the rich entity/document repository stack;
- a general authoritative application database;
- an append-only activity log;
- a metrics store;
- an account ledger;
- a relational transaction layer;
- an arbitrary Mongo API exposed to business repositories.

## Definition of done

The Mongo Scratch capability is ready for first workload migration when:

- `IScratchStore<TRecord>` has a production Mongo implementation;
- `ScratchStorage` settings and DI are complete;
- each record type maps deterministically to its own collection;
- direct POCO persistence is the normal path;
- upsert/get/delete semantics are integration-tested;
- declared indexes reconcile safely and idempotently;
- retention/TTL behavior is implemented and integration-tested;
- query/paging behavior is provider-neutral and tested;
- the Docker Mongo integration harness can run the provider suite repeatably;
- the implementation does not refactor or collide with the concurrent Mongo entity-storage modernization;
- migration/rebuild tooling is separate from normal runtime storage behavior.

At that point Scratch Storage is infrastructure-ready. Individual workload conversions can proceed independently based on lifecycle/value.