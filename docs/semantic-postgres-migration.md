# Semantic catalog PostgreSQL migrations

The authoritative and experimental semantic registries are two logical catalogs,
independent of the DEV/LIVE application environments. On PostgreSQL they use
**separate databases with the same EF Core schema**. No data is copied from SQL Server;
both new PostgreSQL catalogs begin empty.

This repository maintains **separate EF migration lineages**:
- `Migrations/Semantic`: existing SQL Server migrations, retained for compatibility.
- `Migrations/SemanticPostgres`: PostgreSQL migrations for both semantic databases.

Runtime selects `SemanticDb:Provider=Postgres` and `SemanticTestDb:Provider=Postgres`.
Each section requires `HostName`, `InitialCatalog`, `UserName`, and `Password`;
`Port` defaults to 5432. Omitting `Provider` preserves SQL Server behavior.

Migrations **never** run on normal application startup. The companion
`Apps/LagoVista.Relational.SemanticMigrator` runs with environment variables:
- `MIGRATION_CONNECTION_STRING`: secret PostgreSQL connection string for exactly one database
- `MIGRATION_TARGET`: descriptive label for the specific logical registry
- `MIGRATION_ACTION`: `status` or `apply` (default: status)

Use the Kubernetes migration Job workflow in `nuviot/k8s`. Build an immutable runner
image from this project's source. Run status first, inspect pending migrations,
then apply. Use a dedicated schema-migration DB role; ordinary app roles should
not own DDL privileges.

Scaffold future PostgreSQL migrations against `SemanticPostgresMigrationContext`
and output to `src/LagoVista.Relational/Migrations/SemanticPostgres`.
Never use the SQL Server context for PostgreSQL scaffolding.

The PostgreSQL initial migration creates seven semantic tables, indexes, foreign keys
and `dbo.usv_semanticdefinitions`. Verify lookup, registration, prune, and
embedding read/write against fresh databases before changing DEV Config Server.

## Build and execution ownership

The governing architecture is [Relational database ownership and one-shot jobs](relational-database-job-standard.md).
The single `semantic-schema-migrator` image supports both semantic Production and
TEST *catalog roles*: each logical database has its own migration history. The
new [job catalog](../database-jobs.json) declares this additional Docker output
without changing the root NuGet `NuvIoT.buildtarget`.

**Current status:** The runner and DEV-only Kubernetes scripts exist, but Build
Server does not yet read the job manifest, publish this image, or operate a
first-class job lifecycle. Do not infer that a committed Dockerfile means a
published image or that DEV databases have been initialized.

## Bootstrap/convergence contract (required before DEV switch)

The image must eventually accept an **existing PostgreSQL server** even if
`semantic_production` or `semantic_test` has not been created. It should
create the approved database when absent, run all checked-in EF migrations,
verify the resulting schema, and safely report success on replay when current.
An approved bootstrap credential is distinct from the migration credential.
Unknown/newer migration history or drift is a hard stop, not a reset.

**Not yet implemented:** The existing Kubernetes setup provisions databases
before the Job, and the current runner does not perform database bootstrap.
The new convergence contract in [the architecture standard](relational-database-job-standard.md)
is the acceptance test for the next implementation slice; do not cut DEV over
until it passes missing/behind/current and repeat-run scenarios.

**Target identity versus existence:** a trusted target registration approves the
server and database name, **not** the current presence of that database.
The image may receive either an existing target database or a missing one.

The semantic runner now checks for a missing PostgreSQL database, reports
`MISSING` in status mode and creates it on `apply` using
`MIGRATION_BOOTSTRAP_CONNECTION_STRING` when supplied, otherwise the
migration user's credentials against the `postgres` maintenance database.
Both modes refuse unexpected database names. Database creation requires a
role permitted to CREATE DATABASE and assign ownership to the migration user.
The Kubernetes Job must supply approved Secrets; insufficient permissions
must produce a clear failure, never an implicit server-side workaround.

## C# migration-runtime and Config Server bootstrap

