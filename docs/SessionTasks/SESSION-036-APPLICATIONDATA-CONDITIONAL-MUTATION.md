# Session Task 036 — ApplicationData Conditional Mutation

## Status

**COMPLETE — GREEN WORKSTREAM BUILD; READY FOR INTEGRATION REVIEW**

Campaigns Session 033 proved that durable provider-private execution checkpoints cannot be made concurrency-safe on the current `IApplicationDataStore` contract because `UpdateAsync(record)` has no expected version / ETag / compare-and-swap predicate and the Mongo implementation performs an unconditional replacement.

## Objective

Add the smallest provider-neutral conditional/versioned mutation capability to `LagoVista/CloudStorage` so ApplicationData consumers can reject stale writers atomically.

This session belongs to **LagoVista/CloudStorage**. Do not implement Campaigns checkpoint state here.

## Repository

- `LagoVista/CloudStorage`
- Start from current `master`.

Read first:

- `src/LagoVista.CloudStorage/Interfaces/StorageProviders/IApplicationDataStore.cs`
- `src/LagoVista.CloudStorage/Storage/StorageProviders/Mongo/MongoApplicationDataStore.cs`
- `src/LagoVista.CloudStorage/Storage/StorageProviders/Mongo/MongoMutableRecordStore.cs`
- `tests/LagoVista.StorageProvider.Tests/Mongo/MongoApplicationDataStoreIntegrationTests.cs`
- `docs/Application Data Storage Strategy.md`
- Campaigns Session 033 completion report:
  - https://github.com/LagoVista/Campaigns/blob/master/docs/SessionTasks/SESSION-033-PROVIDER-EXECUTION-CHECKPOINT-SPINE.md

## Required semantics

Add a reusable ApplicationData mutation primitive that can distinguish:

- successful update of the version the caller actually read;
- stale-writer conflict;
- missing record;
- invalid input.

The concurrency check must be enforced atomically by the storage provider. A read-then-check-then-replace sequence in application code is not sufficient.

## Contract design

Prefer the smallest additive contract.

Acceptable shapes include, for example:

- `UpdateIfVersionAsync(record, expectedVersion)`;
- `TryUpdateAsync(record, expectedVersion)` returning an explicit result;
- a versioned ApplicationData mutation request.

Do not break existing `UpdateAsync(record)` consumers unless there is a strong repository-wide reason.

The new API must return or expose enough information for a caller to distinguish a stale-version conflict from generic storage failure.

## Version identity

Choose one durable version identity and document it.

Options may include:

- explicit revision/version field on ApplicationData records;
- storage-provider ETag/revision metadata surfaced through the shared contract;
- another provider-neutral opaque concurrency token.

Requirements:

- changes monotonically or uniquely on successful mutation;
- survives serialization/roundtrip;
- cannot be forged accidentally by normal record mutation;
- does not expose Mongo-specific vocabulary in the shared API;
- can be carried by repositories/services without requiring provider-specific types.

Do not use wall-clock timestamps alone as the concurrency token unless atomic uniqueness/equality semantics are proven.

## Mongo implementation

The Mongo provider must implement stale-writer rejection in **one atomic database mutation**.

The write predicate must include:

- record identity / organization key;
- expected concurrency token/version.

Prove:

1. writer A and writer B both load version N;
2. writer A updates successfully to N+1;
3. writer B attempts to update using expected N;
4. Mongo rejects B as stale;
5. A's accepted value remains persisted.

Do not implement this as:

1. load;
2. compare in memory;
3. unconditional replace.

## Existing UpdateAsync compatibility

Preserve current behavior for existing callers unless explicitly migrated.

Document whether:

- `UpdateAsync(record)` remains unconditional;
- it delegates to the new conditional API only when a token is present;
- or a migration strategy is required.

The first goal is an additive safe primitive that Session 033 can consume.

## Storage-provider scope

Implement Mongo fully.

Inspect other `IApplicationDataStore` implementations. If additional providers exist:

- either implement equivalent conditional semantics;
- or make support/capability explicit and fail safely rather than silently downgrading to unconditional update.

Do not claim cross-provider safety without proof.

## Result model

Use a provider-neutral result/error shape.

At minimum callers must be able to identify:

- Updated;
- Conflict / stale version;
- NotFound.

Avoid string-parsing exception messages as the public contract.

## Required tests

At minimum add/extend tests for:

- insert establishes initial concurrency token/version;
- get roundtrips the token;
- successful conditional update advances/replaces token;
- stale writer is rejected;
- stale writer does not overwrite accepted progress;
- missing record is distinct from conflict;
- tenant/organization key remains part of identity;
- ordinary existing `UpdateAsync` behavior remains compatible;
- serialization and record-type mapping remain stable;
- two concurrent tasks reproduce the stale-writer scenario deterministically enough to prove the database predicate.

