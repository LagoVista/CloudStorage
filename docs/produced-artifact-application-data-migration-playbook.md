# ProducedArtifact Application Data Migration Playbook

## Purpose

Use this playbook to move the `LagoVista/ai` ProducedArtifact metadata catalog from the legacy Azure `ProducedArtifacts` table into Mongo-backed Application Data Storage.

The new `ProducedArtifact` document also owns a compact embedded `History` list for future prior-version metadata. The legacy `ProducedArtifactHistory` Azure table is development-era data and is intentionally not migrated as part of this cutover.

## Starting prompt for a future session

> Read `docs/produced-artifact-application-data-migration-playbook.md` in `LagoVista/CloudStorage`. Verify current `LagoVista/ai`, `LagoVista/CloudStorage`, and `nuviot/appsupport` master branches before running the ProducedArtifact migration. Follow the initial-pass, verify, catch-up, cutover, and rollback sequence without introducing runtime dual-write behavior.

## Target runtime architecture

The runtime path is composition:

```text
ProducedArtifactRepo
    -> IApplicationDataStore
        -> MongoApplicationDataStore
            -> ProducedArtifact collection
```

The normal write is simply:

```csharp
await _store.InsertAsync(artifact);
```

The application repository does not supply:

- Mongo collection names
- BSON documents
- partition keys
- provider DTOs
- connection strings
- Mongo filters or index commands

CloudStorage derives the physical collection deterministically from `typeof(ProducedArtifact)`.

## Record contract

`ProducedArtifact` implements `IApplicationDataRecord`:

```csharp
public interface IApplicationDataRecord
{
    NormalizedId32 Id { get; set; }
    EntityHeader Organization { get; set; }
    UtcTimestamp CreationDate { get; set; }
    UtcTimestamp LastUpdatedDate { get; set; }
}
```

The canonical organization storage path is:

```text
Organization.Id
```

`OwnerOrganizationId` may exist temporarily on ProducedArtifact as a compatibility field while existing AI callers are converted. New persistence and query behavior must use `Organization.Id` as the authoritative organization scope.

## Embedded revision history

`ProducedArtifact.History` is persisted in the same Application Data document.

The embedded history item is intentionally narrow. It stores the prior committed content version metadata rather than another complete ProducedArtifact document. The first-pass shape includes:

```text
ContentVersion
CreationDate
CreatedBy
VtmMeeting
WorkItemId
StandardOperatingProcedure
PrimaryVirtualTeamMember
EssentialJobActivity
ContentType
ContentSha256
ContentLength
Summary
```

When `CurrentContentVersion` advances, `ProducedArtifactRepo` loads the current persisted record, appends a compact snapshot of that prior version to `History`, then writes the updated ProducedArtifact document.

Metadata-only updates that do not advance `CurrentContentVersion` do not create another history entry.

This is intentionally a simple first implementation. Revisit the exact embedded revision shape only when real usage gives us a reason.

## Legacy source

The source is the Azure Table Storage table:

```text
ProducedArtifacts
```

Legacy identity is:

```text
PartitionKey = organization id
RowKey       = produced artifact id
```

The old Table DTO flattened nested references into columns such as:

```text
ArtifactSpecificationId
ArtifactSpecificationKey
ArtifactSpecificationText
VtmMeetingId
VtmMeetingKey
VtmMeetingText
ScopeId
ScopeKey
ScopeText
...
```

The migration mapper reconstructs the real `ProducedArtifact` POCO and its `EntityHeader` / typed `EntityHeader<T>` properties before writing to Application Data Storage.

The historical `ProducedArtifactHistory` Azure table is not read by this migration. Existing migrated ProducedArtifact documents begin with an empty embedded `History` list unless future migration requirements explicitly change that choice.

## Target collection

The target collection name is not migration configuration.

CloudStorage determines it from the CLR record type:

```text
ProducedArtifact -> ProducedArtifact
```

Do not add a collection name to the AppSupport JSON definition. A type rename is a storage migration, not a configuration alias.

## Runtime indexes

The AI composition root declares the supported query paths:

```text
Organization.Id                    common Application Data index
ArtifactSpecification.Id
VtmMeeting.Id
SopExecution.Id
ScopeType.Id
Scope.Id
WorkItemId
```

Indexes express real repository query needs. The migration utility does not define a second physical index catalog.

## Migration utility

Historical migration lives in:

```text
nuviot/appsupport
src/LagoVista.StorageMigration
```

The migration catalog key is:

```text
ai-produced-artifacts
```

The definition identifies:

```json
{
  "source": {
    "type": "azure-table",
    "tableName": "ProducedArtifacts"
  },
  "target": {
    "type": "application-data",
    "recordType": "ProducedArtifact"
  }
}
```

There is deliberately no physical collection name in the definition.

## Exact model migration

Unlike the generic Cassandra activity mapper, this migration uses the real `LagoVista.AI.Models.ProducedArtifact` package.

