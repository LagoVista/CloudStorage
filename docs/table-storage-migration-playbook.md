# Table Storage Migration Playbook

Use this playbook when converting an existing LagoVista repository away from Azure Table Storage to one of the semantic storage mechanisms in `LagoVista/CloudStorage`.

This document is intended to be the starting point for future migration sessions. The goal is to make conversions mostly mechanical while preserving deliberate storage design decisions.

## Starting prompt for a future session

> Read `docs/table-storage-migration-playbook.md` in `LagoVista/CloudStorage` and follow it for this migration. Convert the requested Table Storage repository to the appropriate semantic store, keep the storage definition beside the repository, preserve only supported query semantics, and produce/update the AppSupport migration catalog definition for historical data.

## Core rule

Do not replace `TableStorageBase<TEntity>` with another universal repository base class.

The migration target is composition:

```text
business repository
    -> injected semantic storage contract
        -> provider implementation
```

A repository should become an ordinary class whose constructor receives the storage capability it needs.

## First decision: classify the data

Before editing code, determine which semantic storage personality owns the record.

### Activity Records

Use `IActivityRecordStore<TRecord>` when the data is:

- append-only
- high volume or potentially high volume
- naturally time ordered
- audit/history/event-like
- not normally updated or deleted

Initial provider: Cassandra 5.

Examples include access logs, authentication logs, execution history, audit trails, and other immutable operational history.

### Scratch Data

Use `IScratchStore<TRecord>` when the data is:

- mutable
- temporary or working-state oriented
- relatively small
- naturally key-addressed
- optionally expiring

Initial provider: MongoDB.

### Application Data

Use `IApplicationDataStore<TRecord>` when the data is:

- durable and mutable
- application-owned
- document-like rather than relational/transactional
- queried with explicit indexes, ranges, sorting, and paging

Initial provider: MongoDB.

### Metrics

Use `IMetricsStore` for time-series measurements and aggregations.

Initial provider: PostgreSQL + TimescaleDB.

### Account Ledger

Use `IAccountLedgerStore<TRecord>` for authoritative append-only credit/debit ledgers with atomic balance semantics.

Initial provider: PostgreSQL.

Do not create a sixth generic storage capability just because one repository is awkward. First determine whether the repository actually belongs to one of the five existing personalities.

## Activity Record conversion checklist

The remainder of this playbook focuses on the most consequential Table Storage conversion: Activity Records to Cassandra.

### 1. Inspect the current repository and its callers

Before changing the model, identify:

- current `TableStorageBase<TEntity>` inheritance
- current storage period (`All`, `Month`, `Quarter`, `Year`)
- actual write paths
- actual read/query paths
- whether apparently supported repo methods have real callers
- whether existing global/unbounded queries are truly required
- current Table Storage physical table naming

Do not assume every legacy method deserves to survive. Azure Table Storage often allowed broad query patterns that should not be recreated in Cassandra.

### 2. Choose the Cassandra partition shape

A Cassandra activity query must identify the full logical partition.

For organization-owned operational data, the normal starting point is:

```csharp
.PartitionBy(x => x.OrganizationId)
.BucketBy(StoragePeriod.Month)
```

This produces bounded organization/time partitions.

The partition is not an index. It is the physical distribution key and therefore must match the most important access boundary.

For most NuvIoT activity data, organization is the correct first partition dimension.

### 3. Choose indexes only for fields queried inside the partition

Declared indexes are for additional predicates after the partition has been identified.

Example:

```csharp
.Index(x => x.ResourceId)
.Index(x => x.UserId)
.Index(x => x.Action)
.Index(x => x.Authorized)
```

Do not add an index simply because a property exists.

Index only fields needed by real query paths.

Cassandra queries must not use `ALLOW FILTERING`.

### 4. Choose the bucket period

Supported periods are currently:

```text
All
Month
Quarter
Year
```

For high-volume audit/history workloads, `Month` is the normal starting point.

Bucketed queries must have bounded start/end times so the provider knows which buckets to visit.

Do not add `Day` unless there is a real workload that requires changing the storage contract.

### 5. Decide retention / TTL explicitly

The storage definition supports:

```csharp
.RetainFor(TimeSpan.FromDays(...))
```

If `RetainFor(...)` is omitted, retention is effectively forever.

Do not invent a retention policy during a mechanical migration. If the business/audit/legal policy is unknown, omit TTL and record it as a follow-up decision.

### 6. Normalize the record model

Activity records implement:

```csharp
public interface IActivityRecord
{
    string Id { get; set; }
    string OrganizationId { get; set; }
    string Organization { get; set; }
    DateTime CreationDate { get; set; }
}
```

House timestamp convention:

- `CreationDate`
- `LastUpdatedDate` where applicable to mutable stores
- no `Utc` suffix because UTC is implied by platform convention

Activity records have `CreationDate` only.

Remove Azure-specific storage inheritance and fields when they are no longer business data:

```text
TableStorageEntity inheritance
PartitionKey
RowKey
Azure Timestamp / ETag concepts
legacy string timestamp fields when CreationDate replaces them
```

