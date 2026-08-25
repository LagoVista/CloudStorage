using LagoVista;
using LagoVista.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LagoVista.CloudStorage.Storage
{
    public interface ICassandraStorageSettings
    {
        IReadOnlyList<string> ContactPoints { get; }
        string UserName { get; }
        string Password { get; }
        string Keyspace { get; }
        int Port { get; }
        string LocalDataCenter { get; }
    }

    [CriticalCoverage]
    public sealed class CassandraStorageSettings : ICassandraStorageSettings
    {
        public const string SectionName = "CassandraStorage";

        public CassandraStorageSettings(IConfiguration configuration)
        {
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));

            var section = configuration.GetSection(SectionName);
            var contactPoints = section.Require("ContactPoints");
            if(!String.IsNullOrEmpty(contactPoints)) ContactPoints = ReadList(contactPoints);
            UserName = section.Require("UserName");
            Password = section.Require("Password");
            Keyspace = section.Require("Keyspace");
            var port = section.Optional("Port");
            Port = ReadPort(port, 9042);
            var localDataCenter = section.Optional("LocalDataCenter");
            LocalDataCenter = String.IsNullOrWhiteSpace(localDataCenter) ? null : localDataCenter.Trim();
        }

        public IReadOnlyList<string> ContactPoints { get; }
        public string UserName { get; }
        public string Password { get; }
        public string Keyspace { get; }
        public int Port { get; }
        public string LocalDataCenter { get; }

        public override string ToString()
        {
            return $"CassandraStorageSettings(ContactPoints={String.Join(",", ContactPoints)}, Port={Port}, Keyspace={Keyspace}, LocalDataCenter={LocalDataCenter ?? "<default>"}, UserName={UserName}, Password=<redacted>)";
        }

        private static IReadOnlyList<string> ReadList(string value)
        {
            var points = value
                .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(item => item.Trim())
                .Where(item => item.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (points.Count == 0)
            {
                throw new InvalidOperationException("CassandraStorage:ContactPoints must contain at least one host.");
            }

            return points.AsReadOnly();
        }

        private static int ReadPort(string value, int defaultPort)
        {
            if (String.IsNullOrWhiteSpace(value)) return defaultPort;
            if (!Int32.TryParse(value, out var port) || port <= 0 || port > 65535)
            {
                throw new InvalidOperationException("CassandraStorage:Port must be a valid TCP port.");
            }

            return port;
        }
    }
}
