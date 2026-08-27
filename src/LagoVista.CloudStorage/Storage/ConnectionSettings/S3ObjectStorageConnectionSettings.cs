using Microsoft.Extensions.Configuration;
using System;

namespace LagoVista.CloudStorage.Storage.ConnectionSettings
{
    public sealed class S3ObjectStorageConnectionSettings : IS3ObjectStorageConnectionSettings
    {
        public const string SectionName = "S3ObjectStorage";

        public S3ObjectStorageConnectionSettings(IConfiguration configuration)
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            var section = configuration.GetSection(SectionName);

            Host = section.Require("Host");
            AccessKey = section.Require("AccessKey");
            SecretKey = section.Require("SecretKey");
            PublicHost = section.Require("PublicHost");
            PublicPort = Convert.ToInt32(section.Require("PublicPort")); 
            PublicUseTls = Convert.ToBoolean(section.Require("PublicUseTls"));

            var port = section.Optional("Port");
            Port = String.IsNullOrWhiteSpace(port)
                ? 8333
                : Int32.Parse(port);

            var useTls = section.Optional("UseTls");
            UseTls = !String.IsNullOrWhiteSpace(useTls) && Boolean.Parse(useTls);

            var region = section.Optional("Region");
            Region = String.IsNullOrWhiteSpace(region) ? null : region.Trim();
        }

        public string Host { get; }
        public int Port { get; }
        public string AccessKey { get; }
        public string SecretKey { get; }
        public bool UseTls { get; }
        public string Region { get; }
        public string PublicHost { get; }
        public int PublicPort { get; }
        public bool PublicUseTls { get; }

        public override string ToString()
        {
            return $"S3ObjectStorageConnectionSettings(" +
                   $"Host={Host}, Port={Port}, UseTls={UseTls}, " +
                   $"PublicHost={PublicHost}, PublicPort={PublicPort}, PublicUseTls={PublicUseTls}, " +
                   $"Region={Region ?? "<default>"}, " +
                   $"AccessKey={AccessKey}, SecretKey=<redacted>)";
        }
    }
}
