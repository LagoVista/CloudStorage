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

## Product ownership: Build Server V2, not Kubernetes configuration

Database schema migration is a **developer/build capability**. Its primary home
is a dedicated, deliberately simple **Database Migrations** workbench in
**Build Server V2**, alongside other build/publish/deploy operations.

- **Build Server owns the workflow:** detect EF model drift, identify missing
  migration source, build and publish the database-specific immutable image,
  compare image revision with the database's recorded revision, authorize
  execution, create the one-shot job, show live progress/logs and retain the
  receipt. The user should not need to navigate `nuviot/k8s` scripts for
  routine schema changes.
- **CloudStorage owns source truth:** models, checked-in migrations,
  `database-jobs.json` and job images. It is not the operator UI.
- **Kubernetes and `nuviot/k8s` own the substrate:** cluster/SQL-server
  reachability, trusted target registrations, runtime secrets and service
  accounts/RBAC, namespace constraints, job execution primitives and
  infrastructure provisioning. They must **not** be a second migration
  scheduler or decide when schemas need updating.
- **Existing DEV PowerShell scripts** under
  `nuviot/k8s/platform/database-migrations` are bootstrap/proof scaffolding,
  not the intended ongoing product. Build Server should invoke Kubernetes
  Jobs directly through a narrow, permitted job-launch capability once it
  is in place, not require developers to run PowerShell manually.

### Detect, do not silently invent migrations

For each registered EF migration context, Build Server compares the *current
compiled EF model* with the checked-in *migration model snapshot* using EF
Core's pending-model-changes functionality. No database connection is required
for this **source drift** check.

- **Model matches snapshot:** migration source is internally consistent.
- **Model differs:** flag `Migration Required`, block image publication, and
  optionally offer scaffolding into a reviewable source change.
- **Migration source is consistent but target is behind or missing:** build
  (if needed) and run the approved convergence image for that target.
- **Target newer/unknown or drifted:** fail closed; do not guess what data
  transformation or down-migration to run.

Automatic detection is not the same as safe automatic migration authoring:
renames, splits, transformations and destructive operations need developer
review. The first release should be read-only analysis/status, then gated
DEV image build/apply. LIVE promotion remains a separate explicit approval.

**Suggested minimum workbench:** one row per schema job (EF context/provider),
with `Model`, `Image Version`, `DEV Revision`, `LIVE Revision`,
`Last Result`; details show logical targets and recent execution evidence.
Actions: `Analyze Model`, `Build Image`, `Run on DEV` (guarded).
This is a Build Server page, not a Kubernetes management screen.

## Authority boundaries

| Owner | Responsibility |
| --- | --- |
| **CloudStorage Git** | EF context, entities, checked-in migrations, database job recipe and Dockerfile, tests; changes reviewed alongside the application |
| **Build Server V2** | Own the developer-facing Database Migrations workbench, EF model-drift analysis, exact-commit builds, image publication, approved execution and observability |
| **Kubernetes / nuviot/k8s** | Provide trusted infrastructure and Kubernetes Job primitives: cluster readiness, approved runtime secret bindings, service-account/RBAC, bounded execution and cleanup; not the migration product or its scheduler |
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

**Authorized target** means a server plus an approved database **name/identity** in the trusted deployment catalog. Authorization does not require the database to already exist physically: a missing database is an expected starting state, not a preflight error. The database image is responsible for creating it when required.

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

## Config Server and destination registration

Build Server chooses a **registered job type + deployment environment + logical
database target**, not an arbitrary database URL. The self-contained image
contains its own provider-specific EF model/migrations but no passwords.

Config Server is the source of environment-specific database connection
details. Prefer the existing
`LagoVista.Configuration.Client`/`RemoteConfigurationClient` bootstrap
pattern already used by short-lived workers, with a **database-migrator app
identity and deployment key**. It may return separate target connection and
bootstrap roles. The image verifies that resolved server/database identity
matches the approved Build Server target registration; it must not accept
unbounded runtime names or silently switch to another server. Build Server
must never log or persist connection secrets in the operation receipt.

**Today we care about exactly two relational servers/providers:** Azure SQL
Server and the clustered PostgreSQL service. A schema family/provider has one
versioned migrator image; that image may be used for multiple authorized
database instances sharing the same EF model. Each physical database maintains
its **own** EF migration-history ledger. The literal SQL notion of multiple
named schemas inside one database needs a separate history/name policy and is
not part of the initial scope.

Config Server access must be appropriately scoped for a one-off mutating job.
If config lookup or target identity validation fails, the migration is
blocked and journaled—no fallback to an unapproved server.

## OperationalJournal: required, not optional

**Every accepted database migration attempt produces an authoritative
`OperationJournal` record** and is linked to the Build Server
`ProcessExecution` for UI, cancellation and restart recovery.

This is deliberately **stricter** than the existing general-purpose
`ProcessExecutionRuntime`, which currently records some journal transport
failures as diagnostics and permits work to continue. That best-effort
behavior must **not** authorize database writes.

**Start-before-write rule:** the migration Job must not start if the durable
journal start/accepted write has not been acknowledged. The persisted
journal operation identity is deterministic from its accepted execution ID.
That gate is enforced server-side, not by UI visibility or a caller checkbox.

### Required events (at least)

| Phase | Durable information |
| --- | --- |
| Accepted | Job ID, requester/approval identity, environment, logical target, source commit, immutable image digest, operation/process correlation |
| Configuration resolved | Approved server identity, database name and provider (no credentials) |
| Inspection | Database `missing`, `uninitialized`, `behind`, `current`, or `blocked`; observed previous EF revision |
| Bootstrap | If missing, creation started/succeeded/failed; recorded even for first-time installations |
| Migration | Each pending EF migration ID started/completed/failed; total applied and current revision |
| Validation | Final EF history, expected target revision, schema checks, result |
| Terminal | `Succeeded`, `Failed`, `Cancelled`, or `Interrupted/Unknown`; elapsed time and evidence |

Status-only/dry-run and already-current **no-op** executions are also
journaled. Do not generate fake migration-applied events for a no-op.

**Reliability model:** persist each accepted transition with deterministic,
idempotent detail IDs or a durable local/outbox checkpoint for later retry.
A network failure after the Kubernetes Job starts must not silently lose
bootstrap/migration/completion evidence. On restart, reconcile the actual
Kubernetes Job and database's EF history into the same journal record; do
not rerun a destructive custom migration merely because a client timed out.
If final evidence is incomplete, surface `Journal incomplete` or
`Indeterminate` and require reconciliation rather than reporting success.

The same receipt powers the Database Migrations workbench and drill-through
to Engineering Executions. Do **not** invent a second journal/log database or
use browser-only state as authority.

**Implementation status:** strict start gate, append/retry reconciliation,
Config Server credential resolution and Kubernetes dispatch are **not yet
wired** to the new page. Do not enable `Apply to DEV/LIVE` until proven with
failure injection for journal unavailability and interrupted Job recovery.

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
