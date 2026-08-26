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

Definitions select logical Azure Table connections through `source.connection`.

Access log:

```text
MIGRATION_AZURE_ACCESS_LOG_ACCOUNT_ID
MIGRATION_AZURE_ACCESS_LOG_ACCESS_KEY
```

User storage:

```text
MIGRATION_AZURE_USER_STORAGE_ACCOUNT_ID
MIGRATION_AZURE_USER_STORAGE_ACCESS_KEY
```

Either can fall back to:

```text
MIGRATION_AZURE_TABLE_ACCOUNT_ID
MIGRATION_AZURE_TABLE_ACCESS_KEY
```

## Cassandra target

Local defaults match the CloudStorage Cassandra integration harness:

```text
MIGRATION_CASSANDRA_HOSTS               localhost
MIGRATION_CASSANDRA_PORT                19042
MIGRATION_CASSANDRA_USERNAME            cassandra
MIGRATION_CASSANDRA_PASSWORD            cassandra
MIGRATION_CASSANDRA_KEYSPACE            nuviot_cloudstorage_tests
MIGRATION_CASSANDRA_DATACENTER          datacenter1
MIGRATION_CASSANDRA_REPLICATION_FACTOR  1
```

Override these for the dev cluster. The three-node dev cluster should normally use replication factor `3`.

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
