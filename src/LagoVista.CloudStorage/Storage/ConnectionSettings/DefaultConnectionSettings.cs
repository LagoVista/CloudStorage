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
                Uri = docDbSection.Require("Endpoint"),
                AccessKey = docDbSection.Require("AccessKey"),
                ResourceName = docDbSection.Require("DbName"),
            };

            var tsSection = configuration.GetSection("DefaultTableStorage");
            DefaultTableStorageSettings = new ConnectionSettings
            {
                AccountId = tsSection.Require("Name"),
                AccessKey = tsSection.Require("AccessKey"),
            };

            var ehSection = configuration.GetSection("CheckPointStorage");
            EHCheckPointStorageSettings = new ConnectionSettings
            {
                AccountId = ehSection.Require("Name"),
                AccessKey = ehSection.Require("AccessKey"),
            };

        }
    }
}