That is intentional. Application Data persists the application POCO directly, including typed `EntityHeader<T>` fields. Referencing the real model prevents a migration-only shadow schema from drifting away from runtime serialization.

The typed migration mapper reconstructs at least:

- Id
- Organization / OwnerOrganizationId compatibility value
- Name and summaries
- Status
- SampleKind / SamplePurpose
- ArtifactSpecification
- VtmMeeting
- SopExecution
- Scope / ScopeType
- WorkItemId / SopExecutionContextId
- StandardOperatingProcedure
- PrimaryVirtualTeamMember
- EssentialJobActivity
- review fields
- content storage mode and content metadata
- content version
- vector/search routing metadata
- CreatedBy / LastUpdatedBy
- CreationDate / LastUpdatedDate
- ProjectionVersion

The embedded `History` list starts empty for legacy metadata migration because the old development history table is intentionally not imported.

## Timestamp preservation

Historical timestamps are authoritative migration input.

`MongoApplicationDataStore.InsertAsync` follows this rule:

- if `CreationDate` is empty, CloudStorage establishes it
- if `LastUpdatedDate` is empty, CloudStorage establishes it
- if migration/import supplies either timestamp, the supplied value is preserved

Runtime `UpdateAsync` still preserves the existing CreationDate and advances LastUpdatedDate.

Therefore catch-up migration must not call normal `UpdateAsync` for a historical record whose source LastUpdatedDate should remain unchanged.

The ProducedArtifact migration writer uses:

```text
existing target record?
    yes -> delete target record -> insert source record
    no  -> insert source record
```

This keeps replay idempotent while preserving source timestamps.

A crash between delete and insert is recoverable because the checkpoint is not advanced until the destination batch completes. The same Azure source row will be replayed on resume.

## Migration status repository

New Application Data migrations track progress through an Application-Data-backed migration state repository rather than direct Mongo API calls.

The engine-facing state still contains:

```text
MigrationKey
DefinitionSha256
State
PassNumber
CurrentTable
HeadPartitionKey
HeadRowKey
RecordsRead
RecordsWritten
RecordsFailed
PriorPass...
CreationDate
LastUpdatedDate
CompletedDate
```

The repository maps this into a private `IApplicationDataRecord` and stores it through the same `IApplicationDataStore` capability.

Migration status identity is deterministic from `MigrationKey`, so `status ai-produced-artifacts` does not require a query/index merely to locate the current run.

Existing Cassandra migrations keep their current state storage implementation so previously resumable runs are not stranded.

## Definition SHA safety

The migration catalog computes a SHA-256 of the normalized definition.

A partially started migration refuses to resume if the current definition SHA differs from the recorded SHA.

Do not edit the ProducedArtifact migration definition after an initial pass has begun. If the target mapping must change materially, use a new migration identity or explicitly reset the migration after understanding the consequences.

## Operator configuration

The migration utility reads secrets from environment variables, not JSON definitions.

Azure source uses the existing migration Azure Table environment settings.

Application Data target uses:

```text
MIGRATION_APPLICATION_DATA_CONNECTION_STRING
MIGRATION_APPLICATION_DATA_DATABASE
```

For local disposable testing, the utility may default to the local Mongo storage lab values. Production migration runs must explicitly verify the resolved target before execution.

## Preflight

Before moving data:

1. Publish/build a CloudStorage package containing the non-generic `IApplicationDataStore`, deterministic Mongo provider, nested query paths, and timestamp-preserving insert behavior.
2. Publish/build an AI Models package containing `ProducedArtifact : IApplicationDataRecord`, `Organization`, and embedded `History`.
3. Update `LagoVista/ai` and `nuviot/appsupport` package references to those package versions.
4. Confirm AI runtime configuration contains `ApplicationDataStorage` Mongo connection/database values.
5. Confirm AppSupport migration environment variables point to the intended Azure source and Mongo target.
6. Confirm the `ProducedArtifacts` Azure source table is retained and writable until final cutover.

## Validate the definition

Run:

```text
dotnet run --project src/LagoVista.StorageMigration -- validate ai-produced-artifacts
```

Expected output includes:

```text
PASS: ai-produced-artifacts
target type: application-data
record type: ProducedArtifact
collection: deterministic from record type
```

Do not proceed if definition validation fails.

## Inspect status

Run:

```text
dotnet run --project src/LagoVista.StorageMigration -- status ai-produced-artifacts
```

Before the first run the expected state is:

```text
Not Started
```

After a partial run, the state should include the last successfully committed Azure PartitionKey/RowKey head.

## Initial migration

Run:

```text
dotnet run --project src/LagoVista.StorageMigration -- migrate ai-produced-artifacts
```

The engine:

1. resolves the ProducedArtifacts source table
2. resumes after the last successfully checkpointed source row if necessary
3. reads Azure rows
4. maps each row to the exact ProducedArtifact POCO
5. writes through `IApplicationDataStore`
6. checkpoints only after a destination batch succeeds
7. marks the pass complete when the source is exhausted

