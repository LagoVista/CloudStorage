# Relational database ownership and one-shot jobs

**Status:** Architecture directive / commander's intent (13 November 2026)

## Commander's intent

> All **new platform-owned relational databases** are modeled in Entity Framework
> Core and their schema and migrations live in `LagoVista/CloudStorage`.
> Each independently versioned database schema family has one self-contained,
> short-lived Docker **migration job image**. Build Server V2 discovers those
> job recipes in this repository, builds, proves, versions, and publishes their
> images, then launches them as bounded Kubernetes Jobs in an explicitly
> selected environment. The image contains its own executable migrations;
> it never downloads instructions or arbitrary executable code at runtime.

**Outcome:** A database schema change is an ordinary, repeatable, observable
software release, not a manual production SQL session.

This is a direction for **new relational databases** (PostgreSQL by default).
It does **not** force MongoDB documents, Cassandra tables, blobs, vendor-managed
databases, existing critical SQL Server estates, or storage-provider bootstrap
resources into EF Core. Existing databases migrate into this policy only through
explicitly scoped modernization. Specialized PostgreSQL extensions/cluster
bootstrap remain operator/CNPG responsibilities, not EF model objects.

## Authority boundaries

| Owner | Responsibility |
| --- | --- |
| **CloudStorage Git** | EF context, entities, checked-in migrations, database job recipe and Dockerfile, tests; changes reviewed alongside the application |
| **Build Server V2** | Resolve exact source commit, discover approved job recipe, validate inputs, build/prove/publish immutable image, record provenance, dispatch permitted one-shot execution, collect status and journal |
| **Kubernetes / nuviot/k8s** | Controlled environment, dedicated one-shot Job execution, cluster/database readiness, secret references, timeouts, tokenless worker identity, logs and cleanup |
| **EF Core / target DB** | Database-specific `__EFMigrationsHistory` is the sole authority for *which schema migrations are applied* |
| **Config Server** | Application connection settings; no schema authority or implicit application-startup migrations |

No source-controlled job recipe is independently trusted to authorize a secret,
namespace, cluster, LIVE operation or approval bypass. Build Server must bind job
identity + destination to operator-controlled allowed targets.

## One image per schema family, not per migration or environment

- **Semantic catalog PostgreSQL** has two logical databases:
  `semantic_production` (authoritative) and `semantic_test` (experimental).
  Both share the same EF model and **one** `semantic-schema-migrator` image.
  They each maintain independent EF migration histories.
- A newly added EF migration creates a **new image version**, not another image
  repository or job type. The image contains all migrations required to bring
  an older supported schema forward; EF applies only pending ones.
- DEV versus LIVE are deployment environments, unrelated to semantic
  Production versus TEST *catalog roles*.
- A future billing schema, with different EF model/history, gets its own
  migration job image. A data-repair/backfill job may be a separate job type
  if its lifecycle or permissions differ.

CloudStorage retains `NuvIoT.buildtarget: type=dotnet-nuget` for its existing
NuGet publications. Additional job outputs are declared in
[`database-jobs.json`](../database-jobs.json). **Do not change the root
BuildTarget identity to a Docker/application type.**

## Contract for a database migration image

The image includes its compiled EF context and migrations, a pinned runtime,
and an executable with at least:

1. `status` (default, read-only): report target, image/source identity, applied
   and pending migrations; no DDL or data changes.
2. `apply` (explicit): refuse unexpected target/migration history, apply
   pending migrations, requery status and validate expected schema objects.
3. Exit `0` only on verified success. Nonzero exit on mismatch, failed
   migration, unexpected schema state or validation failure. Write bounded,
   structured logs; **never log credentials or connection strings**.

The existing semantic runner implements basic status/apply and table/view
verification. Structured output, history drift checks, immutable provenance
labels and generated SQL preview are follow-up hardening, **not yet complete**.

The image gets database credentials **only at execution**, via a Kubernetes
Secret selected by a trusted environment target. It does not embed passwords,
fetch schema instructions, invoke external LLMs, or contact a third-party
configuration service to decide what SQL to run.

## Build Server contract

1. At an **exact authorized source commit**, read `database-jobs.json`
   separately from the repository's primary NuGet BuildTarget.
2. Validate catalog schema, unique canonical job IDs, provider, project,
   Dockerfile and context paths. Constrain all paths to the checked-out
   CloudStorage tree; reject unsupported registries or arbitrary commands.
