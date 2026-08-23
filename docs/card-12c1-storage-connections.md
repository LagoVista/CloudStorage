# Card 12c1 - Storage connection configuration

Card 12 storage repositories receive semantic storage capabilities through dependency injection. They do not receive provider credentials, endpoints, connection strings, or Kubernetes Secret names.

## Configuration sections

The default application configuration sections are:

```text
Storage:Cassandra
Storage:Mongo
```

Cassandra shape:

```json
{
  "Storage": {
    "Cassandra": {
      "ContactPoints": [ "cassandra-0", "cassandra-1", "cassandra-2" ],
      "Port": 9042,
      "UserName": "app-user",
      "Password": "<secret>",
      "Keyspace": "nuviot",
      "LocalDataCenter": "dc1"
    }
  }
}
```

Mongo shape:

```json
{
  "Storage": {
    "Mongo": {
      "ConnectionString": "<secret-bearing connection string>",
      "DefaultDatabaseName": "nuviot"
    }
  }
}
```

The values above are examples of shape only. Secret values must come from the normal environment/application secret path and must never be committed to Git.

## Registration

```csharp
services.AddCassandraStorageConnection(configuration);
services.AddMongoStorageConnection(configuration);
```

Local development can supply the same sections with workstation-reachable endpoints such as a port-forward. In-cluster deployments supply internal service endpoints. Repository code is unchanged between environments.

Tests and hosts that already possess typed values may register them directly:

```csharp
services.AddCassandraStorageConnection(cassandraSettings);
services.AddMongoStorageConnection(mongoSettings);
```

## Lifecycle ownership

`MongoStorageClientProvider` is registered as a singleton and owns one lazily-created `MongoClient` for the configured Mongo connection. Scratch and Flat Document providers will share this client while selecting their own logical databases/collections.

Cassandra settings are registered as a singleton. Card 12d selects the Cassandra driver and adds the corresponding singleton cluster/session provider. Session creation does not belong in Card 12c1 because choosing and referencing the Cassandra driver is intentionally part of Card 12d.

## Secret safety

Typed settings retain credentials because providers need them, but their `ToString()` representations redact passwords/connection strings. Validation exceptions identify the missing setting name and never include the supplied secret value.

## Ownership boundary

- Card 12b owns platform/bootstrap credential discovery and rotation.
- Card 12c1 owns application runtime configuration and DI wiring.
- Cards 12d/12e/12f own provider behavior.
- Business repositories own none of the above and consume only the semantic storage contracts.
