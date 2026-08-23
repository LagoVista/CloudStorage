# Card 5 - Implement Rich Mongo Document Storage

## Objective

Implement the Mongo provider for the full `IDocumentDBRepoBase<TEntity>` contract used by `DocumentDBRepoBase<TEntity>` while preserving existing repository behavior.

## Status

Core rich Mongo provider implementation is complete and wired through `DocumentStorageFactory`. Local build and live Mongo parity validation remain.

## Implemented

- [x] Replace the previous `MongoDBStorage<TEntity>` stubs with a concrete provider.
- [x] Implement create, get, update/upsert, and delete.
- [x] Implement typed unpaged queries.
- [x] Implement paged queries.
- [x] Implement ascending and descending sorting.
- [x] Implement generic descending-key queries.
- [x] Implement typed summary queries using `ISummaryFactory.CreateSummary()`.
- [x] Stamp `DatabaseName` and `EntityType` before writes.
- [x] Preserve cache read/write/invalidation behavior.
- [x] Preserve dependency checks before deletes.
- [x] Preserve dependency rename propagation during upsert when names change.
- [x] Route Mongo documents by `EntityDescriptionAttribute.Domain` through `IDocumentCollectionNameResolver`.
- [x] Use CLR `Id`/Mongo `_id` conventions rather than maintaining a duplicate `id` field.
- [x] Make `OperationResponse<TEntity>` provider-neutral while retaining its Cosmos constructor.
- [x] Wire the Mongo provider through `DocumentStorageFactory`.
- [x] Define partition-key behavior: Mongo has no partition key and partition-key overloads perform normal ID operations.
- [x] Add non-live contract tests for factory construction, domain collection routing, partition behavior, and provider-neutral operation responses.

## Provider behavior

### Create and upsert

Mongo writes preserve the entity workflow expected by existing repositories:

```text
item.DatabaseName = configured Mongo database
item.EntityType   = typeof(TEntity).Name
```

Creates use `InsertOneAsync`. Upserts use replacement upserts filtered by the entity ID, which maps to Mongo `_id` through the standard Mongo C# driver conventions.

### Get and delete

Reads constrain both ID and `EntityType`, preserving the protection Cosmos previously provided when different entity types shared a physical container.

Delete operations preserve dependency checking and cache invalidation. The Mongo partition-key overload intentionally ignores the partition-key argument because Mongo does not expose Cosmos partition semantics.

### Queries

Normal entity queries automatically add:

```text
EntityType == typeof(TEntity).Name
```

and support paging plus ascending/descending sorts.

Summary queries read the same domain collection using `TEntityFactory`, then call `CreateSummary()` as the Cosmos provider does today.

### Collections

Mongo `GetCollectionName()` resolves:

```text
TEntity -> EntityDescriptionAttribute.Domain -> Mongo collection
```

If metadata cannot be resolved, the existing `{DatabaseName}_Collections` fallback remains available through the collection resolver.

`DeleteCollectionAsync` drops the resolved Mongo collection. It does not drop the Mongo database.

## Remaining validation

- [ ] Build `LagoVista.CloudStorage` after the rich provider changes.
- [ ] Run non-live provider tests.
- [ ] Add/run live Mongo CRUD parity tests.
- [ ] Validate create -> get -> upsert -> get -> delete -> not found.
- [ ] Validate ascending and descending paging against representative entities.
- [ ] Validate summary queries against an entity implementing `ISummaryFactory`.
- [ ] Validate cache behavior with the real cache provider where practical.
- [ ] Validate dependency behavior with a representative repository where practical.

## Acceptance criteria

- All `IDocumentDBRepoBase<TEntity>` methods used by current repositories have a Mongo implementation.
- CRUD and typed query behavior match Cosmos semantics closely enough for existing callers.
- Existing derived repository constructors do not need Mongo-specific changes.
- Mongo documents are written to domain collections.
- No raw Cosmos SQL is required by the Mongo provider.

## Explicit migration islands

The following remain separate rather than being forced into generic Mongo behavior:

- Cosmos users/permissions/resource-token brokers.
- Direct Cosmos container/database management workflows that require provider-specific semantics.
- Any component whose correctness depends on Cosmos-specific partitioning or security semantics.