Do not preserve Azure compatibility aliases if they would become duplicate Cassandra columns.

### 7. Use `anonymous` for unaffiliated organization partitions

If an activity record can legitimately exist before an organization is known, use:

```text
OrganizationId = "anonymous"
Organization   = "Anonymous"
```

Do not use `?`, an empty string, null, or an all-zero GUID as the normal unaffiliated partition key.

This is especially relevant to pre-authentication activity such as authentication failures before user/org resolution.

`anonymous` is a real, intentional logical partition and should be treated as such by migration definitions and runtime writers.

### 8. Put the storage definition beside the repository

Do not grow `Startup.cs` into a catalog of Cassandra schemas.

The repository owns the logical storage shape for its record.

Preferred pattern:

```csharp
public class AccessLogRepo : IAccessLogRepo
{
    private readonly IActivityRecordStore<AccessLog> _store;

    public AccessLogRepo(IActivityRecordStore<AccessLog> store)
    {
        _store = store;
    }

    public static void ConfigureStorage(FlatStorageDefinition<AccessLog> definition)
    {
        definition
            .PartitionBy(x => x.OrganizationId)
            .BucketBy(StoragePeriod.Month)
            .Index(x => x.ResourceId)
            .Index(x => x.UserId)
            .Index(x => x.Action)
            .Index(x => x.Authorized);
    }
}
```

If retention is known:

```csharp
.RetainFor(TimeSpan.FromDays(...))
```

### 9. Keep DI terse

The composition root should register the connection once and the record definitions compactly.

Example:

```csharp
services.AddCassandraStorageConnection();

services.AddActivityRecordStore<AccessLog, CassandraActivityRecordStore<AccessLog>>(
    AccessLogRepo.ConfigureStorage);

services.AddActivityRecordStore<AuthenticationLog, CassandraActivityRecordStore<AuthenticationLog>>(
    AuthenticationLogRepo.ConfigureStorage);

services.AddScoped<IAccessLogRepo, AccessLogRepo>();
services.AddScoped<IAuthenticationLogRepo, AuthenticationLogRepo>();
```

Provider credentials/endpoints do not belong in repositories.

### 10. Convert repo methods to semantic queries

The business repo should translate its domain API into `ActivityRecordQuery` calls.

Important rules:

- require every declared partition field
- for bucketed data, require bounded start and end times
- filter only by partition fields and declared indexed fields
- use opaque continuation tokens from the storage provider
- do not synthesize provider-specific CQL in the business repo
- do not perform client-side full-partition filtering as a substitute for a missing index

If an old global query cannot be expressed without scanning every organization, redesign or retire that query instead of weakening the storage provider.

### 11. Preserve business APIs when practical, not at any cost

A manager/controller API can often remain stable while the repo becomes stricter.

Example pattern:

```text
old manager receives org context but repo ignored it
    -> manager now passes org id
    -> repo becomes correctly org-scoped
```

This is preferable to keeping an unsafe global query merely to preserve historical implementation behavior.

### 12. Verify normalized Cassandra names

Use normal Cassandra identifier normalization.

Do not quote identifiers merely to preserve PascalCase.

Examples:

```text
AccessLog          -> access_log
AuthenticationLog  -> authentication_log
OrganizationId     -> organization_id
CreationDate       -> creation_date
```

The provider and migration tooling must derive/use the same physical naming rules.

## Historical data migration

Runtime conversion and historical-data migration are separate concerns.

The application should switch to the semantic store without containing Azure-to-Cassandra migration logic.

Historical migration lives downstream in:

```text
nuviot/appsupport
src/LagoVista.StorageMigration
```

### Migration is definition-driven

The migration utility should not reference the business model package.

Each migrated record type gets a JSON definition containing everything needed to migrate it, including:

```text
migration key
source Azure Table account/settings reference
table name or table-name pattern
target Cassandra table
partition fields
bucket period
indexes
TTL
field mappings
target field types
ID transformation
timestamp transformation
verification expectations
```

Current first examples:

```text
AccessLog
AuthenticationLog
```

### Historical source table patterns

Legacy `TableStorageBase<TEntity>` created time-sliced physical tables using these conventions:

```text
Month   mYYYYMM{EntityName}
Quarter qYYYYQ{EntityName}
Year    yYYYY{EntityName}
All     {EntityName}
```

A migration definition should encode the real source pattern rather than assuming a single table.

Example:

```text
AccessLog historical source: ^m[0-9]{6}AccessLog$
AuthenticationLog source: AuthenticationLog
```

### Stable migration IDs

Migration must be idempotent.

When the Azure record does not already contain the final activity `Id`, derive a deterministic ID from immutable source identity, currently based on:

```text
source table
PartitionKey
RowKey
```

Use SHA-256 or stronger and normalize to the platform ID form.

Never generate random IDs during historical import because retries/resume would duplicate records.

### Timestamp migration

Map the best historical activity timestamp into `CreationDate`.

