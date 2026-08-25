using LagoVista.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using System;

namespace LagoVista.CloudStorage.Storage.ConnectionSettings
{
    public interface IApplicationDataStorageSettings
    {
        string ConnectionString { get; }
        string DatabaseName { get; }
    }

    public sealed class ApplicationDataStorageSettings : IApplicationDataStorageSettings
    {
        public const string SectionName = "ApplicationDataStorage";

        public ApplicationDataStorageSettings(IConfiguration configuration)
        {
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));

            var section = configuration.GetSection(SectionName);
            ConnectionString = section.Require("ConnectionString");
            DatabaseName = section.Require("DatabaseName");
        }

        public string ConnectionString { get; }
        public string DatabaseName { get; }

        public override string ToString()
        {
            return $"ApplicationDataStorageSettings(DatabaseName={DatabaseName}, ConnectionString=<redacted>)";
        }
    }
}
