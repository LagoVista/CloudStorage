# Storage Modernization Finish-Line Plan

## Goal

Bring the Cosmos-to-Mongo modernization across the finish line using a disposable local storage lab before touching shared dev data.

The local lab contains:

- MongoDB 8 on `localhost:27018`
- Azure Cosmos DB Linux vNext emulator on `https://localhost:18081`
- Cosmos readiness probe on `http://localhost:18080/ready`
- Cosmos Data Explorer on `http://localhost:1234`

The lab is disposable. Tests should create isolated databases/containers and remove them when complete.

## Start and stop the lab

From the repository root:

```powershell
./tests/LagoVista.CloudStorage.Tests/start-storage-lab.ps1
```

Run the complete current baseline:

```powershell
./tests/LagoVista.CloudStorage.Tests/run-storage-lab-baseline.ps1
```

Stop and delete lab data:

```powershell
./tests/LagoVista.CloudStorage.Tests/stop-storage-lab.ps1
```

The existing Mongo-only harness remains available and should stay green independently.

## Gate 0 - Baseline

The baseline runner currently performs:

1. CloudStorage build
2. Mongo integration suite
3. Cosmos emulator SDK smoke test
4. direct-Cosmos production consumer audit

Required state:

- CloudStorage builds.
- Mongo integration suite remains green.
- Cosmos emulator create/read/delete smoke test passes through the shared `CosmosClientProvider`.
- Every production direct-Cosmos file is listed and assigned a Card 6B disposition.

The Cosmos lab endpoint/key are deterministic test-only values in `StorageLabConnections`. Production/dev secrets are not used.

## Gate 1 - Cosmos emulator capability validation

The first `CosmosSandbox` test is implemented and exercises the real `CosmosClientProvider`. Loopback Cosmos endpoints use gateway mode because the Linux vNext emulator supports gateway mode rather than the production Direct-mode path.

Expand `CosmosSandbox` coverage to prove:

- create database/container
- create/get/update/delete document
- typed query with nested `EntityHeader.Id`
- `QueryDefinition` behavior used by current utilities
- patch operations required by `EntityUtilsRepository` and `StorageUtils`
- cleanup of isolated test resources

Do not proceed to shared-entity conversion until the required operation for that slice is known to work in the emulator version used by the lab.

## Gate 2 - Provider-neutral shared-entity utilities

Work through Card 6B one direct-Cosmos consumer at a time.

For each application/shared-entity class:

1. identify every read/write/query/patch capability
2. move the capability behind the smallest provider-neutral seam
3. implement Cosmos and Mongo behavior
4. run the same semantic test against both providers
5. preserve cache/dependency/RAG side effects
6. mark the direct Cosmos reference as classified or removed

Recommended first conversion: `EntityPreparationCandidateRepository`. It is read-only and projection-heavy, making it a good first proof of the provider-neutral query pattern before partial-update semantics are introduced.

Headline classes include:

- `EntityPreparationCandidateRepository`
- `EntityListItemRepo<TEntity>`
- `EntityUtilsRepository`
- shared-entity portions of `StorageUtils`
- `CosmosSyncRepository` after classification

Provider implementations and explicit Cosmos infrastructure may remain Cosmos-aware, but must be documented as such.

## Gate 3 - Storage parity suite

Create a `StorageParity` NUnit category that executes equivalent scenarios against Cosmos emulator and Mongo.

Minimum parity scenarios:

- repository CRUD
- filtered/sorted/paged queries
- projections
- entity headers/list items
- readiness/candidate queries
- partial field/status updates
- ratings/public-state mutations used by `StorageUtils`
- soft/hard delete where applicable
- cache invalidation
- dependency behavior

A scenario is green only when both providers return equivalent business results, not merely successful HTTP/database calls.

## Gate 4 - Sandbox Cosmos-to-Mongo migration

Seed representative Cosmos-emulator documents and run the real migration service into local Mongo.

Validate:

- dry-run route inventory
- root `id` -> Mongo `_id`
- nested `Id` fields remain `Id`
- Cosmos metadata is removed
- domain collection routing
- unknown entity fallback
- bounded page execution
- continuation/resume
- rerun idempotency
- source/target count reconciliation
- representative structural comparison

The vNext emulator does not replace final Azure validation. Emulator limitations must be recorded if they prevent any migration capability from being exercised locally.

## Gate 5 - First-class runtime configuration

Finish Card 2 so normal LagoVista configuration selects Cosmos or Mongo without test-only environment-variable plumbing.

Required proof:

- Cosmos remains the default
- one logical database can select Mongo
- another logical database can remain Cosmos in the same process
- existing derived repository constructors remain unchanged
- Card 6B utilities follow the same selection

## Gate 6 - Real non-production verification

Only after Gates 0-5 are green:

1. run a dry-run inventory against real non-production Cosmos
2. run a small bounded migration into non-production Mongo
3. verify continuation/resume and rerun safety
4. reconcile source/target counts
5. compare representative documents
6. exercise application reads
7. exercise application writes
8. verify rollback to Cosmos while Cosmos source data remains intact

No destructive Cosmos cleanup occurs during this phase.

## Gate 7 - Cutover readiness

Card 7 is cutover-ready when:

- no unclassified direct Cosmos production consumer can bypass the selected provider
- Mongo and Cosmos sandbox parity is green for shared-entity behaviors
- sandbox migration is repeatable
- real non-production migration reconciles
- first-class configuration is proven
- application smoke tests pass
- rollback has been exercised

## Emulator limitations

The Azure Cosmos DB Linux vNext emulator is a development sandbox, not a perfect cloud replica. Current Microsoft documentation notes that it supports the NoSQL API in gateway mode with a subset of features, and the .NET SDK does not support bulk execution against the emulator.

Use the emulator aggressively for safe local development, but retain the real non-production Cosmos validation gate for behaviors the emulator cannot faithfully exercise.
