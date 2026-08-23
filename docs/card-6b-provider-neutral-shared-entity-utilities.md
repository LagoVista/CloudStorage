# Card 6B - Provider-Neutral Shared-Entity Utilities

## Objective

Ensure every CloudStorage utility/repository that reads or mutates the same first-class document entities as `DocumentDBRepoBase<TEntity>` follows the selected document provider instead of remaining hard-wired to Cosmos.

This card is a prerequisite for Mongo dev cutover and Card 7 validation.

## Why this must happen before cutover

The generic repository path can now select Mongo by logical database, but several utility repositories still connect directly to the consolidated Cosmos container.

If a logical database is switched to Mongo while one of these utilities remains Cosmos-only, the application can become internally split-brained:

- normal repository CRUD reads/writes Mongo
- utility queries still read Cosmos
- utility patch/update operations still mutate Cosmos
- users can observe stale or contradictory entity state depending on which code path served the request

For classes that operate on the same entities, direct Cosmos access is therefore not a harmless migration island. They must either become provider-neutral or be proven not to participate in a Mongo-enabled logical database.

## Initial inventory

### Must become provider-neutral before Mongo cutover

#### `EntityUtilsRepository`

Directly owns a Cosmos `Container` and performs cross-entity queries, raw document reads, and partial entity updates/patches. It operates on the same entity records managed by normal repositories.

Representative capabilities include:

- get entity summaries/core records by `EntityType` and organization
- readiness/checklist candidate queries
- status-based candidate queries
- load entity by ID
- patch status/master status and other selected fields
- cache invalidation, dependency and RAG-related follow-up around those mutations

This is a headline acceptance case for this card.

#### `EntityPreparationCandidateRepository`

Directly owns a Cosmos `Container` and performs cross-entity summary/candidate queries over the same first-class entities, including readiness/production-ready state.

These reads must see the same provider that normal entity repositories use.

#### `EntityListItemRepo<TEntity>`

Although it derives from `DocumentDBRepoBase<TEntity>`, its list/header/category paths bypass the provider-neutral repository APIs and execute raw Cosmos `QueryDefinition` queries through `GetContainerAsync()`.

Those paths must be rewritten to provider-neutral typed/projection queries or registered semantic query operations.

#### `StorageUtils`

Directly owns Cosmos connectivity and performs shared-entity reads and mutations including key lookup, ratings, visibility/public-state changes, and patch-style updates.

Any methods that operate on entities participating in Mongo cutover must follow the selected provider.

### Intentional Cosmos-aware infrastructure

The following are not targets merely because they reference the Cosmos SDK:

- `CosmosDBStorage<TEntity>`
- `CosmosDocumentCollection`
- `CosmosClientProvider` / `ICosmosClientProvider`
- Cosmos source side of `DocumentMigrationService`
- explicit Cosmos database/container/security/resource-token management

These are provider implementations or explicit Cosmos operations and may remain Cosmos-aware.

### Requires classification

`CosmosSyncRepository` and any other direct Cosmos consumer found by the audit must be classified as one of:

1. shared-entity application behavior that must become provider-neutral
2. migration/bootstrap infrastructure that intentionally remains Cosmos-specific
3. obsolete/dead code that can be removed separately

## Design direction

Do not create Mongo-specific copies of these repositories.

Prefer extending provider-neutral capabilities at the smallest reusable seam:

- typed filters/projections through `IDocumentCollection` or the rich document provider when sufficient
- registered `DocumentQueryType` semantic operations for complex provider-specific query shapes
- provider-neutral partial-update/patch capability where read-modify-replace would be unsafe or unnecessarily expensive
- raw provider-neutral document access only when a typed contract is genuinely impractical

Both Cosmos and Mongo implementations must preserve the existing public repository interfaces so consuming application code does not need provider-specific branches.

## Tasks

- [ ] Complete a repo-wide inventory of production CloudStorage code that directly references `Microsoft.Azure.Cosmos`, `CosmosClient`, `Container`, `QueryDefinition`, `PatchOperation`, or `GetItemQueryIterator`.
- [ ] Classify every direct Cosmos consumer as shared-entity behavior, intentional Cosmos infrastructure, or obsolete/deferred.
- [ ] Refactor `EntityUtilsRepository` so all shared-entity reads and writes follow the selected document provider.
- [ ] Refactor `EntityPreparationCandidateRepository` so candidate/summary reads follow the selected document provider.
- [ ] Refactor `EntityListItemRepo<TEntity>` raw Cosmos list/header/category queries to provider-neutral query capabilities.
- [ ] Refactor the shared-entity portions of `StorageUtils` to follow the selected provider.
- [ ] Classify and address `CosmosSyncRepository`.
- [ ] Add provider-neutral capability for partial field updates if required by `EntityUtilsRepository`/`StorageUtils` semantics.
- [ ] Add focused Mongo tests for every converted utility path.
- [ ] Add Cosmos regression coverage where existing tests do not already protect behavior.
- [ ] Verify cache invalidation and side effects remain correct after provider-neutralization.
- [ ] Search consuming solution repositories for direct Cosmos access to the same migrated document databases and capture any additional blockers before dev cutover.

## Acceptance criteria

- When a logical document database selects Mongo, no CloudStorage shared-entity utility reads or mutates the corresponding Cosmos copy of those entities.
- `EntityUtilsRepository` reads and mutations operate successfully against Mongo for a Mongo-selected logical database.
- `EntityPreparationCandidateRepository` returns Mongo-backed candidate/summary data for a Mongo-selected logical database.
- `EntityListItemRepo<TEntity>` list/header/category behavior works without direct Cosmos container access when Mongo is selected.
- Shared-entity `StorageUtils` operations work against the selected provider.
- Existing public repository/utility interfaces remain provider-neutral.
- Cosmos remains functional when Cosmos is selected.
- Intentional Cosmos-only infrastructure is explicitly documented rather than accidentally mixed with normal entity behavior.
- Card 7 dev cutover does not begin until this card's shared-entity blockers are complete.

## Out of scope

- Removing the Cosmos SDK from CloudStorage.
- Replacing Cosmos migration source access.
- Reimplementing Cosmos-specific users/permissions/resource-token behavior in Mongo unless a real application requirement demands it.
- Production cutover.
