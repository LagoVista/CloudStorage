using Microsoft.Extensions.Configuration;
using System;

namespace LagoVista.CloudStorage.Storage.ConnectionSettings
{
    public sealed class CosmosConnectionSettings : ICosmosConnectionSettings
    {
        public const string SectionName = "DefaultDocDBStorage";

        public CosmosConnectionSettings(IConfiguration configuration)
        {
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));
            var section = configuration.GetSection(SectionName);
            Endpoint = section.Require("Endpoint");
            AccessKey = section.Require("AccessKey");
            DatabaseName = section.Require("DbName");
        }

        public string Endpoint { get; }
        public string AccessKey { get; }
        public string DatabaseName { get; }
    }
}
