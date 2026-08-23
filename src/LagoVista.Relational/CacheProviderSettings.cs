using LagoVista.Core.Interfaces;
using LagoVista.Core.Models;
using Microsoft.Extensions.Configuration;
using System;

namespace LagoVista.CloudStorage
{
    public class CacheProviderSettings : ICacheProviderSettings
    {
        public bool UseCache { get; }

        public bool UseAuthentication { get; }

        public IConnectionSettings CacheSettings { get; }

        public string Password { get; }

        public CacheProviderSettings(IConfiguration configuration)
        {
            var cacheSection = configuration.GetSection("SystemCache");

            UseCache = Convert.ToBoolean(cacheSection.Require("UseCache"));

            var useAuthenticationValue = cacheSection.Require("UseAuthentication");
            Password = cacheSection.Require("Password");
            UseAuthentication = !String.IsNullOrWhiteSpace(useAuthenticationValue) && Convert.ToBoolean(useAuthenticationValue);
          
            CacheSettings = new ConnectionSettings
            {
                Uri = cacheSection.Require("Uri"),
            };
        }
    }
}
