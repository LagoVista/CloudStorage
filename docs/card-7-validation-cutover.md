# Card 7 - Validation, Cutover, and Operational Runbook

## Objective

Prove Mongo parity with representative production workloads, document rollback, and define a controlled cutover from Cosmos.

## Status

Local Mongo integration validation is green. The repository-owned Docker Mongo 8 harness now proves both the lower-level `MongoDocumentCollection` adapter and the actual `DocumentDBRepoBase<TEntity>` provider-selection path.

Staged dev cutover is **blocked on Card 6B**. Direct Cosmos utilities that operate on the same entity records as normal repositories must be provider-neutral before a logical database can safely switch to Mongo. Otherwise normal repository operations could read/write Mongo while utility paths continue reading or mutating the Cosmos copy of the same entities.

After Card 6B is complete, the next phase is staged environment validation: finish the first-class runtime configuration bridge, exercise representative cache/dependency repositories, run real Cosmos-to-Mongo migration/reconciliation, and then select a dev logical database for controlled cutover.

## Prerequisite - shared-entity provider consistency

See [Card 6B - Provider-Neutral Shared-Entity Utilities](card-6b-provider-neutral-shared-entity-utilities.md).

Before any dev logical database is cut over:

- `EntityUtilsRepository` shared-entity reads and mutations must follow the selected provider.
- `EntityPreparationCandidateRepository` candidate/summary reads must follow the selected provider.
- `EntityListItemRepo<TEntity>` list/header/category paths must not bypass provider selection through direct Cosmos queries.
- shared-entity operations in `StorageUtils` must follow the selected provider.
- any additional direct Cosmos consumer operating on the same migrated entity set must be classified and addressed.

Intentional Cosmos provider/migration infrastructure is not a blocker merely because it references the Cosmos SDK.

## Validation matrix

| Area | Coverage | Status |
| --- | --- | --- |
| Provider selection | Database-specific Mongo override through existing `DocumentDBRepoBase<TEntity>` constructor | **PASS - local Mongo 8** |
| Dynamic connection | `SetConnection` re-resolves Mongo without requiring a Cosmos shared key | **PASS - local Mongo 8** |
| Create | Base repository create path persists to domain collection | **PASS - local Mongo 8** |
| Read | Base repository get path reads Mongo entity | **PASS - local Mongo 8** |
| Update | Base repository upsert path updates data and revision | **PASS - local Mongo 8** |
| List/sort | Base repository filtered and sorted list query | **PASS - local Mongo 8** |
| Soft delete | Default list hides deleted records; `ShowDeleted` reveals them | **PASS - local Mongo 8** |
| Hard delete | Physical Mongo delete removes document | **PASS - local Mongo 8** |
| Domain routing | Test entity routes to `EntityDescriptionAttribute.Domain` collection | **PASS - local Mongo 8** |
| `_id` mapping | Root document ID maps to `_id`; nested `EntityHeader.Id` remains `Id` | **PASS - live + focused tests** |
| LagoVista BSON wire types | `UtcTimestamp`, `NormalizedId32`, `LagoVistaKey` | **PASS - focused + live path** |
| Projections | Lower-level `IDocumentCollection` live fixture | **PASS - local Mongo 8** |
| Semantic queries | Customer aggregate live fixture | **PASS - local Mongo 8** |
| Shared-entity utility consistency | Card 6B direct Cosmos consumers follow selected provider | **BLOCKER - pending** |
| Cache | Representative base repository with real cache provider | Pending |
| Dependency checks | Representative base repository with dependency manager | Pending |
| Migration counts | Cosmos dry-run and Mongo reconciliation | Pending |
| Structural sample comparison | Migrated vs newly written nested headers, enums, dates, arrays, optional/null fields | Partially proven; migration sample run pending |
| Performance-sensitive paths | Representative high-volume/large-document queries | Pending |
| Runtime configuration | First-class `MongoDocumentStorage` settings feed compatibility resolver | Pending |
| Rollback | Provider configuration switched back to Cosmos with data retained | Pending |

## Local Docker Mongo integration tests

The integration-test project owns a local Docker Mongo harness:

