using System;
using System.Collections.Generic;

namespace LagoVista.CloudStorage.Storage
{
    public interface IFlatStorageConnectionSettings
    {
    }

    public sealed class AzureTableStorageConnectionSettings : IFlatStorageConnectionSettings
    {
        public AzureTableStorageConnectionSettings(string accountId, string accountKey)
        {
            if (String.IsNullOrWhiteSpace(accountId)) throw new ArgumentNullException(nameof(accountId));
            if (String.IsNullOrWhiteSpace(accountKey)) throw new ArgumentNullException(nameof(accountKey));

            AccountId = accountId;
            AccountKey = accountKey;
        }

        public string AccountId { get; }
        public string AccountKey { get; }
    }

    public sealed class CassandraConnectionSettings : IFlatStorageConnectionSettings
    {
        public CassandraConnectionSettings(
            IEnumerable<string> contactPoints,
            string userName,
            string password,
            string keyspace,
            int port = 9042,
            string localDataCenter = null)
        {
            if (contactPoints == null) throw new ArgumentNullException(nameof(contactPoints));
            if (String.IsNullOrWhiteSpace(userName)) throw new ArgumentNullException(nameof(userName));
            if (String.IsNullOrWhiteSpace(password)) throw new ArgumentNullException(nameof(password));
            if (String.IsNullOrWhiteSpace(keyspace)) throw new ArgumentNullException(nameof(keyspace));
            if (port <= 0 || port > 65535) throw new ArgumentOutOfRangeException(nameof(port));

            var points = new List<string>();
            foreach (var contactPoint in contactPoints)
            {
                if (!String.IsNullOrWhiteSpace(contactPoint))
                {
                    points.Add(contactPoint);
                }
            }

            if (points.Count == 0)
            {
                throw new ArgumentException("At least one Cassandra contact point is required.", nameof(contactPoints));
            }

            ContactPoints = points.AsReadOnly();
            UserName = userName;
            Password = password;
            Keyspace = keyspace;
            Port = port;
            LocalDataCenter = localDataCenter;
        }

        public IReadOnlyList<string> ContactPoints { get; }
        public string UserName { get; }
        public string Password { get; }
        public string Keyspace { get; }
        public int Port { get; }
        public string LocalDataCenter { get; }
    }

    public sealed class MongoConnectionSettings : IFlatStorageConnectionSettings
    {
        public MongoConnectionSettings(string connectionString, string databaseName)
        {
            if (String.IsNullOrWhiteSpace(connectionString)) throw new ArgumentNullException(nameof(connectionString));
            if (String.IsNullOrWhiteSpace(databaseName)) throw new ArgumentNullException(nameof(databaseName));

            ConnectionString = connectionString;
            DatabaseName = databaseName;
        }

        public string ConnectionString { get; }
        public string DatabaseName { get; }
    }

    public sealed class FlatStorageContext
    {
        public FlatStorageContext(FlatStorageProvider provider, IFlatStorageConnectionSettings connection)
        {
            Connection = connection ?? throw new ArgumentNullException(nameof(connection));
            Provider = provider;

            ValidateProviderMatchesConnection();
        }

        public FlatStorageProvider Provider { get; }
        public IFlatStorageConnectionSettings Connection { get; }

        public TConnection GetConnection<TConnection>() where TConnection : class, IFlatStorageConnectionSettings
        {
            var connection = Connection as TConnection;
            if (connection == null)
            {
                throw new InvalidOperationException(
                    $"Flat storage provider {Provider} is configured with {Connection.GetType().Name}, not {typeof(TConnection).Name}.");
            }

            return connection;
        }

        public static FlatStorageContext AzureTableStorage(string accountId, string accountKey)
        {
            return new FlatStorageContext(
                FlatStorageProvider.AzureTableStorage,
                new AzureTableStorageConnectionSettings(accountId, accountKey));
        }

        private void ValidateProviderMatchesConnection()
        {
            var valid =
                (Provider == FlatStorageProvider.AzureTableStorage && Connection is AzureTableStorageConnectionSettings) ||
                (Provider == FlatStorageProvider.Cassandra && Connection is CassandraConnectionSettings) ||
                (Provider == FlatStorageProvider.MongoDB && Connection is MongoConnectionSettings);

            if (!valid)
            {
                throw new ArgumentException(
                    $"Connection settings type {Connection.GetType().Name} does not match flat storage provider {Provider}.",
                    nameof(Connection));
            }
        }
    }
}
