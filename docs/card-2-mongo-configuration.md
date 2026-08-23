# Card 2 - Mongo Configuration Model

## Objective

Define explicit Mongo configuration instead of overloading Cosmos-oriented endpoint/access-key settings.

## Status

Implementation complete pending local build/test validation.

## Configuration model

Mongo now has a first-class configuration object:

```csharp
public sealed class MongoDocumentStorageSettings
{
    public string ConnectionString { get; set; }
    public string DatabaseName { get; set; }
}
```

`DocumentStorageSettings` retains the existing Cosmos-oriented `Endpoint`, `SharedKey`, and logical `DatabaseName` values and adds a separate `Mongo` settings object. This allows Cosmos source credentials and Mongo target credentials to coexist in the same process.

## Provider selection

Existing provider selection remains unchanged:

```text
NUVIOT_DOCUMENT_STORAGE_PROVIDER=mongo
NUVIOT_DOCUMENT_STORAGE_PROVIDER_<LOGICAL_DATABASE>=mongo
```

Database-specific values take precedence over the global value. If no provider is configured, Cosmos remains the default.

## Mongo settings

Global Mongo settings:

```text
NUVIOT_MONGO_CONNECTION_STRING=mongodb://localhost:27017
NUVIOT_MONGO_DATABASE=Nuviot
```

Database-specific overrides:

```text
NUVIOT_MONGO_CONNECTION_STRING_PROJECTMANAGEMENT=mongodb://localhost:27017
NUVIOT_MONGO_DATABASE_PROJECTMANAGEMENT=ProjectManagement
```

`NUVIOT_MONGO_CONNECTION_STRING` is required when Mongo is selected. `NUVIOT_MONGO_DATABASE` is optional; when omitted, the existing logical database name is used.

Both `mongodb://` and `mongodb+srv://` connection strings are accepted.

Do not commit real connection strings containing credentials. Supply secrets through the deployment configuration or secret-store mechanism used by the host application.

## Explicit application configuration

Applications are not required to use environment variables. `IDocumentCollectionFactory` also accepts a fully resolved `DocumentStorageSettings` instance:

```csharp
var settings = new DocumentStorageSettings
{
    Provider = DocumentStorageProviderType.Mongo,
    DatabaseName = "ProjectManagement",
    Mongo = new MongoDocumentStorageSettings
    {
        ConnectionString = mongoConnectionString,
        DatabaseName = "ProjectManagement"
    }
};

var collection = documentCollectionFactory.Create(settings);
```

This path is intended for hosts that bind settings from configuration providers or secret stores and for migration code that needs explicit Cosmos source and Mongo target settings simultaneously.

## Completed tasks

- [x] Define Mongo document storage settings with connection string and database name.
- [x] Define global and database-specific environment variable names.
- [x] Preserve existing global and database-specific provider selection.
- [x] Resolve provider selection independently from provider credentials.
- [x] Preserve existing Cosmos configuration behavior and default.
- [x] Support explicit application-supplied settings.
- [x] Support both `mongodb://` and `mongodb+srv://` connection strings.
- [x] Fail fast when Mongo is selected without a connection string.
- [x] Add tests for Cosmos default, Mongo selection, database-specific overrides, missing Mongo configuration, invalid provider values, and Mongo URI schemes.
- [x] Keep connection strings out of diagnostic/error output.
- [x] Document local/dev configuration without credentials.

## Validation remaining

Run locally:

```powershell
dotnet build src/LagoVista.CloudStorage/LagoVista.CloudStorage.csproj
dotnet test tests/LagoVista.CloudStorage.Tests/LagoVista.CloudStorage.IntegrationTests.csproj
```

## Acceptance criteria

- Cosmos and Mongo connection information can coexist in one process.
- Selecting Mongo does not require repurposing a Cosmos access-key field.
- Migration code can receive explicit source Cosmos and target Mongo settings simultaneously.
- Existing Cosmos-only deployments continue working without configuration changes.

## Out of scope

- Domain collection routing. See Card 3.
- Secret-store implementation changes outside the configuration seam.
