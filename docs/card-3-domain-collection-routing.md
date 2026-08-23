# Card 3 - Domain-Based Collection Routing

## Objective

Use each entity's required `EntityDescriptionAttribute` domain as the default Mongo collection name.

## Design

For entities stored through `DocumentDBRepoBase<TEntity>`, collection routing should resolve from CLR metadata:

`TEntity -> EntityDescriptionAttribute -> Domain -> Mongo collection`

Cosmos may continue using the consolidated `{DatabaseName}_Collections` layout during the migration period.

## Tasks

- Add a provider-neutral collection-name resolver abstraction.
- Resolve domain from `EntityDescriptionAttribute` for normal typed repository access.
- Build an `EntityType -> Domain` catalog for raw-document migration based on loaded entity metadata.
- Normalize domain values into valid, stable Mongo collection names without changing their semantic identity.
- Allow an explicit collection override for exceptional cases.
- Define a safe fallback to `{DatabaseName}_Collections` when domain metadata cannot be resolved.
- Report fallback/unresolved entity types rather than silently losing data.
- Update `DocumentCollectionFactory`/Mongo construction to use the resolver where entity metadata is available.
- Add unit tests for multiple entities sharing one domain, explicit override, unresolved entity type, and fallback behavior.

## Acceptance criteria

- Two different entity types with the same domain resolve to the same Mongo collection.
- Domain is the normal/default Mongo routing rule.
- Existing Cosmos collection behavior remains unchanged.
- Raw migration code can resolve a destination collection from a document's `EntityType`.
- Unknown entity types remain migratable through a visible fallback path.

## Out of scope

- Per-entity collection configuration as the normal mechanism.
- Mongo sharding strategy.
