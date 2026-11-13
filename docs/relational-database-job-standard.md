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

## Single mission: converge the target database

A database migration image is an **apply-to-current converger**, not a one-off
script for one old/new revision pair. The only infrastructure precondition is
that its **authorized database server is reachable and ready**. The named
database itself may be absent, empty, at an earlier supported revision, or
already current.

**Today there are exactly two supported relational database providers:**

1. **Azure SQL Server / Azure SQL Database** (a supported, explicitly authorized
   server and database target).
2. **PostgreSQL in the Kubernetes cluster** (a supported, explicitly authorized
   server and database target).

There is no implicit third provider, generic SQL dialect, or runtime
instruction interpreter. **An image is built for one database schema family
and provider**, so it may contain PostgreSQL migrations or SQL Server
migrations; it does not need to support both providers internally. Existing
legacy databases remain subject to their own migration-authority decisions.

### Required behavior

| State of the authorized target | Required result |
| --- | --- |
| Database does not exist | Create it with the approved bootstrap identity, apply the full migration chain, verify the final schema |
| Database exists, but is empty/uninitialized | Initialize migration history, apply the initial and subsequent migrations, verify |
| Database is 1–N supported revisions behind | Apply **all** pending migrations in order, preserving data, then verify |
| Database is already at the image's revision | Do nothing destructive; verify and exit success |
| Database has newer/unknown migration IDs, conflicting schema, unsafe drift, or migration history gaps | **Fail closed** with diagnostics and **no destructive guess** |

Executing the **same image again** against the same already-current database
must be a successful no-op. For any supported older revision, the image
must include the complete migration chain required to converge to its
published current revision; it cannot assume the previous image ran.

The runner must distinguish server availability from database existence.
**`status`** reports `missing`, `uninitialized`, `behind`, `current`,
or `blocked` without creating a database. **`apply`** performs bootstrap
(if missing), migrations and final verification in one tracked operation.
The Build Server returns success **only after** the image exits successfully
and confirms its target schema revision.

**Bootstrap credentials:** Creating a database often requires server-level
permissions beyond the normal application or schema-migration user. The Job
may receive a separately scoped **bootstrap credential**, selected by
the trusted environment registration, solely for create-if-absent. PostgreSQL
may connect to an existing maintenance database for `CREATE DATABASE`;
Azure SQL may use a suitably privileged connection to `master`. After
database creation, subsequent EF migrations use the dedicated schema role.
Credentials are injected at runtime from trusted Secrets and never baked
into an image, manifest or logs. If create permission is unavailable, fail
explicitly rather than silently delegating creation to an undocumented
manual step.

Database creation must not delete/recreate an existing database, adopt a
nonempty unmanaged schema, drop arbitrary objects, or run down-migrations.
Concurrent invocations for the same target must be serialized **including
before a database exists**. Production execution requires the normal
approval/backup/compatibility gates; "always converge" does **not** mean
"always apply without authorization." Expand-first migrations and
data-preserving forward scripts remain the default; destructive migrations
require separately approved staging/backup procedures.

### Verification contract

- Determine and report the exact current migration IDs and the expected
  target migration ID embedded in the image.
- Detect missing, duplicate, out-of-order, unknown or newer migration history
  before modifying an existing database.
- Verify expected schema objects/constraints and application data invariants
  after applying; a missing or corrupt history table in a nonempty database
  is a blocker, not an excuse to replay the initial schema.
- Emit an immutable receipt: source commit, image digest, environment,
  database target identity, previous revision, applied revisions, final
  revision, outcome, elapsed time and error diagnostics.
- A retry after interruption must either safely resume forward progress
  using authoritative EF history or report that manual investigation is
  required. Not every partially completed custom migration can be
  auto-repaired; never report success without verification.

**Implementation gap:** The current semantic runner can use EF migrations
once a PostgreSQL database is available. It does **not yet** provide the
separate server-level database bootstrap flow, missing-database status,
strong history/drift checks, or end-to-end convergence evidence. The current
`nuviot/k8s` scripts still require a pre-created Database CRD. **Do not
treat the charter as an already deployed capability.** Converger
implementation and its first missing/behind/current integration tests are
blocking work before DEV cutover.

## Contract for a database migration image

The image includes its compiled EF context and migrations, a pinned runtime,
and an executable with at least:

1. `status` (default, read-only): report target, image/source identity, applied
   and pending migrations; no DDL or data changes.
2. `apply` (explicit): create the authorized database if missing, refuse unexpected target/migration history, apply
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
- [ ] Database image converges missing/empty/behind/current states safely, with verified no-op replay and explicit bootstrap permission.
- [ ] Build Server validates and publishes exact-commit job image and immutable digest.
- [ ] Bootstrapping missing DEV semantic Production and TEST databases via their image, apply migrations,
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
- CloudStorage owns the **relational schema and create-if-absent database job**;
  operational database-server/cluster provisioning (CNPG, Azure SQL server)
  and access control stay in their respective infrastructure controls.
- Revisit the optional semantic TEST catalog roughly six months after adoption;
  keep its availability out of the authoritative artifact workflow.

## Current implementation references

- [Semantic PostgreSQL guide](semantic-postgres-migration.md)
- [Semantic migration project](../Apps/LagoVista.Relational.SemanticMigrator/)
- [DEV Kubernetes job runner](https://github.com/nuviot/k8s/tree/main/platform/database-migrations)
