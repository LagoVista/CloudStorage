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
| **Operational Data** | Small, standalone, row-like records; non-relational; full CRUD; schema described by persisted class fields | `IOperationalDataRecord` *(planned)* | Cassandra |
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
- is expected to use Cassandra as its primary backend

`IOperationalDataRecord` is the planned base contract for this class and will be finalized with the implementation.

### Relational Storage

Use **Relational Storage** when the relationships and transactional behavior are part of the data model rather than merely an implementation detail.

PostgreSQL is the primary relational platform.

## Critical relational storage

A small but important portion of the platform remains in **SQL Server relational storage**.

This is the system of record for data where loss is unacceptable, including financial and other critical transactional data. It currently represents roughly 30 tables and should be treated as a distinct legacy/critical relational estate during modernization.

The eventual PostgreSQL direction does not change the durability and integrity requirements of these workloads.

## Legacy storage patterns

The following patterns still exist and should be treated as migration sources rather than new architectural choices.

### Legacy Table Storage

- flat, row-like records
- commonly used for archive/history-style workloads
- base record: `TableStorageEntity`
- provider: Microsoft Azure Table Storage

### Legacy Cloud File Storage

- read/write blob or file payloads
- provider: Microsoft Azure Blob Storage

### Legacy Blob + Table Storage

`BlobTableStorageRepoBase` combines:

- a summary/index record in Azure Table Storage
- a larger detail payload in Azure Blob Storage

This pattern is conceptually closest to modern **Application Data** in many cases and should normally be evaluated for migration there rather than reproduced as a new storage abstraction.

## Utility repositories

**Utility Repositories** are provider-specific data-access helpers that perform general-purpose operations against entities.

They are primarily a side door into the Entity Storage universe for operations that do not cleanly belong in a business-specific repository.

Utility repositories should remain narrowly scoped. New business behavior should prefer business-specific repositories or provider-neutral storage contracts rather than growing utility repositories into broad alternate persistence APIs.

## Repository base classes

CloudStorage contains repository **base classes** used to build concrete repositories.

These typically:

- are generic over the persisted entity or record type
- centralize common repository behavior
- historically encapsulate provider-specific storage mechanics
- are inherited by business-specific repositories

As storage is modernized, inheritance-based provider coupling should be reduced where practical in favor of constructor-injected semantic storage contracts. Existing base classes remain part of the legacy and transitional architecture and should not automatically become the model for new storage capabilities.

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

## Terminology

**FlatStorage / Flat Storage is retired terminology.**

It originated as a general description for Azure Table Storage replacement work, but no longer describes the architecture accurately. Use the specific storage class above instead.
