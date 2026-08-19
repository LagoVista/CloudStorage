using LagoVista.Core.Interfaces;
using LagoVista.Core.Models;
using Microsoft.Extensions.Configuration;
using System;

namespace LagoVista.CloudStorage
{
    public class CacheProviderSettings : ICacheProviderSettings
    {
        public bool UseCache { get; }

        public IConnectionSettings CacheSettings { get; }

        public string Password { get; }

        public CacheProviderSettings(IConfiguration configuration)
        {
            var cacheSection = configuration.GetSection("SystemCache");

            UseCache = Convert.ToBoolean(cacheSection.Require("UseCache"));
            Password = cacheSection.Require("Password");
            CacheSettings = new ConnectionSettings
            {
                Uri = cacheSection.Require("Uri"),
            };
        }
    }
}
