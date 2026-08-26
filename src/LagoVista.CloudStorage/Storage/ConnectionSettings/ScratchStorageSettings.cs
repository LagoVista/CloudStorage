using LagoVista.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using System;

namespace LagoVista.CloudStorage.Storage.ConnectionSettings
{
    public interface IScratchStorageSettings : IMongoDocumentStorageConnectionSettings
    {
    }

    public sealed class ScratchStorageSettings : MongoDocumentStorageConnectionSettings, IScratchStorageSettings
    {
        public ScratchStorageSettings(IConfiguration configuration) : base(configuration)
        {
        }

    }
}
