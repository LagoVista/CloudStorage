# Card 2 - Mongo Configuration Model

## Objective

Define one explicit Mongo server connection model instead of overloading Cosmos-oriented endpoint/access-key settings or maintaining parallel Mongo configuration stacks.

## Status

The primary Mongo server connection model is `MongoDocumentStorageConnectionSettings` / `IMongoDocumentStorageConnectionSettings`. The older provider-client experiment and its duplicate Mongo/Cosmos settings wrappers have been removed.

The compatibility `DocumentStorageSettingsResolver` remains for existing repository constructors and provider selection during the strangler migration, but its resolved Mongo value is now deliberately named `MongoDocumentStorageTarget` so it is not confused with server configuration.

## Canonical Mongo server connection settings

Primary Mongo uses the LagoVista-style application configuration contract:

```text
MongoDocumentStorage
```

`MongoDocumentStorageConnectionSettings` exposes:

- `Hosts`
- `Port`
- `UserName`
- `Password`
- `AuthenticationDatabase`
- optional `ReplicaSet`
- `UseTls`

It owns Mongo server connection-string construction. `IMongoDocumentStorageConnectionSettings` and `IMongoStorageClientFactory` are registered through the normal CloudStorage DI setup.

This is the only first-class Mongo server connection model in CloudStorage.

## Resolved document-storage target

Provider-neutral document operations sometimes need a small resolved value containing:

```text
ConnectionString
DatabaseName
```

That value is `MongoDocumentStorageTarget`.

It is intentionally not another connection-settings abstraction. It represents the final Mongo target selected for one logical document-storage operation after server connection and database selection have been resolved.

This keeps the concepts distinct:

```text
MongoDocumentStorageConnectionSettings
    = how to connect to the Mongo server

MongoDocumentStorageTarget
    = connection string + database selected for this operation
```

## Retired parallel client seam

An earlier modernization attempt introduced a second provider stack:

- `IDocumentStorageClient`
- `ICosmosDocumentStorageClient`
- `IMongoDocumentStorageClient`
- `IDocumentStorageClientProvider`
- `DocumentStorageClientProvider`
- `CosmosDocumentStorageClient`
- `MongoDocumentStorageClient`
- `ICosmosConnectionSettings`
- `IMongoConnectionSettings`
- `MongoConnectionSettings`

That stack was registered and partially plumbed into `DocumentCloudServices`, but the active provider-neutral repositories use `DocumentCollectionFactory` / `IDocumentCollection` instead. The duplicate stack has therefore been removed rather than maintained in parallel.

This does **not** remove live Cosmos compatibility repositories such as `CosmosSyncRepository`, `CosmosDBStorage`, or the remaining Cosmos implementation behind the strangler path. Those still have active callers and will be retired only as their consumers are converted.

## Local/test and migration configuration

`TestConnections` exposes structured Mongo server settings for production, development, and the disposable local Mongo harness. The EntityBase migration runner consumes these canonical connection settings and builds its `MongoDocumentStorageTarget` from them.

The compatibility resolver still supports:

```text
NUVIOT_DOCUMENT_STORAGE_PROVIDER=mongo
NUVIOT_DOCUMENT_STORAGE_PROVIDER_<LOGICAL_DATABASE>=mongo
```

and the older `NUVIOT_MONGO_CONNECTION_STRING*` / `NUVIOT_MONGO_DATABASE*` values for repository paths that have not yet been moved onto injected structured configuration.

Cosmos remains the default provider when no provider is selected.

## Completed tasks

- [x] Define Mongo server connection settings separate from Cosmos endpoint/shared-key values.
- [x] Add structured `MongoDocumentStorage` application settings.
- [x] Register structured Mongo settings and singleton Mongo client lifecycle through DI.
- [x] Build Mongo connection strings from structured host/auth/TLS/replica-set values.
- [x] Keep credentials out of diagnostic/error output.
- [x] Preserve global and database-specific provider selection during migration.
- [x] Preserve Cosmos as the default provider.
- [x] Support explicit provider-neutral Mongo targets for migration/cutover code.
- [x] Add deterministic local Docker test settings through `TestConnections`.
- [x] Remove the abandoned parallel `IDocumentStorageClient` provider stack.
- [x] Remove duplicate `MongoConnectionSettings` / `IMongoConnectionSettings` wrappers.
- [x] Move the Mongo document-storage smoke test onto `IMongoDocumentStorageConnectionSettings`.

## Remaining compatibility work

Existing constructors still use `DocumentStorageSettingsResolver` and its environment-variable compatibility path. As those consumers move to injected storage configuration, the remaining `NUVIOT_MONGO_*` compatibility resolver can be retired separately.

That is intentionally not coupled to this cleanup because the current repository strangler still depends on provider selection without constructor changes.

## Acceptance criteria

- [x] There is one first-class Mongo server connection model.
- [x] A resolved Mongo operation target is clearly distinguished from server connection settings.
- [x] Cosmos and Mongo connection information can coexist in one process.
- [x] Selecting Mongo does not require repurposing a Cosmos access-key field.
- [x] Migration code can receive explicit source Cosmos and target Mongo values simultaneously.
- [x] Existing Cosmos-only deployments continue working without configuration changes.
- [x] Dead Mongo/Cosmos provider-client duplication is removed without deleting live strangler implementations.

## Out of scope

- Removing live Cosmos repositories that still have consumers.
- Application Data or Scratch Data storage configuration changes.
- Secret-store implementation changes outside the configuration seam.
