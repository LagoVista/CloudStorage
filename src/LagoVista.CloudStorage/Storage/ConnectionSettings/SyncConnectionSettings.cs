using LagoVista.CloudStorage.Interfaces;
using LagoVista.Core.Interfaces;
using LagoVista.Core.Models;
using Microsoft.Extensions.Configuration;
using System;
using CoreConnectionSettings = LagoVista.Core.Models.ConnectionSettings;

namespace LagoVista.CloudStorage.Storage.ConnectionSettings
{
    public class SyncConnections : ISyncConnectionSettings
    {
        public IConnectionSettings SyncConnectionSettings { get; }

        public SyncConnections(IConfiguration configuration)
        {
            var section = configuration.GetSection("DefaultDocDBStorage");
            SyncConnectionSettings = new CoreConnectionSettings
            {
                Uri = section.Require("Endpoint"),
                AccessKey = section.Require("AccessKey"),
                ResourceName = section.Require("DbName"),
            };
        }
    }
}
