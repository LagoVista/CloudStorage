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
