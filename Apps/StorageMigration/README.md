# StorageMigration

Storage migration utility owned by LagoVista/CloudStorage.

The operator app intentionally does not reference downstream business model assemblies. Source/target shape and field mappings for structured migrations live in `Definitions/*.json`; environment-specific endpoints and credentials stay in environment variables.

Current migration paths and tooling:

```text
Azure Table Storage -> Cassandra Activity and Operational Records
Azure Table Storage -> MongoDB ApplicationData (insert-if-absent)
Azure Blob Storage  -> SeaweedFS/S3
```

Both migration paths are resumable and persist checkpoints through CloudStorage Application Data. S3 object PUTs use the original Azure container and blob names so retry/resume is idempotent.

## Commands

From `Apps/StorageMigration`:

```powershell
dotnet run -- catalog
dotnet run -- validate useradmin-access-log
dotnet run -- status useradmin-access-log
dotnet run -- status --all
dotnet run -- probe useradmin-access-log
dotnet run -- migrate useradmin-access-log
dotnet run -- verify useradmin-access-log
```

Run `dotnet run -- status --all` for a read-only summary of **every**
cataloged Table Storage migration checkpoint plus the Azure Blob-to-S3 checkpoint.
It reads persisted ApplicationData state for the environment selected by
`MIGRATION_ENVIRONMENT`; it does not contact Azure, Cassandra, or S3 and
does not copy data. The table displays checkpoint state, pass, read/processed
counts, failures, target, and definition SHA status (`OK`, `CHANGED`, or `-`
for not started). `CHANGED` means the current definition no longer matches
the checkpoint; review it before attempting to resume. `Completed` only
indicates the current pass was checkpointed as complete, not that destination
row counts or data quality were independently verified.

For UserAdmin's four production migrations, use [the UserAdmin migration runbook](USERADMIN-MIGRATION-RUNBOOK-2026-11-15.md). Mongo backfills require the target runtime's environment-scoped `DEV_SystemOwnerOrg:Id/Key/Text` or `PROD_SystemOwnerOrg:Id/Key/Text` settings; the selected environment is resolved automatically and shared owner settings are rejected. With `MIGRATION_ENVIRONMENT=prod`, `probe` and `migrate` require `--confirm-production`; neither changes source Azure rows.

For a deliberate post-completion replay/catch-up pass:

```powershell
dotnet run -- migrate useradmin-access-log --catch-up
```

`--catch-up` is accepted only after the current pass has completed.

Azure Table migrations print one progress line after each successfully committed
100-record batch (and any shorter final batch). Each line shows the source table,
batch size, records written in this invocation, cumulative checkpointed records,
failures, and average records/second for the current invocation. The progress
count includes existing destination records accepted by insert-if-absent semantics;
it does not mean that every record was newly inserted. A failed batch is not
reported as committed. The last line of the command still prints the persisted
migration state.

### Historical ContactDTO and sent-email lookup backfill

The Cassandra runtime is already active in DEV. Azure retains the older records; there is no dual-write or cutover step in the backfill. Import contacts first, then the phone index, then sent-email cross-references:

```powershell
$env:MIGRATION_ENVIRONMENT = "dev"
dotnet run -- catalog
dotnet run -- validate marketing-contact-dto
dotnet run -- probe marketing-contact-dto
dotnet run -- migrate marketing-contact-dto --max-records 100
dotnet run -- migrate marketing-contact-dto
dotnet run -- verify marketing-contact-dto

dotnet run -- probe marketing-contact-phone-index
dotnet run -- migrate marketing-contact-phone-index
dotnet run -- verify marketing-contact-phone-index

dotnet run -- probe useradmin-sent-email-xref
dotnet run -- migrate useradmin-sent-email-xref
dotnet run -- verify useradmin-sent-email-xref
```

`probe` opens the Azure source and ensures/validates the Cassandra target schema (it may create missing tables or indexes, but does not copy rows). `--max-records` commits a bounded number of eligible records then keeps the checkpoint Running; invoke `migrate` again without a limit to finish. `verify` checks completed migration accounting against Azure source counts and no failed records; it does not independently count Cassandra rows. Check representative target records separately.

