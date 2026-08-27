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
       public new const string SectionName = "ScratchDataStorage";

        public ScratchStorageSettings(IConfiguration configuration) : base(configuration)
        {
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));
            DatabaseName = configuration.GetSection(SectionName).Require("DatabaseName");        
        }

    }
}
