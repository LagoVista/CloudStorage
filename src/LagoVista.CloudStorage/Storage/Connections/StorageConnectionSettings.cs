using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LagoVista.CloudStorage.Storage
{
    public sealed class CassandraStorageSettings
    {
        public CassandraStorageSettings(
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

            var points = contactPoints
                .Where(point => !String.IsNullOrWhiteSpace(point))
                .Select(point => point.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (points.Count == 0)
            {
                throw new ArgumentException("At least one Cassandra contact point is required.", nameof(contactPoints));
            }

            ContactPoints = points.AsReadOnly();
            UserName = userName;
            Password = password;
            Keyspace = keyspace;
            Port = port;
            LocalDataCenter = String.IsNullOrWhiteSpace(localDataCenter) ? null : localDataCenter.Trim();
        }

        public IReadOnlyList<string> ContactPoints { get; }
        public string UserName { get; }
        public string Password { get; }
        public string Keyspace { get; }
        public int Port { get; }
        public string LocalDataCenter { get; }

        public static CassandraStorageSettings FromConfiguration(IConfiguration configuration)
        {
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));

            var contactPoints = ReadList(configuration, "ContactPoints");
            var port = ReadPort(configuration["Port"], 9042);

            return new CassandraStorageSettings(
                contactPoints,
                Required(configuration, "UserName"),
                Required(configuration, "Password"),
                Required(configuration, "Keyspace"),
                port,
                configuration["LocalDataCenter"]);
        }

        public override string ToString()
        {
            return $"CassandraStorageSettings(ContactPoints={String.Join(",", ContactPoints)}, Port={Port}, Keyspace={Keyspace}, LocalDataCenter={LocalDataCenter ?? "<default>"}, UserName={UserName}, Password=<redacted>)";
        }

        private static IReadOnlyList<string> ReadList(IConfiguration configuration, string key)
        {
            var section = configuration.GetSection(key);
            var children = section.GetChildren()
                .Select(child => child.Value)
                .Where(value => !String.IsNullOrWhiteSpace(value))
                .ToList();

            if (children.Count > 0) return children;

            var value = configuration[key];
            if (String.IsNullOrWhiteSpace(value)) return Array.Empty<string>();

            return value
                .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(item => item.Trim())
                .Where(item => item.Length > 0)
                .ToList();
        }

        private static int ReadPort(string value, int defaultPort)
        {
            if (String.IsNullOrWhiteSpace(value)) return defaultPort;
            if (!Int32.TryParse(value, out var port))
            {
                throw new InvalidOperationException("Storage:Cassandra:Port must be a valid integer.");
            }

            return port;
        }

        private static string Required(IConfiguration configuration, string key)
        {
            var value = configuration[key];
            if (String.IsNullOrWhiteSpace(value))
            {
                throw new InvalidOperationException($"Required Cassandra storage setting '{key}' is missing.");
            }

            return value;
        }
    }

    public sealed class MongoStorageSettings
    {
        public MongoStorageSettings(string connectionString, string defaultDatabaseName = null)
        {
            if (String.IsNullOrWhiteSpace(connectionString)) throw new ArgumentNullException(nameof(connectionString));

            ConnectionString = connectionString;
            DefaultDatabaseName = String.IsNullOrWhiteSpace(defaultDatabaseName) ? null : defaultDatabaseName.Trim();
        }

        public string ConnectionString { get; }
        public string DefaultDatabaseName { get; }

        public static MongoStorageSettings FromConfiguration(IConfiguration configuration)
        {
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));

            var connectionString = configuration["ConnectionString"];
            if (String.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException("Required Mongo storage setting 'ConnectionString' is missing.");
            }

            return new MongoStorageSettings(connectionString, configuration["DefaultDatabaseName"] ?? configuration["DatabaseName"]);
        }

        public override string ToString()
        {
            return $"MongoStorageSettings(DefaultDatabaseName={DefaultDatabaseName ?? "<none>"}, ConnectionString=<redacted>)";
        }
    }
}
