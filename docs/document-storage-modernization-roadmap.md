# Document Storage Modernization Roadmap

## Goal

Move LagoVista document persistence from a Cosmos-specific implementation to a provider-neutral document storage layer with MongoDB as the first alternate provider, while preserving existing repository behavior and providing a safe, repeatable Cosmos-to-Mongo migration path.

## Architectural principles

- Existing repositories derived from `DocumentDBRepoBase<TEntity>` should not require constructor changes to adopt MongoDB.
- Normal repository queries should use typed expressions and projections rather than provider-specific query strings.
- Exceptional query shapes use `DocumentQueryType` and are implemented natively by each provider.
- Cosmos and Mongo provider details must not leak into application repositories.
- Mongo collections should be organized by the required domain on `EntityDescriptionAttribute` rather than reproducing the single consolidated Cosmos collection.
- Cosmos `id` becomes Mongo `_id` during migration.
- Migration must operate on raw documents, be resumable and idempotent, and never silently discard an unrecognized document.
- Cosmos remains the default provider until an explicit cutover.

## Completed groundwork

- Removed raw Cosmos SQL from normal repository contracts and converted callers to typed LINQ/projection where practical.
- Added `DocumentQueryType` for provider-specific semantic query shapes.
- Added provider selection through `DocumentStorageSettingsResolver`.
- Added `IDocumentCollection` and `DocumentCollectionFactory`.
- Added Cosmos and initial Mongo `IDocumentCollection` implementations.
- Added native Cosmos and Mongo implementations for `CustomerIndustryNicheSalesStageCounts`.

## Cards

| Card | Title | Status |
| --- | --- | --- |
| 1 | [Stabilize Mongo IDocumentCollection Adapter](card-1-stabilize-mongo-document-collection.md) | In progress |
| 2 | [Mongo Configuration Model](card-2-mongo-configuration.md) | Planned |
| 3 | [Domain-Based Collection Routing](card-3-domain-collection-routing.md) | Planned |
| 4 | [Cosmos-to-Mongo Migration Tooling](card-4-cosmos-mongo-migration.md) | Planned |
| 5 | [Implement Rich Mongo Document Storage](card-5-rich-mongo-document-storage.md) | Planned |
| 6 | [Wire DocumentDBRepoBase to Provider Factory](card-6-wire-document-db-repo-base.md) | Planned |
| 7 | [Validation, Cutover, and Operational Runbook](card-7-validation-cutover.md) | Planned |

## Recommended order

Cards should normally be completed in numerical order. Card 4 depends on Card 3 so migration writes directly into the final domain-based Mongo collection topology. Card 6 should not begin until the rich Mongo provider in Card 5 has parity for the repository operations actually used by the application.

## Deferred work

These are intentionally outside the initial migration runway unless validation proves they are required:

- Mongo-specific index optimization beyond correctness-critical indexes.
- Automatic collection sharding.
- Cross-region or multi-cluster Mongo topology.
- Removal of Cosmos packages and Cosmos-only operational components.
- Migration of special Cosmos security/token-broker behaviors that depend on Cosmos users, permissions, or temporary resource tokens.
