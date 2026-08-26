using LagoVista.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using System;

namespace LagoVista.CloudStorage.Storage.ConnectionSettings
{
    public interface IApplicationDataStorageSettings : IMongoDocumentStorageConnectionSettings
    {
 
    }

    public sealed class ApplicationDataStorageSettings : MongoDocumentStorageConnectionSettings, IApplicationDataStorageSettings
    {
        public ApplicationDataStorageSettings(IConfiguration configuration) : base(configuration)
        {
        }
    }
}