Prefer an integration proof against the same Mongo path used by `MongoApplicationDataStore`.

## Documentation

Update `docs/Application Data Storage Strategy.md` with:

- conditional mutation semantics;
- token/version lifecycle;
- stale-writer behavior;
- when callers should use unconditional vs conditional update;
- provider-support expectations.

## Build / release proof

- run focused tests;
- run exact Build Server validation for the final source commit;
- record proof id/commit in the Completion Report;
- do not publish/release stable packages unless explicitly requested by the integration owner.

## Guardrails

Do not:

- add Campaigns-specific concepts to CloudStorage;
- add Pinterest/Reddit/Instagram vocabulary;
- implement provider execution checkpoints in this repo;
- depend on timestamps as a CAS token without atomic proof;
- silently fall back from conditional update to unconditional replace;
- redesign unrelated mutable/scratch/activity storage APIs;
- publish/release stable packages.

## Definition of done

One of these must be true:

1. ApplicationData exposes a provider-neutral conditional/versioned mutation primitive and Mongo proves atomic stale-writer rejection with exact green Build Server proof; or
2. a smaller underlying storage limitation is precisely documented and the session stops without pretending concurrency safety exists.

Successful completion unblocks the Campaigns provider-private checkpoint spine from Session 033.

## Completion Report

### Summary
Implemented the additive ApplicationData conditional-mutation primitive on `session-036-applicationdata-conditional-mutation`. Mongo now persists a provider-owned opaque revision token and performs conditional replacement with an atomic predicate containing record identity, organization scope, and expected token. Existing unconditional `UpdateAsync` behavior remains available. The workstream enrollment/dependency blockers were resolved by enrolling CloudStorage and current Logging in `feature/campaign-execution-foundation`; the authoritative Build Server proof is green.

### Final contract
- `GetVersionedAsync<TRecord>(StorageKey)` returns the record plus an opaque `ApplicationDataConcurrencyToken`.
- `UpdateIfVersionAsync(record, expectedVersion)` returns `Updated`, `Conflict`, or `NotFound` through `ApplicationDataMutationResult`.
- Successful conditional updates return the replacement token for the next write.

### Concurrency token/version decision
The token is provider-neutral and opaque to consumers. Mongo stores a unique revision value in provider-owned `_storageVersion` metadata rather than requiring a concurrency property on every `IApplicationDataRecord`. Inserts establish a token; successful unconditional and conditional replacements assign a fresh token; legacy records are initialized lazily on first versioned read.

### Mongo atomic mutation
Mongo conditional replacement uses one `ReplaceOneAsync` whose filter combines `_id`, canonical organization scope, and `_storageVersion == expectedVersion`. No read/compare/unconditional-replace sequence is used to accept a conditional write. A post-failure existence read is used only to distinguish `Conflict` from `NotFound`.

### Other provider behavior
Mongo is the current `IApplicationDataStore` implementation in this repository. The documented contract requires future providers to implement equivalent atomic semantics or fail safely; silent fallback to unconditional update is not permitted.

### Backwards compatibility
`UpdateAsync(record)` remains the existing unconditional/last-writer-wins API. It preserves stored `CreationDate`, advances `LastUpdatedDate`, and now refreshes provider-owned version metadata. Existing record POCOs do not need a new property.

### Stale-writer proof
Integration coverage was added for two readers loading the same version, writer A succeeding, writer B receiving `Conflict`, and A's accepted value remaining persisted. A concurrent `Task.WhenAll` proof asserts exactly one `Updated` and one `Conflict` result.

### Tests added
Added Mongo ApplicationData integration coverage for token roundtrip, successful version advancement, stale-writer rejection, accepted-value preservation, missing-record distinction, tenant isolation, and simultaneous writers.

### Build proof
Initial Build Server attempt `ac6c2d95ea2c4da4b7651bde04b50c65` failed at `validate-workstream-repository` with `PLAT005` because CloudStorage was not enrolled in the active campaign workstream. After enrolling CloudStorage and the required current Logging dependency, Logging workstream build `c8702c365b4f4478925031d2a8c5fc95` succeeded and supplied `LagoVista.IoT.Logging 7.0.12-ws-c-84165457`. The exact CloudStorage workstream build `ae7c9cdf69334a80a4b3260991c106c3` then succeeded, producing verified workstream packages `7.0.56-ws-c-08dd53b6`. The build compiled the Mongo integration-test assembly; the NuGet build workflow does not execute those integration tests as a separate runtime test phase. No stable package release was performed.

### Campaigns Session 033 resume impact
The reusable storage primitive required by Session 033 is implemented in source, but Campaigns should not resume against it until CloudStorage receives an exact green Build Server proof and the resulting package/integration path is intentionally made available.
