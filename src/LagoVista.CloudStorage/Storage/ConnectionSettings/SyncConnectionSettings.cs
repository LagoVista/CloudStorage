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
                Uri = section.Require("Endpoint"),
                AccessKey = section.Require("AccessKey"),
                ResourceName = section.Require("DbName"),
            };
        }
    }
}
