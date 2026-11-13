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

## Verified DEV status and next authorized create

As of 2026-11-13, Build Server V2 has published the
`nuviot/semantic-schema-migrator` Docker image and successfully run a
read-only Kubernetes status Job against the registered DEV `semantic_test`
target. That operation reported **MISSING**; it neither created a database nor
applied any migrations. The next gated Build Server increment adds a dedicated
SystemAdmin-approved DEV `semantic_test` create/converge action, immutable
digest verification, durable journal authorization, and a second independent
status check that must return `CURRENT`. This new apply workflow is not yet
deployed or proven against a real database.

The worker uses the **shared Config Server application `db-migrator`**, not
a separate `semantic-schema-migrator` AppDefinition. In DEV it resolves:

- `DbMigrator:PostgresUrl` — approved PostgreSQL host and optional port
- `DbMigrator:PostgresUserName` and `DbMigrator:PostgresPassword` —
  dedicated schema owner / EF migrator identity (password secret)
- Optional `DbMigrator:PostgresBootstrapUserName` and
  `DbMigrator:PostgresBootstrapPassword` — separate restricted creator
  identity on **the exact same PostgreSQL server** (password secret)

The bootstrap identity connects to the `postgres` maintenance database.
The read-only status result now tests both database-creation permission and
the ability to assign database ownership to the migrator user. PostgreSQL
15 tests membership; PostgreSQL 16+ tests `SET ROLE` eligibility. If
`CREATE_PRIVILEGE: BLOCKED`, the Build Server refuses apply: configure an
appropriately restricted role through Config Server, then rerun status.
Neither password, connection string, nor Config Server bootstrap token is
stored in Git, displayed in the Build Server UI or included in the journal.

Both the `status` probe and `apply` use the **registered DEV database
identity**, and `apply` keeps a PostgreSQL advisory lock across creation,
migrations and schema verification. LIVE and DEV `semantic_production` apply
remain unsupported in this pilot.

## Build and execution ownership

The governing architecture is [Relational database ownership and one-shot jobs](relational-database-job-standard.md).
The single `semantic-schema-migrator` image supports both semantic Production and
TEST *catalog roles*: each logical database has its own migration history. The
new [job catalog](../database-jobs.json) declares this additional Docker output
without changing the root NuGet `NuvIoT.buildtarget`.

**Current status:** Build Server publishes and records an immutable migrator
image, assigns DEV desired truth and executes audited read-only status Jobs.
The initial DEV `semantic_test` database has been confirmed missing. The
new authorized create/converge operation is committed but not yet deployed
or executed; no schema initialization has occurred.

## Bootstrap/convergence contract (required before DEV switch)

The image must eventually accept an **existing PostgreSQL server** even if
`semantic_production` or `semantic_test` has not been created. It should
create the approved database when absent, run all checked-in EF migrations,
verify the resulting schema, and safely report success on replay when current.
An approved bootstrap credential is distinct from the migration credential.
Unknown/newer migration history or drift is a hard stop, not a reset.

**Implementation state:** The runner implements create-if-missing,
forward-only EF convergence and actual schema verification, protected by
full-lifecycle PostgreSQL advisory locking. DEV runtime acceptance still
requires the expressly approved first apply, an independent `CURRENT`
status result and a repeat-run/no-op check. Do not cut consumers over before
those production-path proofs pass.

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

The semantic image is a .NET worker. It uses
`LagoVista.Relational.MigrationRuntime` and the established
`RemoteConfigurationClient` to fetch approved settings for the **shared
`db-migrator` Config Server AppDefinition**, in the selected `dev` or
`live` deployment. This is an ordinary Config Server app registration;
no new application-type enum is needed.

The Kubernetes Job receives only a fixed logical target (`test` in the DEV
pilot), action, deployment, `MIGRATION_CONFIG_SOURCE=config-server` and
Config Server bootstrap settings. The remote values are used to construct
the approved PostgreSQL connection, with optional separate bootstrap
credentials as defined above. The runner never trusts an arbitrary
database name from the remote configuration.

If the target is absent, apply can create it with an authorized owner,
then run EF migrations as the ordinary migrator identity. The status
check is read-only, including the `CREATE_PRIVILEGE` check. A successful
status is not approval to apply: Build Server requires a SystemAdmin
confirmation, current-source verified image digest, recent matching
status, absence of another apply Job and a durable OperationalJournal
acceptance before Kubernetes Job creation.

Direct `MIGRATION_CONNECTION_STRING` / optional
`MIGRATION_BOOTSTRAP_CONNECTION_STRING` remain limited to legacy DEV
pilot scripts; Build Server forces Config Server mode and never falls
back to direct credentials after a remote lookup failure.

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

## Config Server DEV registration and credential checklist

The logical Config Server app key is `db-migrator` for all approved schema
families. It is shared; database identities remain source-catalog allowlists,
not arbitrary values provided by Config Server.

- [ ] Confirm the `db-migrator` AppDefinition and its published DEV
  configuration resolve the regular PostgreSQL host, username and secret
  password. The earlier successful status Job proved basic connectivity.
- [ ] Keep `PostgresPassword` marked secret. Only if the initial read-only
  privilege probe returns `BLOCKED`, register both optional
  `PostgresBootstrapUserName` and secret `PostgresBootstrapPassword`
  on the **same approved PostgreSQL host**. Neither is needed when the
  existing role has sufficient creator/owner authority.
- [ ] Grant the bootstrap login only database creation plus the ability to
  assign ownership to the dedicated migrator account. PostgreSQL 16+
  requires SET-role capability for that owner. Do not give a broad
  database superuser unless there is an independently approved reason.
- [ ] Keep the Config Server bootstrap token in the existing DEV Kubernetes
  Secret; do not supply passwords to Build Server, CLI arguments or chat.
- [ ] Publish the newly proved immutable image, assign DEV desired,
  run read-only status to verify `CREATE_PRIVILEGE: READY`, and then
  approve first apply through Build Server SystemAdmin UI.
- [ ] Require the final recorded EF revision, independent `CURRENT`
  status and a safe no-op repeat before enabling dependent consumers.

Export the key-only configuration registration manifest offline with:

```bash
dotnet run --project Apps/LagoVista.Relational.SemanticMigrator -- --export-config-manifest
```

LIVE settings and execution remain out of scope for this DEV-first proof.