The semantic image is a .NET console executable. Shared configuration bootstrap
lives in `Apps/LagoVista.Relational.MigrationRuntime` (project reference, not a
new NuGet package). It uses the existing
`LagoVista.Configuration.Client` / `RemoteConfigurationClient` convention
also used by the video-processing worker. The actual EF schema and PostgreSQL
migration code stay within the semantic application.

Config Server needs an **AppDefinition** with app key
`semantic-schema-migrator`, published for `dev` and eventually `live`.
No new Config Server application-type enum is required for the first slice.
It must have its own restricted identity/bootstrap access, not general-purpose
application credentials.

On startup the container receives only its logical target (`production`
or `test`), action (`status` or `apply`), environment
(`MIGRATION_DEPLOYMENT=dev|live`) and Config Server bootstrap settings
(`RemoteConfigurationBootstrap__ConfigurationServiceBaseUrl` and
`RemoteConfigurationBootstrap__AuthorizationToken`). Set
`MIGRATION_CONFIG_SOURCE=config-server` explicitly when launched by
Build Server V2. It requests the matching AppDefinition/deployment from
Config Server and reads:

| Config Server setting | Example / role |
| --- | --- |
| `DatabaseMigration:Targets:production:Provider` | `postgresql` |
| `DatabaseMigration:Targets:production:Database` | `semantic_production` |
| `DatabaseMigration:Targets:production:ServerHost` | Approved PostgreSQL server host |
| `DatabaseMigration:Targets:production:ConnectionString` | Secret migration-role connection string |
| `DatabaseMigration:Targets:production:BootstrapConnectionString` | Optional secret server-level CREATE DATABASE identity |

Define the same keys under `DatabaseMigration:Targets:test:*` for
`semantic_test`. Logical target authorization is defined in the container
and must match the resolved database, provider and registered server host.
For database creation the bootstrap credential must address the same server
through a separate maintenance database, and the created database is owned by
the intended migration user.

**Compatibility:** `MIGRATION_CONFIG_SOURCE=auto` (default) uses Config
Server if either bootstrap setting is present; otherwise it accepts the
existing DEV-only `MIGRATION_CONNECTION_STRING` and optional
`MIGRATION_BOOTSTRAP_CONNECTION_STRING`. Remote resolution failure NEVER
falls back to direct mode. Direct mode is rejected if
`MIGRATION_DEPLOYMENT=live`. The final Build Server execution path must
force `config-server`; this legacy direct mode is a DEV pilot escape hatch,
not an approved production configuration path.

**Not yet provisioned:** AppDefinition/PublishedApp settings in DEV Config
Server, trusted bootstrap Secret, first-class Build Server one-shot image
publish/execute, mandatory OperationalJournal start gate, and DEV real-Postgres
convergence proof. This commit does not change an environment or invoke SQL.

## Offline model-drift analysis (Build Server readiness)

The compiled migrator supports `--analyze-model`, a source-only command
that compares the current provider-specific EF model to the checked-in
migration snapshot using `HasPendingModelChanges()`. It does **not**
read Config Server or connect to the database. It reports the latest
checked-in migration ID and `drift=none|required` without connection data.
Exit code `0` means the snapshot matches; exit code `2` means a new
migration must be authored and reviewed before publishing the image.
This is distinct from `MIGRATION_ACTION=status|apply`, which inspects or
converges an actual database target.

The test `SemanticModelDriftTests` is the corresponding source gate.

### First EF model-drift proof and initial schema correction

The first `HasPendingModelChanges()` regression detected 25 unintended
operations before any DEV database migration was run: key/constraint casing
and an implicit `definitionview` table mapping. Because the PostgreSQL
lineage has **not been published or applied**, its initial migration and
snapshot were corrected in source to match the model's lower-case PostgreSQL
naming policy. The view DTO explicitly has no table mapping (consistent
with the existing SQL Server view snapshot). This repair is intentionally
limited to the as-yet-unreleased PostgreSQL migration lineage; existing
SQL Server migration files and databases are unchanged.

Future released EF migrations must never be edited in place. At that point
schema differences require a new additive migration and review.
