# Card 12c - Injected storage contracts

## Decision

Storage is a composed capability, not a repository base class.

Business repositories should be ordinary classes and request exactly one semantic storage contract through constructor injection:

```text
IAppendHistoryStore<TEntity>  append-only, time-oriented history
IScratchStore<TEntity>        small mutable durable-cache/scratch data
IFlatDocumentStore<TEntity>   mutable flat application data with indexed queries
```

Initial provider mappings are Cassandra for append history and MongoDB for Scratch and Flat Document. Those mappings belong in dependency-injection/provider registration, not in business repositories.

`TableStorageBase<TEntity>` remains legacy infrastructure for repositories that have not yet migrated. New repositories should not inherit from it or from a replacement common storage base class.

## Repository examples

Append history:

```csharp
public sealed class DeviceLogRepo
{
    private readonly IAppendHistoryStore<DeviceLog> _store;

    public DeviceLogRepo(IAppendHistoryStore<DeviceLog> store)
    {
        _store = store;
    }
}
```

Scratch storage:

```csharp
public sealed class WorkingStateRepo
{
    private readonly IScratchStore<WorkingState> _store;

    public WorkingStateRepo(IScratchStore<WorkingState> store)
    {
        _store = store;
    }
}
```

Flat document storage:

```csharp
public sealed class DeviceStateRepo
{
    private readonly IFlatDocumentStore<DeviceState> _store;

    public DeviceStateRepo(IFlatDocumentStore<DeviceState> store)
    {
        _store = store;
    }
}
```

## Registration

Provider cards supply concrete implementations and register them through the capability-specific extensions:

```csharp
services.AddAppendHistoryStore<DeviceLog, CassandraAppendHistoryStore<DeviceLog>>(storage => storage
    .PartitionBy(x => x.OrganizationId)
    .PartitionBy(x => x.DeviceId)
    .TimeBy(x => x.Timestamp)
    .BucketBy(StoragePeriod.Month)
    .Index(x => x.MessageType)
    .RetainFor(TimeSpan.FromDays(90)));

services.AddScratchStore<WorkingState, MongoScratchStore<WorkingState>>(storage => storage
    .KeyBy(x => x.Id)
    .Index(x => x.OrganizationId));

services.AddFlatDocumentStore<DeviceState, MongoFlatDocumentStore<DeviceState>>(storage => storage
    .KeyBy(x => x.Id)
    .Index(x => x.OrganizationId)
    .Index(x => x.DeviceId));
```

Append registrations require `TimeBy(...)`. Mutable registrations require `KeyBy(...)`. These fail fast during registration so a provider is never handed an incomplete semantic definition.

## Query boundary

Queries use typed property selectors, a small provider-neutral operator set, and opaque continuation tokens. Provider-specific query languages and continuation-key structures do not cross the capability boundary.

Append-history queries expose canonical time windows plus declared filters. Mutable stores expose filters, ordering, and paging.

## Intentional differences

`IAppendHistoryStore<TEntity>` does not expose update or delete operations.

`IScratchStore<TEntity>` exposes upsert semantics because scratch data is replaceable working state.

`IFlatDocumentStore<TEntity>` keeps insert and update separate because it represents durable mutable application data rather than a cache-like scratch surface.

Keeping Scratch and Flat Document separate preserves intent even though both initially use MongoDB and prevents backend capability from defining application semantics.

## Migration boundary

Azure Table migration is not part of these runtime contracts. Legacy table discovery, monthly/org table naming, inverse-tick row keys, Azure continuation keys, checkpoint/resume, recent/all import, and validation belong to the external migration service in Card 12g.
