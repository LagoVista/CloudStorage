using LagoVista.Core.Interfaces;
using LagoVista.Core.Models;
using Microsoft.Extensions.Configuration;
using System;
using System.ComponentModel.DataAnnotations;

namespace LagoVista.CloudStorage
{
    public class CacheProviderSettings : ICacheProviderSettings
    {
        public bool UseCache { get; }

        public IConnectionSettings CacheSettings { get; }

        public CacheProviderSettings(IConfiguration configuration)
        {
            var cacheSection = configuration.GetRequiredSection("SystemCache");

            UseCache = Convert.ToBoolean(cacheSection["UseCache"]);
            CacheSettings = new ConnectionSettings
            {
                Uri = cacheSection.GetValue<string>("Uri"),
            };

            if(String.IsNullOrEmpty(CacheSettings.Uri))
            {
                throw new ValidationException("Cache URI is required when UseCache is true.");
            }
        }
    }
}
