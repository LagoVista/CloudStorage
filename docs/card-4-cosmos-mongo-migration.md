# Card 4 - Cosmos-to-Mongo Migration Tooling

## Objective

Provide a safe, repeatable utility that copies raw documents from Cosmos into Mongo using the final domain-based collection routing rules.

## Migration shape

`Cosmos consolidated collection -> raw document -> transform -> domain resolver -> Mongo collection`

The migrator should not depend on application entity deserialization.

## Required transforms

- Move Cosmos `id` to Mongo `_id`.
- Do not retain a duplicate top-level `id` field.
- Strip Cosmos system metadata such as `_rid`, `_self`, `_etag`, `_attachments`, and `_ts`.
- Preserve the remaining document shape and nested values.

## Tasks

- Add migration request/result models.
- Support explicit source Cosmos and target Mongo settings.
- Stream Cosmos documents rather than loading an entire database into memory.
- Support configurable batch size.
- Route each document by `EntityType` through the domain collection resolver.
- Bulk upsert Mongo documents by `_id` so reruns are idempotent.
- Add optional `EntityType` filtering for incremental migrations.
- Add dry-run mode that reports counts and target collections without writing.
- Return/document continuation/checkpoint information sufficient to resume interrupted migrations.
- Count read, written, skipped, failed, and unresolved-route documents.
- Include per-entity-type and per-destination-collection statistics.
- Add validation mode comparing source and destination counts.
- Ensure secrets never appear in reports or logs.

## Acceptance criteria

- A dry run can inventory a Cosmos collection and show exactly where each entity type will land in Mongo.
- A real run can be interrupted and safely rerun without duplicate documents.
- `id`/`_id` mapping is deterministic.
- Cosmos metadata is absent from migrated Mongo documents.
- Unknown entity types are reported and written to the configured fallback rather than discarded.
- Source and target counts can be reconciled by entity type.

## Out of scope

- Live dual-write/change-feed synchronization during the first implementation.
- Destructive removal from Cosmos.
- Application cutover.
