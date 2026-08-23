# Card 5 - Implement Rich Mongo Document Storage

## Objective

Implement the Mongo provider for the full `IDocumentDBRepoBase<TEntity>` contract used by `DocumentDBRepoBase<TEntity>` while preserving existing repository behavior.

## Status

Core implementation and primary live parity validation are complete. Cache-provider, dependency-manager, and summary-factory-specific integration cases remain as targeted follow-up validation rather than blockers for the basic Mongo repository path.

## Implemented

- [x] Replace the previous `MongoDBStorage<TEntity>` stubs with a concrete provider.
- [x] Implement create, get, update/upsert, and delete.
- [x] Implement typed unpaged queries.
- [x] Implement paged queries.
- [x] Implement ascending and descending sorting.
- [x] Implement generic descending-key queries.
- [x] Implement typed summary queries using `ISummaryFactory.CreateSummary()`.
- [x] Stamp `DatabaseName` and `EntityType` before writes.
- [x] Preserve cache read/write/invalidation behavior in the provider contract.
- [x] Preserve dependency checks before deletes.
- [x] Preserve dependency rename propagation during upsert when names change.
- [x] Route Mongo documents by `EntityDescriptionAttribute.Domain` through `IDocumentCollectionNameResolver`.
- [x] Use CLR root `Id` / Mongo `_id` mapping without maintaining a duplicate top-level `id` field.
- [x] Preserve nested `EntityHeader.Id` as `Id` so new Mongo writes match migrated document shape.
- [x] Add BSON serializers for LagoVista wire value types encountered by real entity graphs: `UtcTimestamp`, `NormalizedId32`, and `LagoVistaKey`.
- [x] Make `OperationResponse<TEntity>` provider-neutral while retaining its Cosmos constructor.
- [x] Wire the Mongo provider through `DocumentStorageFactory`.
- [x] Define partition-key behavior: Mongo has no partition key and partition-key overloads perform normal ID operations.
- [x] Add non-live contract tests for factory construction, domain collection routing, partition behavior, and provider-neutral operation responses.

## Live validation

The August 23, 2026 Docker-backed Mongo 8 integration suite proves the real repository path can:

- create and read a domain entity
- upsert changes and revision state
- execute filtered/sorted list queries
- execute lower-level projections and paging
- execute the server-side `CustomerIndustryNicheSalesStageCounts` aggregate
- soft delete and honor `ShowDeleted`
- hard delete and return not-found
- use a database-specific Mongo provider override without a Cosmos shared key

Validation result:

```text
Test summary: total: 6, failed: 0, succeeded: 6, skipped: 0, duration: 1.1s
```

## Remaining targeted validation

- [ ] Validate summary queries against a representative real entity implementing `ISummaryFactory`.
- [ ] Validate cache behavior with the real cache provider.
- [ ] Validate dependency behavior with a representative dependency manager/repository.
- [ ] Add targeted descending-sort coverage if a consuming repository is selected for staged cutover and relies on it heavily.

## Acceptance criteria

- [x] All `IDocumentDBRepoBase<TEntity>` methods used by current repositories have a Mongo implementation.
- [x] Core CRUD and typed query behavior match Cosmos semantics closely enough for existing callers.
- [x] Existing derived repository constructors do not need Mongo-specific changes.
- [x] Mongo documents are written to domain collections.
- [x] No raw Cosmos SQL is required by the Mongo provider.

## Explicit migration islands

The following remain separate rather than being forced into generic Mongo behavior:

- Cosmos users/permissions/resource-token brokers.
- Direct Cosmos container/database management workflows that require provider-specific semantics.
- Any component whose correctness depends on Cosmos-specific partitioning or security semantics.
