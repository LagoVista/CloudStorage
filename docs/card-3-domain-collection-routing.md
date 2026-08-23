# Card 3 - Domain-Based Collection Routing

## Objective

Use each entity's required `EntityDescriptionAttribute` domain as the default Mongo collection name.

## Status

Complete. Domain routing is implemented and has now been exercised through the live `DocumentDBRepoBase<TEntity>` Mongo integration path.

## Design

For entities stored through `DocumentDBRepoBase<TEntity>`, collection routing resolves from CLR metadata:

`TEntity -> EntityDescriptionAttribute -> Domain -> Mongo collection`

Cosmos intentionally continues using the consolidated `{DatabaseName}_Collections` layout during the migration period.

## Runtime routing

`IDocumentCollectionNameResolver` provides the provider-neutral collection naming policy.

Typed Mongo factory calls use the entity's `EntityDescriptionAttribute.Domain` by default. Two different CLR entity types with the same domain therefore land in the same Mongo collection.

An explicit collection name still wins when one is supplied.

## Migration routing

Raw Cosmos documents contain `EntityType`. The resolver maps that type to domain metadata without deserializing the document.

If the entity type cannot be resolved, lacks domain metadata, or is ambiguous across loaded assemblies, routing falls back safely to:

```text
{DatabaseName}_Collections
```

Migration code reports these unresolved/fallback cases and does not discard the document.

## Collection-name normalization

Domain values retain their semantic name. Mongo-reserved/problematic characters are normalized conservatively:

- null characters become `_`
- `$` becomes `_`
- names beginning with `system.` are prefixed with `_`

## Completed tasks

- [x] Add provider-neutral `IDocumentCollectionNameResolver`.
- [x] Resolve domain from `EntityDescriptionAttribute` for typed access.
- [x] Support raw `EntityType -> Domain` lookup from loaded assemblies.
- [x] Normalize domain values into stable Mongo collection names.
- [x] Allow explicit collection override.
- [x] Fall back to `{DatabaseName}_Collections` when metadata cannot be resolved.
- [x] Expose unresolved raw entity types so migration can report them.
- [x] Add typed `IDocumentCollectionFactory` overloads.
- [x] Keep existing Cosmos consolidated collection behavior unchanged.
- [x] Register collection-name resolver with dependency injection.
- [x] Add tests for shared domain, explicit override, unresolved metadata, raw entity lookup, fallback, and normalization.
- [x] Validate a real `DocumentDBRepoBase<TEntity>` Mongo write lands in the entity domain collection.

## Validation evidence

The August 23, 2026 local Mongo integration run confirmed the test entity routes to its `EntityDescriptionAttribute.Domain` collection (`RichMongoDomain`) and the full six-test Mongo suite passes.

## Acceptance criteria

- [x] Two different entity types with the same domain resolve to the same Mongo collection.
- [x] Domain is the normal/default Mongo routing rule.
- [x] Existing Cosmos collection behavior remains unchanged.
- [x] Raw migration code can resolve a destination collection from a document's `EntityType`.
- [x] Unknown entity types remain migratable through a visible fallback path.

## Out of scope

- Per-entity collection configuration as the normal mechanism.
- Mongo sharding strategy.
