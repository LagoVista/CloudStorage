
//#define USE_DEV_CONNECTIONS

using LagoVista.CloudStorage.Interfaces;
using LagoVista.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;


namespace DataMigration
{
    class SyncSettings : ISyncConnectionSettings, IDefaultConnectionSettings
    {
        public SyncSettings(string? environment)
        {
            if(environment == "prod")
            {
                SyncConnectionSettings = LagoVista.CloudStorage.Utils.TestConnections.ProductionDocDB;
                DefaultTableStorageSettings = LagoVista.CloudStorage.Utils.TestConnections.ProductionTableStorageDB;
            }
            else
            {
                SyncConnectionSettings = LagoVista.CloudStorage.Utils.TestConnections.DevDocDB;
                DefaultTableStorageSettings = LagoVista.CloudStorage.Utils.TestConnections.DevTableStorageDB;
            }
        }

        public IConnectionSettings SyncConnectionSettings { get; }

        public IConnectionSettings DefaultTableStorageSettings { get; }

        public IConnectionSettings DefaultDocDbSettings => throw new NotImplementedException();

        public IConnectionSettings EHCheckPointStorageSettings => throw new NotImplementedException();
    }
}
