# Card 7 - Validation, Cutover, and Operational Runbook

## Objective

Prove Mongo parity with representative production workloads, document rollback, and define a controlled cutover from Cosmos.

## Tasks

- Build a validation matrix covering CRUD, list queries, projections, semantic queries, caching, deletes, and dependency checks.
- Select representative logical databases and entity domains for staged validation.
- Run Cosmos-to-Mongo migration in dry-run mode and review routing/count reports.
- Run migration into a non-production Mongo database and reconcile counts by `EntityType` and domain collection.
- Validate representative documents structurally, including `_id`, nested headers, enums, dates, arrays, and optional/null fields.
- Exercise application reads against Mongo before enabling writes where practical.
- Run targeted performance comparisons for known large-document/high-volume query paths.
- Validate domain collection names and required indexes.
- Document provider/environment configuration for local, dev, and later production use.
- Define cutover sequence, smoke tests, rollback criteria, and rollback steps.
- Keep Cosmos data intact through the initial stabilization window.
- Document known Cosmos-specific migration islands that remain after generic document cutover.

## Acceptance criteria

- Migration reports reconcile source and target counts.
- Representative application workflows pass against Mongo.
- Known performance-sensitive queries do not regress materially.
- Cutover can be enabled by configuration without repository code changes.
- Rollback to Cosmos is documented and configuration-driven.
- No destructive Cosmos cleanup is required to complete the initial cutover.

## Follow-up after stable cutover

Only after stability is established should we consider:

- Mongo index tuning based on observed queries.
- Retiring Cosmos-specific generic storage code.
- Removing unused Cosmos package dependencies.
- Archiving or deleting migrated Cosmos data according to an explicit retention decision.
