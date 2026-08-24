# Card 4 - Cosmos-to-Mongo Migration Tooling

## Objective

Provide a safe, repeatable utility that copies raw EntityBase documents from Cosmos into Mongo using the canonical `Entities` collection.

## Status

Core migration and validation implementation is complete. EntityBase migration now has one provider-neutral destination collection, and transform/count reconciliation coverage is in place. Live execution against a real Cosmos source and non-production Mongo target remains the substantive validation step.

## Migration shape

`Cosmos consolidated collection -> raw JObject -> transform -> Entities -> Mongo bulk upsert`

The migrator does not deserialize documents into application entity types.

## Implemented transforms

- Move Cosmos root `id` to Mongo `_id`.
- Do not retain a duplicate top-level `id` field.
- Strip `_rid`, `_self`, `_etag`, `_attachments`, and `_ts`.
- Preserve the remaining raw JSON document shape and nested values, including nested `Id` fields.
- Preserve `EntityType`, `OwnerOrganization`, and `IsPublic` exactly as stored.
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

All normal EntityBase documents migrate to the same provider-neutral collection:

```text
Entities
```

`EntityType` remains part of every document and continues to drive typed queries, validation statistics, diagnostics, and generic infrastructure after migration.

The migration service uses the same canonical `IDocumentCollectionNameResolver` fallback as runtime EntityBase storage. It does not attempt to classify documents into shareable, organization, domain, or dedicated physical collections.

An unknown or unloaded `EntityType` therefore does not make the physical migration route unresolved. The document still belongs in `Entities`; missing or unexpected `EntityType` values should be treated separately as data-quality concerns.

## Mongo writes

Documents are grouped by destination collection for each Cosmos page and written with unordered Mongo bulk writes. For EntityBase migration there is one destination batch, `Entities`.

Each write is a replacement upsert filtered by `_id`, making normal reruns idempotent.

Dry-run mode executes source reading, transforms, routing, and statistics collection without opening a Mongo connection or writing data.

## Validation

`ValidateCosmosToMongoAsync` performs a full dry-run Cosmos inventory using the same destination rule, then counts corresponding documents in Mongo `Entities` by `EntityType`.

The result reports source and destination totals plus per-entity-type counts and a `Matches` flag. Missing/null/empty `EntityType` values are validated only against equivalent Mongo documents rather than counting the entire collection.

The local Mongo integration work has additionally proven that newly-written Mongo documents preserve the same root/nested identity contract expected by this migration path: root identity uses `_id`, while nested `EntityHeader.Id` remains `Id`.

## Result models

`CosmosToMongoMigrationResult` reports:

- pages read
- documents read
- documents written
- documents skipped
- documents failed
- continuation token
- completed flag
- dry-run flag
- per-entity-type/per-destination statistics

The legacy unresolved-route counter remains on the result model for compatibility, but normal EntityBase migration no longer increments it because the physical destination is always known.

`CosmosToMongoValidationResult` reports:

- total source count
- total destination count
- overall match status
- per-entity-type source and destination counts
- per-route match status

No credentials or connection strings are included in either result.

## Completed tasks

- [x] Add migration request/result models.
- [x] Support explicit source Cosmos and target Mongo settings.
- [x] Stream Cosmos documents page-by-page.
- [x] Support configurable Cosmos page/batch size.
- [x] Route all EntityBase documents to the canonical `Entities` collection.
- [x] Transform root `id` to `_id` while preserving nested `Id` fields.
- [x] Strip Cosmos system metadata.
- [x] Preserve `EntityType`, `OwnerOrganization`, and `IsPublic` during transformation.
- [x] Bulk upsert Mongo documents by `_id`.
- [x] Support optional `EntityType` filtering.
- [x] Add dry-run mode.
- [x] Add continuation token input/output and bounded `MaxPages` execution.
- [x] Count read, written, skipped, and failed documents.
- [x] Include per-entity-type/per-destination statistics.
- [x] Register migration service with dependency injection.
- [x] Keep secrets out of migration reports.
- [x] Add transform-focused unit tests for `id -> _id`, Cosmos metadata removal, EntityBase field preservation, nested-shape preservation, missing IDs, and source immutability.
- [x] Add validation mode comparing Cosmos and Mongo counts by entity type in `Entities`.

## Remaining tasks

- [ ] Build CloudStorage and run the full non-integration/unit suite after the canonical destination change.
- [ ] Run a dry-run against a real Cosmos database and review entity-type inventory.
- [ ] Run a small bounded migration into dev/non-production Mongo `Entities`.
- [ ] Validate continuation/resume behavior against a real Cosmos feed.
- [ ] Validate rerunning the same page does not create duplicates.
- [ ] Run count reconciliation and confirm per-entity-type matches.
- [ ] Compare representative migrated documents with newly-written Mongo documents for structural compatibility.

## Local validation

```powershell
dotnet build src/LagoVista.CloudStorage/LagoVista.CloudStorage.csproj
dotnet test tests/LagoVista.CloudStorage.Tests/LagoVista.CloudStorage.IntegrationTests.csproj --filter "TestCategory!=Integration"
./tests/LagoVista.CloudStorage.Tests/run-mongo-tests.ps1
```

## Acceptance criteria

- [ ] A dry run can inventory a real Cosmos collection and show all EntityBase records targeting `Entities`.
- [ ] A real run can be interrupted and safely rerun without duplicate documents.
- [x] `id`/`_id` transformation is deterministic in focused coverage.
- [x] Cosmos metadata removal is covered by focused tests.
- [x] `EntityType`, `OwnerOrganization`, and `IsPublic` preservation is covered by focused tests.
- [x] Unknown entity types still have a deterministic `Entities` destination.
- [ ] Source and target counts reconcile by entity type against real data.

## Out of scope

- ApplicationData or ScratchData migration.
- Live dual-write/change-feed synchronization during the first implementation.
- Destructive removal from Cosmos.
- Application cutover.
