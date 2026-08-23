# Card 6B - Audit and Provider-Neutralize Direct Cosmos Consumers

## Objective

Audit every production `LagoVista.CloudStorage` code path that directly references Cosmos and give it an explicit disposition before Mongo cutover.

The rule is intentionally broad: if production CloudStorage code references `Microsoft.Azure.Cosmos`, `CosmosClient`, `Container`, `QueryDefinition`, `PatchOperation`, `GetItemQueryIterator`, or another Cosmos-specific API, it belongs in this audit.

Each direct Cosmos consumer must be one of:

1. converted so normal application behavior follows the selected document provider
2. retained as the Cosmos implementation behind a provider-neutral abstraction
3. explicitly documented as intentionally Cosmos-only infrastructure
4. removed/deferred if obsolete

Nothing is allowed to remain Cosmos-bound merely because it was missed by the migration work.

This card is a prerequisite for Mongo dev cutover and Card 7 validation.

## Local storage lab

Card 6B should be developed primarily against the disposable local storage lab rather than shared dev data.

The lab contains:

- MongoDB 8 on `localhost:27018`
- Azure Cosmos DB Linux vNext emulator on `https://localhost:18081`
- Cosmos readiness probe on `http://localhost:18080/ready`
- Cosmos Data Explorer on `http://localhost:1234`

Start it with:

```powershell
./tests/LagoVista.CloudStorage.Tests/start-storage-lab.ps1
```

Stop and delete local lab data with:

```powershell
./tests/LagoVista.CloudStorage.Tests/stop-storage-lab.ps1
```

Run the direct Cosmos consumer audit with:

```powershell
./tests/LagoVista.CloudStorage.Tests/audit-cosmos-consumers.ps1
```

The detailed gated execution plan is in `docs/storage-modernization-finish-line.md`.

The emulator is a development sandbox, not the final Azure verification environment. Current Microsoft documentation notes that the vNext emulator supports the NoSQL API in gateway mode with a subset of features and that .NET bulk execution is not supported. Any emulator gap must remain covered by the final real non-production Cosmos validation gate.

## Why this must happen before cutover

The generic repository path can now select Mongo by logical database, but production CloudStorage still contains direct Cosmos consumers.

If a logical database is switched to Mongo while one of those code paths still reads or mutates the Cosmos copy, the application can become internally split-brained:

- normal repository CRUD reads/writes Mongo
- utility/query paths may still read Cosmos
- patch/update paths may still mutate Cosmos
- application behavior can vary depending on which code path served the request

Even direct Cosmos consumers that do not touch shared first-class entities need an explicit classification so we know whether they are legitimate provider implementations or accidental application coupling.

## Current SDK-reference audit backbone

A repo-wide search for `Microsoft.Azure.Cosmos` currently identifies production references including:

- `OperationResponse.cs`
- `Storage/CosmosClientProvider.cs`
- `Interfaces/ICosmosClientProvider.cs`
- `StorageProviders/CosmosDocumentCollection.cs`
- `StorageProviders/CosmosDBStorage.cs`
- `Storage/DocumentDBRepoBase.cs`
- `Storage/DocumentMigrationService.cs`
- `Storage/StorageUtils.cs`
- `Storage/EntityListItemRepo.cs`
- `Storage/EntityUtilsRepository.cs`
- `Storage/EntityPreparationCandidateRepository.cs`
- `Storage/CosmosSyncRepository.cs`

Package/project references and test-only Cosmos usages are tracked separately from production behavior.

This SDK search is the starting inventory, not the final inventory. The audit must also search for indirect Cosmos-specific types and operations such as `QueryDefinition`, `PatchOperation`, `Container`, `CosmosClient`, `FeedIterator`, `GetItemQueryIterator`, and direct database/container management.

## Known application-facing consumers that must become provider-neutral

### `EntityUtilsRepository`

Directly owns a Cosmos `Container` and performs cross-entity queries, raw document reads, and partial entity updates/patches over the same records managed by normal repositories.

Representative capabilities include entity summaries, readiness/checklist candidates, status queries, load-by-ID, selected-field patches, cache invalidation, dependency handling, and RAG-related follow-up.

### `EntityPreparationCandidateRepository`

Directly owns a Cosmos `Container` and performs candidate/summary queries over the same first-class entities, including readiness and production-ready state.

### `EntityListItemRepo<TEntity>`

Although it derives from `DocumentDBRepoBase<TEntity>`, list/header/category paths bypass provider-neutral repository APIs and execute raw Cosmos queries through `GetContainerAsync()`.

### `StorageUtils`

Directly owns Cosmos connectivity and performs shared-entity reads and mutations including key lookup, ratings, visibility/public-state changes, and patch-style updates.

### `CosmosSyncRepository`

Must be fully classified method-by-method. Any normal application entity behavior must follow provider selection. Truly Cosmos-specific synchronization/bootstrap behavior may remain explicitly Cosmos-aware.

## Expected intentional Cosmos-aware infrastructure

The following may remain Cosmos-specific where they are clearly acting as Cosmos provider/infrastructure implementations rather than normal application data access:

- `CosmosDBStorage<TEntity>`
- `CosmosDocumentCollection`
- `CosmosClientProvider` / `ICosmosClientProvider`
- Cosmos source side of `DocumentMigrationService`
- explicit Cosmos database/container management
- explicit Cosmos users/permissions/resource-token operations
- Cosmos-specific constructors or compatibility surfaces in otherwise provider-neutral result types, such as `OperationResponse`, where required for the Cosmos provider

These still receive an explicit audit disposition. They are not excluded from Card 6B simply because we expect them to remain.

## Design direction

Do not create Mongo-specific copies of application repositories.

Prefer extending provider-neutral capabilities at the smallest reusable seam:

- typed filters/projections through `IDocumentCollection` or the rich document provider when sufficient
- registered `DocumentQueryType` semantic operations for complex provider-specific query shapes
- provider-neutral partial-update/patch capability where read-modify-replace would be unsafe or unnecessarily expensive
- raw provider-neutral document access only when a typed contract is genuinely impractical

Both Cosmos and Mongo implementations must preserve existing public application-facing repository interfaces so consuming code does not need provider-specific branches.

For converted application-facing behavior, prefer semantic parity tests that run the same scenario against both the Cosmos emulator and Mongo in the local storage lab.

## Tasks

- [x] Add disposable Mongo + Cosmos emulator storage lab.
- [x] Add repeatable direct Cosmos consumer audit script.
- [x] Add gated finish-line runbook for sandbox parity, migration, configuration, and real non-production verification.
- [x] Add deterministic Cosmos emulator settings to `TestConnections` and prove basic SDK connectivity.
- [x] Add a `CosmosSandbox` NUnit category covering the Cosmos operations required by Card 6B.
- [ ] Complete a repo-wide inventory of every production CloudStorage direct Cosmos reference, not only shared-entity repositories.
- [ ] Search by SDK namespace and Cosmos-specific types/operations so indirect direct-access paths are not missed.
- [ ] Create a disposition table for every production Cosmos consumer: provider-neutralize, Cosmos provider implementation, intentional Cosmos-only infrastructure, or obsolete/deferred.
- [ ] Refactor `EntityUtilsRepository` so all normal application reads and writes follow the selected document provider.
- [x] Refactor `EntityPreparationCandidateRepository` so candidate/summary reads follow the selected document provider.
- [ ] Refactor `EntityListItemRepo<TEntity>` raw Cosmos list/header/category queries to provider-neutral query capabilities.
- [ ] Refactor the application-facing portions of `StorageUtils` to follow the selected provider.
- [ ] Classify and address `CosmosSyncRepository` method-by-method.
- [ ] Audit remaining Cosmos references in `DocumentDBRepoBase<TEntity>` and ensure Mongo-selected normal operations never fall through to direct Cosmos access.
- [ ] Audit `OperationResponse` and similar compatibility types so Cosmos SDK coupling does not leak into normal application contracts unnecessarily.
- [ ] Add provider-neutral partial-update capability if required by existing patch semantics.
- [ ] Add focused Mongo tests for every converted application-facing path.
- [ ] Preserve/add Cosmos regression coverage for converted paths.
- [x] Add a `StorageParity` suite that executes equivalent shared-entity scenarios against local Cosmos and Mongo.
- [ ] Run the real Cosmos-to-Mongo migration service inside the storage lab and validate routing, transforms, idempotency, continuation, and counts where emulator support permits.
- [ ] Verify cache invalidation, dependency updates, RAG side effects, revision behavior, and other workflow semantics remain correct after provider-neutralization.
- [ ] Search consuming solution repositories for direct Cosmos access to the same document databases and capture additional cutover blockers.
- [ ] Re-run the production Cosmos-reference audit at completion and verify every remaining reference has a documented intentional disposition.

## Acceptance criteria

- Every production Cosmos reference in `LagoVista.CloudStorage` is inventoried and explicitly classified.
- When a logical document database selects Mongo, no normal CloudStorage application path silently reads or mutates the Cosmos copy instead.
- `EntityUtilsRepository` reads and mutations operate successfully against Mongo for a Mongo-selected logical database.
- `EntityPreparationCandidateRepository` returns Mongo-backed candidate/summary data for a Mongo-selected logical database.
- `EntityListItemRepo<TEntity>` list/header/category behavior works without direct Cosmos container access when Mongo is selected.
- Application-facing `StorageUtils` operations work against the selected provider.
- `CosmosSyncRepository` has no unclassified provider bypasses.
- Equivalent Card 6B application behaviors have Cosmos-emulator and Mongo semantic parity coverage where the emulator supports the required Cosmos feature.
- Remaining Cosmos SDK references are intentional provider, migration, security, or platform infrastructure and are documented as such.
- Existing public application-facing interfaces remain provider-neutral.
- Cosmos remains functional when Cosmos is selected.
- Card 7 dev cutover does not begin until this audit is complete and all application-facing blockers are resolved.

## Out of scope

- Removing the Cosmos SDK merely for aesthetic/package cleanup.
- Replacing Cosmos as the source for Cosmos-to-Mongo migration.
- Treating emulator success as a substitute for final real non-production Cosmos verification.
- Reimplementing Cosmos-specific users/permissions/resource-token behavior in Mongo unless a real application requirement demands it.
- Production cutover.
