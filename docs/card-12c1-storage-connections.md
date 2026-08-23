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
