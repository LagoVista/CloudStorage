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

## Provider mapping

```text
Entity Storage       -> MongoDB
Application Data     -> MongoDB
Scratch Storage      -> MongoDB
History Storage      -> Cassandra
Operational Data     -> Cassandra
Relational Storage   -> PostgreSQL
```

These mappings are implementation choices. Application code should depend on the semantic storage contract rather than MongoDB, Cassandra, PostgreSQL, Azure Table Storage, or another provider directly.

## Terminology

**FlatStorage / Flat Storage is retired terminology.**

It originated as a general description for Azure Table Storage replacement work, but no longer describes the architecture accurately. Use the specific storage class above instead.
