using LagoVista.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using System;

namespace LagoVista.CloudStorage.Storage.ConnectionSettings
{
    public interface IAccountLedgerStorageSettings : IPostgresConnectionSettings
    {
    }

    public sealed class AccountLedgerStorageSettings : PostgresConnectionSettings, IAccountLedgerStorageSettings
    {
        public const string SectionName = "AccountLedgerStorage";

        public AccountLedgerStorageSettings(IConfiguration configuration) : base(configuration)
        {
            SchemaName = "AccountLedger";
        }
    }
}