Operational inserts use Cassandra `IF NOT EXISTS` to preserve any post-cutover rows already written by the live service. For these imports, "Records written" means source rows successfully processed/attempted, including rows that already exist at the target. Contact phone-index import only includes contacts with a nonempty `Phone` field. The historical `SentEmailHistoryDTO` event stream is not part of these three definitions.


## AI historical backfills retired from the catalog

The previous `ai-*.json` migration definitions were retired from this
operator catalog after confirming no AI migration checkpoints had started.
The AI runtime's Cassandra operational, conditional-operational, and activity
stores create/reconcile their own tables on first use; migration jobs are
not needed for schema creation. Retiring a definition **does not** migrate
historical Azure Table records and **does not** delete or modify any Azure
or Cassandra data. If historical AI data must later be recovered, the
definitions remain available in Git history and require a fresh migration
review before restoration.


### Billing invoice/payroll audit history (migration preparation only)

The `nuviot/billing` runtime is being converted from Azure Table Storage
to Cassandra Activity Records. **Plaid did not go live and needs no historical
backfill or migration definition.**

Legacy invoice/payroll audit rows may exist. Before cutting over LIVE,
inventory and verify the original Azure tables; do not assume they are empty.
The billing definitions use the shared Azure DEV/PROD account and import
append-only activity history into three Cassandra tables:

- `billing-invoice-audit`: InvoiceAuditTrailDTO, partitioned by invoice ID.
- `billing-payroll-run-audit`: PayrollRunAuditTrailDTO, partitioned by payroll run.
- `billing-payroll-time-period-audit`: the same payroll source, partitioned by
  time period to support the other existing read API without a global scan.

The two payroll definitions are independent projections over the *same source
data*. They must both complete and be verified before the runtime cutover.
No source Azure rows are deleted. Retention is intentionally unspecified.

Use `catalog`, `validate`, `probe`, then a bounded `migrate` and `verify`
for each definition in DEV. Follow the production confirmation and final-delta
safeguards in the migration playbook before switching LIVE. These definitions
only configure imports; adding them does **not** run a migration.

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

**Production safeguards:** Use `$env:MIGRATION_ENVIRONMENT = "prod"` and
`dotnet run -- object-inventory` to inspect Azure source containers/counts/bytes
without any writes or S3 connection. A small
`object-inventory --max-objects 100` is only a partial inventory.
Both `object-migrate` and `object-reset` require the explicit
`--confirm-production` flag in PROD; neither is part of the structured
Table Storage migration workflow. **Do not run PROD object migration
until target-bucket contents, ownership, destination overwrite policy,
and application storage cutover are reviewed.** Object uploads reuse
Azure container/blob names and will overwrite matching existing S3 keys.

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

Definitions select the Azure Storage account by `source.connection`, using the standard CloudStorage `TestConnections` DEV/PROD settings as defaults. No separate `MIGRATION_AZURE_*` credential namespace is required.

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

All supported logical Azure Table source names (`access-log`, `billing-storage`, `marketing-storage`,
`user-storage`, and `ai-ml-table-storage`) use the same standard CloudStorage
DEV/PROD Table Storage account selected by `MIGRATION_ENVIRONMENT`. The names
identify migration definitions; they are **not** separate credentials or accounts.
Do not configure `DEV_USER_*`, `DEV_MARKETING_*`, or `DEV_AI_ML_*` Table
Storage credential pairs. Unknown logical source names are rejected instead of
silently selecting an account.

The Azure Blob migration uses the same standard account. No source credentials
or connection strings belong in the JSON definitions.

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

Structured definitions include:

```text
useradmin-access-log
useradmin-authentication-log
marketing-contact-dto
marketing-contact-phone-index
useradmin-sent-email-xref
```

A structured definition contains the source table/table pattern, target table, partition fields, time bucket, indexes, TTL, field mappings, target field types, and mapping transforms. The engine and writer do not require the downstream CLR record type.
