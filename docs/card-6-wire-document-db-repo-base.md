# Card 6 - Wire DocumentDBRepoBase to Provider Factory

## Objective

Make `DocumentDBRepoBase<TEntity>` resolve its storage provider internally so existing derived repositories can switch between Cosmos and Mongo through configuration alone.

## Key acceptance rule

No derived `DocumentDBRepoBase<TEntity>` repository constructor changes are required to adopt the new adapter.

## Status

Complete for the primary repository path. `DocumentDBRepoBase<TEntity>` now selects and delegates to the configured storage provider while preserving existing constructor signatures, and the real Mongo path has passed Docker-backed integration validation.

## Completed work

- [x] `DocumentStorageSettingsResolver` selects Cosmos or Mongo by logical database configuration.
- [x] `DocumentStorageFactory` constructs both rich Cosmos and rich Mongo providers.
- [x] `DocumentDBRepoBase<TEntity>` resolves the provider through the existing constructor path.
- [x] `_storage` can be re-resolved when `SetConnection` changes the logical connection.
- [x] Existing derived repository constructor signatures remain unchanged.
- [x] Cosmos remains the default provider.
- [x] Mongo collection routing resolves from `EntityDescriptionAttribute.Domain`.
- [x] Mongo create/get/upsert/delete operations delegate through the provider-neutral storage contract while preserving the surrounding base-class workflow.
- [x] Typed Mongo queries route through the Mongo provider.
- [x] Mongo paged queries honor `ShowDeleted` and `ShowDrafts`.
- [x] Mongo summary queries honor category filtering and `OrderBy`/`OrderByDesc` in the provider implementation.
- [x] Mongo `QueryAllAsync` and `DescOrderQueryAsync` preserve their existing cross-entity semantics.
- [x] Database-specific Mongo overrides do not require a Cosmos shared key.
- [x] Dynamic `SetConnection` re-resolves the Mongo provider.
- [x] Direct Cosmos-only operations remain explicit migration islands rather than being silently emulated.

## Live validation evidence

The August 23, 2026 local Mongo integration fixture instantiates a derived test repository using the same legacy constructor shape used by application repositories and validates:

- provider selection by logical database
- domain collection identity
- create/get/upsert
- filtered and sorted query
- soft delete with `ShowDeleted`
- hard delete
- `SetConnection`
- database-specific provider override without a Cosmos shared key

Result:

```text
Test summary: total: 6, failed: 0, succeeded: 6, skipped: 0, duration: 1.1s
```

This directly proves the key acceptance rule: the derived repository did not need a Mongo-specific constructor.

## Remaining follow-up validation

These are useful Card 7/staged-cutover checks rather than missing Card 6 wiring:

- validate a representative production repository with a real cache provider
- validate a representative repository with dependency checks/rename propagation
- compile and exercise selected consuming repositories as they are staged onto Mongo
- continue isolating Cosmos SDK escape hatches when a staged repository encounters one

## Acceptance criteria

- [x] Existing derived repository constructors compile unchanged.
- [x] Switching a logical database to Mongo is configuration-driven.
- [x] Cosmos and Mongo can coexist for different logical databases in the same process.
- [x] Normal repository CRUD/query operations no longer require direct Cosmos access when Mongo is selected.
- [x] Cosmos-specific escape hatches are explicitly treated as migration islands.

## Out of scope

- Removal of Cosmos packages.
- Production cutover.
- Replacing the temporary compatibility resolver with the final first-class application configuration bridge. See Card 2.
