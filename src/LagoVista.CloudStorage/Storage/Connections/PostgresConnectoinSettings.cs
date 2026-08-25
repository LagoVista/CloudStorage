using Microsoft.Extensions.Configuration;
using System;

namespace LagoVista.CloudStorage.Storage.Connections
{
    public interface IPostgresConnectionSettings
    {
        public string HostName { get; }

        public string UserName { get; }
        public string Password { get; }
        public int Port { get; }
        public string SchemaName { get; }
    }

    public class PostgresConnectionSettings : IPostgresConnectionSettings
    {
        

        public PostgresConnectionSettings(IConfiguration configuration)
        {
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));

            var section = configuration.GetSection("ClusterPostgres");
            HostName = section.Require("HostName");
            UserName = section.Require("UserName");
            Password = section.Require("Password");
            Port = int.Parse(section.Require("Port"));
            SchemaName = section.Require("SchemaName");
        }

        public string HostName { get; }

        public string UserName { get; }
        public string Password { get; }
        public int Port { get; }
        public string SchemaName { get; }

    }
}
