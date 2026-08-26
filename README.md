# LagoVista CloudStorage

CloudStorage provides the storage capabilities used by LagoVista/NuvIoT applications.

Storage is classified by **application behavior**, not by the physical database that happens to implement it. Multiple storage classes may use the same backend while keeping separate contracts and semantics.

## Storage classes

| Storage class | Core behavior | Base record / contract | Primary backend |
| --- | --- | --- | --- |
| **Entity Storage** | First-class domain entities with identity, lifecycle, ownership, and richer object graphs | `EntityBase` | MongoDB |
| **Application Data** | Durable supporting application objects that do not rise to the level of a full entity; full CRUD and indexed queries | `IApplicationDataRecord` | MongoDB |
| **Scratch Storage** | Temporary or intermediate working state; mutable get/upsert/delete with optional expiration | `IScratchDataRecord` | MongoDB |
| **History Storage** | Immutable activity records; append/write and query only; optimized for high-volume time-oriented data | `IActivityRecord` | Cassandra |
| **Operational Data** | Small, standalone, row-like records; non-relational; full CRUD; schema described by persisted class fields | `IOperationalDataRecord` | Cassandra |
| **Relational Storage** | Data requiring relational semantics such as constraints, transactions, joins, or strongly related tables | Purpose-specific relational models | PostgreSQL |

## Canonical meanings

### Entity Storage

Use **Entity Storage** for first-class objects in the LagoVista domain model. These normally derive from `EntityBase` and have their own identity and lifecycle.

The current provider-neutral entry point is `IDocumentStorageClient` / `IDocumentStorageClientProvider`.

### Application Data

Use **Application Data** for durable data that belongs to the application or an object graph but does not need the full `EntityBase` entity model.

Application Data supports normal mutable CRUD and declared/indexed queries.

### Scratch Storage

Use **Scratch Storage** for working state, intermediate processing data, durable-cache scenarios, and records where expiration may be part of the lifecycle.

Scratch is intentionally separate from Application Data even when both use MongoDB.

### History Storage

Use **History Storage** for **Activity Records**.

Activity Records represent things that happened. They are written/appended and queried, but are not normally updated or deleted as part of application behavior.

### Operational Data

Use **Operational Data** for small, standalone records that behave much like a traditional database row but do not have meaningful relational structure.

Operational Data:

- supports full create/read/update/delete behavior
- consists of a relatively small number of scalar/row-like fields
- does not require joins or relational navigation
- derives its persisted column shape from the fields on the record class
- uses Cassandra as its primary backend
- is conventionally partitioned by `OrganizationId` and keyed by `Id`

`IOperationalDataRecord` intentionally establishes only record identity, organization scope, and lifecycle metadata:

- `Id`
- `OrganizationId`
- `CreationDate`
- `LastUpdatedDate`

Additional operational fields belong on the concrete record type rather than the base contract. `CreationDate` is assigned on first upsert when not already supplied, while `LastUpdatedDate` is refreshed on each upsert.

#### History vs. Operational Data

| Behavior | History Storage | Operational Data |
| --- | --- | --- |
| Record contract | `IActivityRecord` | `IOperationalDataRecord` |
| Store contract | `IActivityRecordStore<TRecord>` | `IOperationalDataStore<TRecord>` |
| Primary backend | Cassandra | Cassandra |
| Physical model | One table per record type | One table per record type |
| Schema source | Persisted record fields | Persisted record fields |
| Storage definition | `StorageDefinition<TRecord>` | `StorageDefinition<TRecord>` |
| Create | Insert / append | Create / upsert |
| Read | Query | Get + query |
| Update | No | Yes |
| Delete | No | Yes |
| Time buckets | Common / supported | Not supported |
| Retention / TTL | Optional | Optional |
| Time-oriented semantics | Yes | No; timestamps describe record lifecycle |
| Typical Cassandra key shape | `((partition fields), CreationDate, Id)` | `((OrganizationId), Id)` |

The two storage classes intentionally keep their behavioral implementations separate. They share only mechanical Cassandra infrastructure where doing so remains obvious and low-risk. History-specific time and bucketing behavior stays in the History store; Operational Data owns deterministic key-based get, upsert, delete, and partition query semantics.

### Relational Storage

Use **Relational Storage** when the relationships and transactional behavior are part of the data model rather than merely an implementation detail.

PostgreSQL is the primary relational platform.

## Critical relational storage

A small but important portion of the platform remains in **SQL Server relational storage**.

This is the system of record for data where loss is unacceptable, including financial and other critical transactional data. It currently represents roughly 30 tables and should be treated as a distinct legacy/critical relational estate during modernization.

The eventual PostgreSQL direction does not change the durability and integrity requirements of these workloads.

## Legacy storage patterns

### Legacy Table Storage

- flat, row-like records
- commonly used for archive/history-style workloads
- base record: `TableStorageEntity`
- provider: Microsoft Azure Table Storage

