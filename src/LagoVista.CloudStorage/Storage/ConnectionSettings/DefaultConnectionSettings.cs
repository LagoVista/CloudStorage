using LagoVista.CloudStorage.Interfaces;
using LagoVista.Core.Interfaces;
using LagoVista.Core.Models;
using Microsoft.Extensions.Configuration;
using System;
using CoreConnectionSettings = LagoVista.Core.Models.ConnectionSettings;

namespace LagoVista.CloudStorage.Storage.ConnectionSettings
{
    public class DefaultConnectionSettings : IDefaultConnectionSettings
    {
        private readonly Lazy<IConnectionSettings> _defaultDocDbSettings;

        public IConnectionSettings DefaultDocDbSettings => _defaultDocDbSettings.Value;

        public IConnectionSettings DefaultTableStorageSettings { get; }

        public IConnectionSettings EHCheckPointStorageSettings { get; }

        public DefaultConnectionSettings(IConfiguration configuration)
        {
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));

            // Cosmos is an optional legacy provider when Mongo is selected. Do not
            // require Cosmos endpoint/key/database values during ordinary CloudStorage
            // composition; resolve them only for a consumer that explicitly uses Cosmos.
            _defaultDocDbSettings = new Lazy<IConnectionSettings>(() =>
            {
                var docDbSection = configuration.GetSection("DefaultDocDBStorage");
                return new CoreConnectionSettings
                {
                    Uri = docDbSection.Require("Endpoint"),
                    AccessKey = docDbSection.Require("AccessKey"),
                    ResourceName = docDbSection.Require("DbName"),
                };
            });

            var tsSection = configuration.GetSection("DefaultTableStorage");
            DefaultTableStorageSettings = new CoreConnectionSettings
            {
                AccountId = tsSection.Require("Name"),
                AccessKey = tsSection.Require("AccessKey"),
            };

            var ehSection = configuration.GetSection("CheckPointStorage");
            EHCheckPointStorageSettings = new CoreConnectionSettings
            {
                AccountId = ehSection.Require("Name"),
                AccessKey = ehSection.Require("AccessKey"),
            };
        }
    }
}
