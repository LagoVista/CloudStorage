using LagoVista.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using System;

namespace LagoVista.CloudStorage.Storage
{
    public interface IScratchStorageSettings
    {
        string ConnectionString { get; }
        string DatabaseName { get; }
    }

    public sealed class ScratchStorageSettings : IScratchStorageSettings
    {
        public const string SectionName = "ScratchStorage";

        public ScratchStorageSettings(IConfiguration configuration)
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
            return $"ScratchStorageSettings(DatabaseName={DatabaseName}, ConnectionString=<redacted>)";
        }
    }
}