### Legacy Cloud File Storage

- read/write blob or file payloads
- provider: Microsoft Azure Blob Storage

### Legacy Blob + Table Storage

`BlobTableStorageRepoBase` combines a summary/index record in Azure Table Storage with a larger detail payload in Azure Blob Storage.

This pattern is conceptually closest to modern **Application Data** in many cases and should normally be evaluated for migration there rather than reproduced as a new storage abstraction.

## Utility repositories

**Utility Repositories** are general-purpose data-access helpers that operate across the Entity Storage universe when behavior does not cleanly belong in a business-specific repository.

They should remain narrowly scoped and should increasingly use provider-neutral entity storage contracts rather than direct Cosmos/Mongo SDK access.

## Repository base classes

CloudStorage contains generic repository base classes used to build concrete repositories. They centralize common persistence behavior, but several historically also encapsulate provider-specific mechanics.

As storage is modernized, provider coupling in these base classes should be reduced in favor of constructor-injected semantic storage contracts.

## Current repository inventory

This inventory reflects the `refactor/cloudstorage-project-layout` branch and groups the major production components by architectural role.

### Entity Storage

| Component | Role | Status |
| --- | --- | --- |
| `IDocumentStorageClient` | Provider-neutral entity/document storage API | Canonical |
| `DocumentStorageClientProvider` | Selects the configured document provider | Canonical |
| `MongoDocumentStorageClient` | Mongo implementation of Entity Storage | Canonical target |
| `MongoStorageClientProvider` | Mongo client/provider lifecycle | Canonical target |
| `DocumentCollectionNameResolver` | Maps entity/domain types to physical collections | Supporting |
| `EntityDocumentStoragePolicy` | Entity document storage policy/routing | Supporting |
| `CosmosDocumentStorageClient` | Cosmos implementation of the provider seam | Transitional / migration source |
| `CosmosClientProvider` | Cosmos client lifecycle | Transitional |
| `CosmosDocumentCollectionProvisioner` | Cosmos collection provisioning | Transitional |

### Application Data

| Component | Role | Status |
| --- | --- | --- |
| `IApplicationDataStore<TRecord>` | Application Data contract | Canonical |
| `IApplicationDataRecord` | Base record contract | Canonical |
| `MongoApplicationDataStore<TRecord>` | Mongo Application Data implementation | Canonical |
| `MongoMutableRecordStore<TRecord>` | Shared Mongo CRUD/query machinery | Supporting implementation |
| `ApplicationDataStorageSettings` | Application Data connection/config boundary | Canonical |

### Scratch Storage

| Component | Role | Status |
| --- | --- | --- |
| `IScratchStore<TRecord>` | Scratch storage contract | Canonical |
| `IScratchDataRecord` | Base scratch record contract | Canonical |
| `MongoScratchStore<TRecord>` | Mongo Scratch implementation | Canonical |
| `ScratchStorageSettings` | Scratch connection/config boundary | Canonical |

### History Storage

| Component | Role | Status |
| --- | --- | --- |
| `IActivityRecordStore<TRecord>` | History Storage contract | Canonical |
| `IActivityRecord` | Activity Record base contract | Canonical |
| `CassandraActivityRecordStore<TRecord>` | Cassandra History Storage implementation | Canonical |
| `CassandraRecordMap` | Maps record classes to Cassandra shape | Supporting |
| `CassandraSessionFactory` | Cassandra connection/session lifecycle | Supporting |
| `CassandraStorageSettings` | Cassandra storage configuration | Canonical |

`StorageDefinition<TRecord>` is the provider-neutral description of a record's logical storage shape, including keys, partitions, indexes, time buckets, and retention. It is shared by semantic storage capabilities and is not itself a storage class.

### Operational Data

| Component | Role | Status |
| --- | --- | --- |
| `IOperationalDataStore<TRecord>` | Operational Data contract | Canonical |
| `IOperationalDataRecord` | Minimal operational record contract | Canonical |
| `CassandraOperationalDataStore<TRecord>` | Cassandra Operational Data implementation | Canonical |
| `CassandraOperationalRecordMap<TRecord>` | Operational record-to-Cassandra mapping | Supporting |
| `OperationalDataStoreOptions<TRecord>` | Per-record index/retention configuration | Supporting |

Operational records use the conventional Cassandra primary key `((OrganizationId), Id)`. `StorageDefinition<TRecord>` may declare additional indexes and retention, but Operational Data deliberately does not support redefining its primary-key convention or introducing time buckets.

### Relational Storage

The `LagoVista.Relational` project contains the current relational persistence estate.

Major areas visible in the repository include:

