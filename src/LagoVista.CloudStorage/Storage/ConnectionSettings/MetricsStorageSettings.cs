using LagoVista.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using System;

namespace LagoVista.CloudStorage.Storage.ConnectionSettings
{
    public interface IMetricsStorageSettings : IPostgresConnectionSettings
    {
    }

    public sealed class MetricsStorageSettings : PostgresConnectionSettings, IMetricsStorageSettings
    {
        public MetricsStorageSettings(IConfiguration configuration) : base(configuration)
        {
            SchemaName = "Metrics";
        }
    }
}
