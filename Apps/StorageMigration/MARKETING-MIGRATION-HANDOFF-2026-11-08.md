# Marketing Azure Table Storage migration handoff
_Last updated: 2026-11-08_

## Goal and scope
Complete the historical backfill of Marketing records from Azure Table Storage into the semantic storage providers in CloudStorage. Preserve Azure tables until successful verification. This is a **handoff/status note**, not confirmation that an import has been run.

The existing DEV application cutover for `ContactDTO` uses Cassandra. The historical `ContactDTO` and phone-index backfill definitions already exist; do not recreate them.

## Existing definitions ready to inspect/run

| Key | Azure source | Cassandra destination | State |
| --- | --- | --- | --- |
| `marketing-contact-dto` | `ContactDTO` in `marketing-storage` | `contact_operational_record` | Definition exists; execution/completion **not verified** |
| `marketing-contact-phone-index` | `ContactDTO` rows with nonempty `Phone` | `contact_phone_index_record` | Definition exists; execution/completion **not verified** |
| `useradmin-sent-email-xref` | UserAdmin legacy cross-reference data | Cassandra target defined in its own JSON | Definition exists; execution/completion **not verified** |

Definition files:
- [contact-dto.json](Definitions/contact-dto.json)
- [contact-phone-index.json](Definitions/contact-phone-index.json)
- [StorageMigration README](README.md) (complete setup, commands, credentials, and verification caveats)

## Resume ContactDTO backfill

From `Apps/StorageMigration`, after configuring the standard DEV connections as documented in [README.md](README.md):

```powershell
$env:MIGRATION_ENVIRONMENT = "dev"
dotnet run -- catalog
dotnet run -- status marketing-contact-dto
dotnet run -- validate marketing-contact-dto
dotnet run -- probe marketing-contact-dto
dotnet run -- migrate marketing-contact-dto --max-records 100
dotnet run -- migrate marketing-contact-dto
dotnet run -- verify marketing-contact-dto

dotnet run -- status marketing-contact-phone-index
dotnet run -- probe marketing-contact-phone-index
dotnet run -- migrate marketing-contact-phone-index
dotnet run -- verify marketing-contact-phone-index
```

**Before running `migrate`:** inspect existing checkpoint status and source/target configuration. Don't restart a completed migration blindly. Run `--catch-up` only when a completed pass needs an intentional follow-up. A `probe` may create Cassandra tables/indexes but doesn't copy rows; `verify` reconciles source counts and failures, **not** independent destination-row counts. Inspect representative Cassandra records after each import.

The operational import uses Cassandra `IF NOT EXISTS` so older Azure rows do not overwrite records already created after cutover. No default dual-write.

## Marketing repositories recently converted in nuviot/Marketing

Source branch: `master`, final implementation commit: [0c935d4d9e8b](https://github.com/nuviot/marketing/commit/0c935d4d9e8b5c3f3bc1f89f04e6cfc690dba735). Exact-commit **NuGet build proof succeeded**; this is not a package publication or application deployment.

| Record/repository | New runtime storage | Historical backfill definition |
| --- | --- | --- |
| `CommunicationsLogRepo` / `CommunicationsLog` | Cassandra operational records: `CommunicationsLogRecord` plus `CommunicationsLogEventRecord` reporting projection | **Still required** |
| `UnsubscribeRepo` / `EmailUnsubscribeDTO` | Cassandra operational record: `EmailUnsubscribeRecord` | **Still required** |
| `BulkEmailRepo` / `BulkEmailContactDTO` | Unused empty Table Storage repository removed | None unless unexpected historical data/consumers are discovered |

The cross-org `GetAllForTypeAsync` legacy reporting path is handled by an event projection, not a cross-partition scan. When designing its backfill, populate **both** the primary communications record and the event lookup, preserving legacy identity and event-filter behavior. Check the current source before authoring definitions.

**Important:** no publication, deployment, migration run, or production cutover was performed as part of this implementation review.

## Other outstanding Marketing cutovers
- `ContactIntake` currently has a `TableStorageBase<ContactIntake>` repository. Cassandra conversion was previously planned, but is not confirmed complete in this handoff.
- `CustomerFollowupDTO` currently has a `TableStorageBase<CustomerFollowupDTO>` repository. The intended durable mutable target is MongoDB ApplicationData. This is not yet confirmed complete.
- `CustomerActivityScheduler` derives from `TableStorageEntity`; no active Table Storage repository was identified in the prior search. Verify usage before including it.
- `SentEmailHistoryRepo` is already on `IActivityRecordStore<SentEmailHistoryActivityRecord>` (Cassandra). Historical event-stream import is **not** covered by the `ContactDTO`/phone-index/XRef definitions.

## Follow-up checklist
- [ ] Inspect `status` for the existing ContactDTO and phone-index definitions; establish whether earlier import runs occurred.
- [ ] Validate source account selection (`marketing-storage`) and Cassandra target before importing. Confirm no environment-variable fallback points at the wrong Azure account.
- [ ] Probe, bounded import, complete import, verify source accounting, and independently spot-check Cassandra contact records and phone lookup.
- [ ] Add structured migration definition(s) and tests for `CommunicationsLogRecord` plus `CommunicationsLogEventRecord`.
- [ ] Add structured migration definition and tests for `EmailUnsubscribeRecord`; preserve organization + normalized-email key and opt-out behavior.
- [ ] Decide how to migrate historical mutable rows without clobbering post-cutover updates/deletes (especially unsubscribe removals); `IF NOT EXISTS` alone does not prevent deleted legacy unsubscribes being resurrected.
- [ ] Verify `BulkEmailRepo` has no active external consumers or meaningful legacy table content before final retirement.
- [ ] Finish `ContactIntake` and `CustomerFollowupDTO` runtime conversions separately and add appropriate historical migration definitions if needed.
- [ ] Run integration/query tests for Cassandra paging and reporting projections; publish/deploy only after validation.

## Design authority
Read [CloudStorage table-storage migration playbook](../../docs/table-storage-migration-playbook.md) before editing or importing. It specifies semantic stores, schema/partition/query design, idempotent IDs, source conventions, and checkpoint requirements. The [StorageMigration README](README.md) is the operator runbook.

**Guiding rule:** make the happy path work first. Keep the migration in CloudStorage and runtime data access in the business repository; do not add dual-write infrastructure by default.
