# Card 2 - Mongo Configuration Model

## Objective

Define explicit Mongo configuration instead of overloading Cosmos-oriented endpoint/access-key settings, while preserving configuration-driven provider selection for existing repositories.

## Status

Core configuration model is implemented and locally validated. One cleanup remains before dev cutover: bridge the new first-class structured Mongo settings into the legacy `DocumentStorageSettingsResolver` provider-selection path so primary Mongo runtime configuration no longer depends on ad-hoc `NUVIOT_MONGO_*` connection-string variables.

## First-class Mongo connection settings

Primary Mongo now has a LagoVista-style application configuration contract:

```text
MongoDocumentStorage
```

The structured settings expose:

- `Hosts`
- `Port` (default `27017`)
- `UserName`
- `Password`
- `AuthenticationDatabase` (default `admin`)
- optional `ReplicaSet`
- `UseTls` (default `false`)

The settings build the driver connection string internally and redact credentials from diagnostic output.

`IMongoDocumentStorageConnectionSettings` and `IMongoStorageClientFactory` are registered through the normal CloudStorage DI setup.

## Local test configuration

`TestConnections.TestMongoDocumentStorage` is intentionally deterministic and matches the repository-owned Docker Mongo harness:

```text
Host: localhost
Port: 27018
User: nuviot-test
Authentication database: admin
TLS: false
Replica set: none
```

The local runner therefore does not need `TEST_MONGO_*` environment variables.

Production/test deployment credentials remain configuration/secret driven; deterministic credentials are only for the disposable local Docker test instance.

## Existing provider selection

The compatibility resolver still supports:

```text
NUVIOT_DOCUMENT_STORAGE_PROVIDER=mongo
NUVIOT_DOCUMENT_STORAGE_PROVIDER_<LOGICAL_DATABASE>=mongo
```

and the older Mongo connection/database resolver variables. Database-specific values take precedence and Cosmos remains the default when no provider is selected.

This compatibility path is what currently lets existing `DocumentDBRepoBase<TEntity>` constructors switch to Mongo without changes.

## Completed tasks

- [x] Define explicit Mongo document storage settings separate from Cosmos endpoint/shared-key values.
- [x] Add first-class structured `MongoDocumentStorage` application settings.
- [x] Register structured Mongo settings through dependency injection.
- [x] Add singleton Mongo client lifecycle management.
- [x] Build Mongo connection strings from structured host/auth/TLS/replica-set values.
- [x] Keep credentials out of diagnostic/error output.
- [x] Preserve global and database-specific provider selection.
- [x] Preserve Cosmos as the default provider.
- [x] Support explicit application-supplied provider settings for migration code.
- [x] Add deterministic local Docker test settings through `TestConnections`.
- [x] Validate the Mongo settings path through the local integration suite.

## Remaining task

- [ ] Configure `DocumentStorageSettingsResolver` from the host `IConfiguration` / `IMongoDocumentStorageConnectionSettings` seam so primary runtime Mongo credentials come from the first-class configuration model rather than the legacy connection-string environment variables.

The existing `CloudStorageModule.AddCloudStorageModule(IServiceCollection, IConfigurationRoot, ILogger)` already receives the host configuration and is the preferred compatibility seam. Derived repository constructors should remain unchanged.

## Acceptance criteria

- [x] Cosmos and Mongo connection information can coexist in one process.
- [x] Selecting Mongo does not require repurposing a Cosmos access-key field.
- [x] Migration code can receive explicit source Cosmos and target Mongo settings simultaneously.
- [x] Existing Cosmos-only deployments continue working without configuration changes.
- [ ] Primary Mongo runtime credentials are sourced through the first-class LagoVista configuration model during normal host startup.

## Out of scope

- Domain collection routing. See Card 3.
- Secret-store implementation changes outside the configuration seam.
