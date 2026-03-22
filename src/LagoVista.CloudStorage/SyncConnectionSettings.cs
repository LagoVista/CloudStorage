using LagoVista.CloudStorage.Interfaces;
using LagoVista.Core.Interfaces;
using LagoVista.Core.Models;
using Microsoft.Extensions.Configuration;
using System;

namespace LagoVista.CloudStorage
{
    public class SyncConnections : ISyncConnectionSettings
    {
        public IConnectionSettings SyncConnectionSettings { get; }

        public SyncConnections(IConfiguration configuration)
        {
            var section = configuration.GetSection("DefaultDocDBStorage");
            SyncConnectionSettings = new ConnectionSettings
            {
                Uri = section.GetValue<string>("Endpoint"),
                AccessKey = section.GetValue<string>("AccessKey"),
                ResourceName = section.GetValue<string>("DbName"),
            };

            if (String.IsNullOrEmpty(SyncConnectionSettings.Uri) || String.IsNullOrEmpty(SyncConnectionSettings.AccessKey) || String.IsNullOrEmpty(SyncConnectionSettings.ResourceName))
            {
                throw new ArgumentException("DefaultDocDBStorage settings are required for SyncConnections.");
            }
        }
    }
}
