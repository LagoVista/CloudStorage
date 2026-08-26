# StorageMigration

JSON-driven historical storage migration utility owned by LagoVista/CloudStorage.

The operator app intentionally does not reference downstream business model assemblies. Source/target shape and field mappings live in `Definitions/*.json`; environment-specific endpoints and credentials stay in environment variables.

Current migration path:

```text
Azure Table Storage -> Cassandra Activity Records
```

The engine is resumable and persists checkpoints through CloudStorage Application Data. A migration run stores the SHA-256 of its JSON definition and refuses to resume if the definition changes mid-run. Destination IDs are deterministic so replay/catch-up remains idempotent.

## Commands

From `Apps/StorageMigration`:

```powershell
dotnet run -- catalog
dotnet run -- validate useradmin-access-log
dotnet run -- status useradmin-access-log
dotnet run -- migrate useradmin-access-log
dotnet run -- verify useradmin-access-log
```

For a deliberate post-completion replay/catch-up pass:

```powershell
dotnet run -- migrate useradmin-access-log --catch-up
```

`--catch-up` is accepted only after the current pass has completed.

## Source connections

Definitions keep a logical `source.connection` label to describe the workload, but Azure Table credentials come from the standard CloudStorage `TestConnections` settings.

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

The migration utility does not maintain a separate `MIGRATION_AZURE_*` credential namespace.

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

The first migrated definitions are:

```text
useradmin-access-log
useradmin-authentication-log
```

A definition contains the source table/table pattern, target table, partition fields, time bucket, indexes, TTL, field mappings, target field types, and mapping transforms. The engine and writer do not require the downstream CLR record type.
