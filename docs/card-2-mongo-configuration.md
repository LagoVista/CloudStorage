# Card 2 - Mongo Configuration Model

## Objective

Define explicit Mongo configuration instead of overloading Cosmos-oriented endpoint/access-key settings.

## Why

The current seam can select Mongo, but it temporarily interprets the existing document storage endpoint as a Mongo connection string. Migration also needs Cosmos and Mongo settings simultaneously, so the two providers need distinct configuration values.

## Tasks

- Define Mongo document storage settings with at least connection string and database name.
- Define how settings are supplied by application configuration and environment variables.
- Preserve `NUVIOT_DOCUMENT_STORAGE_PROVIDER` and database-specific provider overrides.
- Update `DocumentStorageSettingsResolver` so provider selection and provider credentials are resolved independently.
- Ensure secrets are not logged or included in diagnostic output.
- Support both `mongodb://` and `mongodb+srv://` connection strings.
- Add tests for Cosmos default, Mongo selection, database-specific override, missing Mongo configuration, and invalid provider values.
- Document local/dev configuration examples without committing credentials.

## Acceptance criteria

- Cosmos and Mongo connection information can coexist in one process.
- Selecting Mongo does not require repurposing a Cosmos access-key field.
- Migration code can receive explicit source Cosmos and target Mongo settings simultaneously.
- Existing Cosmos-only deployments continue working without configuration changes.

## Out of scope

- Domain collection routing.
- Secret-store implementation changes outside the configuration seam.
