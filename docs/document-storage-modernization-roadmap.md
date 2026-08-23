# Document Storage Modernization Roadmap

## Goal

Move LagoVista document persistence from a Cosmos-specific implementation to a provider-neutral document storage layer with MongoDB as the first alternate provider, while preserving existing repository behavior and providing a safe, repeatable Cosmos-to-Mongo migration path.

## Architectural principles

- Existing repositories derived from `DocumentDBRepoBase<TEntity>` should not require constructor changes to adopt MongoDB.
- Normal repository queries should use typed expressions and projections rather than provider-specific query strings.
- Exceptional query shapes use `DocumentQueryType` and are implemented natively by each provider.
- Cosmos and Mongo provider details must not leak into application repositories.
- Mongo collections should be organized by the required domain on `EntityDescriptionAttribute` rather than reproducing the single consolidated Cosmos collection.
- Cosmos root `id` becomes Mongo `_id` during migration; nested business/value-object `Id` fields remain `Id`.
- Migration must operate on raw documents, be resumable and idempotent, and never silently discard an unrecognized document.
- Cosmos remains the default provider until an explicit cutover.

## Current checkpoint - August 23, 2026

The local Mongo path is green end to end against the repository-owned Mongo 8 Docker harness.

```text
Test summary: total: 6, failed: 0, succeeded: 6, skipped: 0, duration: 1.1s
```

This proves the lower-level Mongo adapter and the actual `DocumentDBRepoBase<TEntity>` path can execute CRUD, filtered/sorted queries, projections, semantic aggregation, soft delete, hard delete, domain routing, dynamic `SetConnection`, and database-specific Mongo provider selection without changing derived repository constructors.

The validation work also established BSON contracts for LagoVista wire types (`UtcTimestamp`, `NormalizedId32`, `LagoVistaKey`) and preserved nested `EntityHeader.Id` compatibility between migrated and newly-written Mongo documents.

## Cards

| Card | Title | Status |
| --- | --- | --- |
| 1 | [Stabilize Mongo IDocumentCollection Adapter](card-1-stabilize-mongo-document-collection.md) | **Complete** |
| 2 | [Mongo Configuration Model](card-2-mongo-configuration.md) | **In progress - first-class runtime bridge remains** |
| 3 | [Domain-Based Collection Routing](card-3-domain-collection-routing.md) | **Complete** |
| 4 | [Cosmos-to-Mongo Migration Tooling](card-4-cosmos-mongo-migration.md) | **Implementation complete; live migration/reconciliation pending** |
| 5 | [Implement Rich Mongo Document Storage](card-5-rich-mongo-document-storage.md) | **Core complete; targeted cache/dependency validation remains** |
| 6 | [Wire DocumentDBRepoBase to Provider Factory](card-6-wire-document-db-repo-base.md) | **Complete for primary repository path** |
| 7 | [Validation, Cutover, and Operational Runbook](card-7-validation-cutover.md) | **Local validation green; staged dev cutover pending** |

## Recommended next order

1. Finish Card 2's first-class runtime configuration bridge so the compatibility resolver receives primary Mongo credentials from the normal LagoVista configuration system.
2. Add the two targeted Card 5/7 integration checks most likely to expose workflow assumptions: real cache-provider behavior and dependency-manager behavior.
3. Execute Card 4 against a real non-production Cosmos database: dry-run inventory, bounded migration, rerun/idempotency, and count reconciliation.
4. Select one representative logical database for dev cutover and run the Card 7 application smoke/rollback plan.
5. Expand staged cutover database-by-database, documenting any Cosmos-specific migration islands encountered.

## Completed groundwork

- Removed raw Cosmos SQL from normal repository contracts and converted callers to typed LINQ/projection where practical.
- Added `DocumentQueryType` for provider-specific semantic query shapes.
- Added provider selection through `DocumentStorageSettingsResolver`.
- Added `IDocumentCollection` and `DocumentCollectionFactory`.
- Added Cosmos and Mongo `IDocumentCollection` implementations.
- Added native Cosmos and Mongo implementations for `CustomerIndustryNicheSalesStageCounts`.
- Added rich Mongo `IDocumentDBRepoBase<TEntity>` implementation.
- Wired `DocumentDBRepoBase<TEntity>` to provider selection without changing derived constructors.
- Added first-class structured Mongo connection settings and shared Mongo client lifecycle management.
- Added repeatable local Mongo 8 Docker integration testing.

## Deferred work

These are intentionally outside the initial migration runway unless validation proves they are required:

- Mongo-specific index optimization beyond correctness-critical indexes.
- Automatic collection sharding.
- Cross-region or multi-cluster Mongo topology.
- Removal of Cosmos packages and Cosmos-only operational components.
- Migration of special Cosmos security/token-broker behaviors that depend on Cosmos users, permissions, or temporary resource tokens.