- financial/billing records such as `AccountDTO`, `AccountTransactionDTO`, `BillingEventDTO`, `InvoiceDTO`, `PaymentDTO`, payroll and expense records
- subscription/product records such as `SubscriptionDTO`, `SubscriptionLevelDTO`, `ProductDTO`, `LicenseDTO`
- semantic/catalog records such as `ArtifactDTO`, `ConceptDTO`, `DefinitionDTO`, `SubjectDTO`, `EmbeddingDTO`
- EF contexts including `BillingDataContext`, `SemanticDataContext`, and `MetricsDataContext`
- shared relational infrastructure through `RelationalBase`

The repo also exposes specialized relational capabilities:

- `IMetricsStore` / metrics models
- `IAccountLedgerStore<TRecord>` / account-ledger models

These are specialized semantic contracts built on relational storage rather than new top-level storage classes.

### Legacy Azure Table Storage

| Component | Role |
| --- | --- |
| `TableStorageBase<T>` | Generic Azure Table repository base |
| `BlobTableStorageRepoBase<T>` | Azure Table summary + Blob detail pattern |
| `FKeyWriter` | Azure Table foreign-key/index support |
| `NodeLocatorReader` / `NodeLocatorWriter` | Azure Table locator/index support |
| `TableStoragePruner` | Table cleanup/retention utility |
| `TableStorageAudit` | Table inspection/audit utility |
| `TableStorageQuery` | Legacy Table query helper |
| `SummaryTablePropertyReader` / `SummaryTablePropertyWriter` | Legacy summary record helpers |
| `AzureTableStorageConnectionSettings` | Azure Table configuration |

This entire group is legacy/migration-oriented and should shrink as workloads move into History, Operational, Application Data, or other canonical storage classes.

### Legacy Cloud File Storage

| Component | Role |
| --- | --- |
| `CloudFileStorage` | Azure Blob/file read-write abstraction |

### Utility Repositories

| Component | Current role | Direction |
| --- | --- | --- |
| `EntityUtilsRepository` | Broad general-purpose operations across entity documents | Provider-neutralize and reduce direct provider knowledge |
| `SyncRepository` | Cross-entity synchronization/query utilities | Provider-neutralize |
| `EntityListItemRepo` | Entity list-item projection/access | Provider-neutral seam already underway |
| `EntityPreparationCandidateRepository` | Cross-entity preparation candidate queries | Provider-neutral seam already underway |
| `EntityListItemRepoFactory` | Factory for list-item utility repository | Supporting |
| `EntityDetailResponseFactory` | Builds entity detail responses using storage-backed data | Supporting |

### Repository base classes

| Component | Role | Direction |
| --- | --- | --- |
| `DocumentDBRepoBase<T>` | Generic base for Entity Storage repositories | Transitional; move provider mechanics behind `IDocumentStorageClient` |
| `TableStorageBase<T>` | Generic Azure Table repository base | Legacy; migrate consumers away |
| `BlobTableStorageRepoBase<T>` | Generic Table + Blob repository base | Legacy; usually evaluate for Application Data |
| `RelationalBase<...>` | Generic relational repository base | Active relational infrastructure |

### Cache and supporting infrastructure

These components support storage behavior but are not storage classes themselves:

- `CacheProvider`
- `InmemoryCache`
- `EntityListItemCache`
- `ICacheAborter`
- `IEntityListCacheInvalidator`
- `IEntityListItemCache`
- `ForeignKeyService`
- `NodeLocator*`
- `StorageNameUtility`
- `JsonUtils`

### Migration, diagnostics, and verification

These are tooling/support surfaces rather than runtime storage classes:

- `DocumentMigrationService`
- `DocumentMigrationTransformer`
- `Apps/DataMigration`
- Cosmos/Mongo/Cassandra/Postgres platform smoke tests
- `SchemaVerify`
- storage-provider integration tests and the local storage lab

## Modernization hotspots

The inventory highlights several areas that do not fit the final architecture cleanly and therefore deserve deliberate cleanup:

1. `EntityUtilsRepository` remains a very large utility side door into Entity Storage.
2. `SyncRepository` still carries historical query/provider assumptions.
3. `DocumentDBRepoBase<T>` is a transitional inheritance seam and should delegate storage mechanics to the provider-neutral client.
4. The Azure Table stack remains substantial and must be classified workload-by-workload into History, Operational Data, Application Data, or an intentional legacy remainder.
5. Cosmos provider code is now a migration/compatibility implementation, not the architectural center of Entity Storage.

## Provider mapping

```text
Entity Storage       -> MongoDB
Application Data     -> MongoDB
Scratch Storage      -> MongoDB
History Storage      -> Cassandra
Operational Data     -> Cassandra
Relational Storage   -> PostgreSQL

Legacy Table         -> Azure Table Storage
Legacy Cloud File    -> Azure Blob Storage
Critical Relational  -> SQL Server
```

These mappings are implementation choices. Application code should depend on the semantic storage contract rather than MongoDB, Cassandra, PostgreSQL, SQL Server, Azure Table Storage, Azure Blob Storage, or another provider directly.
