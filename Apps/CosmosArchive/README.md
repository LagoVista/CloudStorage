# CosmosArchive — archival export to DigitalOcean Spaces

Independent, read-only-on-Cosmos operator program. It archives the native JSON documents from both physical containers (`{database}_Collections` and `Devices`) to a private DigitalOcean Spaces bucket. There are **no MongoDB writes** and **no Azure deletes**.

## Configuration

Reuse CloudStorage `TestConnections.DevDocDB` and `TestConnections.ProductionDocDB` for Cosmos, as in the August 2026 migration.

Additional required environment variables:

- `COSMOS_ARCHIVE_SPACES_ENDPOINT` — Spaces S3 endpoint host (without protocol)
- `COSMOS_ARCHIVE_SPACES_BUCKET` — pre-created private bucket
- `COSMOS_ARCHIVE_SPACES_ACCESS_KEY`
- `COSMOS_ARCHIVE_SPACES_SECRET_KEY`

No secrets are printed or committed. Use an archive-specific limited-access Spaces key.

## Operator commands

```powershell
dotnet run --project Apps/CosmosArchive -- probe dev 2026-10-final 5 200
dotnet run --project Apps/CosmosArchive -- archive dev 2026-10-final 5 200
dotnet run --project Apps/CosmosArchive -- archive dev 2026-10-final 0 200
dotnet run --project Apps/CosmosArchive -- verify dev 2026-10-final
dotnet run --project Apps/CosmosArchive -- probe prod 2026-10-final 5 200
dotnet run --project Apps/CosmosArchive -- archive prod 2026-10-final 0 200
dotnet run --project Apps/CosmosArchive -- verify prod 2026-10-final
# Restore only into a NEW database; never overwrites source or an existing database.
dotnet run --project Apps/CosmosArchive -- restore dev 2026-10-final CosmosArchiveRecoveryDev20261029
```

`archive` requires the exact console confirmation `ARCHIVE`. Page limit 0 means all pages; by default runs are bounded to five pages. One gzip JSONL part is created per Cosmos page. JSON documents are archived without Mongo migration transforms, exclusions, or routing.

After each part upload, the exporter re-downloads and checks the remote SHA-256 before writing the continuation checkpoint to Spaces. Retry with the same snapshot ID and environment after interruption. Object names are deterministic; incomplete pages can be uploaded again safely.

Bucket prefix: `<snapshot>/<dev|prod>/<source-container>/part-NNNNNNNN.jsonl.gz`. The remote `checkpoint.json` tracks progress, and completed exports publish a `manifest.json`.

**Important limitations / shutdown gate:** This is a logical read export, not a transactionally consistent snapshot. Before starting a final archive, stop all writers to the Cosmos source or confirm it is completely dormant; paginated queries otherwise can see concurrent modifications. The manifest preserves documents but not the full Azure resource configuration: export database/container metadata, partition-key paths, indexing, TTL, throughput and relevant account settings separately. Verification checks object checksums and recorded counts, not each decoded document or restoration compatibility. Before deleting Azure resources, separately prove a recovery into a disposable environment, reconcile source counts, keep independent copies of the manifests, and obtain explicit decommission approval. Never delete Cosmos as part of this utility.

## Restore proof

`restore` uses the environment's configured Cosmos endpoint, checks the completed manifest and archived SHA-256 values, creates a separate database and source-named containers using source partition-key paths, and creates documents (no upserts). It refuses pre-existing restore databases. It does **not** restore Cosmos-specific indexing, TTL, throughput, or account policy; capture those independently. It can incur Azure charges. A full fresh-account recovery requires connection configuration work. Complete a successful DEV round trip before LIVE operation or Azure cleanup.
