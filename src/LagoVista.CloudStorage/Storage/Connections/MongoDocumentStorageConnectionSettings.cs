using LagoVista.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LagoVista.CloudStorage.Storage
{
    public interface IMongoDocumentStorageConnectionSettings
    {
        IReadOnlyList<string> Hosts { get; }
        int Port { get; }
        string UserName { get; }
        string Password { get; }
        string AuthenticationDatabase { get; }
        string ReplicaSet { get; }
        bool UseTls { get; }
        string BuildConnectionString();
    }

    public sealed class MongoDocumentStorageConnectionSettings : IMongoDocumentStorageConnectionSettings
    {
        public const string SectionName = "MongoDocumentStorage";

        public MongoDocumentStorageConnectionSettings(IConfiguration configuration)
        {
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));

            var section = configuration.GetSection(SectionName);
            Hosts = ReadHosts(section.Require("Hosts"));
            Port = ReadPort(section["Port"], 27017);
            UserName = section.Require("UserName");
            Password = section.Require("Password");
            AuthenticationDatabase = String.IsNullOrWhiteSpace(section["AuthenticationDatabase"]) ? "admin" : section["AuthenticationDatabase"].Trim();
            ReplicaSet = String.IsNullOrWhiteSpace(section["ReplicaSet"]) ? null : section["ReplicaSet"].Trim();
            UseTls = ReadBoolean(section["UseTls"], false, "UseTls");
        }

        public IReadOnlyList<string> Hosts { get; }
        public int Port { get; }
        public string UserName { get; }
        public string Password { get; }
        public string AuthenticationDatabase { get; }
        public string ReplicaSet { get; }
        public bool UseTls { get; }

        public string BuildConnectionString()
        {
            var builder = new StringBuilder();
            builder.Append("mongodb://");
            builder.Append(Uri.EscapeDataString(UserName));
            builder.Append(':');
            builder.Append(Uri.EscapeDataString(Password));
            builder.Append('@');
            builder.Append(String.Join(",", Hosts.Select(host => $"{host}:{Port}")));
            builder.Append("/?authSource=");
            builder.Append(Uri.EscapeDataString(AuthenticationDatabase));

            if (!String.IsNullOrWhiteSpace(ReplicaSet))
            {
                builder.Append("&replicaSet=");
                builder.Append(Uri.EscapeDataString(ReplicaSet));
            }

            if (UseTls) builder.Append("&tls=true");
            return builder.ToString();
        }

        public override string ToString()
        {
            return $"MongoDocumentStorageConnectionSettings(Hosts={String.Join(",", Hosts)}, Port={Port}, AuthenticationDatabase={AuthenticationDatabase}, ReplicaSet={ReplicaSet ?? "<none>"}, UseTls={UseTls}, UserName={UserName}, Password=<redacted>)";
        }

        private static IReadOnlyList<string> ReadHosts(string value)
        {
            var hosts = value
                .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(item => item.Trim())
                .Where(item => item.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (hosts.Count == 0) throw new InvalidOperationException("MongoDocumentStorage:Hosts must contain at least one host.");
            return hosts.AsReadOnly();
        }

        private static int ReadPort(string value, int defaultPort)
        {
            if (String.IsNullOrWhiteSpace(value)) return defaultPort;
            if (!Int32.TryParse(value, out var port) || port <= 0 || port > 65535) throw new InvalidOperationException("MongoDocumentStorage:Port must be a valid TCP port.");
            return port;
        }

        private static bool ReadBoolean(string value, bool defaultValue, string fieldName)
        {
            if (String.IsNullOrWhiteSpace(value)) return defaultValue;
            if (!Boolean.TryParse(value, out var result)) throw new InvalidOperationException($"MongoDocumentStorage:{fieldName} must be true or false.");
            return result;
        }
    }
}
