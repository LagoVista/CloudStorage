# Card 7 - Validation, Cutover, and Operational Runbook

## Objective

Prove Mongo parity with representative production workloads, document rollback, and define a controlled cutover from Cosmos.

## Status

Runtime validation has started. The first integration fixtures exercise both the lower-level Mongo document collection and the actual `DocumentDBRepoBase<TEntity>` provider-selection path.

Local Mongo integration testing is Docker-backed and does not require access to the Kubernetes Mongo deployment. The fixtures use `TestConnections.TestMongoDocumentStorage`, create isolated random databases, and remove those databases during teardown.

## Validation matrix

| Area | Coverage | Status |
| --- | --- | --- |
| Provider selection | Database-specific Mongo override through existing `DocumentDBRepoBase<TEntity>` constructor | Implemented, live run pending |
| Dynamic connection | `SetConnection` re-resolves Mongo without requiring a Cosmos shared key | Implemented, live run pending |
| Create | Base repository create path persists to domain collection | Implemented, live run pending |
| Read | Base repository get path reads Mongo entity | Implemented, live run pending |
| Update | Base repository upsert path updates data and revision | Implemented, live run pending |
| List/sort | Base repository filtered and sorted list query | Implemented, live run pending |
| Soft delete | Default list hides deleted records; `ShowDeleted` reveals them | Implemented, live run pending |
| Hard delete | Physical Mongo delete removes document | Implemented, live run pending |
| Domain routing | Test entity routes to `EntityDescriptionAttribute.Domain` collection | Implemented, live run pending |
| `_id` mapping | Existing Mongo adapter coverage plus live base-path persistence | Implemented, live run pending |
| Projections | Lower-level `IDocumentCollection` live fixture | Implemented, live run pending |
| Semantic queries | Customer aggregate live fixture | Implemented, live run pending |
| Cache | Representative base repository with cache provider | Pending |
| Dependency checks | Representative base repository with dependency manager | Pending |
| Migration counts | Cosmos dry-run and Mongo reconciliation | Pending |
| Structural sample comparison | Nested headers, enums, dates, arrays, optional/null fields | Pending |
| Performance-sensitive paths | Representative high-volume/large-document queries | Pending |
| Rollback | Provider configuration switched back to Cosmos with data retained | Pending |

## Local Docker Mongo integration tests

The integration-test project owns a local Docker Mongo harness:

- `tests/LagoVista.CloudStorage.Tests/docker-compose.mongo.yml`
- `tests/LagoVista.CloudStorage.Tests/start-mongo-tests.ps1`
- `tests/LagoVista.CloudStorage.Tests/run-mongo-tests.ps1`
- `tests/LagoVista.CloudStorage.Tests/stop-mongo-tests.ps1`

The Docker instance runs Mongo 8 on `localhost:27018` with disposable local-test credentials. `run-mongo-tests.ps1` starts Mongo, waits for its health check, sets process-scoped `TEST_MONGO_*` configuration, and runs only the NUnit `Mongo` category.

Run the Mongo integration suite with:

```powershell
./tests/LagoVista.CloudStorage.Tests/run-mongo-tests.ps1
```

Stop and remove the local Mongo test container with:

```powershell
./tests/LagoVista.CloudStorage.Tests/stop-mongo-tests.ps1
```

The Docker credentials are test-only and intentionally deterministic. Production credentials are not used by these scripts.

## Tasks

- Build a validation matrix covering CRUD, list queries, projections, semantic queries, caching, deletes, and dependency checks.
- Select representative logical databases and entity domains for staged validation.
- Run Cosmos-to-Mongo migration in dry-run mode and review routing/count reports.
- Run migration into a non-production Mongo database and reconcile counts by `EntityType` and domain collection.
- Validate representative documents structurally, including `_id`, nested headers, enums, dates, arrays, and optional/null fields.
- Exercise application reads against Mongo before enabling writes where practical.
- Run targeted performance comparisons for known large-document/high-volume query paths.
- Validate domain collection names and required indexes.
- Document provider/environment configuration for local, dev, and later production use.
- Define cutover sequence, smoke tests, rollback criteria, and rollback steps.
- Keep Cosmos data intact through the initial stabilization window.
- Document known Cosmos-specific migration islands that remain after generic document cutover.

## Acceptance criteria

- Migration reports reconcile source and target counts.
- Representative application workflows pass against Mongo.
- Known performance-sensitive queries do not regress materially.
- Cutover can be enabled by configuration without repository code changes.
- Rollback to Cosmos is documented and configuration-driven.
- No destructive Cosmos cleanup is required to complete the initial cutover.

## Follow-up after stable cutover

Only after stability is established should we consider:

- Mongo index tuning based on observed queries.
- Retiring Cosmos-specific generic storage code.
- Removing unused Cosmos package dependencies.
- Archiving or deleting migrated Cosmos data according to an explicit retention decision.
