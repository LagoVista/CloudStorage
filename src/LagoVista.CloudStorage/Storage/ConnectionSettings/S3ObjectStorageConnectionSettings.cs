
using LagoVista.CloudStorage.Interfaces.ConnectionSettings;
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

            var port = section.Optional("Port");
            Port = String.IsNullOrWhiteSpace(port)
                ? 8333
                : Int32.Parse(port);

            var useTls = section.Optional("UseTls");
            UseTls = !String.IsNullOrWhiteSpace(useTls) &&
                     Boolean.Parse(useTls);

            var region = section.Optional("Region");
            Region = String.IsNullOrWhiteSpace(region)
                ? null
                : region.Trim();
        }

        public string Host { get; }
        public int Port { get; }
        public string AccessKey { get; }
        public string SecretKey { get; }
        public bool UseTls { get; }
        public string Region { get; }

        public override string ToString()
        {
            return $"S3ObjectStorageConnectionSettings(" +
                   $"Host={Host}, Port={Port}, UseTls={UseTls}, " +
                   $"Region={Region ?? "<default>"}, " +
                   $"AccessKey={AccessKey}, SecretKey=<redacted>)";
        }
    }
}