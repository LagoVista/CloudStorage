# Card 6 - Wire DocumentDBRepoBase to Provider Factory

## Objective

Make `DocumentDBRepoBase<TEntity>` resolve its storage provider internally so existing derived repositories can switch between Cosmos and Mongo through configuration alone.

## Key acceptance rule

No derived `DocumentDBRepoBase<TEntity>` repository constructor changes are required to adopt the new adapter.

## Status

Provider-side handoff is ready. The final `DocumentDBRepoBase<TEntity>` wiring remains pending because the base class is a large legacy file and must be changed surgically without rewriting unrelated workflow code.

The current master branch remains behavior-preserving for `DocumentDBRepoBase<TEntity>` until that final wiring is applied.

## Completed supporting work

- [x] `DocumentStorageSettingsResolver` selects Cosmos or Mongo by logical database configuration.
- [x] `DocumentStorageFactory` constructs both rich Cosmos and rich Mongo providers.
- [x] Added `DocumentStorageFactory.ResolveAndCreate<TEntity>(...)` so the base class can resolve and construct the provider with one call while preserving its existing constructor arguments.
- [x] Mongo collection routing resolves from `EntityDescriptionAttribute.Domain`.
- [x] Mongo rich provider implements the current `IDocumentDBRepoBase<TEntity>` contract.
- [x] Mongo paged queries honor `ShowDeleted` and `ShowDrafts`.
- [x] Mongo summary queries honor category filtering and `OrderBy`/`OrderByDesc`.
- [x] Mongo `QueryAllAsync` and `DescOrderQueryAsync` preserve their existing cross-entity semantics rather than implicitly adding an entity-type filter.
- [x] Resolver/factory tests prove Cosmos remains the default and a database-specific Mongo override constructs the Mongo provider.

## Final base-class wiring

The `DocumentDBRepoBase<TEntity>` edit should preserve the existing Cosmos implementation and add Mongo delegation only at provider-neutral persistence boundaries.

Required insertion points:

- make the `_storage` provider replaceable so `SetConnection` can re-resolve configuration
- resolve `_storage` through `DocumentStorageFactory.ResolveAndCreate<TEntity>(...)` in the existing constructor
- keep Cosmos on the current direct implementation path
- route Mongo collection/partition identity through `_storage`
- route Mongo create/get/upsert/delete persistence through `_storage` while preserving base-class validation, revision/hash, audit, discussion, dependency, RAG, produced-artifact, and cache-invalidation workflow
- route typed Mongo queries and summaries through `_storage`
- have direct Cosmos container helpers fail explicitly if called while Mongo is selected
- keep raw Cosmos SQL and Cosmos resource/security operations as explicit migration islands
- reject Mongo ETag conditional writes until Mongo optimistic concurrency has an explicit equivalent

## Tasks

- [ ] Route the primary `DocumentDBRepoBase<TEntity>` construction path through `DocumentStorageSettingsResolver` and `DocumentStorageFactory`.
- [x] Preserve current constructor signatures used throughout application repositories.
- [x] Ensure Cosmos remains the default provider.
- [x] Ensure Mongo selection uses explicit Mongo settings and domain-based collection routing.
- [ ] Refactor direct Cosmos operations in the base class to delegate through the provider contract for Mongo.
- [ ] Handle dynamic connection changes/`SetConnection` without bypassing provider selection.
- [ ] Identify base-class methods that still return or require Cosmos SDK types and isolate them when Mongo is selected.
- [ ] Verify cache, dependency, summary, and entity workflow paths continue functioning through the base class.
- [ ] Compile representative consuming repositories without modifications.

## Acceptance criteria

- Existing derived repository constructors compile unchanged.
- Switching a logical database to Mongo is configuration-driven.
- Cosmos and Mongo can coexist for different logical databases in the same process.
- Normal repository operations no longer require direct Cosmos access from `DocumentDBRepoBase<TEntity>` when Mongo is selected.
- Cosmos-specific escape hatches are explicitly documented as migration islands.

## Out of scope

- Removal of Cosmos packages.
- Production cutover.
