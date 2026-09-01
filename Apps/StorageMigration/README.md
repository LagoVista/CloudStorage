# StorageMigration

Storage migration utility owned by LagoVista/CloudStorage.

The operator app intentionally does not reference downstream business model assemblies. Source/target shape and field mappings for structured migrations live in `Definitions/*.json`. Runtime endpoints and credentials come from the normal CloudStorage configuration model loaded from the NuvIoT remote configuration service.

Current migration paths and tooling:

```text
Azure Table Storage -> Cassandra Activity Records
Azure Blob Storage  -> SeaweedFS/S3
```

Both migration paths are resumable and persist checkpoints through CloudStorage Application Data. S3 object PUTs use the original Azure container and blob names so retry/resume is idempotent.

## Configuration bootstrap

Commands that access storage load remote configuration before resolving connections.

Defaults:

```text
Application:          web
Deployment:           dev
Configuration server: https://config.nuviot.com
```

Environment selection is taken from `CFG_ENVIRONMENT_KEY`, then the legacy `MIGRATION_ENVIRONMENT`, then defaults to `dev`. `prod` remains accepted as an alias for `live`.

The configuration token is injected through the standard V1 token convention:

```text
CFG_WEB_DEV_TOKEN
CFG_WEB_LIVE_TOKEN
```

`CFG_APP_KEY` and `CFG_SRVR_URL` may override the default application key and configuration server URL.

After loading the remote configuration, the utility registers the normal `LagoVista.CloudStorage.Startup` services and resolves the same typed connection settings used by the application:

```text
IDefaultConnectionSettings
IS3ObjectStorageConnectionSettings
ICassandraStorageSettings
IApplicationDataStorageSettings
```

The migration engine no longer selects DEV/PROD `TestConnections` for these runtime data sources.

`catalog` and `validate <migration-key>` operate only on local migration definitions and therefore do not require remote configuration.

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

## Azure Blob -> SeaweedFS/S3

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

For a source-only inventory that does not connect to SeaweedFS:

```powershell
dotnet run -- object-inventory
dotnet run -- object-inventory --max-objects 100
```

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

Optional performance controls remain unchanged:

```powershell
dotnet run -- object-migrate --batch-size 10 --parallelism 8
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

## Azure source

Azure Table and Blob migration use `IDefaultConnectionSettings.DefaultTableStorageSettings` from resolved configuration. The source connection string is constructed from:

```text
DefaultTableStorage:Name
DefaultTableStorage:AccessKey
```

The same Azure Storage account credentials are used by the object migration path for Blob service access. If historical blobs are found in additional Azure Storage accounts, add those as explicit migration source connections rather than embedding credentials in migration definitions.

## SeaweedFS / S3 target

The object migration path resolves `IS3ObjectStorageConnectionSettings` from the normal CloudStorage DI registration. Its configuration section is:

```text
S3ObjectStorage:Host
S3ObjectStorage:Port
S3ObjectStorage:AccessKey
S3ObjectStorage:SecretKey
S3ObjectStorage:UseTls
S3ObjectStorage:Region
S3ObjectStorage:PublicHost
S3ObjectStorage:PublicPort
S3ObjectStorage:PublicUseTls
```

`Port`, `UseTls`, and `Region` retain the defaults and parsing behavior defined by `S3ObjectStorageConnectionSettings`; the migration utility does not duplicate those defaults.

## Cassandra target

The structured migration path resolves `ICassandraStorageSettings` from the normal CloudStorage DI registration. Its configuration section is:

```text
CassandraStorage:ContactPoints
CassandraStorage:Port
CassandraStorage:UserName
CassandraStorage:Password
CassandraStorage:Keyspace
CassandraStorage:LocalDataCenter
```

Replication factor is migration/bootstrap-specific rather than part of `ICassandraStorageSettings`, so it remains an operator override:

```text
MIGRATION_CASSANDRA_REPLICATION_FACTOR
```

If omitted it defaults to `1`.

## Migration checkpoint state

Checkpoint state resolves the normal `IApplicationDataStorageSettings` from remote configuration.

Because this operator app normally runs outside Kubernetes while only one Mongo endpoint is mapped externally, its Application Data client deliberately wraps those resolved settings with `directConnection=true` and disables replica-set discovery. Runtime services inside the cluster are not affected.

The connection endpoint, credentials, authentication database, database name, and TLS setting all come directly from the resolved `ApplicationDataStorage` settings; the migration utility no longer rebuilds those values from environment variables or `TestConnections`.

## Definitions

The first migrated structured definitions are:

```text
useradmin-access-log
useradmin-authentication-log
```

A structured definition contains the source table/table pattern, target table, partition fields, time bucket, indexes, TTL, field mappings, target field types, and mapping transforms. The engine and writer do not require the downstream CLR record type.
