# Card 5 - Implement Rich Mongo Document Storage

## Objective

Implement the Mongo provider for the full `IDocumentDBRepoBase<TEntity>` contract used by `DocumentDBRepoBase<TEntity>` while preserving existing repository behavior.

## Tasks

- Replace the current `MongoDBStorage<TEntity>` stubs with working Mongo implementations.
- Implement create, get, update/upsert, and delete.
- Implement typed unpaged queries.
- Implement paged ascending and descending queries.
- Implement typed summary projections.
- Preserve entity workflow expectations such as `EntityType` and `DatabaseName` assignment.
- Preserve cache behavior expected by `DocumentDBRepoBase<TEntity>`.
- Preserve dependency checks before deletes where currently required.
- Use domain-based Mongo collection routing.
- Ensure CLR `Id` consistently targets Mongo `_id`.
- Define behavior for partition-key overloads in a provider-neutral way; Mongo should not invent meaningless partition semantics.
- Fail explicitly for any Cosmos-only operation that does not have a Mongo equivalent rather than silently changing behavior.
- Add focused parity tests against representative entity types.

## Acceptance criteria

- All `IDocumentDBRepoBase<TEntity>` methods actually used by current repositories have a tested Mongo implementation.
- CRUD and typed query behavior match Cosmos semantics closely enough for existing callers.
- Existing derived repository constructors do not need Mongo-specific changes.
- Mongo documents are written to domain collections.
- No raw Cosmos SQL is required by the Mongo provider.

## Explicit migration islands

The following should be identified and handled separately rather than forced into generic Mongo behavior:

- Cosmos users/permissions/resource-token brokers.
- Direct container/database management APIs.
- Any component whose correctness depends on Cosmos-specific partitioning or security semantics.
