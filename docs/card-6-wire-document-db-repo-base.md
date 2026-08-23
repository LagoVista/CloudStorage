# Card 6 - Wire DocumentDBRepoBase to Provider Factory

## Objective

Make `DocumentDBRepoBase<TEntity>` resolve its storage provider internally so existing derived repositories can switch between Cosmos and Mongo through configuration alone.

## Key acceptance rule

No derived `DocumentDBRepoBase<TEntity>` repository constructor changes are required to adopt the new adapter.

## Tasks

- Route the primary `DocumentDBRepoBase<TEntity>` construction path through `DocumentStorageSettingsResolver` and `DocumentStorageFactory`.
- Preserve current constructor signatures used throughout application repositories.
- Ensure Cosmos remains the default provider.
- Ensure Mongo selection uses explicit Mongo settings and domain-based collection routing.
- Refactor direct Cosmos operations in the base class to delegate through the provider contract where needed.
- Handle dynamic connection changes/`SetConnection` without bypassing provider selection.
- Identify base-class methods that still return or require Cosmos SDK types and remove or isolate them.
- Verify cache, dependency, summary, and entity workflow paths continue functioning.
- Compile representative consuming repositories without modifications.

## Acceptance criteria

- Existing derived repository constructors compile unchanged.
- Switching a logical database to Mongo is configuration-driven.
- Cosmos and Mongo can coexist for different logical databases in the same process.
- Normal repository operations no longer require direct Cosmos access from `DocumentDBRepoBase<TEntity>`.
- Cosmos-specific escape hatches are explicitly documented as migration islands.

## Out of scope

- Removal of Cosmos packages.
- Production cutover.