3. Detect a new release when the **job's effective inputs** change: EF model,
   migrations, runner, Dockerfile, referenced packages and build configuration.
   Do not rely solely on the migration folder changing. For the first cut,
   an explicit operator-triggered exact-commit image publication is acceptable.
4. Build and test the .NET project, prove EF model/migration compatibility,
   assemble the image, publish to a fixed repository, resolve its immutable
   digest, and record commit, version, digest, job ID and proof evidence.
5. Execution selects **job ID + immutable digest + environment + approved
   database target + action**, never a caller-supplied Dockerfile, shell
   command, connection string, secret name or arbitrary image.
6. `status` is the non-mutating default. `apply` is an authorized mutation.
   LIVE requires an explicit human release/backup gate. Prevent simultaneous
   schema changes to the same physical database. Jobs are bounded, observable,
   and return an execution receipt with logs and migration IDs.
7. Release remains separate from application deployment. Run compatible
   expand-first migrations, then deploy the application. A code rollback
   **does not** silently downgrade the database.

For eventual idempotency cleanup and data backfills, reuse the same **job
build, authorization, dispatch and observability** machinery. Their executable
logic stays in the image and must own its own replay/concurrency/checkpoint
contract; EF migration history is only for EF schema migrations.

## Minimum job recipe

`database-jobs.json` is a **build-time catalog**, not runtime instructions.

| Property | Purpose |
| --- | --- |
| `schemaVersion` | Fail closed on unsupported manifest revisions |
| `jobs[].id` | Stable job type identifier |
| `jobs[].kind` | `ef-schema-migration` for the initial supported family |
| `jobs[].project` / `dockerfile` / `buildContext` | Repository-relative source inputs |
| `jobs[].imageRepository` | Approved fixed Docker image repository |
| `jobs[].provider` / `migrationContext` / `migrationsPath` | EF migration lineage |
| `jobs[].logicalTargets` | Named logical catalogs, **not** secret references |
| `jobs[].actions` | Supported bounded runner actions |

Source metadata may help Build Server determine what to build; it cannot
override environment-level permissions or secrets. The known job definition
and approved target mapping should be code-reviewed together.

## Incremental delivery / acceptance

### Phase 1: semantic PostgreSQL proof (in progress)

- [x] Provider-selectable semantic contexts and PostgreSQL-specific initial EF
  migration in CloudStorage.
- [x] Self-contained `SemanticMigrator` console project and Dockerfile.
- [x] DEV-only Kubernetes Job/preflight mechanism in `nuviot/k8s`.
- [x] This commander-intent document and explicit job recipe.
- [ ] Build Server validates and publishes exact-commit job image and immutable digest.
- [ ] Provision empty DEV semantic Production and TEST databases, apply migrations,
  verify schema, read/write/registration/pruning, and repeat `apply` safely.
- [ ] Switch DEV configuration only after schema proof; leave LIVE and legacy
  SQL Server records untouched.

### Phase 2: make job images first-class Build Server V2 outputs

- [ ] Durable per-job image/catalog receipt; rebuild on effective-input change.
- [ ] Model-facing and operator UI: list/status/plan and guarded run, async
  operation journal, logs, result.
- [ ] Separate migration credentials from app credentials; LIVE backup proof,
  explicit approval and compatibility gate.
- [ ] Establish reusable one-shot jobs for safe data migration/backfill/cleanup.

## Guardrails

- **Make the happy path work, then tighten the screws.** Do not build a generic
  migration-language interpreter or remote instruction store.
- Don't deploy or mutate LIVE while establishing this workflow in DEV.
- New EF migrations are additive source changes: no modifying already
  published migration history unless explicitly recovering an unreleased
  DEV-only lineage.
- Changes must be tested against a disposable database before promotion.
- CloudStorage owns the **relational schema**; operational provisioning (CNPG)
  and access control stay in `nuviot/k8s`.
- Revisit the optional semantic TEST catalog roughly six months after adoption;
  keep its availability out of the authoritative artifact workflow.

## Current implementation references

- [Semantic PostgreSQL guide](semantic-postgres-migration.md)
- [Semantic migration project](../Apps/LagoVista.Relational.SemanticMigrator/)
- [DEV Kubernetes job runner](https://github.com/nuviot/k8s/tree/main/platform/database-migrations)
