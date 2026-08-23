using LagoVista.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using System;

namespace LagoVista.CloudStorage.Storage
{
    public interface IAccountLedgerStorageSettings
    {
        string Host { get; }
        int Port { get; }
        string DatabaseName { get; }
        string UserName { get; }
        string Password { get; }
    }

    public sealed class AccountLedgerStorageSettings : IAccountLedgerStorageSettings
    {
        public const string SectionName = "AccountLedgerStorage";

        public AccountLedgerStorageSettings(IConfiguration configuration)
        {
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));

            var section = configuration.GetSection(SectionName);
            Host = section.Require("Host");
            DatabaseName = section.Require("DatabaseName");
            UserName = section.Require("UserName");
            Password = section.Require("Password");

            var portValue = section["Port"];
            if (String.IsNullOrWhiteSpace(portValue))
            {
                Port = 5432;
            }
            else if (!Int32.TryParse(portValue, out var port) || port <= 0 || port > 65535)
            {
                throw new InvalidOperationException("AccountLedgerStorage:Port must be a valid TCP port.");
            }
            else
            {
                Port = port;
            }
        }

        public string Host { get; }
        public int Port { get; }
        public string DatabaseName { get; }
        public string UserName { get; }
        public string Password { get; }

        public override string ToString()
        {
            return $"AccountLedgerStorageSettings(Host={Host}, Port={Port}, DatabaseName={DatabaseName}, UserName={UserName}, Password=<redacted>)";
        }
    }
}
