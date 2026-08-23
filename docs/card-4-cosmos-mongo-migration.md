# Card 4 - Cosmos-to-Mongo Migration Tooling

## Objective

Provide a safe, repeatable utility that copies raw documents from Cosmos into Mongo using the final domain-based collection routing rules.

## Status

Initial migration engine implemented. Local build/API validation and end-to-end migration testing remain.

## Migration shape

`Cosmos consolidated collection -> raw JObject -> transform -> domain resolver -> Mongo collection -> bulk upsert`

The migrator does not deserialize documents into application entity types.

## Implemented transforms

- Move Cosmos `id` to Mongo `_id`.
- Do not retain a duplicate top-level `id` field.
- Strip `_rid`, `_self`, `_etag`, `_attachments`, and `_ts`.
- Preserve the remaining raw JSON document shape and nested values.

## Request model

`CosmosToMongoMigrationRequest` accepts:

- explicit Cosmos source `DocumentStorageSettings`
- explicit Mongo target `MongoDocumentStorageSettings`
- optional source collection override
- optional `EntityType` filter
- configurable `BatchSize`
- optional `ContinuationToken`
- optional `MaxPages`
- `DryRun`

`MaxPages` plus `ContinuationToken` provides a controlled checkpoint/resume mechanism. A migration can intentionally process a small number of Cosmos pages, inspect its result, then continue using the returned token.

## Routing

Each raw Cosmos document is inspected for `EntityType` and routed through `IDocumentCollectionNameResolver`.

Resolved entities are written to their `EntityDescriptionAttribute.Domain` collection. Unknown or ambiguous entity types are counted as unresolved and routed to the safe fallback:

```text
{MongoDatabaseName}_Collections
```

Documents are never dropped solely because route metadata cannot be resolved.

## Mongo writes

Documents are grouped by destination collection for each Cosmos page and written with unordered Mongo bulk writes.

Each write is a replacement upsert filtered by `_id`, making normal reruns idempotent.

Dry-run mode executes source reading, transforms, routing, and statistics collection without opening a Mongo connection or writing data.

## Result model

`CosmosToMongoMigrationResult` currently reports:

- pages read
- documents read
- documents written
- documents skipped
- documents failed
- unresolved routes
- continuation token
- completed flag
- dry-run flag
- per-entity-type/per-destination route statistics

No credentials or connection strings are included in the result.

## Completed tasks

- [x] Add migration request/result models.
- [x] Support explicit source Cosmos and target Mongo settings.
- [x] Stream Cosmos documents page-by-page.
- [x] Support configurable Cosmos page/batch size.
- [x] Route each document by `EntityType` using domain collection routing.
- [x] Transform `id` to `_id`.
- [x] Strip Cosmos system metadata.
- [x] Bulk upsert Mongo documents by `_id`.
- [x] Support optional `EntityType` filtering.
- [x] Add dry-run mode.
- [x] Add continuation token input/output and bounded `MaxPages` execution.
- [x] Count read, written, skipped, failed, and unresolved-route documents.
- [x] Include per-entity-type/per-destination statistics.
- [x] Register migration service with dependency injection.
- [x] Keep secrets out of migration reports.

## Remaining tasks

- [ ] Build against current Cosmos and Mongo SDK versions and clear any API signature mismatches.
- [ ] Add transform-focused unit tests for `id -> _id` and Cosmos metadata removal.
- [ ] Run a dry-run against a real Cosmos database and review route inventory.
- [ ] Run a small bounded migration into dev Mongo.
- [ ] Add validation mode comparing Cosmos and Mongo counts by entity type.
- [ ] Validate continuation/resume behavior against a real Cosmos feed.
- [ ] Validate rerunning the same page does not create duplicates.

## Local validation

```powershell
dotnet build src/LagoVista.CloudStorage/LagoVista.CloudStorage.csproj
dotnet test tests/LagoVista.CloudStorage.Tests/LagoVista.CloudStorage.IntegrationTests.csproj
```

## Acceptance criteria

- A dry run can inventory a Cosmos collection and show exactly where each entity type will land in Mongo.
- A real run can be interrupted and safely rerun without duplicate documents.
- `id`/`_id` mapping is deterministic.
- Cosmos metadata is absent from migrated Mongo documents.
- Unknown entity types are reported and written to the configured fallback rather than discarded.
- Source and target counts can be reconciled by entity type.

## Out of scope

- Live dual-write/change-feed synchronization during the first implementation.
- Destructive removal from Cosmos.
- Application cutover.
