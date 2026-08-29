# StorageMigration

Storage migration utility owned by LagoVista/CloudStorage.

The operator app intentionally does not reference downstream business model assemblies. Source/target shape and field mappings for structured migrations live in `Definitions/*.json`; environment-specific endpoints and credentials stay in environment variables.

Current migration paths and tooling:

```text
Azure Table Storage -> Cassandra Activity Records
Azure Blob Storage  -> SeaweedFS/S3
```

Both migration paths are resumable and persist checkpoints through CloudStorage Application Data. S3 object PUTs use the original Azure container and blob names so retry/resume is idempotent.

## Commands

From `Apps/StorageMigration`:

```powershell
dotnet run -- catalog
dotnet run -- validate useradmin-access-log
dotnet run -- status useradmin-access-log
dotnet run -- probe useradmin-access-log
dotnet run -- migrate useradmin-access-log
dotnet run -- verify useradmin-access-log
```

For a deliberate post-completion replay/catch-up pass:

```powershell
dotnet run -- migrate useradmin-access-log --catch-up
```

`--catch-up` is accepted only after the current pass has completed.

## Azure Blob -> S3

For a non-destructive Azure Blob -> SeaweedFS/S3 connectivity and inventory probe:

```powershell
dotnet run -- object-probe
dotnet run -- object-probe --max-objects 100
```

The object probe:

- connects to the configured SeaweedFS S3 endpoint and lists visible buckets;
- enumerates Azure Blob containers and object metadata;
- reports object count and total bytes per container;
- reports oldest/newest last-modified timestamps per container;
- never uploads, modifies, or deletes an object.

To copy a deliberately bounded batch:

```powershell
dotnet run -- object-migrate --max-objects 10
dotnet run -- object-status
```

To continue from the persisted checkpoint:

```powershell
dotnet run -- object-migrate --max-objects 100
```

To let the migration run through all remaining containers and blobs:

```powershell
dotnet run -- object-migrate
```

Object migration behavior:

- every Azure container is created as the corresponding S3 bucket when needed;
- every Azure blob is copied using the same object name/path;
- content type and cache-control are preserved when present;
- successful S3 PUTs are overwrite-safe, so retry/resume is idempotent;
- the Application Data checkpoint records current container, last completed object, object counts, failures, and bytes read/written;
- `--max-objects` limits the number copied in the current invocation, not the cumulative migration total;
- if one object fails, the run stops without advancing the checkpoint beyond that object;
- no Azure source object is deleted by this utility.

## Azure source connections

Definitions keep a logical `source.connection` label to describe the workload, but Azure Storage credentials come from the standard CloudStorage `TestConnections` settings.

Set `MIGRATION_ENVIRONMENT=dev` to use:

```text
TestConnections.DevTableStorageDB
DEV_TS_STORAGE_ACCOUNT_ID
DEV_TS_STORAGE_ACCOUNT_ACCESS_KEY
```

Set `MIGRATION_ENVIRONMENT=prod` to use:

```text
TestConnections.ProductionTableStorageDB
PROD_TS_STORAGE_ACCOUNT_ID
PROD_TS_STORAGE_ACCOUNT_ACCESS_KEY
```

The same Azure Storage account credentials are currently used by the object migration path for Blob service access. If historical blobs are found in additional Azure Storage accounts, add those as explicit migration source connections rather than embedding credentials in migration definitions.

The migration utility does not maintain a separate `MIGRATION_AZURE_*` credential namespace.

## SeaweedFS / S3 target

The object migration path uses the standard `S3ObjectStorageConnectionSettings` shape through `LagoVista.CloudStorage.Utils.TestConnections`.

Set `MIGRATION_ENVIRONMENT=dev` to load:

```text
DEV_S3ObjectStorage:Host
DEV_S3ObjectStorage:Port
DEV_S3ObjectStorage:AccessKey
DEV_S3ObjectStorage:SecretKey
DEV_S3ObjectStorage:UseTls
DEV_S3ObjectStorage:Region
DEV_S3ObjectStorage:PublicHost
DEV_S3ObjectStorage:PublicPort
DEV_S3ObjectStorage:PublicUseTls
```

For production use the same names with the `PROD_` prefix.

`Port`, `UseTls`, and `Region` retain the defaults and parsing behavior defined by `S3ObjectStorageConnectionSettings`; the migration utility does not duplicate those defaults.

## Cassandra target

The migration utility uses the standard `CassandraStorageSettings` shape through `LagoVista.CloudStorage.Utils.TestConnections`.

Set `MIGRATION_ENVIRONMENT=dev` to load:

```text
DEV_CassandraStorage:ContactPoints
DEV_CassandraStorage:Port
DEV_CassandraStorage:UserName
DEV_CassandraStorage:Password
DEV_CassandraStorage:Keyspace
DEV_CassandraStorage:LocalDataCenter
```

Set `MIGRATION_ENVIRONMENT=prod` to use the same names with the `PROD_` prefix.

`ContactPoints` accepts comma- or semicolon-separated hosts, matching the standard CloudStorage Cassandra settings parser.

Replication factor is migration/bootstrap-specific rather than part of `CassandraStorageSettings`, so it remains:

```text
MIGRATION_CASSANDRA_REPLICATION_FACTOR
```

If omitted it defaults to `1`. The three-node dev cluster should normally use replication factor `3` when the migration utility is responsible for creating the keyspace.

## Migration checkpoint state

Checkpoint state uses the shared Mongo connection already configured for CloudStorage and the Application Data database name.

Set:

```text
MIGRATION_ENVIRONMENT=dev
DEV_ApplicationDataStorage:DatabaseName=ApplicationData
```

For production use `MIGRATION_ENVIRONMENT=prod` and the corresponding `PROD_ApplicationDataStorage:DatabaseName` setting.

Because this operator app normally runs outside Kubernetes while only one Mongo endpoint is mapped externally, its Application Data client deliberately uses `directConnection=true` and disables replica-set discovery. Runtime services inside the cluster are not affected.

## Definitions

The first migrated structured definitions are:

```text
useradmin-access-log
useradmin-authentication-log
```

A structured definition contains the source table/table pattern, target table, partition fields, time bucket, indexes, TTL, field mappings, target field types, and mapping transforms. The engine and writer do not require the downstream CLR record type.
