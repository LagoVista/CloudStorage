# UserAdmin Azure Table backfill — operator runbook

Last updated: 2026-11-15

## Scope

These are four **source-migrated** UserAdmin repositories that still require historical production records to be reconciled. This runbook prepares an operator workflow; it does **not** report that data has been copied or cut over.

| Migration key | Azure source table | Destination | Data changes |
| --- | --- | --- | --- |
| `useradmin-app-instance` | `AppInstance` | MongoDB `AppInstanceRecord` | Import if absent; never overwrite runtime updates |
| `useradmin-org-membership` | `OrgUser` | MongoDB `OrgMembershipRecord` | Preserve admin/build flags and existing tombstones |
| `useradmin-user-role` | `UserRoleDTO` | Cassandra `user_role_operational_record` | Version=1, permanent tombstones take precedence |
| `useradmin-role-access` | `RoleAccessDTO` | Cassandra `role_access_operational_record` | Private org partition or global public partition |

The migration app is independent of UserAdmin packages. Record identity, fields, and indexes are declared in `Definitions/*.json`. Cassandra uses `IF NOT EXISTS`; Mongo uses `$setOnInsert`. Both preserve newer destination rows. The original Azure tables remain untouched.

## Environment prerequisites

Use `MIGRATION_ENVIRONMENT=dev` first; `prod` is explicit LIVE. Required standard connection settings are documented in [README.md](README.md).

For UserAdmin Azure sources, use the standard CloudStorage account selected by
`MIGRATION_ENVIRONMENT`. There is no separate UserAdmin Azure Storage account:

```text
DEV_TS_STORAGE_ACCOUNT_ID
DEV_TS_STORAGE_ACCOUNT_ACCESS_KEY
PROD_TS_STORAGE_ACCOUNT_ID
PROD_TS_STORAGE_ACCOUNT_ACCESS_KEY
```

For Mongo AppInstance and OrgUser, use the exact target runtime's `IAppConfig.SystemOwnerOrg` Id/Key/Text, **separately for DEV and PROD**. The migration utility selects them automatically using `MIGRATION_ENVIRONMENT` and does not accept a shared fallback:

```text
DEV_SystemOwnerOrg:Id
DEV_SystemOwnerOrg:Key
DEV_SystemOwnerOrg:Text

PROD_SystemOwnerOrg:Id
PROD_SystemOwnerOrg:Key
PROD_SystemOwnerOrg:Text
```

The logical JSON definition keys are `SystemOwnerOrg:Id`, `SystemOwnerOrg:Key`, and `SystemOwnerOrg:Text`, not environment-specific literals. These values are ordinary non-secret environment variables; they must match the DEV/LIVE runtime's Configuration Server values respectively.

Mongo credentials and ApplicationData database name are the same CloudStorage settings used for the existing checkpoint store; Cassandra credentials and keyspace use the existing settings. No secrets belong in JSON definitions or Git.

Before writing, verify the selected Azure account, the exact system organization ID, database name, Cassandra keyspace, and environment. `probe` can create target schema/indexes; it is not strictly read-only. In `prod`, both `probe` and `migrate` require the `--confirm-production` flag.

## One migration at a time

From `Apps/StorageMigration`, with correctly configured environment:

```powershell
$env:MIGRATION_ENVIRONMENT = "dev"
dotnet run -- catalog
dotnet run -- validate useradmin-app-instance
dotnet run -- status useradmin-app-instance
dotnet run -- probe useradmin-app-instance
dotnet run -- migrate useradmin-app-instance --max-records 100
dotnet run -- status useradmin-app-instance
dotnet run -- migrate useradmin-app-instance
dotnet run -- verify useradmin-app-instance
```

Repeat for `useradmin-org-membership`, `useradmin-user-role`, and `useradmin-role-access` in that order.

For an approved production run, set `MIGRATION_ENVIRONMENT=prod`, check credentials and target scopes, then add `--confirm-production` to `probe` and `migrate` commands. Verify definitions and checkpoints before starting.

The `--max-records` batch checkpoint stays Running; another `migrate` resumes. Replaying a completed migration requires `--catch-up`. Do not change a JSON definition after a run has started: its SHA-256 is checkpointed.

## Reconciliation and cutover

**Do not treat `verify` as proof of target completeness.** It checks that the completed pass's Azure source row count equals the number of rows processed and that none failed. It does not independently enumerate destination records; an insert-if-absent operation counts as processed even when Mongo/Cassandra already contained the key.

1. Run DEV probes, a small DEV import, and representative Mongo/Cassandra reads using the runtime repositories.
2. Record Azure source row count, target key count, sampled IDs, owner organization IDs, and sample authorization/membership states.
3. Investigate any missing source fields and any duplicate public permission `RowKey` across legacy source organizations; all global public permissions use a shared Cassandra partition.
4. For LIVE, coordinate a quiet write window/final catch-up. Import-if-absent does not replay updates to keys already present; never run a historical import as an overwrite of current permissions.
5. Verify deletions remain tombstoned, reactivated memberships are accurate, and the effective role/permission evaluation matches pre-cutover behavior.
6. Keep Azure source tables and legacy read-only readers until all key-level checks and rollback window have passed.
7. Only then remove legacy readers and promote the new libraries/host as a separate deployment decision.

## Other accumulated migration definitions

The existing catalog contains Marketing contacts/phone indexes, sent-email XRefs, and UserAdmin migrations. The 20 unstarted AI historical backfill definitions were retired from the catalog; AI runtime Cassandra stores provision their own tables on first use, while historical Azure data is not copied automatically. Use `dotnet run -- catalog` and `dotnet run -- status --all` for the authoritative local list and checkpoint state. **Catalog entries are not proof of execution or completion.**

Marketing, UserAdmin, and AI logical source names all resolve to the same environment-specific CloudStorage Table Storage account. No source-specific `USER`, `MARKETING`, or `AI_ML` credential pair is required. The migration tool still rejects unknown logical source names.

Always run `validate`/source reconciliation on each backlog entry before copying; older definitions can have different data-loss/revocation requirements.
