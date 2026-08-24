# Card 12c1 - Storage connection configuration

Card 12 storage repositories receive semantic storage capabilities through dependency injection. They do not receive provider credentials, endpoints, connection strings, or Kubernetes Secret names.

## Configuration pattern

Card 12 follows the existing LagoVista `I*Settings` convention. Each settings implementation accepts `IConfiguration`, reads its named section, and is registered in DI through its interface.

The three new application configuration contracts are intentionally independent:

```text
CassandraStorage
ScratchStorage
ApplicationDataStorage
```

The existing primary Mongo / DocumentDB configuration remains separate from both new Mongo-backed mechanisms.

This means Scratch and Application Data may point at the same Mongo server today while still having independent configuration keys, credentials, and database names. They can therefore be split later without changing repository constructors or storage contracts.

## Cassandra configuration

```json
{
  "CassandraStorage": {
    "ContactPoints": "cassandra-0,cassandra-1,cassandra-2",
    "Port": 9042,
    "UserName": "app-user",
    "Password": "<secret>",
    "Keyspace": "nuviot",
    "LocalDataCenter": "dc1"
  }
}
```

Registered as:

```csharp
services.AddSingleton<ICassandraStorageSettings, CassandraStorageSettings>();
```

`AddCassandraStorageConnection()` provides the canonical registration helper.

### Explicit Development / Production environment settings

Operator and migration tools sometimes need to explicitly choose the shared Development or Production storage environment rather than rely on the host's active `IConfiguration` section.

CloudStorage exposes:

```csharp
CassandraStorageEnvironmentSettings.Development
CassandraStorageEnvironmentSettings.Production
```

Both return `ICassandraStorageSettings` and use the same validated `CassandraStorageSettings` implementation as normal applications.

Development reads:

```text
DEV_CASSANDRA_CONTACT_POINTS
DEV_CASSANDRA_PORT
DEV_CASSANDRA_USERNAME
DEV_CASSANDRA_PASSWORD
DEV_CASSANDRA_KEYSPACE
DEV_CASSANDRA_LOCAL_DATA_CENTER
```

Production reads:

```text
PROD_CASSANDRA_CONTACT_POINTS
PROD_CASSANDRA_PORT
PROD_CASSANDRA_USERNAME
PROD_CASSANDRA_PASSWORD
PROD_CASSANDRA_KEYSPACE
PROD_CASSANDRA_LOCAL_DATA_CENTER
```

`*_CASSANDRA_PORT` is optional and defaults to `9042`. `*_CASSANDRA_LOCAL_DATA_CENTER` is optional. Contact points, username, password, and keyspace are required by the canonical settings validation.

These prefixed settings are intended for explicit operator/tool selection, matching the existing Development/Production Table Storage connection pattern. Runtime services should normally continue to use the standard `CassandraStorage` configuration section so Kubernetes/environment configuration can select endpoints without application code changes.

## Scratch configuration

```json
{
  "ScratchStorage": {
    "ConnectionString": "<secret-bearing Mongo connection string>",
    "DatabaseName": "nuviot-scratch"
  }
}
```

Registered as:

```csharp
services.AddSingleton<IScratchStorageSettings, ScratchStorageSettings>();
```

## Application Data configuration

```json
{
  "ApplicationDataStorage": {
    "ConnectionString": "<secret-bearing Mongo connection string>",
    "DatabaseName": "nuviot-application"
  }
}
```

Registered as:

```csharp
services.AddSingleton<IApplicationDataStorageSettings, ApplicationDataStorageSettings>();
```

The values above are examples of shape only. Secret values must come from the normal environment/application secret path and must never be committed to Git.

## Registration

```csharp
services.AddCassandraStorageConnection();
services.AddScratchStorageConnection();
services.AddApplicationDataStorageConnection();
```

These helpers register the semantic settings interfaces. The host's normal `IConfiguration` registration supplies the environment-specific values.

Local development can supply the same keys with workstation-reachable endpoints such as a port-forward. In-cluster deployments supply internal service endpoints. Repository code is unchanged between environments.

## Mongo lifecycle ownership

`IMongoStorageClientFactory` is registered as a singleton. It caches clients by connection string.

Therefore:

- Scratch and Application Data remain independent configuration contracts.
- They may use different Mongo servers with no architectural change.
- If they currently contain the same connection string, both resolve to the same pooled `MongoClient`.
- The existing primary Mongo/DocumentDB settings remain independent.

This preserves semantic/configuration separation without creating duplicate physical Mongo connection pools unnecessarily.

## Cassandra lifecycle ownership

Cassandra settings are registered as a singleton through `ICassandraStorageSettings`. Card 12d selects the Cassandra driver and adds the corresponding singleton cluster/session provider. Session creation does not belong in Card 12c1 because choosing and referencing the Cassandra driver is intentionally part of Card 12d.

## Secret safety

Typed settings retain credentials because providers need them, but their `ToString()` representations redact passwords/connection strings. Configuration validation identifies the missing setting and does not emit supplied secret values.

## Ownership boundary

- Card 12b owns platform/bootstrap credential discovery and rotation.
- Card 12c1 owns application runtime configuration and DI wiring.
- Cards 12d/12e/12f own provider behavior.
- Business repositories own none of the above and consume only the semantic storage contracts.
