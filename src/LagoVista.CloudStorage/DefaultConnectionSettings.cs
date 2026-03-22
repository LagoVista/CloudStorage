using LagoVista.CloudStorage.Interfaces;
using LagoVista.Core.Interfaces;
using LagoVista.Core.Models;
using Microsoft.Extensions.Configuration;
using System;

namespace LagoVista.CloudStorage
{
    public class DefaultConnectionSettings : IDefaultConnectionSettings
    {
        public IConnectionSettings DefaultDocDbSettings { get; }

        public IConnectionSettings DefaultTableStorageSettings { get; }

        public IConnectionSettings EHCheckPointStorageSettings { get; }

        public DefaultConnectionSettings(IConfiguration configuration)
        {
            var docDbSection = configuration.GetSection("DefaultDocDBStorage");
            DefaultDocDbSettings = new ConnectionSettings
            {
                Uri = docDbSection.GetValue<string>("Endpoint"),
                AccessKey = docDbSection.GetValue<string>("AccessKey"),
                ResourceName = docDbSection.GetValue<string>("DbName"),
            };

            if (String.IsNullOrEmpty(DefaultDocDbSettings.Uri) || String.IsNullOrEmpty(DefaultDocDbSettings.AccessKey) || String.IsNullOrEmpty(DefaultDocDbSettings.ResourceName))
            {
                throw new ArgumentException("DefaultDocDBStorage settings are required for SyncConnections.");
            }

            var tsSection = configuration.GetSection("DefaultTableStorage");
            DefaultTableStorageSettings = new ConnectionSettings
            {
                AccountId = tsSection.GetValue<string>("Name"),
                AccessKey = tsSection.GetValue<string>("AccessKey"),
            };

            var ehSection = configuration.GetSection("CheckPointStorage");
            EHCheckPointStorageSettings = new ConnectionSettings
            {
                AccountId = ehSection.GetValue<string>("Name"),
                AccessKey = ehSection.GetValue<string>("AccessKey"),
            };

        }
    }
}