Do not deploy the new AI runtime write path halfway through this initial pass unless that is part of an explicitly planned cutover window.

## Verify the initial pass

Run:

```text
dotnet run --project src/LagoVista.StorageMigration -- verify ai-produced-artifacts
```

Verification requires:

- migration state is Completed
- definition SHA matches
- current-pass source count equals RecordsWritten
- RecordsFailed is zero
- target ProducedArtifact collection count equals source table count

Count equality is necessary but not sufficient. Before cutover also inspect representative records from multiple organizations and lifecycle states.

## Representative data validation

Sample records should cover:

- Draft artifact
- Approved/Published artifact if available
- sample artifact
- normal non-sample artifact
- artifact with VtmMeeting
- artifact with SOP execution
- artifact with Scope/ScopeType
- artifact with WorkItemId
- artifact with review data
- artifact with blob-backed content metadata
- artifact with inline/external content if present

For each sampled record confirm:

- Id matches Azure RowKey
- Organization.Id matches Azure PartitionKey
- Name/summary match
- nested EntityHeaders contain expected Id/Key/Text
- typed status/sample/content mode match
- CurrentContentVersion matches
- content hash/length/location fields match
- CreationDate matches source
- LastUpdatedDate matches source
- History is empty immediately after legacy metadata migration

## Catch-up pass

If Azure can change between the initial migration and runtime cutover, run a final replay:

```text
dotnet run --project src/LagoVista.StorageMigration -- migrate ai-produced-artifacts --catch-up
```

Catch-up starts only after the prior pass is Completed.

The pass replays the full source table and replaces matching target records. This is intentionally simple and safe for the expected ProducedArtifact data volume. It avoids runtime dual-write and eliminates a second reconciliation protocol.

Run verify again immediately after catch-up.

## Cutover

Recommended cutover sequence:

1. complete and verify the initial migration
2. enter the short cutover window
3. run final catch-up
4. verify again
5. deploy AI version whose ProducedArtifactRepo composes `IApplicationDataStore`
6. smoke-test ProducedArtifact read/write/list queries
7. leave the Azure ProducedArtifacts table intact through the rollback window

The new AI runtime should not dual-write Azure and Mongo by default.

## Runtime smoke tests

After deployment verify:

- create ProducedArtifact
- get by artifact id + organization
- update and reload
- advance CurrentContentVersion and confirm the prior version appears once in embedded History
- perform a metadata-only update and confirm no duplicate history entry is created
- query by ArtifactSpecification.Id
- query by VtmMeeting.Id
- query by SopExecution.Id
- query by ScopeType.Id + Scope.Id
- query by WorkItemId
- sort by CreationDate
- sort by Name where required
- page using the opaque continuation token passed through the existing ListResponse cursor fields
- content blob retrieval still works independently

## Rollback

During the rollback window the Azure source remains the safety net.

If the new runtime must be rolled back:

1. stop/roll back the AI version writing Application Data
2. redeploy the prior Table Storage runtime
3. identify ProducedArtifact mutations that occurred only in Mongo after cutover
4. decide whether those few records must be copied back to Azure before reopening writes
5. do not delete the Mongo target while investigating

The short rollback window is one reason to avoid a long-running dual-write mode.

## Legacy ProducedArtifactHistory table

Do not include the old `ProducedArtifactHistory` Azure table in this migration.

The current data is primarily development-era history and is not important enough to justify a second historical migration lane. New revision metadata is embedded directly in `ProducedArtifact.History` going forward.

Keep the old history table untouched through the normal rollback/safety window. It can later be archived or removed according to development-data cleanup policy.

## Cleanup after the rollback window

Once the cutover has been stable and rollback is no longer required:

- mark the legacy ProducedArtifacts Azure table read-only/retired according to operational policy
- retain or archive it for the agreed safety period
- archive/remove the legacy ProducedArtifactHistory table when no longer useful
- remove obsolete Table Storage-specific ProducedArtifact settings only when no other AI repositories use them
- keep the migration definition and state record as operational evidence

## Completion checklist

The ProducedArtifact migration is complete when:

- ProducedArtifact implements `IApplicationDataRecord`
- ProducedArtifactRepo uses composition with `IApplicationDataStore`
- ProducedArtifactRepo no longer inherits TableStorageBase
- the mutable ProducedArtifact Table DTO is removed
- ProducedArtifact revision metadata is embedded in the same Application Data document
- the standalone ProducedArtifact history repo/DTO are removed
- runtime indexes are declared through `ConfigureApplicationData<ProducedArtifact>`
- migration definition validates
- migration state is stored through Application Data for this migration lane
- initial migration completes
- source count, RecordsWritten, and target count agree
- representative field/header/timestamp checks pass
- final catch-up completes if required
- new runtime CRUD/query/history smoke tests pass
- legacy ProducedArtifactHistory data is intentionally not migrated
- Azure source is retained through the rollback window

That is the repeatable ProducedArtifact path from legacy flat Table Storage to deterministic Application Data Storage.
