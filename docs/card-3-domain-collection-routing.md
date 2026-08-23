# Card 3 - Domain-Based Collection Routing

## Objective

Use each entity's required `EntityDescriptionAttribute` domain as the default Mongo collection name.

## Status

Core implementation complete pending local build/test validation.

## Design

For entities stored through `DocumentDBRepoBase<TEntity>`, collection routing resolves from CLR metadata:

`TEntity -> EntityDescriptionAttribute -> Domain -> Mongo collection`

Cosmos intentionally continues using the consolidated `{DatabaseName}_Collections` layout during the migration period.

## Runtime routing

`IDocumentCollectionNameResolver` provides the provider-neutral collection naming policy.

Typed Mongo factory calls use the entity's `EntityDescriptionAttribute.Domain` by default:

```csharp
var collection = documentCollectionFactory.Create<WorkTask>(settings);
```

If `WorkTask` has domain `ProjectManagement`, the Mongo collection is `ProjectManagement`.

Two different CLR entity types with the same domain therefore land in the same Mongo collection.

An explicit collection name always wins:

```csharp
var collection = documentCollectionFactory.Create<WorkTask>(settings, "SpecialCollection");
```

The existing non-generic factory remains available for arbitrary-document callers that already choose or know their physical collection.

## Migration routing

Raw Cosmos documents contain `EntityType`. The resolver supports:

```csharp
var resolved = resolver.TryResolve(databaseName, entityTypeName, out var collectionName);
```

The lookup scans loaded CLR entity types and reads `EntityDescriptionAttribute.Domain` without deserializing the raw document.

If the entity type cannot be resolved, lacks domain metadata, or is ambiguous across loaded assemblies, `TryResolve` returns `false` and returns the safe fallback collection:

```text
{DatabaseName}_Collections
```

Migration code must report these unresolved/fallback cases. It must not discard the document.

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
- [x] Expose unresolved raw entity types through a `false` result so migration can report them.
- [x] Add typed `IDocumentCollectionFactory` overloads.
- [x] Keep existing Cosmos consolidated collection behavior unchanged.
- [x] Register collection-name resolver with dependency injection.
- [x] Add tests for shared domain, explicit override, unresolved metadata, raw entity lookup, fallback, and normalization.

## Validation remaining

Run locally:

```powershell
dotnet build src/LagoVista.CloudStorage/LagoVista.CloudStorage.csproj
dotnet test tests/LagoVista.CloudStorage.Tests/LagoVista.CloudStorage.IntegrationTests.csproj
```

## Acceptance criteria

- Two different entity types with the same domain resolve to the same Mongo collection.
- Domain is the normal/default Mongo routing rule.
- Existing Cosmos collection behavior remains unchanged.
- Raw migration code can resolve a destination collection from a document's `EntityType`.
- Unknown entity types remain migratable through a visible fallback path.

## Out of scope

- Per-entity collection configuration as the normal mechanism.
- Mongo sharding strategy.
