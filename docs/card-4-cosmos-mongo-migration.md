# Card 4 - Cosmos-to-Mongo Migration Tooling

## Objective

Provide a safe, repeatable utility that copies raw documents from Cosmos into Mongo using the final domain-based collection routing rules.

## Status

Core migration and validation implementation is complete and builds successfully. Transform coverage and count reconciliation logic are implemented. Live execution against a real Cosmos source and non-production Mongo target remains the substantive validation step.

## Migration shape

`Cosmos consolidated collection -> raw JObject -> transform -> domain resolver -> Mongo collection -> bulk upsert`

The migrator does not deserialize documents into application entity types.

## Implemented transforms

- Move Cosmos root `id` to Mongo `_id`.
- Do not retain a duplicate top-level `id` field.
- Strip `_rid`, `_self`, `_etag`, `_attachments`, and `_ts`.
- Preserve the remaining raw JSON document shape and nested values, including nested `Id` fields.
- Perform transforms on a cloned document so the source object is not mutated.

The transformation is isolated in `DocumentMigrationTransformer` and has focused unit coverage.

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

## Validation

`ValidateCosmosToMongoAsync` performs a full dry-run Cosmos inventory using the same routing rules, then counts the corresponding documents in Mongo by destination collection and `EntityType`.

The result reports source and destination totals plus route-level source/destination counts and a `Matches` flag. Missing/null/empty `EntityType` values are validated only against equivalent Mongo documents in the fallback collection rather than counting the entire fallback collection.

The local Mongo integration work has additionally proven that newly-written Mongo documents preserve the same root/nested identity contract expected by this migration path: root identity uses `_id`, while nested `EntityHeader.Id` remains `Id`.

## Result models

`CosmosToMongoMigrationResult` reports:

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

`CosmosToMongoValidationResult` reports:

- total source count
- total destination count
- overall match status
- per-entity-type/per-destination source and destination counts
- per-route match status

No credentials or connection strings are included in either result.

## Completed tasks

- [x] Add migration request/result models.
- [x] Support explicit source Cosmos and target Mongo settings.
- [x] Stream Cosmos documents page-by-page.
- [x] Support configurable Cosmos page/batch size.
- [x] Route each document by `EntityType` using domain collection routing.
- [x] Transform root `id` to `_id` while preserving nested `Id` fields.
- [x] Strip Cosmos system metadata.
- [x] Bulk upsert Mongo documents by `_id`.
- [x] Support optional `EntityType` filtering.
- [x] Add dry-run mode.
- [x] Add continuation token input/output and bounded `MaxPages` execution.
- [x] Count read, written, skipped, failed, and unresolved-route documents.
- [x] Include per-entity-type/per-destination statistics.
- [x] Register migration service with dependency injection.
- [x] Keep secrets out of migration reports.
- [x] Add transform-focused unit tests for `id -> _id`, Cosmos metadata removal, nested-shape preservation, missing IDs, and source immutability.
- [x] Add validation mode comparing Cosmos and Mongo counts by entity type and destination collection.
- [x] Rebuild CloudStorage and the integration-test project after the Mongo/runtime validation changes.

## Remaining tasks

- [ ] Run the full non-integration/unit suite after the latest BSON/runtime changes.
- [ ] Run a dry-run against a real Cosmos database and review route inventory.
- [ ] Run a small bounded migration into dev/non-production Mongo.
- [ ] Validate continuation/resume behavior against a real Cosmos feed.
- [ ] Validate rerunning the same page does not create duplicates.
- [ ] Run count reconciliation and confirm route-level matches.
- [ ] Compare representative migrated documents with newly-written Mongo documents for structural compatibility.

## Local validation

```powershell
dotnet build src/LagoVista.CloudStorage/LagoVista.CloudStorage.csproj
dotnet test tests/LagoVista.CloudStorage.Tests/LagoVista.CloudStorage.IntegrationTests.csproj --filter "TestCategory!=Integration"
./tests/LagoVista.CloudStorage.Tests/run-mongo-tests.ps1
```

## Acceptance criteria

- [ ] A dry run can inventory a real Cosmos collection and show exactly where each entity type will land in Mongo.
- [ ] A real run can be interrupted and safely rerun without duplicate documents.
- [x] `id`/`_id` transformation is deterministic in focused coverage.
- [x] Cosmos metadata removal is covered by focused tests.
- [x] Unknown entity types are routed through the configured fallback rather than discarded by the implementation.
- [ ] Source and target counts reconcile by entity type against real data.

## Out of scope

- Live dual-write/change-feed synchronization during the first implementation.
- Destructive removal from Cosmos.
- Application cutover.