The generic migration mapper may define an ordered list of candidate fields and choose the first parseable value.

Do not carry multiple legacy timestamp string fields into Cassandra merely because they existed in Azure.

### Anonymous historical organization values

When historical records have missing, empty, `?`, or otherwise explicitly unaffiliated organization IDs and the domain allows pre-org activity, normalize them to:

```text
anonymous
```

and organization display text to:

```text
Anonymous
```

The runtime writer and migration definition must agree on this convention.

## Migration checkpointing

Migration progress must be durable and resumable.

The intended state store is the semantic Scratch mechanism.

Migration state should include at least:

```text
Id
MigrationKey
DefinitionSha256
State
CurrentTable
HeadPartitionKey
HeadRowKey
RecordsRead
RecordsWritten
RecordsFailed
CreationDate
LastUpdatedDate
CompletedDate
```

### Definition SHA safety

Persist the SHA-256 of the migration JSON with the run.

A run must refuse to resume if the current definition SHA differs from the SHA recorded when the migration started.

This enforces the rule that storage shape/mapping definitions do not silently change mid-migration.

### Checkpoint semantics

Checkpoint only after the corresponding destination batch has been written successfully.

A completed source table should be explicitly distinguishable from a partially processed source table so restart does not needlessly replay the whole table.

Deterministic IDs remain the final idempotency safety net.

## Cutover model

For activity records the cutover can be intentionally simple:

1. finalize the new record/repo shape
2. deploy/create Cassandra schema
3. migrate historical Azure rows into Cassandra
4. verify counts/samples/query behavior
5. deploy the application version that writes/reads Cassandra
6. new records naturally begin flowing only to Cassandra
7. preserve Azure source until migration verification/rollback window is complete

If historical completeness from the exact switchover instant matters, run a final delta/head pass before or immediately around cutover.

Do not build dual-write behavior by default. It adds complexity and creates reconciliation problems unless the migration specifically requires it.

## Validation checklist

For every converted Activity Record repository, validate all of the following.

### Model

- no `TableStorageEntity` inheritance
- implements `IActivityRecord`
- has `Id`
- has `OrganizationId`
- has `Organization`
- has `CreationDate`
- no accidental Azure-only columns remain

### Definition

- partition field(s) intentionally chosen
- bucket period intentionally chosen
- each index maps to a real query
- TTL explicitly chosen or intentionally omitted

### Repository

- no `TableStorageBase<T>` inheritance
- constructor receives `IActivityRecordStore<T>`
- no Azure SDK storage logic
- no CQL/provider logic
- queries include the full partition
- bucketed queries are time bounded
- no global scans recreated

### DI

- Cassandra connection registered once
- record store registration uses repo-local definition
- business repo registration remains ordinary

### Cassandra provider

- table creates/reconciles successfully
- required SAI indexes exist
- insert succeeds
- partition/time query succeeds
- each declared indexed query succeeds
- paging succeeds
- cross-bucket paging succeeds where applicable
- additive schema reconciliation succeeds
- incompatible type/primary-key drift fails loudly
- TTL is correct when configured

### Migration

- catalog definition validates
- definition SHA is stable
- historical source tables resolve correctly
- sample Azure rows map to expected Cassandra fields
- missing/legacy org IDs normalize correctly when applicable
- stable IDs are deterministic
- rerun does not duplicate records
- checkpoint/resume works
- source/destination counts are explainable
- representative record samples match

### Application

- solution builds
- focused repo/manager tests pass
- relevant auth/audit flows still pass
- read APIs return the intended data
- no callers rely on removed Azure-only fields

## Current reference migrations

The first conversions performed with this pattern are:

```text
LagoVista/UserAdmin
    AccessLog / AccessLogRepo
    AuthenticationLog / AuthenticationLogRepo

nuviot/appsupport
    LagoVista.StorageMigration
    access-log.json
    authentication-log.json
```

Use these as reference implementations, but inspect current master before copying code because storage infrastructure may evolve.

## Scope guardrails

While converting a repository:

- do not refactor unrelated rich Mongo/DocumentDB entity infrastructure
- do not add a new universal storage base class
- do not expose provider settings to business repositories
- do not add global scans to make Cassandra imitate Azure
- do not invent retention policy
- do not invent indexes without a query requirement
- do not add migration code to the runtime application
- do not require migration tooling to reference the business model NuGet/package
- do not silently change a migration definition after a run has started

## Completion definition

A Table Storage migration is complete when:

1. the runtime repository uses the correct semantic storage capability through composition
2. the record model no longer carries Azure storage mechanics
3. its logical storage shape is explicit and local to the repository
4. real query paths are supported and unsafe legacy query paths are removed/redesigned
5. historical migration is represented by a validated JSON catalog definition
6. migration progress is resumable and definition-SHA protected
7. provider integration tests and application tests are green
8. the cutover can occur without runtime migration code

That is the repeatable path. The individual repository should supply only the domain-specific choices: storage personality, partition, bucket, indexes, retention, mappings, and any legitimate exceptional query behavior.