- `tests/LagoVista.CloudStorage.Tests/docker-compose.mongo.yml`
- `tests/LagoVista.CloudStorage.Tests/start-mongo-tests.ps1`
- `tests/LagoVista.CloudStorage.Tests/run-mongo-tests.ps1`
- `tests/LagoVista.CloudStorage.Tests/stop-mongo-tests.ps1`

The Docker instance runs Mongo 8 on `localhost:27018` with disposable local-test credentials. `TestConnections.TestMongoDocumentStorage` owns the deterministic local connection values, so the runner does not need `TEST_MONGO_*` environment variables.

Run the Mongo integration suite with:

```powershell
./tests/LagoVista.CloudStorage.Tests/run-mongo-tests.ps1
```

Stop and remove the local Mongo test container with:

```powershell
./tests/LagoVista.CloudStorage.Tests/stop-mongo-tests.ps1
```

The Docker credentials are test-only and intentionally deterministic. Production credentials remain application/secret configuration.

## Green checkpoint - August 23, 2026

User-executed local validation produced:

```text
LagoVista.CloudStorage netstandard2.1 succeeded
LagoVista.CloudStorage.IntegrationTests net9.0 succeeded
NUnit Adapter discovered 6 of 6 Mongo integration tests
Test summary: total: 6, failed: 0, succeeded: 6, skipped: 0, duration: 1.1s
```

The run exercised:

- lower-level Mongo filtering, sorting, paging, and projection
- server-side customer metrics aggregation
- real `DocumentDBRepoBase<TEntity>` Mongo provider selection
- create/read/update
- domain routing
- soft and hard delete semantics
- database-specific override and `SetConnection`
- root `_id` identity mapping
- nested `EntityHeader.Id` compatibility with migration shape
- LagoVista value-type BSON serialization required by `EntityBase`

Most importantly, the derived test repository used the existing constructor shape. No Mongo-specific derived repository constructor was required.

## Tasks

- [x] Build a validation matrix covering CRUD, list queries, projections, semantic queries, caching, deletes, and dependency checks.
- [x] Add repeatable Docker-backed local Mongo validation.
- [x] Validate primary CRUD/query/delete semantics through `DocumentDBRepoBase<TEntity>`.
- [x] Validate domain collection routing and root/nested identity shape.
- [x] Validate lower-level semantic aggregation against real Mongo.
- [ ] Complete Card 6B and prove shared-entity utilities follow the selected provider.
- [ ] Bridge first-class `MongoDocumentStorage` configuration into runtime provider resolution.
- [ ] Select representative logical databases and entity domains for staged validation.
- [ ] Validate a representative repository with the real cache provider.
- [ ] Validate a representative repository with dependency manager behavior.
- [ ] Run Cosmos-to-Mongo migration in dry-run mode and review routing/count reports.
- [ ] Run migration into a non-production Mongo database and reconcile counts by `EntityType` and domain collection.
- [ ] Compare representative migrated documents with newly-written Mongo documents structurally.
- [ ] Exercise application reads against Mongo before enabling writes where practical.
- [ ] Run targeted performance comparisons for known large-document/high-volume query paths.
- [ ] Validate domain collection names and correctness-critical indexes for the selected cutover database.
- [ ] Document provider/application configuration for local, dev, and later production use.
- [ ] Define cutover sequence, smoke tests, rollback criteria, and rollback steps.
- [ ] Prove rollback by switching the staged logical database back to Cosmos while source data remains intact.
- [ ] Document known Cosmos-specific migration islands that remain after generic document cutover.

## Acceptance criteria

- [ ] Card 6B confirms no shared-entity utility silently accesses Cosmos when its logical database is configured for Mongo.
- [ ] Migration reports reconcile source and target counts.
- [ ] Representative application workflows pass against Mongo in dev.
- [ ] Known performance-sensitive queries do not regress materially.
- [x] Existing repository constructors can use Mongo without code changes.
- [ ] Final cutover can be enabled through first-class application configuration without repository code changes.
- [ ] Rollback to Cosmos is documented, configuration-driven, and exercised.
- [x] No destructive Cosmos cleanup is required to complete the initial cutover.

## Follow-up after stable cutover

Only after stability is established should we consider:

- Mongo index tuning based on observed queries.
- Retiring Cosmos-specific generic storage code.
- Removing unused Cosmos package dependencies.
- Archiving or deleting migrated Cosmos data according to an explicit retention decision.
