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
